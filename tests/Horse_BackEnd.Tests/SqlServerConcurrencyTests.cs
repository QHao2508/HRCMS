using System.Net;
using System.Net.Http.Json;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class SqlServerConcurrencyTests
{
    [SqlServerFact]
    public async Task SeparateHosts_ConcurrentApprovalCreatesOneHorse()
    {
        await using var f = new ClubFactory(); using var first = await f.Client();
        var owner = await f.User(Role.HorseOwner); var reg = await PendingRegistration(f, owner);
        await using var replica = f.Replica(); using var second = await replica.Client(); AssertIndependentGates(f, replica);
        var responses = await Task.WhenAll(first.PostAsJsonAsync($"/api/registrations/{reg.Id}/review", new { approve = true }), second.PostAsJsonAsync($"/api/registrations/{reg.Id}/review", new { approve = true }));
        AssertOneWinner(responses);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(RegistrationStatus.Approved, (await db.Registrations.FindAsync(reg.Id))!.Status);
        Assert.Equal(1, await db.Horses.CountAsync(x => x.RegistrationId == reg.Id));
        Assert.Equal(1, await db.Measurements.CountAsync());
        Assert.Equal(1, await db.Audit.CountAsync(x => x.ReferenceId == reg.Id && x.Action == AuditAction.RegistrationApproved));
        Assert.Equal(1, await db.Notifications.CountAsync(x => x.RecipientId == owner.Id && x.Type == NotificationType.RegistrationApproved));
    }

    [SqlServerFact]
    public async Task SeparateHosts_ConcurrentOccupancyCannotDoubleBookStall()
    {
        await using var f = new ClubFactory(); using var first = await f.Client(); var owner = await f.User(Role.HorseOwner);
        var horseA = await f.Horse(owner); var horseB = await f.Horse(owner);
        var stable = await ClubFactory.Post(first, "/api/care/stables", new NameRequest("Stable"));
        var stall = await ClubFactory.Post(first, "/api/care/stalls", new StallRequest(stable.GetProperty("id").GetGuid(), "A1")); var id = stall.GetProperty("id").GetGuid();
        await using var replica = f.Replica(); using var second = await replica.Client(); AssertIndependentGates(f, replica);
        AssertOneWinner(await Task.WhenAll(first.PostAsJsonAsync($"/api/care/stalls/{id}/occupancy", new OccupancyRequest(horseA.Id)), second.PostAsJsonAsync($"/api/care/stalls/{id}/occupancy", new OccupancyRequest(horseB.Id))));
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(1, await db.Occupancies.CountAsync(x => x.StallId == id && x.EndedAt == null));
        Assert.Equal(1, await db.Audit.CountAsync(x => x.ReferenceId == id && x.Action == AuditAction.StallOccupied));
    }

    [SqlServerFact]
    public async Task SeparateHosts_CompetingWithdrawalsDoNotLoseStockOrMovements()
    {
        await using var f = new ClubFactory(); using var first = await f.Client();
        var item = await ClubFactory.Post(first, "/api/inventory", new InventoryRequest("Feed", "Food", "kg", 5)); var id = item.GetProperty("id").GetGuid();
        await ClubFactory.Post(first, $"/api/inventory/{id}/movements", new MovementRequest(10, "Receipt"));
        await using var replica = f.Replica(); using var second = await replica.Client(); AssertIndependentGates(f, replica);
        AssertOneWinner(await Task.WhenAll(first.PostAsJsonAsync($"/api/inventory/{id}/movements", new MovementRequest(-6, "Use")), second.PostAsJsonAsync($"/api/inventory/{id}/movements", new MovementRequest(-6, "Use"))));
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(4m, (await db.Inventory.FindAsync(id))!.Stock);
        var movements = await db.StockMovements.Where(x => x.ItemId == id).ToListAsync(); Assert.Equal(2, movements.Count); Assert.Equal(4m, movements.Sum(x => x.Quantity));
        Assert.Equal(1, await db.Notifications.CountAsync(x => x.ReferenceId == id && x.Type == NotificationType.InventoryLowStock));
    }

    [SqlServerFact]
    public async Task SeparateHosts_ReassignmentLeavesOneActiveTrainerAndCompleteHistory()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer);
        var original = await f.User(Role.Trainer); var a = await f.User(Role.Trainer); var b = await f.User(Role.Trainer); var horse = await f.Horse(owner, head, original);
        using var first = await f.Client(head); await using var replica = f.Replica(); using var second = await replica.Client(head); AssertIndependentGates(f, replica);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var responses = await Task.WhenAll(first.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(a.Id, Role.Trainer, today, "A"), ClubFactory.Json), second.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(b.Id, Role.Trainer, today, "B"), ClubFactory.Json));
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.Conflict, r.StatusCode.ToString()));
        Assert.Contains(responses, r => r.IsSuccessStatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var assignments = await db.Assignments.Where(x => x.HorseId == horse.Id && x.Role == Role.Trainer).ToListAsync();
        Assert.Single(assignments, x => x.Active); Assert.Equal(1 + responses.Count(r => r.IsSuccessStatusCode), assignments.Count);
        Assert.All(assignments.Where(x => !x.Active), x => Assert.Equal(today, x.EndDate));
        Assert.DoesNotContain(assignments, x => x.Active && x.StaffId == original.Id);
        Assert.Equal(responses.Count(r => r.IsSuccessStatusCode), await db.Audit.CountAsync(x => x.ReferenceId == horse.Id && x.Action == AuditAction.HorseStaffAssigned));
    }

    [SqlServerFact]
    public async Task SeparateHosts_RestrictionRacingStartHasSerializableOutcome()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        await using var replica = f.Replica(); using var vet = await replica.Client(await FindVet(f, s.HorseId)); AssertIndependentGates(f, replica);
        var restrictionTask = vet.PostAsJsonAsync($"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction, ClubFactory.Json);
        var startTask = s.RiderClient.PostAsJsonAsync($"/api/training/sessions/{s.SessionId}/start", new { });
        await Task.WhenAll(restrictionTask, startTask);
        var restriction = await restrictionTask; var start = await startTask;
        Assert.True(restriction.IsSuccessStatusCode || restriction.StatusCode == HttpStatusCode.Conflict, await restriction.Content.ReadAsStringAsync());
        Assert.True(start.IsSuccessStatusCode || start.StatusCode == HttpStatusCode.Conflict, await start.Content.ReadAsStringAsync());
        // A conflict rolls the request back; the caller can explicitly submit a new medical decision.
        if (!restriction.IsSuccessStatusCode) await ClubFactory.Post(vet, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction);
        if (start.IsSuccessStatusCode)
            await ClubFactory.Post(s.RiderClient, $"/api/training/sessions/{s.SessionId}/results", new ResultRequest(1000, 100, null, Intensity.Heavy, "Stopped after restriction", false));
        else
            Assert.Equal(HttpStatusCode.Conflict, (await s.RiderClient.PostAsJsonAsync($"/api/training/sessions/{s.SessionId}/start", new { })).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(1, await db.Restrictions.CountAsync(x => x.HorseId == s.HorseId && !x.Cleared));
        Assert.Equal(start.IsSuccessStatusCode ? SessionStatus.IssueReported : SessionStatus.Assigned, (await db.Sessions.FindAsync(s.SessionId))!.Status);
        Assert.Equal(start.IsSuccessStatusCode ? 1 : 0, await db.Results.CountAsync(x => x.SessionId == s.SessionId));
        Assert.Equal(start.IsSuccessStatusCode ? 1 : 0, await db.Incidents.CountAsync(x => x.SessionId == s.SessionId));
        if (start.IsSuccessStatusCode) Assert.Equal(1, await db.Notifications.CountAsync(x => x.RecipientId == s.Rider.Id && x.Type == NotificationType.MedicalRestrictionCreated));
    }

    [SqlServerFact]
    public async Task ApprovalDatabaseFailure_RollsBackHorseMeasurementAuditAndNotification()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client(); var owner = await f.User(Role.HorseOwner); var reg = await PendingRegistration(f, owner);
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            // Test-owned database only: force a failure after an INSERT reaches SQL Server.
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER [TestRejectMeasurement] ON [Measurements] AFTER INSERT AS BEGIN THROW 51000, 'Injected test failure', 1; END;");
        }
        var response = await manager.PostAsJsonAsync($"/api/registrations/{reg.Id}/review", new { approve = true });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var verifyScope = f.Services.CreateScope(); var verify = verifyScope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var stored = (await verify.Registrations.FindAsync(reg.Id))!;
        Assert.Equal(RegistrationStatus.PendingReview, stored.Status); Assert.Null(stored.ReviewedBy);
        Assert.Equal(0, await verify.Horses.CountAsync()); Assert.Equal(0, await verify.Measurements.CountAsync());
        Assert.Equal(0, await verify.Audit.CountAsync()); Assert.Equal(0, await verify.Notifications.CountAsync());
        Assert.DoesNotContain("Injected test failure", await response.Content.ReadAsStringAsync());
    }

    [SqlServerFact]
    public async Task ForeignKeyFailure_DoesNotPersistOrphanTrainingSession()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client();
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        db.Sessions.Add(new TrainingSession { HorseId = Guid.NewGuid(), PlanId = Guid.NewGuid(), ScheduledAt = DateTimeOffset.UtcNow });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(547, Assert.IsType<SqlException>(error.InnerException).Number);
        db.ChangeTracker.Clear(); Assert.Equal(0, await db.Sessions.CountAsync());
    }

    private static void AssertOneWinner(HttpResponseMessage[] responses)
    {
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }
    private static void AssertIndependentGates(ClubFactory first, ClubFactory second)
    {
        Assert.NotSame(first.Services.GetRequiredService<WriteGate>(), second.Services.GetRequiredService<WriteGate>());
        using var firstScope = first.Services.CreateScope(); using var secondScope = second.Services.CreateScope();
        var firstDb = firstScope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var secondDb = secondScope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.True(firstDb.Database.IsSqlServer()); Assert.True(secondDb.Database.IsSqlServer());
        Assert.Equal(firstDb.Database.GetDbConnection().Database, secondDb.Database.GetDbConnection().Database);
    }
    private static async Task<User> FindVet(ClubFactory f, Guid horseId)
    {
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var id = await db.Assignments.Where(x => x.HorseId == horseId && x.Role == Role.Veterinarian && x.Active).Select(x => x.StaffId).SingleAsync();
        return (await db.Users.FindAsync(id))!;
    }
    private static async Task<HorseRegistration> PendingRegistration(ClubFactory f, User owner)
    {
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var r = new HorseRegistration { OwnerId = owner.Id, Name = "Thunder", Sire = "Sire", Dam = "Dam", DateOfBirth = new DateOnly(2020, 1, 1), Gender = HorseGender.Male, Breed = "Breed", DeclaredHealth = "Monitoring declared", HeightCm = 160, WeightKg = 500, MeasurementDate = today, BoardingStart = today, Status = RegistrationStatus.PendingReview };
        db.Registrations.Add(r);
        foreach (var type in new[] { AttachmentType.HorsePhoto, AttachmentType.Certificate })
            db.Attachments.Add(new Attachment { RegistrationId = r.Id, UploadedBy = owner.Id, Type = type, FileName = "test.png", StorageName = Guid.NewGuid().ToString("N"), ContentType = "image/png", Length = StorageRecoveryTests.Png.Length });
        await db.SaveChangesAsync(); return r;
    }
}
