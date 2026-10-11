using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class HorseLifecycleTests
{
    [Theory]
    [InlineData(Role.HeadTrainer)]
    [InlineData(Role.Trainer)]
    [InlineData(Role.Veterinarian)]
    [InlineData(Role.Groom)]
    public async Task FormerStaffOnlyReadTheirAssignmentHistoryAndCannotWriteCurrentHorse(Role role)
    {
        await using var f = new ClubFactory(); var staff = await f.User(role); var other = await f.User(role);
        var horse = await f.Horse(await f.User(Role.HorseOwner), staff, other);
        await WorkerTests.Read(f, async db => { var old = await db.Assignments.SingleAsync(x => x.HorseId == horse.Id && x.StaffId == staff.Id); old.Active = false; old.EndDate = DateOnly.FromDateTime(DateTime.UtcNow); await db.SaveChangesAsync(); });
        using var client = await f.Client(staff);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/horses/{horse.Id}")).StatusCode);
        var history = await client.GetFromJsonAsync<JsonElement>($"/api/horses/{horse.Id}/assignment-history");
        var assignment = Assert.Single(history.GetProperty("assignments").EnumerateArray());
        Assert.Equal(staff.Id, assignment.GetProperty("staffId").GetGuid());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/horses/{horse.Id}/measurements", new MeasurementRequest(DateOnly.FromDateTime(DateTime.UtcNow), 160, 500))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/horses/{horse.Id}/medical/records")).StatusCode);
    }
    [Fact]
    public async Task ArchiveClosesOperationalWorkPreservesClinicalStateAndReadOnlyHistory()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        using var manager = await f.Client(); var groom = await f.User(Role.Groom);
        await WorkerTests.Read(f, async db =>
        {
            db.CareTasks.Add(new CareTask { HorseId = s.HorseId, GroomId = groom.Id, ScheduledAt = DateTimeOffset.UtcNow, Status = CareStatus.Pending });
            db.Treatments.Add(new TreatmentPlan { HorseId = s.HorseId, MedicalRecordId = s.RecordId });
            db.Restrictions.Add(new MedicalRestriction { HorseId = s.HorseId, MedicalRecordId = s.RecordId, TrainingLock = true, ValidFrom = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        });
        await ClubFactory.Post(manager, $"/api/horses/{s.HorseId}/archive", new { });
        await WorkerTests.Read(f, async db =>
        {
            Assert.True((await db.Horses.FindAsync(s.HorseId))!.Archived);
            Assert.All(await db.Assignments.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => { Assert.False(x.Active); Assert.NotNull(x.EndDate); });
            Assert.Equal(PlanStatus.Archived, (await db.Plans.FindAsync(s.PlanId))!.Status);
            var session = (await db.Sessions.FindAsync(s.SessionId))!;
            Assert.Equal(SessionStatus.Skipped, session.Status); Assert.Contains("HorseArchived", session.Notes);
            var care = await db.CareTasks.SingleAsync(x => x.HorseId == s.HorseId);
            Assert.Equal(CareStatus.Skipped, care.Status); Assert.Contains("HorseArchived", care.Notes);
            Assert.False((await db.Treatments.SingleAsync(x => x.HorseId == s.HorseId)).Completed);
            Assert.False((await db.Restrictions.SingleAsync(x => x.HorseId == s.HorseId)).Cleared);
            Assert.Contains(await db.TrainingRevisions.Where(x => x.PlanId == s.PlanId).ToListAsync(), x => x.SessionId == s.SessionId);
        });
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"/api/horses/{s.HorseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.RiderClient.GetAsync($"/api/training/sessions/{s.SessionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.TrainerClient.GetAsync($"/api/training/plans/{s.PlanId}/history")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.TrainerClient.GetAsync($"/api/horses/{s.HorseId}/assignment-history")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.RiderClient.PostAsJsonAsync($"/api/training/sessions/{s.SessionId}/start", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await s.TrainerClient.PutAsJsonAsync($"/api/training/plans/{s.PlanId}/status", new PlanStatusRequest(PlanStatus.Active), ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task ArchiveDatabaseFailureRollsBackAllOperationalChanges()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f); using var manager = await f.Client();
        await WorkerTests.Read(f, async db => await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectHorseArchive ON Horses AFTER UPDATE AS IF EXISTS (SELECT 1 FROM inserted WHERE Archived = 1) THROW 51000, 'Injected archive failure', 1;"));
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync($"/api/horses/{s.HorseId}/archive", new { })).StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            Assert.False((await db.Horses.FindAsync(s.HorseId))!.Archived);
            Assert.Equal(PlanStatus.Active, (await db.Plans.FindAsync(s.PlanId))!.Status);
            Assert.Equal(SessionStatus.Assigned, (await db.Sessions.FindAsync(s.SessionId))!.Status);
            Assert.All(await db.Assignments.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => Assert.True(x.Active));
            Assert.False(await db.Audit.AnyAsync(x => x.ReferenceId == s.HorseId && x.Action == AuditAction.HorseArchived));
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ArchiveRejectsActiveSessionOrCareWithoutPartialChanges(bool training)
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f); using var manager = await f.Client();
        if (training) await ClubFactory.Post(s.RiderClient, $"/api/training/sessions/{s.SessionId}/start", new { });
        else await WorkerTests.Read(f, async db => { db.CareTasks.Add(new CareTask { HorseId = s.HorseId, GroomId = (await f.User(Role.Groom)).Id, Status = CareStatus.InProgress }); await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PostAsJsonAsync($"/api/horses/{s.HorseId}/archive", new { })).StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            Assert.False((await db.Horses.FindAsync(s.HorseId))!.Archived);
            Assert.Equal(PlanStatus.Active, (await db.Plans.FindAsync(s.PlanId))!.Status);
            Assert.All(await db.Assignments.Where(x => x.HorseId == s.HorseId).ToListAsync(), x => Assert.True(x.Active));
            Assert.False(await db.Audit.AnyAsync(x => x.ReferenceId == s.HorseId && x.Action == AuditAction.HorseArchived));
        });
    }

    [Fact]
    public async Task RiderHistoryDoesNotGrantCurrentHorseOrMedicalScope()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        await ClubFactory.Post(s.RiderClient, $"/api/training/sessions/{s.SessionId}/start", new { });
        await ClubFactory.Post(s.RiderClient, $"/api/training/sessions/{s.SessionId}/results", new ResultRequest(1000, 100, null, Intensity.Heavy, "Done", false));
        Assert.Equal(HttpStatusCode.OK, (await s.RiderClient.GetAsync($"/api/training/sessions/{s.SessionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await s.RiderClient.GetAsync($"/api/horses/{s.HorseId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await s.RiderClient.GetAsync($"/api/horses/{s.HorseId}/medical/summary")).StatusCode);
        var plan = await s.RiderClient.GetFromJsonAsync<JsonElement>($"/api/training/plans/{s.PlanId}");
        Assert.Empty(plan.GetProperty("restrictions").EnumerateArray());
        var list = await s.RiderClient.GetFromJsonAsync<JsonElement>("/api/training/sessions");
        Assert.Contains(list.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == s.SessionId);
    }
}
