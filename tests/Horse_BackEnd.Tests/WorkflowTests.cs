using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public async Task Registration_VerifiesOtp_AndTokenRevokedAfterReset()
    {
        await using var f = new ClubFactory(); var anonymous = f.CreateClient(); const string email = "owner@example.test";
        var registration = new RegisterRequest(email, "owner", "Test", "Owner", "0900000000", "Test address", ClubFactory.Password, ClubFactory.Password);
        await ClubFactory.Post(anonymous, "/api/auth/register", registration);
        var loginBefore = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = ClubFactory.Password }); Assert.Equal(HttpStatusCode.Unauthorized, loginBefore.StatusCode);
        var code = await f.Code(email, "Verify");
        Assert.Matches("^[0-9]{6}$", code);
        var verify = await ClubFactory.Post(anonymous, "/api/auth/verify-email", new { email, code }); Assert.True(verify.GetProperty("verified").GetBoolean());
        var repeated = await ClubFactory.Post(anonymous, "/api/auth/verify-email", new { email, code }); Assert.False(repeated.GetProperty("verified").GetBoolean());
        var login = await ClubFactory.Post(anonymous, "/api/auth/login", new { email, password = ClubFactory.Password });
        anonymous.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("accessToken").GetString());
        await ClubFactory.Post(anonymous, "/api/auth/forgot-password", new { email });
        var resetCode = await f.Code(email, "Reset");
        Assert.Matches("^[0-9]{6}$", resetCode);
        var reset = await ClubFactory.Post(anonymous, "/api/auth/reset-password", new { email, code = resetCode, password = "NewPassword123!", confirmPassword = "NewPassword123!" }); Assert.True(reset.GetProperty("changed").GetBoolean());
        var resetAgain = await ClubFactory.Post(anonymous, "/api/auth/reset-password", new { email, code = resetCode, password = "NewPassword456!", confirmPassword = "NewPassword456!" }); Assert.False(resetAgain.GetProperty("changed").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task OtpAttemptLimit_AndLoginLockout_ArePersisted()
    {
        await using var f = new ClubFactory(); var c = f.CreateClient(); const string email = "attempts@example.test";
        await ClubFactory.Post(c, "/api/auth/register", new RegisterRequest(email, "attempts", "Test", "Owner", "0900", "Address", ClubFactory.Password, ClubFactory.Password));
        var code = await f.Code(email, "Verify");
        for (var i = 0; i < 5; i++) Assert.False((await ClubFactory.Post(c, "/api/auth/verify-email", new { email, code = "invalid" })).GetProperty("verified").GetBoolean());
        Assert.False((await ClubFactory.Post(c, "/api/auth/verify-email", new { email, code })).GetProperty("verified").GetBoolean());
        var u = await f.User(Role.HorseOwner);
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new { email = u.Email, password = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/auth/login", new { email = u.Email, password = ClubFactory.Password })).StatusCode);
    }

    [Fact]
    public async Task StaffInvitation_RequiresManager_AndNoPublicRoleInjection()
    {
        await using var f = new ClubFactory(); var manager = await f.Client(); var owner = await f.Client(await f.User(Role.HorseOwner));
        var staff = new StaffRequest("vet@example.test", "vet", "Test", "Vet", "0900", "Address", Role.Veterinarian);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/staff", staff, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(manager, "/api/staff", staff);
        var anonymous = f.CreateClient(); var code = await f.Code(staff.Email, "Invite");
        Assert.Matches("^[0-9]{6}$", code);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new { email = staff.UserName, password = ClubFactory.Password })).StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            var message = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.EmailMessages, x => x.Recipient == staff.Email);
            Assert.Contains("Tên đăng nhập: vet", message.Body);
            Assert.DoesNotContain("http", message.Body);
        });
        var accepted = await ClubFactory.Post(anonymous, "/api/auth/accept-invitation", new { email = staff.Email, code, password = ClubFactory.Password, confirmPassword = ClubFactory.Password });
        Assert.True(accepted.GetProperty("changed").GetBoolean());
        var again = await ClubFactory.Post(anonymous, "/api/auth/accept-invitation", new { email = staff.Email, code, password = ClubFactory.Password, confirmPassword = ClubFactory.Password });
        Assert.False(again.GetProperty("changed").GetBoolean());
        var usernameLogin = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = " VET ", password = ClubFactory.Password });
        Assert.Equal(HttpStatusCode.OK, usernameLogin.StatusCode);
        var injected = await anonymous.PostAsJsonAsync("/api/auth/register", new { email = "bad@example.test", userName = "bad", firstName = "A", lastName = "B", phone = "0900", address = "Here", password = ClubFactory.Password, confirmPassword = ClubFactory.Password, role = "ClubManager" });
        Assert.Equal(HttpStatusCode.BadRequest, injected.StatusCode);
    }

    [Fact]
    public async Task StaffDirectory_IsAvailableToAuthenticatedRolesWithoutContactDetails_ManagementIsManagerOnly()
    {
        await using var f = new ClubFactory();
        var manager = await f.Client();
        var trainer = await f.User(Role.Trainer, "private-trainer@example.test");
        await f.User(Role.Veterinarian, "private-vet@example.test");

        foreach (var role in Enum.GetValues<Role>())
        {
            var client = role == Role.ClubManager ? manager : await f.Client(await f.User(role));
            var directoryResponse = await client.GetAsync("/api/staff/directory?page=1&pageSize=20");
            Assert.Equal(HttpStatusCode.OK, directoryResponse.StatusCode);
            var directory = await directoryResponse.Content.ReadFromJsonAsync<JsonElement>();
            var directoryJson = directory.GetProperty("items").GetRawText();
            Assert.Contains(trainer.FirstName, directoryJson);
            Assert.DoesNotContain("private-trainer@example.test", directoryJson);
            Assert.DoesNotContain("private-vet@example.test", directoryJson);

            var staffListResponse = await client.GetAsync("/api/staff?page=1&pageSize=20");
            Assert.Equal(role == Role.ClubManager ? HttpStatusCode.OK : HttpStatusCode.Forbidden, staffListResponse.StatusCode);

            if (role != Role.ClubManager)
            {
                var activateResponse = await client.PutAsJsonAsync(
                    $"/api/staff/{trainer.Id}/active", new ActiveRequest(true), ClubFactory.Json);
                Assert.Equal(HttpStatusCode.Forbidden, activateResponse.StatusCode);
            }
        }
    }

    [Fact]
    public async Task Intake_Revision_Approval_Assignment_AndCrossOwnerIsolation()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer); var trainer = await f.User(Role.Trainer);
        var c = await f.Client(owner); var manager = await f.Client();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var r = new RegistrationRequest("Thunder", "Sire", "Dam", new DateOnly(2020, 1, 1), HorseGender.Male, "Thoroughbred", "T-001", 160, 500, today, "Fit declared", "", today, today.AddMonths(2), head.Id, null, null);
        var reg = await ClubFactory.Post(c, "/api/registrations", r); var id = reg.GetProperty("id").GetGuid();
        await Upload(c, id, "HorsePhoto", "photo.png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0 });
        await Upload(c, id, "Certificate", "certificate.pdf", Encoding.ASCII.GetBytes("%PDF-1.4\nTest"));
        await ClubFactory.Post(c, $"/api/registrations/{id}/submit", new { });
        Assert.Equal(0, (await c.GetFromJsonAsync<JsonElement>("/api/horses")).GetProperty("total").GetInt32());
        await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = false, reason = "Clarify pedigree" });
        Assert.True((await c.PutAsJsonAsync($"/api/registrations/{id}", r, ClubFactory.Json)).IsSuccessStatusCode);
        await ClubFactory.Post(c, $"/api/registrations/{id}/submit", new { });
        var approved = await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = true }); var horseId = approved.GetProperty("horseId").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync($"/api/registrations/{id}/review", new { approve = true })).StatusCode);
        var otherOwner = await f.Client(await f.User(Role.HorseOwner));
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.GetAsync($"/api/horses/{horseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherOwner.GetAsync($"/api/registrations/{id}/attachments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync($"/api/horses/{horseId}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, today, ""), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(manager, $"/api/horses/{horseId}/assignments", new AssignmentRequest(head.Id, Role.HeadTrainer, today, ""));
        var hc = await f.Client(head); await ClubFactory.Post(hc, $"/api/horses/{horseId}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, today, "First"));
        var replacement = await f.User(Role.Trainer); await ClubFactory.Post(hc, $"/api/horses/{horseId}/assignments", new AssignmentRequest(replacement.Id, Role.Trainer, today, "Replacement"));
        var detail = await c.GetFromJsonAsync<JsonElement>($"/api/horses/{horseId}"); Assert.Equal(3, detail.GetProperty("assignments").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.Client(trainer)).GetAsync($"/api/horses/{horseId}")).StatusCode);
    }

    [Fact]
    public async Task MedicalLock_RecheckedAtStart_Clearance_ThenExecutionEvaluation()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var trainer = await f.User(Role.Trainer); var head = await f.User(Role.HeadTrainer); var vet = await f.User(Role.Veterinarian); var rider = await f.User(Role.WorkRider);
        var horse = await f.Horse(owner, trainer, head, vet); var tc = await f.Client(trainer); var hc = await f.Client(head); var vc = await f.Client(vet); var rc = await f.Client(rider);
        var template = await ClubFactory.Post(hc, "/api/training/templates", new TemplateRequest("Speed", "Goal", "Phase", 1000, Intensity.Heavy, "Sand", 3, ""));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = await ClubFactory.Post(tc, "/api/training/plans", new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Phase", today, today.AddDays(7), ""));
        var sid = (await ClubFactory.Post(tc, $"/api/training/plans/{plan.GetProperty("id").GetGuid()}/sessions", new SessionRequest(DateTimeOffset.UtcNow.AddMinutes(10), TrainingType.Sprint, 1000, Intensity.Heavy, "Sand", "Target", "", rider.Id))).GetProperty("id").GetGuid();
        var medical = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(DateTimeOffset.UtcNow.AddMinutes(-1), "Check", "Symptoms", "Findings", "Private diagnosis", HealthStatus.Monitoring, "Private notes")); var medicalId = medical.GetProperty("id").GetGuid();
        await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/restrictions", new RestrictionRequest(medicalId, true, false, Intensity.Light, 500, true, DateTimeOffset.UtcNow.AddMinutes(-1), null, "Light training only"));
        Assert.Equal(HttpStatusCode.Conflict, (await rc.PostAsJsonAsync($"/api/training/sessions/{sid}/start", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await tc.GetAsync($"/api/horses/{horse.Id}/medical/records")).StatusCode);
        var followup = new FollowUpRequest(medicalId, new MedicalRequest(DateTimeOffset.UtcNow.AddSeconds(-1), "Review", "Normal", "Recovered", "Cleared", HealthStatus.Fit, ""), true, "Recovered");
        Assert.Equal(HttpStatusCode.Forbidden, (await tc.PostAsJsonAsync($"/api/horses/{horse.Id}/medical/follow-ups", followup, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/follow-ups", followup);
        await ClubFactory.Post(rc, $"/api/training/sessions/{sid}/start", new { });
        var result = await ClubFactory.Post(rc, $"/api/training/sessions/{sid}/results", new ResultRequest(1000, 100, 120, Intensity.Heavy, "Completed well", false));
        Assert.Equal(10m, result.GetProperty("speedMetresPerSecond").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, (await rc.PostAsJsonAsync($"/api/training/sessions/{sid}/results", new ResultRequest(1000, 100, 120, Intensity.Heavy, "Again", false), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(tc, $"/api/training/sessions/{sid}/evaluation", new EvaluationRequest("Continue plan", false));
        var otherRider = await f.Client(await f.User(Role.WorkRider));
        Assert.Equal(HttpStatusCode.Forbidden, (await otherRider.GetAsync($"/api/training/sessions/{sid}")).StatusCode);
        var reportText = await (await f.Client(owner)).GetStringAsync($"/api/reports?horseId={horse.Id}");
        Assert.DoesNotContain("Private diagnosis", reportText); Assert.DoesNotContain("Private notes", reportText);
        var actualReport = await (await f.Client(owner)).GetFromJsonAsync<JsonElement>($"/api/reports?horseId={horse.Id}&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"))}");
        Assert.Equal(1, actualReport.GetProperty("kpi").GetProperty("completed").GetInt32());
    }

    [Fact]
    public async Task EnumJson_RejectsNumbersAndUnknownNames_AndConfiguredPasswordPolicyIsUsed()
    {
        const string longPassword = "LongTestPassword123!";
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Security:PasswordMinLength"] = "20", ["Bootstrap:ManagerPassword"] = longPassword, ["Business:MaxPageSize"] = "7", ["Business:DefaultPageSize"] = "7" });
        var anonymous = f.CreateClient();
        var invalidPassword = await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest("config@example.test", "config", "Test", "Owner", "0900", "Here", ClubFactory.Password, ClubFactory.Password));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPassword.StatusCode);
        Assert.Contains("20", await invalidPassword.Content.ReadAsStringAsync());
        var login = await ClubFactory.Post(anonymous, "/api/auth/login", new { email = "manager@example.test", password = longPassword });
        anonymous.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/staff?pageSize=8")).StatusCode);
        var invalidNumber = await anonymous.PostAsJsonAsync("/api/staff", new { email = "staff@example.test", userName = "staff", firstName = "Test", lastName = "Staff", phone = "0900", address = "Here", role = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, invalidNumber.StatusCode);
        var invalidName = await anonymous.PostAsJsonAsync("/api/staff", new { email = "staff@example.test", userName = "staff", firstName = "Test", lastName = "Staff", phone = "0900", address = "Here", role = "UndefinedRole" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidName.StatusCode);
        var valid = await ClubFactory.Post(anonymous, "/api/staff", new StaffRequest("staff@example.test", "staff", "Test", "Staff", "0900", "Here", Role.Trainer));
        Assert.Equal("Trainer", valid.GetProperty("role").GetString());
    }

    [Fact]
    public async Task RefreshAndLogout_InvalidateBothAccessAndRefreshTokens()
    {
        await using var f = new ClubFactory(); var u = await f.User(Role.HorseOwner); var anonymous = f.CreateClient();
        var login = await ClubFactory.Post(anonymous, "/api/auth/login", new { email = u.Email, password = ClubFactory.Password });
        var refreshed = await ClubFactory.Post(anonymous, "/api/auth/refresh", new { refreshToken = login.GetProperty("refreshToken").GetString() });
        anonymous.DefaultRequestHeaders.Authorization = new("Bearer", refreshed.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/auth/me")).StatusCode);
        await ClubFactory.Post(anonymous, "/api/auth/logout", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refreshed.GetProperty("refreshToken").GetString() })).StatusCode);
    }

    [Fact]
    public async Task CareTask_TreatmentPrivacy_AndInventoryCannotGoNegative()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var groom = await f.User(Role.Groom); var otherGroom = await f.User(Role.Groom); var vet = await f.User(Role.Veterinarian);
        var horse = await f.Horse(owner, groom, vet); var manager = await f.Client(); var gc = await f.Client(groom); var vc = await f.Client(vet);
        var care = await ClubFactory.Post(manager, "/api/care/tasks", new CareRequest(horse.Id, groom.Id, null, CareType.Feeding, DateTimeOffset.UtcNow, "Feed", 4)); var careId = care.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await (await f.Client(otherGroom)).PostAsJsonAsync($"/api/care/tasks/{careId}/record", new CareCompletionRequest(CareStatus.Completed, 4, "Done"), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(gc, $"/api/care/tasks/{careId}/record", new CareCompletionRequest(CareStatus.Completed, 3.5m, "Slightly reduced"));
        Assert.Equal(HttpStatusCode.Conflict, (await gc.PostAsJsonAsync($"/api/care/tasks/{careId}/record", new CareCompletionRequest(CareStatus.Completed, 4, "Duplicate"), ClubFactory.Json)).StatusCode);
        var record = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(DateTimeOffset.UtcNow.AddSeconds(-1), "Check", "Signs", "Exam", "Private diagnosis", HealthStatus.Monitoring, ""));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var treatment = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/treatments", new TreatmentRequest(record.GetProperty("id").GetGuid(), null, today, today.AddDays(5), today.AddDays(3), "Recover", "Private medication instruction", "Medicine", "Daily"));
        await ClubFactory.Post(vc, "/api/care/tasks", new CareRequest(horse.Id, groom.Id, treatment.GetProperty("id").GetGuid(), CareType.Treatment, DateTimeOffset.UtcNow, "Private medication instruction", null));
        Assert.DoesNotContain("Private medication instruction", await (await f.Client(owner)).GetStringAsync($"/api/care/tasks?horseId={horse.Id}"));
        var item = await ClubFactory.Post(manager, "/api/inventory", new InventoryRequest("Feed", "Food", "kg", 5)); var itemId = item.GetProperty("id").GetGuid();
        await ClubFactory.Post(manager, $"/api/inventory/{itemId}/movements", new MovementRequest(10, "Received"));
        Assert.Equal(HttpStatusCode.Conflict, (await gc.PostAsJsonAsync($"/api/inventory/{itemId}/movements", new MovementRequest(-11, "Use"), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(gc, $"/api/inventory/{itemId}/movements", new MovementRequest(-6, "Feeding"));
        var low = await manager.GetFromJsonAsync<JsonElement>("/api/inventory?lowStock=true"); Assert.Equal(4m, low.GetProperty("items")[0].GetProperty("stock").GetDecimal());
    }

    [Fact]
    public async Task ConcurrentApproval_CreatesOneHorse_AndInvalidUploadRejected()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var c = await f.Client(owner); var manager = await f.Client(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var reg = await ClubFactory.Post(c, "/api/registrations", new RegistrationRequest("Horse", "Sire", "Dam", new DateOnly(2020, 1, 1), HorseGender.Female, "Breed", null, 160, 500, today, "Healthy", "", today, null, null, null, null)); var id = reg.GetProperty("id").GetGuid();
        using (var bad = new MultipartFormDataContent())
        {
            bad.Add(new StringContent("HorsePhoto"), "type"); bad.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("<script>alert(1)</script>")), "file", "bad.png");
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync($"/api/registrations/{id}/attachments", bad)).StatusCode);
        }
        await Upload(c, id, "HorsePhoto", "photo.png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0 }); await Upload(c, id, "Certificate", "cert.pdf", Encoding.ASCII.GetBytes("%PDF-1.4"));
        await ClubFactory.Post(c, $"/api/registrations/{id}/submit", new { });
        var responses = await Task.WhenAll(manager.PostAsJsonAsync($"/api/registrations/{id}/review", new { approve = true }), manager.PostAsJsonAsync($"/api/registrations/{id}/review", new { approve = true }));
        Assert.Single(responses, x => x.IsSuccessStatusCode); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, (await c.GetFromJsonAsync<JsonElement>("/api/horses")).GetProperty("total").GetInt32());
    }

    private static async Task Upload(HttpClient c, Guid registrationId, string type, string filename, byte[] bytes)
    {
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(type), "type"); form.Add(new ByteArrayContent(bytes), "file", filename);
        var response = await c.PostAsync($"/api/registrations/{registrationId}/attachments", form); Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }
}
