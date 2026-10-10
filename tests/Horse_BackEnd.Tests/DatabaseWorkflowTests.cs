using System.Net;
using System.Net.Http.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class DatabaseWorkflowTests
{
    [Fact]
    public async Task CompetingStockWithdrawals_KeepStockMovementsAndNotificationsConsistent()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client();
        var item = await ClubFactory.Post(manager, "/api/inventory", new InventoryRequest("Feed", "Food", "kg", 5));
        var id = item.GetProperty("id").GetGuid();
        await ClubFactory.Post(manager, $"/api/inventory/{id}/movements", new MovementRequest(10, "Receipt"));
        var responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => manager.PostAsJsonAsync($"/api/inventory/{id}/movements", new MovementRequest(-6, "Use"))));
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(4m, (await db.Inventory.FindAsync(id))!.Stock);
        var movements = await db.StockMovements.Where(x => x.ItemId == id).ToListAsync();
        Assert.Equal(2, movements.Count); Assert.Equal(4m, movements.Sum(x => x.Quantity));
        Assert.Equal(2, await db.Audit.CountAsync(x => x.ReferenceId == id && x.Action == AuditAction.InventoryStockMoved));
        Assert.Equal(1, await db.Notifications.CountAsync(x => x.ReferenceId == id && x.Type == NotificationType.InventoryLowStock));
    }

    [Fact]
    public async Task ReplenishmentApproval_DoesNotReceiveStock_AndCannotBeReviewedTwice()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client();
        var groom = await f.User(Role.Groom); using var gc = await f.Client(groom);
        var item = await ClubFactory.Post(manager, "/api/inventory", new InventoryRequest("Feed", "Food", "kg", 5)); var id = item.GetProperty("id").GetGuid();
        var request = await ClubFactory.Post(gc, "/api/inventory/replenishments", new ReplenishmentRequestDto(id, 10, "Need feed"));
        var requestId = request.GetProperty("id").GetGuid();
        await ClubFactory.Post(manager, $"/api/inventory/replenishments/{requestId}/review", new ReplenishmentReviewRequest(true, "Approved"));
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync($"/api/inventory/replenishments/{requestId}/review", new ReplenishmentReviewRequest(false, "Again"))).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(0m, (await db.Inventory.FindAsync(id))!.Stock);
        Assert.False(await db.StockMovements.AnyAsync(x => x.ItemId == id));
        await ClubFactory.Post(manager, $"/api/inventory/{id}/movements", new MovementRequest(10, "Actually received"));
        db.ChangeTracker.Clear(); Assert.Equal(10m, (await db.Inventory.FindAsync(id))!.Stock);
        Assert.Equal(1, await db.Notifications.CountAsync(x => x.RecipientId == groom.Id && x.ReferenceId == requestId && x.Type == NotificationType.InventoryReplenishmentReviewed));
    }

    [Fact]
    public async Task Reassignment_PreservesHistoryAndRevokesPreviousTrainerWriteScope()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer);
        var trainer = await f.User(Role.Trainer); var replacement = await f.User(Role.Trainer); var horse = await f.Horse(owner, head, trainer);
        await WorkerTests.Read(f, async db => { var user = (await db.Users.FindAsync(replacement.Id))!; user.FirstName = "Replacement"; user.LastName = "Trainer"; await db.SaveChangesAsync(); });
        using var hc = await f.Client(head); using var oldClient = await f.Client(trainer); using var newClient = await f.Client(replacement);
        var template = await ClubFactory.Post(hc, "/api/training/templates", new TemplateRequest("Base", "Goal", "Phase", 400, Intensity.Light, "Sand", 2, ""));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = await ClubFactory.Post(oldClient, "/api/training/plans", new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Phase", today, today.AddDays(2), "")); var planId = plan.GetProperty("id").GetGuid();
        await ClubFactory.Post(hc, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(replacement.Id, Role.Trainer, today, "Replacement"));
        Assert.Equal(HttpStatusCode.Forbidden, (await oldClient.PutAsJsonAsync($"/api/training/plans/{planId}/status", new PlanStatusRequest(PlanStatus.Paused), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await newClient.PutAsJsonAsync($"/api/training/plans/{planId}/status", new PlanStatusRequest(PlanStatus.Paused), ClubFactory.Json)).StatusCode);
        var detail = await newClient.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/training/plans/{planId}");
        Assert.Equal("Replacement Trainer", detail.GetProperty("trainerName").GetString());
        var searched = await newClient.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/training/plans?search=Replacement");
        Assert.Equal(1, searched.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await oldClient.GetAsync($"/api/training/plans/{planId}/history")).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var assignments = await db.Assignments.Where(x => x.HorseId == horse.Id && x.Role == Role.Trainer).ToListAsync();
        Assert.Equal(2, assignments.Count); Assert.Single(assignments, x => x.Active && x.StaffId == replacement.Id);
        Assert.Single(assignments, x => !x.Active && x.StaffId == trainer.Id && x.EndDate == today);
        Assert.Equal(trainer.Id, (await db.Plans.FindAsync(planId))!.TrainerId);
    }

    [Fact]
    public async Task LockDuringExecution_PreservesResultAndCreatesOneIncident()
    {
        await using var f = new ClubFactory(); var setup = await TrainingScenario.Create(f);
        await ClubFactory.Post(setup.RiderClient, $"/api/training/sessions/{setup.SessionId}/start", new { });
        await ClubFactory.Post(setup.VetClient, $"/api/horses/{setup.HorseId}/medical/restrictions", setup.Restriction);
        await ClubFactory.Post(setup.RiderClient, $"/api/training/sessions/{setup.SessionId}/results", new ResultRequest(1000, 100, null, Intensity.Heavy, "Stopped after restriction", false));
        Assert.Equal(HttpStatusCode.Conflict, (await setup.RiderClient.PostAsJsonAsync($"/api/training/sessions/{setup.SessionId}/results", new ResultRequest(1000, 100, null, Intensity.Heavy, "Duplicate", false), ClubFactory.Json)).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(SessionStatus.IssueReported, (await db.Sessions.FindAsync(setup.SessionId))!.Status);
        var result = Assert.Single(await db.Results.Where(x => x.SessionId == setup.SessionId).ToListAsync()); Assert.True(result.AbnormalObservation); Assert.Equal(10m, result.SpeedMetresPerSecond);
        var incident = Assert.Single(await db.Incidents.Where(x => x.SessionId == setup.SessionId).ToListAsync()); Assert.Equal(Role.Veterinarian, incident.RoutedTo);
        Assert.Equal(1, await db.Notifications.CountAsync(x => x.RecipientId == setup.Rider.Id && x.Type == NotificationType.MedicalRestrictionCreated));
    }

    [Fact]
    public async Task Clearance_ClosesOpenMedicalStateWithoutResumingPausedPlan()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var restriction = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction);
        var injury = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/injuries", new InjuryRequest(s.RecordId, today, InjuryType.Muscle, "Leg", InjurySeverity.Mild, "Training", today.AddDays(2)));
        var treatment = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/treatments", new TreatmentRequest(s.RecordId, null, today, today.AddDays(5), today.AddDays(2), "Recover", "Rest", "", "Daily"));
        Assert.Equal(HttpStatusCode.OK, (await s.TrainerClient.PutAsJsonAsync($"/api/training/plans/{s.PlanId}/status", new PlanStatusRequest(PlanStatus.Paused), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/follow-ups", new FollowUpRequest(s.RecordId, new MedicalRequest(DateTimeOffset.UtcNow.AddSeconds(-1), "Review", "Normal", "Recovered", "Fit", HealthStatus.Fit, ""), true, "Recovered",
            [restriction.GetProperty("id").GetGuid()], [injury.GetProperty("id").GetGuid()], [treatment.GetProperty("id").GetGuid()]));
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(HealthStatus.Fit, (await db.Horses.FindAsync(s.HorseId))!.HealthStatus);
        Assert.All(await db.Restrictions.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => Assert.True(x.Cleared));
        Assert.All(await db.Injuries.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => Assert.Equal(InjuryStatus.Recovered, x.Status));
        Assert.All(await db.Treatments.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => Assert.True(x.Completed));
        Assert.Equal(PlanStatus.Paused, (await db.Plans.FindAsync(s.PlanId))!.Status);
        Assert.Equal(2, await db.MedicalRecords.CountAsync(x => x.HorseId == s.HorseId));
        Assert.Equal(1, await db.FollowUps.CountAsync(x => x.HorseId == s.HorseId));
    }

    [Fact]
    public async Task StaleVersionCannotOverwriteCommittedStock()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client();
        var item = await ClubFactory.Post(manager, "/api/inventory", new InventoryRequest("Feed", "Food", "kg", 5)); var id = item.GetProperty("id").GetGuid();
        using var firstScope = f.Services.CreateScope(); using var staleScope = f.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<ClubDbContext>(); var stale = staleScope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var freshItem = (await first.Inventory.FindAsync(id))!; var staleItem = (await stale.Inventory.FindAsync(id))!;
        freshItem.Stock = 10; await first.SaveChangesAsync(); staleItem.Stock = 20;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        stale.ChangeTracker.Clear(); Assert.Equal(10m, (await stale.Inventory.FindAsync(id))!.Stock);
    }
}

internal sealed record TrainingScenario(Guid HorseId, Guid PlanId, Guid SessionId, Guid RecordId, User Rider,
    HttpClient TrainerClient, HttpClient RiderClient, HttpClient VetClient)
{
    public RestrictionRequest Restriction => new(RecordId, false, true, null, null, false, DateTimeOffset.UtcNow.AddMinutes(-1), null, "Rest only");
    public static async Task<TrainingScenario> Create(ClubFactory f)
    {
        var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer); var trainer = await f.User(Role.Trainer);
        var vet = await f.User(Role.Veterinarian); var rider = await f.User(Role.WorkRider); var horse = await f.Horse(owner, head, trainer, vet);
        var tc = await f.Client(trainer); var rc = await f.Client(rider); var vc = await f.Client(vet); using var hc = await f.Client(head);
        var template = await ClubFactory.Post(hc, "/api/training/templates", new TemplateRequest("Base", "Goal", "Phase", 1000, Intensity.Heavy, "Sand", 2, "")); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = await ClubFactory.Post(tc, "/api/training/plans", new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Phase", today, today.AddDays(2), "")); var planId = plan.GetProperty("id").GetGuid();
        var session = await ClubFactory.Post(tc, $"/api/training/plans/{planId}/sessions", new SessionRequest(DateTimeOffset.UtcNow.AddMinutes(5), TrainingType.Sprint, 1000, Intensity.Heavy, "Sand", "Goal", "", rider.Id));
        var record = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(DateTimeOffset.UtcNow.AddSeconds(-1), "Check", "Normal", "Normal", "Private diagnosis", HealthStatus.Monitoring, "Private note"));
        return new(horse.Id, planId, session.GetProperty("id").GetGuid(), record.GetProperty("id").GetGuid(), rider, tc, rc, vc);
    }
}
