using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class IntakeTests
{
    [Fact]
    public async Task AssignmentSavesOptionalEndDateAndRejectsEndBeforeStart()
    {
        await using var f = new ClubFactory();
        var owner = await f.User(Role.HorseOwner); var vet = await f.User(Role.Veterinarian);
        var horse = await f.Horse(owner); using var manager = await f.Client();
        var today = Today(f); var path = $"/api/horses/{horse.Id}/assignments";
        var request = new AssignmentRequest(vet.Id, Role.Veterinarian, today, "Fixed term", EndDate: today.AddDays(7));
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync(path, request with { EndDate = today.AddDays(-1) }, ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Empty(await db.Assignments.Where(a => a.HorseId == horse.Id).ToListAsync()));
        await ClubFactory.Post(manager, path, request);
        await WorkerTests.Read(f, async db => Assert.Equal(today.AddDays(7), (await db.Assignments.SingleAsync(a => a.HorseId == horse.Id && a.Active)).EndDate));
        await ClubFactory.Post(manager, path, request with { EndDate = null });
        await WorkerTests.Read(f, async db => Assert.Null((await db.Assignments.SingleAsync(a => a.HorseId == horse.Id && a.Active)).EndDate));
    }

    [Fact]
    public async Task PartialDraftRequiresCompleteIntakeAndBothFilesThenRevisionApprovalAndAssignment()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer);
        var trainer = await f.User(Role.Trainer); var vet = await f.User(Role.Veterinarian); var groom = await f.User(Role.Groom);
        using var client = await f.Client(owner); using var manager = await f.Client();
        var draft = await ClubFactory.Post(client, "/api/registrations", new { name = "Thunder" }); var id = draft.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, draft.GetProperty("gender").ValueKind);
        Assert.Equal(JsonValueKind.Null, draft.GetProperty("dateOfBirth").ValueKind);
        Assert.Equal(JsonValueKind.Null, draft.GetProperty("heightCm").ValueKind);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/registrations/{id}/submit", new { })).StatusCode);
        await WorkerTests.Read(f, async db => { Assert.Empty(await db.Horses.ToListAsync()); Assert.Null((await db.Registrations.FindAsync(id))!.Gender); });
        Assert.True((await client.PutAsJsonAsync($"/api/registrations/{id}", Complete(f) with { PreferredHeadTrainerId = head.Id, PreferredVeterinarianId = vet.Id, PreferredGroomId = groom.Id }, ClubFactory.Json)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/registrations/{id}/submit", new { })).StatusCode);
        await Upload(client, id, AttachmentType.HorsePhoto);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/registrations/{id}/submit", new { })).StatusCode);
        await Upload(client, id, AttachmentType.Certificate); await ClubFactory.Post(client, $"/api/registrations/{id}/submit", new { });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/registrations/{id}", Complete(f), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync($"/api/registrations/{id}/review", new { approve = false })).StatusCode);
        await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = false, reason = "Confirm pedigree" });
        Assert.True((await client.PutAsJsonAsync($"/api/registrations/{id}", Complete(f), ClubFactory.Json)).IsSuccessStatusCode);
        await ClubFactory.Post(client, $"/api/registrations/{id}/submit", new { });
        await WorkerTests.Read(f, async db => { Assert.Empty(await db.Horses.ToListAsync()); Assert.Empty(await db.Assignments.ToListAsync()); });
        var result = await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = true }); var horseId = result.GetProperty("horseId").GetGuid();
        await ClubFactory.Post(manager, $"/api/horses/{horseId}/assignments", new AssignmentRequest(head.Id, Role.HeadTrainer, Today(f), "Official"));
        using var headClient = await f.Client(head);
        await ClubFactory.Post(headClient, $"/api/horses/{horseId}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, Today(f), "Official"));
        await WorkerTests.Read(f, async db =>
        {
            Assert.Single(await db.Horses.ToListAsync()); Assert.Single(await db.Measurements.ToListAsync());
            Assert.Equal(HealthStatus.Monitoring, (await db.Horses.FindAsync(horseId))!.HealthStatus);
            Assert.Equal(2, await db.Assignments.CountAsync());
        });
    }

    [Fact]
    public async Task ManagerEditsOnlyAdministrativeFieldsAndAuditsBeforeAfter()
    {
        await using var f = new ClubFactory(); using var owner = await f.Client(await f.User(Role.HorseOwner)); using var manager = await f.Client();
        var id = (await ClubFactory.Post(owner, "/api/registrations", Complete(f))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync($"/api/registrations/{id}", new { name = "Draft edit" })).StatusCode);
        await Upload(owner, id, AttachmentType.HorsePhoto); await Upload(owner, id, AttachmentType.Certificate);
        await ClubFactory.Post(owner, $"/api/registrations/{id}/submit", new { });
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/registrations/{id}", new { sire = "Other sire" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/registrations/{id}", new { declaredHealth = "Other health" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PutAsJsonAsync($"/api/registrations/{id}", new { name = " " })).StatusCode);
        var response = await manager.PutAsJsonAsync($"/api/registrations/{id}", new { name = "Thunder corrected", registrationNumber = "CLUB-002", boardingStart = Today(f), boardingEnd = Today(f).AddMonths(2) });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            var row = (await db.Registrations.FindAsync(id))!; Assert.Equal("Sire", row.Sire); Assert.Equal("Declared monitoring", row.DeclaredHealth);
            var audit = await db.Audit.SingleAsync(x => x.ReferenceId == id && x.Action == AuditAction.RegistrationEdited);
            var detail = JsonDocument.Parse(audit.Detail).RootElement; Assert.Equal("Thunder", detail.GetProperty("Before").GetProperty("Name").GetString());
            Assert.Equal("Thunder corrected", detail.GetProperty("After").GetProperty("Name").GetString());
        });
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/registrations/{id}", new { name = "Thunder corrected" })).StatusCode);
        await WorkerTests.Read(f, async db =>
        { var row = (await db.Registrations.FindAsync(id))!; Assert.Equal("CLUB-002", row.RegistrationNumber); Assert.Equal(Today(f).AddMonths(2), row.BoardingEnd); });
        var clearDocument = await manager.PutAsJsonAsync($"/api/registrations/{id}", new { externalRegistrationNumber = " " });
        clearDocument.EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Null, (await clearDocument.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("externalRegistrationNumber").ValueKind);
        var approved = await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = true });
        await WorkerTests.Read(f, async db => Assert.Equal("Thunder corrected", (await db.Horses.FindAsync(approved.GetProperty("horseId").GetGuid()))!.Name));
    }

    [Theory]
    [InlineData("{\"gender\":\"Unknown\"}")]
    [InlineData("{\"gender\":0}")]
    [InlineData("{\"heightCm\":0}")]
    [InlineData("{\"dateOfBirth\":\"0001-01-01\"}")]
    [InlineData("{\"preferredTrainerId\":\"00000000-0000-0000-0000-000000000001\"}")]
    public async Task PartialDraftStillRejectsInvalidValuesAndTrainerInjection(string body)
    {
        await using var f = new ClubFactory(); using var client = await f.Client(await f.User(Role.HorseOwner));
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/registrations", content)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Empty(await db.Registrations.ToListAsync()));
    }

    [Fact]
    public async Task IncompleteLegacyPendingCannotBeApprovedAndFailuresRollbackReviewActor()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var r = await StorageRecoveryTests.Registration(f, owner);
        await WorkerTests.Read(f, async db => { (await db.Registrations.FindAsync(r.Id))!.Status = RegistrationStatus.PendingReview; await db.SaveChangesAsync(); });
        using var manager = await f.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsJsonAsync($"/api/registrations/{r.Id}/review", new { approve = true })).StatusCode);
        await WorkerTests.Read(f, async db =>
        { Assert.Empty(await db.Horses.ToListAsync()); Assert.Empty(await db.Measurements.ToListAsync()); Assert.Null((await db.Registrations.FindAsync(r.Id))!.ReviewedBy); });
    }

    [Fact]
    public async Task DraftLimitsComeFromConfigurationAndWrongPreferredRoleIsRejected()
    {
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:MaxHorseHeightCm"] = "180" });
        using var client = await f.Client(await f.User(Role.HorseOwner)); var trainer = await f.User(Role.Trainer);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registrations", new { heightCm = 181 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/registrations")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/registrations", new { heightCm = 180 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/registrations", new { preferredVeterinarianId = trainer.Id })).StatusCode);
    }

    [Fact]
    public async Task OnlyAssignedHeadTrainerChoosesTrainerAndManagerConfirmsAdministrativeStaff()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer);
        var otherHead = await f.User(Role.HeadTrainer); var trainer = await f.User(Role.Trainer); var vet = await f.User(Role.Veterinarian); var groom = await f.User(Role.Groom);
        var horse = await f.Horse(owner); using var manager = await f.Client(); using var ownerClient = await f.Client(owner);
        var trainerRequest = new AssignmentRequest(trainer.Id, Role.Trainer, Today(f), "Official");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", trainerRequest with { Notes = "" }, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", trainerRequest, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(manager, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(head.Id, Role.HeadTrainer, Today(f), "Official"));
        using var otherHeadClient = await f.Client(otherHead); using var headClient = await f.Client(head);
        Assert.Equal(HttpStatusCode.Forbidden, (await otherHeadClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", trainerRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await headClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(vet.Id, Role.Veterinarian, Today(f), "Official"), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => { (await db.Users.FindAsync(trainer.Id))!.Active = false; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.BadRequest, (await headClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", trainerRequest, ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => { (await db.Users.FindAsync(trainer.Id))!.Active = true; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", trainerRequest with { Notes = "HeadTrainer unavailable" }, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(headClient, $"/api/horses/{horse.Id}/assignments", trainerRequest);
        await ClubFactory.Post(manager, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(vet.Id, Role.Veterinarian, Today(f), "Official"));
        await ClubFactory.Post(manager, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(groom.Id, Role.Groom, Today(f), "Official"));
        await WorkerTests.Read(f, async db => Assert.Equal(4, await db.Assignments.CountAsync(x => x.HorseId == horse.Id && x.Active)));
    }

    [Fact]
    public async Task CancelledDraftAndCrossOwnerCannotBeEditedOrSubmitted()
    {
        await using var f = new ClubFactory(); using var client = await f.Client(await f.User(Role.HorseOwner)); using var other = await f.Client(await f.User(Role.HorseOwner));
        var id = (await ClubFactory.Post(client, "/api/registrations", new { name = "Draft" })).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync($"/api/registrations/{id}", new { name = "Other" })).StatusCode);
        await ClubFactory.Post(client, $"/api/registrations/{id}/cancel", new { });
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/registrations/{id}", new { name = "Other" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/registrations/{id}/submit", new { })).StatusCode);
    }

    [Fact]
    public async Task DraftMigrationPreservesPreviouslyCompleteIntake()
    {
        await using var f = new ClubFactory(); using var client = await f.Client(await f.User(Role.HorseOwner));
        var id = (await ClubFactory.Post(client, "/api/registrations", Complete(f))).GetProperty("id").GetGuid();
        await WorkerTests.Read(f, async db =>
        {
            var migrator = db.GetService<IMigrator>(); var previous = db.Database.GetMigrations().Single(x => x.EndsWith("_WorkerDeliveryReliability"));
            await migrator.MigrateAsync(previous); await migrator.MigrateAsync(); db.ChangeTracker.Clear();
            var row = (await db.Registrations.FindAsync(id))!; Assert.Equal("Thunder", row.Name); Assert.Equal(HorseGender.Male, row.Gender);
            Assert.Equal(160m, row.HeightCm); Assert.Equal(Today(f), row.MeasurementDate);
        });
    }

    [Fact]
    public async Task ApprovalIssuesUniqueClubRegistrationNumbers()
    {
        // Mã đăng ký của ngựa do câu lạc bộ cấp khi duyệt, không trùng giữa các ngựa.
        await using var f = new ClubFactory(); using var manager = await f.Client();
        var horseIds = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            using var owner = await f.Client(await f.User(Role.HorseOwner));
            var created = await ClubFactory.Post(owner, "/api/registrations", Complete(f) with { RegistrationNumber = null, ExternalRegistrationNumber = "  001/OWNER-1  " });
            Assert.Equal("001/OWNER-1", created.GetProperty("externalRegistrationNumber").GetString());
            Assert.Equal("001/OWNER-1", created.GetProperty("registrationNumber").GetString());
            var id = created.GetProperty("id").GetGuid();
            await Upload(owner, id, AttachmentType.HorsePhoto); await Upload(owner, id, AttachmentType.Certificate);
            await ClubFactory.Post(owner, $"/api/registrations/{id}/submit", new { });
            horseIds.Add((await ClubFactory.Post(manager, $"/api/registrations/{id}/review", new { approve = true })).GetProperty("horseId").GetGuid());
            var detail = await owner.GetFromJsonAsync<JsonElement>($"/api/horses/{horseIds[^1]}");
            var responseHorse = detail.GetProperty("horse");
            Assert.Equal("001/OWNER-1", responseHorse.GetProperty("externalRegistrationNumber").GetString());
            Assert.StartsWith("HRC-", responseHorse.GetProperty("systemRegistrationCode").GetString());
            Assert.Equal(responseHorse.GetProperty("registrationNumber").GetString(), responseHorse.GetProperty("systemRegistrationCode").GetString());
            var search = await owner.GetFromJsonAsync<JsonElement>("/api/horses?search=001%2FOWNER-1");
            var item = Assert.Single(search.GetProperty("items").EnumerateArray());
            Assert.Equal(horseIds[^1], item.GetProperty("id").GetGuid());
            Assert.Equal("001/OWNER-1", item.GetProperty("externalRegistrationNumber").GetString());
            var bySystemCode = await owner.GetFromJsonAsync<JsonElement>("/api/horses?search=" + responseHorse.GetProperty("systemRegistrationCode").GetString());
            Assert.Equal(horseIds[^1], Assert.Single(bySystemCode.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
        }
        var legacy = await f.Horse(await f.User(Role.HorseOwner));
        await WorkerTests.Read(f, async db =>
        {
            var horses = await db.Horses.Where(h => horseIds.Contains(h.Id)).ToListAsync();
            Assert.All(horses, h => Assert.Matches("^HRC-[0-9]{6}$", h.RegistrationNumber!));
            Assert.Equal(2, horses.Select(h => h.RegistrationNumber).Distinct().Count());
            // Số chủ ngựa khai vẫn giữ trên hồ sơ đăng ký.
            foreach (var h in horses) Assert.Equal("001/OWNER-1", (await db.Registrations.FindAsync(h.RegistrationId))!.RegistrationNumber);
            // Cơ sở dữ liệu chặn trùng mã kể cả khi ghi trực tiếp.
            (await db.Horses.FindAsync(legacy.Id))!.RegistrationNumber = horses[0].RegistrationNumber;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }

    [Fact]
    public async Task DocumentNumberSupportsLegacyRequestsAndRejectsConflictingAliases()
    {
        await using var f = new ClubFactory(); using var owner = await f.Client(await f.User(Role.HorseOwner));
        var legacy = await ClubFactory.Post(owner, "/api/registrations", new { registrationNumber = "  000123/A  " });
        Assert.Equal("000123/A", legacy.GetProperty("externalRegistrationNumber").GetString());
        var id = legacy.GetProperty("id").GetGuid();
        var conflict = await owner.PutAsJsonAsync($"/api/registrations/{id}", new { registrationNumber = "OLD", externalRegistrationNumber = "NEW" });
        Assert.Equal(HttpStatusCode.BadRequest, conflict.StatusCode);
        Assert.Equal("registration_number_conflict", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        var matching = await ClubFactory.Post(owner, "/api/registrations", new { registrationNumber = " PAPER-1 ", externalRegistrationNumber = "PAPER-1" });
        Assert.Equal("PAPER-1", matching.GetProperty("externalRegistrationNumber").GetString());
        var hrcDocument = await ClubFactory.Post(owner, "/api/registrations", new { externalRegistrationNumber = "HRC-000123" });
        Assert.Equal("HRC-000123", hrcDocument.GetProperty("externalRegistrationNumber").GetString());
        Assert.False(hrcDocument.TryGetProperty("systemRegistrationCode", out _));
        var cleared = await owner.PutAsJsonAsync($"/api/registrations/{id}", new { externalRegistrationNumber = "  " });
        cleared.EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Null, (await cleared.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("externalRegistrationNumber").ValueKind);
        var spoofed = await owner.PostAsJsonAsync("/api/registrations", new { name = "No papers yet", systemRegistrationCode = "HRC-999999" });
        Assert.Equal(HttpStatusCode.BadRequest, spoofed.StatusCode);
        var absent = await ClubFactory.Post(owner, "/api/registrations", new { name = "No papers yet" });
        Assert.Equal(JsonValueKind.Null, absent.GetProperty("externalRegistrationNumber").ValueKind);
        Assert.False(absent.TryGetProperty("systemRegistrationCode", out _));
    }

    private static DateOnly Today(ClubFactory f)
    {
        using var scope = f.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ClubCalendar>().Today(scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }
    private static RegistrationDraftRequest Complete(ClubFactory f) => new("Thunder", "Sire", "Dam", new DateOnly(2020, 1, 1), HorseGender.Male,
        "Thoroughbred", "CLUB-001", 160, 500, Today(f), "Declared monitoring", "", Today(f), Today(f).AddMonths(1));
    private static async Task Upload(HttpClient client, Guid id, AttachmentType type)
    {
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(type.ToString()), "type");
        form.Add(new ByteArrayContent(StorageRecoveryTests.Png), "file", "horse.png");
        var response = await client.PostAsync($"/api/registrations/{id}/attachments", form); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
