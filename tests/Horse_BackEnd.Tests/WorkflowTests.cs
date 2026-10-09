using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        var verify = await ClubFactory.Post(anonymous, "/api/auth/verify-email", new { email, code }); Assert.True(verify.GetProperty("verified").GetBoolean());
        var repeated = await ClubFactory.Post(anonymous, "/api/auth/verify-email", new { email, code }); Assert.False(repeated.GetProperty("verified").GetBoolean());
        var login = await ClubFactory.Post(anonymous, "/api/auth/login", new { email, password = ClubFactory.Password });
        anonymous.DefaultRequestHeaders.Authorization = new("Bearer", login.GetProperty("accessToken").GetString());
        await ClubFactory.Post(anonymous, "/api/auth/forgot-password", new { email });
        var resetCode = await f.Code(email, "Reset");
        var reset = await ClubFactory.Post(anonymous, "/api/auth/reset-password", new { email, code = resetCode, password = "NewPassword123!", confirmPassword = "NewPassword123!" }); Assert.True(reset.GetProperty("changed").GetBoolean());
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
        var accepted = await ClubFactory.Post(anonymous, "/api/auth/accept-invitation", new { email = staff.Email, code, password = ClubFactory.Password, confirmPassword = ClubFactory.Password });
        Assert.True(accepted.GetProperty("changed").GetBoolean());
        var injected = await anonymous.PostAsJsonAsync("/api/auth/register", new { email = "bad@example.test", userName = "bad", firstName = "A", lastName = "B", phone = "0900", address = "Here", password = ClubFactory.Password, confirmPassword = ClubFactory.Password, role = "ClubManager" });
        Assert.Equal(HttpStatusCode.BadRequest, injected.StatusCode);
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
    public async Task FormerTrainerLosesCurrentWriteScope_AndRiderHistoryDoesNotGrantHorseOrMedicalScope()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner);
        var formerTrainer = await f.User(Role.Trainer);
        var currentTrainer = await f.User(Role.Trainer);
        var headTrainer = await f.User(Role.HeadTrainer);
        var veterinarian = await f.User(Role.Veterinarian);
        var rider = await f.User(Role.WorkRider);
        var horse = await f.Horse(owner, formerTrainer, headTrainer, veterinarian);
        var formerTrainerClient = await f.Client(formerTrainer);
        var headTrainerClient = await f.Client(headTrainer);
        var riderClient = await f.Client(rider);
        var vetClient = await f.Client(veterinarian);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var template = await ClubFactory.Post(headTrainerClient, "/api/training/templates",
            new TemplateRequest("History scope", "Goal", "Phase", 500, Intensity.Light, "Sand", 2, ""));
        var plan = await ClubFactory.Post(formerTrainerClient, "/api/training/plans",
            new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Phase", today, today.AddDays(7), ""));
        var session = await ClubFactory.Post(formerTrainerClient,
            $"/api/training/plans/{plan.GetProperty("id").GetGuid()}/sessions",
            new SessionRequest(DateTimeOffset.UtcNow.AddMinutes(30), TrainingType.Trot, 500, Intensity.Light, "Sand", "Easy work", "", rider.Id));
        var sessionId = session.GetProperty("id").GetGuid();

        await ClubFactory.Post(headTrainerClient, $"/api/horses/{horse.Id}/assignments",
            new AssignmentRequest(currentTrainer.Id, Role.Trainer, today, "Replacement"));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await formerTrainerClient.PutAsJsonAsync($"/api/training/plans/{plan.GetProperty("id").GetGuid()}",
                new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Changed", "Phase", today, today.AddDays(7), ""),
                ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await formerTrainerClient.GetAsync($"/api/horses/{horse.Id}")).StatusCode);
        var formerTrainerPlan = await formerTrainerClient.GetFromJsonAsync<JsonElement>($"/api/training/plans/{plan.GetProperty("id").GetGuid()}");
        Assert.False(formerTrainerPlan.TryGetProperty("restrictions", out _));
        Assert.Equal(HttpStatusCode.OK, (await formerTrainerClient.GetAsync($"/api/training/plans/{plan.GetProperty("id").GetGuid()}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await formerTrainerClient.GetAsync($"/api/training/sessions/{sessionId}")).StatusCode);

        var medical = await ClubFactory.Post(vetClient, $"/api/horses/{horse.Id}/medical/records",
            new MedicalRequest(DateTimeOffset.UtcNow, "Examination", "None", "Normal", "Private diagnosis", HealthStatus.Monitoring, "Private notes"));
        await ClubFactory.Post(vetClient, $"/api/horses/{horse.Id}/medical/restrictions",
            new RestrictionRequest(medical.GetProperty("id").GetGuid(), false, false, Intensity.Light, 500, false,
                DateTimeOffset.UtcNow.AddMinutes(-1), null, "Operational limit"));

        Assert.Equal(HttpStatusCode.OK, (await riderClient.GetAsync($"/api/horses/{horse.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await riderClient.GetAsync($"/api/horses/{horse.Id}/medical/records")).StatusCode);
        var currentRiderPlan = await riderClient.GetFromJsonAsync<JsonElement>($"/api/training/plans/{plan.GetProperty("id").GetGuid()}");
        Assert.True(currentRiderPlan.GetProperty("restrictions").GetArrayLength() > 0);

        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var persistedSession = await db.Sessions.SingleAsync(x => x.Id == sessionId);
            persistedSession.Status = SessionStatus.Completed;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await riderClient.GetAsync($"/api/horses/{horse.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await riderClient.GetAsync($"/api/horses/{horse.Id}/medical/summary")).StatusCode);
        var riderHorses = await riderClient.GetFromJsonAsync<JsonElement>("/api/horses?page=1&pageSize=20");
        Assert.Equal(0, riderHorses.GetProperty("total").GetInt32());

        var historicalSession = await riderClient.GetFromJsonAsync<JsonElement>($"/api/training/sessions/{sessionId}");
        Assert.Equal(sessionId, historicalSession.GetProperty("session").GetProperty("id").GetGuid());
        var historicalPlan = await riderClient.GetFromJsonAsync<JsonElement>($"/api/training/plans/{plan.GetProperty("id").GetGuid()}");
        Assert.False(historicalPlan.TryGetProperty("restrictions", out _));
        Assert.Contains(sessionId, historicalPlan.GetProperty("sessions").EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        var historicalSessions = await riderClient.GetFromJsonAsync<JsonElement>($"/api/training/sessions?horseId={horse.Id}");
        Assert.Equal(1, historicalSessions.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task HorseArchive_ClosesOperationalWorkAndPreservesReason_AndActiveSessionBlocksAtomically()
    {
        await using var f = new ClubFactory();
        var manager = await f.Client();
        var owner = await f.User(Role.HorseOwner);
        var trainer = await f.User(Role.Trainer);
        var headTrainer = await f.User(Role.HeadTrainer);
        var veterinarian = await f.User(Role.Veterinarian);
        var groom = await f.User(Role.Groom);
        var rider = await f.User(Role.WorkRider);
        var horse = await f.Horse(owner, trainer, headTrainer, veterinarian, groom);
        var trainerClient = await f.Client(trainer);
        var headTrainerClient = await f.Client(headTrainer);
        var riderClient = await f.Client(rider);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var template = await ClubFactory.Post(headTrainerClient, "/api/training/templates",
            new TemplateRequest("Archive", "Goal", "Phase", 500, Intensity.Light, "Sand", 2, ""));
        var plan = await ClubFactory.Post(trainerClient, "/api/training/plans",
            new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Phase", today, today.AddDays(7), ""));
        var planId = plan.GetProperty("id").GetGuid();
        var planned = await ClubFactory.Post(trainerClient, $"/api/training/plans/{planId}/sessions",
            new SessionRequest(DateTimeOffset.UtcNow.AddHours(1), TrainingType.Walk, 400, Intensity.Light, "Sand", "Walk", "", null));
        var assigned = await ClubFactory.Post(trainerClient, $"/api/training/plans/{planId}/sessions",
            new SessionRequest(DateTimeOffset.UtcNow.AddHours(2), TrainingType.Trot, 500, Intensity.Light, "Sand", "Trot", "", rider.Id));
        var pendingCareTask = await ClubFactory.Post(manager, "/api/care/tasks",
            new CareRequest(horse.Id, groom.Id, null, CareType.Feeding, DateTimeOffset.UtcNow.AddHours(3), "Feed", 5));

        var stable = await ClubFactory.Post(manager, "/api/care/stables", new NameRequest("Archive stable"));
        var stall = await ClubFactory.Post(manager, "/api/care/stalls",
            new StallRequest(stable.GetProperty("id").GetGuid(), "Archive stall"));
        await ClubFactory.Post(manager, $"/api/care/stalls/{stall.GetProperty("id").GetGuid()}/occupancy",
            new OccupancyRequest(horse.Id));

        const string archiveReason = "Retired after owner request";
        var archiveResponse = await manager.PostAsJsonAsync($"/api/horses/{horse.Id}/archive",
            new ReasonRequest(archiveReason), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var archived = await db.Horses.SingleAsync(x => x.Id == horse.Id);
            Assert.True(archived.Archived);
            Assert.All(await db.Assignments.Where(x => x.HorseId == horse.Id).ToListAsync(), x => Assert.False(x.Active));
            Assert.All(await db.Assignments.Where(x => x.HorseId == horse.Id).ToListAsync(), x => Assert.Equal(today, x.EndDate));
            Assert.Equal(PlanStatus.Archived, (await db.Plans.SingleAsync(x => x.Id == planId)).Status);
            Assert.Equal(SessionStatus.Skipped, (await db.Sessions.SingleAsync(x => x.Id == planned.GetProperty("id").GetGuid())).Status);
            Assert.Equal(SessionStatus.Skipped, (await db.Sessions.SingleAsync(x => x.Id == assigned.GetProperty("id").GetGuid())).Status);
            Assert.Contains(archiveReason, (await db.Sessions.SingleAsync(x => x.Id == assigned.GetProperty("id").GetGuid())).Notes);
            var cancelledCareTask = await db.CareTasks.SingleAsync(x => x.Id == pendingCareTask.GetProperty("id").GetGuid());
            Assert.Equal(CareStatus.Skipped, cancelledCareTask.Status);
            Assert.Contains(archiveReason, cancelledCareTask.Notes);
            Assert.NotNull((await db.Occupancies.SingleAsync(x => x.HorseId == horse.Id)).EndedAt);
            Assert.Equal(archiveReason, (await db.Audit.SingleAsync(x => x.ReferenceId == horse.Id && x.Action == AuditAction.HorseArchived)).Detail);
            Assert.True(await db.TrainingRevisions.AnyAsync(x => x.PlanId == planId));
            Assert.True(await db.Notifications.AnyAsync(x => x.RecipientId == rider.Id && x.ReferenceId == assigned.GetProperty("id").GetGuid()));
        }

        var activeHorse = await f.Horse(owner, trainer, headTrainer, veterinarian, groom);
        var activePlan = await ClubFactory.Post(trainerClient, "/api/training/plans",
            new PlanRequest(activeHorse.Id, template.GetProperty("id").GetGuid(), "Active", "Phase", today, today.AddDays(7), ""));
        var activeSession = await ClubFactory.Post(trainerClient,
            $"/api/training/plans/{activePlan.GetProperty("id").GetGuid()}/sessions",
            new SessionRequest(DateTimeOffset.UtcNow.AddMinutes(10), TrainingType.Walk, 400, Intensity.Light, "Sand", "Active", "", rider.Id));
        var activeCareTask = await ClubFactory.Post(manager, "/api/care/tasks",
            new CareRequest(activeHorse.Id, groom.Id, null, CareType.Feeding, DateTimeOffset.UtcNow.AddHours(1), "Feed", 5));
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            (await db.CareTasks.SingleAsync(x => x.Id == activeCareTask.GetProperty("id").GetGuid())).Status = CareStatus.InProgress;
            await db.SaveChangesAsync();
        }
        await ClubFactory.Post(riderClient, $"/api/training/sessions/{activeSession.GetProperty("id").GetGuid()}/start", new { });

        var blockedArchive = await manager.PostAsJsonAsync($"/api/horses/{activeHorse.Id}/archive",
            new ReasonRequest("Archive while a session is running"), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict, blockedArchive.StatusCode);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            Assert.False((await db.Horses.SingleAsync(x => x.Id == activeHorse.Id)).Archived);
            Assert.Contains(await db.Assignments.Where(x => x.HorseId == activeHorse.Id).ToListAsync(), x => x.Active);
            Assert.Equal(PlanStatus.Active, (await db.Plans.SingleAsync(x => x.Id == activePlan.GetProperty("id").GetGuid())).Status);
            Assert.Equal(SessionStatus.InProgress, (await db.Sessions.SingleAsync(x => x.Id == activeSession.GetProperty("id").GetGuid())).Status);
            Assert.Equal(CareStatus.InProgress, (await db.CareTasks.SingleAsync(x => x.Id == activeCareTask.GetProperty("id").GetGuid())).Status);
        }

        var careOnlyHorse = await f.Horse(owner, trainer, headTrainer, veterinarian, groom);
        var careOnlyTask = await ClubFactory.Post(manager, "/api/care/tasks",
            new CareRequest(careOnlyHorse.Id, groom.Id, null, CareType.Feeding, DateTimeOffset.UtcNow.AddHours(1), "Feed", 5));
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            (await db.CareTasks.SingleAsync(x => x.Id == careOnlyTask.GetProperty("id").GetGuid())).Status = CareStatus.InProgress;
            await db.SaveChangesAsync();
        }
        var blockedByCare = await manager.PostAsJsonAsync($"/api/horses/{careOnlyHorse.Id}/archive",
            new ReasonRequest("Archive while care is running"), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict, blockedByCare.StatusCode);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            Assert.False((await db.Horses.SingleAsync(x => x.Id == careOnlyHorse.Id)).Archived);
            Assert.Contains(await db.Assignments.Where(x => x.HorseId == careOnlyHorse.Id).ToListAsync(), x => x.Active);
            Assert.Equal(CareStatus.InProgress, (await db.CareTasks.SingleAsync(x => x.Id == careOnlyTask.GetProperty("id").GetGuid())).Status);
        }
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
