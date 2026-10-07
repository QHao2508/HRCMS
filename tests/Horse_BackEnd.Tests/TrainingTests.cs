using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class TrainingTests
{
    [Fact]
    public async Task PlannedAssignmentExecutionEvaluationAndManualAdjustmentKeepHistory()
    {
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:SpeedDecimalPlaces"] = "2" });
        var x = await Setup(f); using var trainer = await f.Client(x.Trainer); using var rider = await f.Client(x.Rider);
        var session = await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(5)));
        var id = session.GetProperty("id").GetGuid(); Assert.Equal("Planned", session.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/start", new { })).StatusCode);
        await ClubFactory.Post(trainer, $"/api/training/sessions/{id}/assign", new RiderRequest(x.Rider.Id));
        var future = await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddHours(2)));
        var futureId = future.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/results", Result(), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(rider, $"/api/training/sessions/{id}/start", new { });
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/start", new { })).StatusCode);
        var result = await ClubFactory.Post(rider, $"/api/training/sessions/{id}/results", Result());
        Assert.Equal(11.11m, result.GetProperty("speedMetresPerSecond").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/results", Result(), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(trainer, $"/api/training/sessions/{id}/evaluation", new EvaluationRequest("Reduce future workload", true));
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PostAsJsonAsync($"/api/training/sessions/{id}/evaluation", new EvaluationRequest("Again", true), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Equal(1000m, (await db.Sessions.FindAsync(futureId))!.DistanceMetres));
        Assert.Equal(HttpStatusCode.OK, (await trainer.PutAsJsonAsync($"/api/training/sessions/{futureId}", Request(DateTimeOffset.UtcNow.AddHours(2)) with { DistanceMetres = 800 }, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PutAsJsonAsync($"/api/training/sessions/{id}", Request(DateTimeOffset.UtcNow.AddMinutes(5)), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            var revisions = await db.TrainingRevisions.Where(r => r.SessionId == id).ToListAsync();
            Assert.Contains(revisions, r => JsonDocument.Parse(r.Snapshot).RootElement.TryGetProperty("status", out var state) && state.GetString() == "InProgress" && r.ActorId == x.Rider.Id);
            Assert.Contains(revisions, r => JsonDocument.Parse(r.Snapshot).RootElement.TryGetProperty("result", out var value) && value.ValueKind == JsonValueKind.Object);
            Assert.Contains(revisions, r => JsonDocument.Parse(r.Snapshot).RootElement.TryGetProperty("evaluation", out var value) && value.ValueKind == JsonValueKind.Object && value.GetProperty("comment").GetString() == "Reduce future workload" && r.ActorId == x.Trainer.Id);
            Assert.Equal(800m, (await db.Sessions.FindAsync(futureId))!.DistanceMetres); Assert.Single(await db.Results.ToListAsync());
        });
    }

    [Theory]
    [InlineData(Role.HorseOwner)]
    [InlineData(Role.ClubManager)]
    [InlineData(Role.Trainer)]
    [InlineData(Role.WorkRider)]
    [InlineData(Role.Veterinarian)]
    public async Task OnlyHeadTrainerCanCreateTemplates(Role role)
    {
        await using var f = new ClubFactory(); using var client = await f.Client(await f.User(role));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/training/templates", Template(), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Empty(await db.Templates.ToListAsync()));
    }

    [Fact]
    public async Task PausedCompletedArchivedAndSkippedTransitionsAreGuarded()
    {
        await using var f = new ClubFactory(); var x = await Setup(f); using var trainer = await f.Client(x.Trainer); using var rider = await f.Client(x.Rider);
        var id = (await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(5), x.Rider.Id))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Completed), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Paused), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/start", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PostAsJsonAsync($"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(10)), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Active), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(rider, $"/api/training/sessions/{id}/start", new { });
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Archived), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(rider, $"/api/training/sessions/{id}/skip", new ReasonRequest("Stopped before result"));
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/skip", new ReasonRequest("Again"), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Completed), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PutAsJsonAsync($"/api/training/plans/{x.PlanId}/status", new PlanStatusRequest(PlanStatus.Active), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Contains(await db.TrainingRevisions.Where(r => r.SessionId == id).ToListAsync(), r => r.Snapshot.Contains("Stopped before result") && r.ActorId == x.Rider.Id));
    }

    [Fact]
    public async Task RiderOnlySeesPlansAndSessionsAssignedToThemAndOwnerCannotSeeOtherHorse()
    {
        await using var f = new ClubFactory(); var x = await Setup(f); using var trainer = await f.Client(x.Trainer); using var rider = await f.Client(x.Rider);
        var otherRider = await f.User(Role.WorkRider); var otherPlan = await ClubFactory.Post(trainer, "/api/training/plans", Plan(f, x.Horse.Id, x.TemplateId));
        var otherPlanId = otherPlan.GetProperty("id").GetGuid();
        await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(5), x.Rider.Id));
        var hidden = await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(10), otherRider.Id));
        await ClubFactory.Post(trainer, $"/api/training/plans/{otherPlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(20), otherRider.Id));
        var plans = await rider.GetFromJsonAsync<JsonElement>("/api/training/plans"); Assert.Equal(1, plans.GetProperty("total").GetInt32());
        var detail = await rider.GetFromJsonAsync<JsonElement>($"/api/training/plans/{x.PlanId}"); Assert.Equal(1, detail.GetProperty("sessionTotal").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await rider.GetAsync($"/api/training/plans/{otherPlanId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rider.GetAsync($"/api/training/sessions/{hidden.GetProperty("id").GetGuid()}")).StatusCode);
        using var stranger = await f.Client(await f.User(Role.HorseOwner)); Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/training/plans/{x.PlanId}")).StatusCode);
        using var owner = await f.Client(x.Owner); Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/training/plans/{x.PlanId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync($"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(30)), ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task PlanDetailPagesAllSessionsBeyondOneHundredAndRejectsInvalidPage()
    {
        await using var f = new ClubFactory(); var x = await Setup(f); using var owner = await f.Client(x.Owner);
        await WorkerTests.Read(f, async db =>
        {
            for (var index = 0; index < 105; index++) db.Sessions.Add(new TrainingSession { HorseId = x.Horse.Id, PlanId = x.PlanId, ScheduledAt = DateTimeOffset.UtcNow.AddMinutes(index), Status = SessionStatus.Planned });
            await db.SaveChangesAsync();
        });
        var first = await owner.GetFromJsonAsync<JsonElement>($"/api/training/plans/{x.PlanId}?sessionPage=1&sessionPageSize=100");
        var second = await owner.GetFromJsonAsync<JsonElement>($"/api/training/plans/{x.PlanId}?sessionPage=2&sessionPageSize=100");
        Assert.Equal(105, first.GetProperty("sessionTotal").GetInt32()); Assert.Equal(100, first.GetProperty("sessions").GetArrayLength()); Assert.Equal(5, second.GetProperty("sessions").GetArrayLength());
        Assert.Empty(first.GetProperty("sessions").EnumerateArray().Select(s => s.GetProperty("id").GetGuid()).Intersect(second.GetProperty("sessions").EnumerateArray().Select(s => s.GetProperty("id").GetGuid())));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync($"/api/training/plans/{x.PlanId}?sessionPage=0")).StatusCode);
    }

    [Fact]
    public async Task ArchivedTemplatePreservesExistingPlanButCannotCreateNewPlanAndFinalWithoutResultCannotEvaluate()
    {
        await using var f = new ClubFactory(); var x = await Setup(f); using var head = await f.Client(x.Head); using var trainer = await f.Client(x.Trainer);
        await ClubFactory.Post(head, $"/api/training/templates/{x.TemplateId}/archive", new { });
        Assert.Equal(HttpStatusCode.BadRequest, (await trainer.PostAsJsonAsync("/api/training/plans", Plan(f, x.Horse.Id, x.TemplateId), ClubFactory.Json)).StatusCode);
        var id = (await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(5)))).GetProperty("id").GetGuid();
        await WorkerTests.Read(f, async db => { (await db.Sessions.FindAsync(id))!.Status = SessionStatus.Completed; await db.SaveChangesAsync(); });
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PostAsJsonAsync($"/api/training/sessions/{id}/evaluation", new EvaluationRequest("Missing source result", false), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Empty(await db.Evaluations.ToListAsync()));
    }

    [Fact]
    public async Task StartRespectsConfiguredEarlyWindowAndResultRejectsZeroTime()
    {
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:StartEarlyMinutes"] = "0" }); var x = await Setup(f);
        using var trainer = await f.Client(x.Trainer); using var rider = await f.Client(x.Rider);
        var id = (await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", Request(DateTimeOffset.UtcNow.AddMinutes(30), x.Rider.Id))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/start", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/results", Result() with { TimeSeconds = 0 }, ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task ExistingMedicalLockBlocksCreateEditAssignAndStartWithoutTrainerOverride()
    {
        await using var f = new ClubFactory(); var x = await Setup(f); using var trainer = await f.Client(x.Trainer); using var rider = await f.Client(x.Rider);
        var at = DateTimeOffset.UtcNow.AddMinutes(5); var heavy = Request(at, x.Rider.Id) with { Intensity = Intensity.Heavy };
        var id = (await ClubFactory.Post(trainer, $"/api/training/plans/{x.PlanId}/sessions", heavy)).GetProperty("id").GetGuid();
        var vet = await f.User(Role.Veterinarian);
        await WorkerTests.Read(f, async db =>
        {
            var record = new MedicalRecord { HorseId = x.Horse.Id, VeterinarianId = vet.Id, ExaminationAt = DateTimeOffset.UtcNow.AddMinutes(-1), Diagnosis = "Private clinical diagnosis" };
            db.MedicalRecords.Add(record); db.Restrictions.Add(new MedicalRestriction { HorseId = x.Horse.Id, MedicalRecordId = record.Id, TrainingLock = true, ValidFrom = DateTimeOffset.UtcNow.AddMinutes(-1), Reason = "Avoid heavy training" });
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PostAsJsonAsync($"/api/training/plans/{x.PlanId}/sessions", heavy with { ScheduledAt = at.AddMinutes(1) }, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PutAsJsonAsync($"/api/training/sessions/{id}", heavy, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await trainer.PostAsJsonAsync($"/api/training/sessions/{id}/assign", new RiderRequest(x.Rider.Id), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await rider.PostAsJsonAsync($"/api/training/sessions/{id}/start", new { })).StatusCode);
        Assert.DoesNotContain("Private clinical diagnosis", await rider.GetStringAsync($"/api/training/plans/{x.PlanId}"));
        await WorkerTests.Read(f, async db => { Assert.Equal(SessionStatus.Assigned, (await db.Sessions.FindAsync(id))!.Status); Assert.Single(await db.Restrictions.ToListAsync()); });
        Assert.Equal(HttpStatusCode.OK, (await trainer.PutAsJsonAsync($"/api/training/sessions/{id}", heavy with { Intensity = Intensity.Light }, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(rider, $"/api/training/sessions/{id}/start", new { });
    }

    private sealed record Scenario(User Owner, User Head, User Trainer, User Rider, Horse Horse, Guid TemplateId, Guid PlanId);
    private static async Task<Scenario> Setup(ClubFactory f)
    {
        var owner = await f.User(Role.HorseOwner); var head = await f.User(Role.HeadTrainer); var trainer = await f.User(Role.Trainer); var rider = await f.User(Role.WorkRider);
        var horse = await f.Horse(owner, head, trainer); using var headClient = await f.Client(head); using var trainerClient = await f.Client(trainer);
        var templateId = (await ClubFactory.Post(headClient, "/api/training/templates", Template())).GetProperty("id").GetGuid();
        var planId = (await ClubFactory.Post(trainerClient, "/api/training/plans", Plan(f, horse.Id, templateId))).GetProperty("id").GetGuid();
        return new Scenario(owner, head, trainer, rider, horse, templateId, planId);
    }
    private static TemplateRequest Template() => new("Endurance", "Build stamina", "Base", 1000, Intensity.Light, "Sand", 3, "Standard reference");
    private static PlanRequest Plan(ClubFactory f, Guid horseId, Guid templateId)
    {
        using var scope = f.Services.CreateScope(); var today = scope.ServiceProvider.GetRequiredService<ClubCalendar>().Today(scope.ServiceProvider.GetRequiredService<TimeProvider>());
        return new PlanRequest(horseId, templateId, "Individual goal", "Base", today, today.AddDays(7), "Individual plan");
    }
    private static SessionRequest Request(DateTimeOffset at, Guid? riderId = null) => new(at, TrainingType.Trot, 1000, Intensity.Light, "Sand", "Steady effort", "", riderId);
    private static ResultRequest Result() => new(1000, 90, 120, Intensity.Light, "Completed steadily", false);
}
