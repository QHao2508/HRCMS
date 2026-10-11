using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class MedicalPolicyTests
{
    [Fact]
    public async Task AssessmentTieBreakMatchesSqlServerGuidOrdering()
    {
        await using var f = new ClubFactory(); var vet = await f.User(Role.Veterinarian); var horse = await f.Horse(await f.User(Role.HorseOwner), vet);
        var at = DateTimeOffset.UtcNow.AddMinutes(-1); var recordedAt = DateTimeOffset.UtcNow;
        await WorkerTests.Read(f, async db => { db.MedicalRecords.Add(new MedicalRecord { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = at, CreatedAt = recordedAt }); await db.SaveChangesAsync(); });
        using var scope = f.Services.CreateScope(); var repository = scope.ServiceProvider.GetRequiredService<IMedicalRepository>();
        var candidate = new MedicalRecord { Id = Guid.Parse("00000001-0000-0000-0000-000000000000"), HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = at, CreatedAt = recordedAt };
        Assert.False(await repository.IsLatestAssessmentAsync(candidate));
        candidate.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Assert.True(await repository.IsLatestAssessmentAsync(candidate));
    }
    private static MedicalRequest Exam(DateTimeOffset at, HealthStatus status) => new(at, "Assessment", "Private symptoms", "Private findings", "Private diagnosis", status, "Private clinical notes");

    [Fact]
    public async Task ClearanceOnlyClosesSelectedIssueAndLeavesOtherAndFutureIssuesOpen()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        var second = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/records", Exam(DateTimeOffset.UtcNow.AddMinutes(-10), HealthStatus.Monitoring));
        var secondId = second.GetProperty("id").GetGuid(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var a = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction);
        var b = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction with { MedicalRecordId = secondId });
        var future = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction with { MedicalRecordId = secondId, ValidFrom = DateTimeOffset.UtcNow.AddDays(1) });
        var injury = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/injuries", new InjuryRequest(s.RecordId, today, InjuryType.Muscle, "Leg A", InjurySeverity.Mild, "Training", today.AddDays(2)));
        var treatment = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/treatments", new TreatmentRequest(s.RecordId, null, today, today.AddDays(5), today.AddDays(2), "Rest", "Private instructions", "Private medication", "Daily"));
        var aId = a.GetProperty("id").GetGuid(); var injuryId = injury.GetProperty("id").GetGuid();
        await WorkerTests.Read(f, async db => { (await db.Injuries.FindAsync(injuryId))!.Status = InjuryStatus.Recovering; await db.SaveChangesAsync(); });
        var follow = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/follow-ups",
            new FollowUpRequest(s.RecordId, Exam(DateTimeOffset.UtcNow.AddMilliseconds(-50), HealthStatus.Monitoring), true, "Issue A cleared", [aId], [injuryId]));
        await WorkerTests.Read(f, async db =>
        {
            Assert.True((await db.Restrictions.FindAsync(aId))!.Cleared);
            Assert.False((await db.Restrictions.FindAsync(b.GetProperty("id").GetGuid()))!.Cleared);
            Assert.False((await db.Restrictions.FindAsync(future.GetProperty("id").GetGuid()))!.Cleared);
            Assert.Equal(InjuryStatus.Recovered, (await db.Injuries.FindAsync(injuryId))!.Status);
            Assert.False((await db.Treatments.FindAsync(treatment.GetProperty("id").GetGuid()))!.Completed);
            Assert.Equal(HealthStatus.Monitoring, (await db.Horses.FindAsync(s.HorseId))!.HealthStatus);
            Assert.Equal(PlanStatus.Active, (await db.Plans.FindAsync(s.PlanId))!.Status);
            var audit = await db.Audit.SingleAsync(x => x.ReferenceId == follow.GetProperty("id").GetGuid() && x.Action == AuditAction.MedicalClearanceIssued);
            var detail = JsonDocument.Parse(audit.Detail).RootElement;
            Assert.Equal(aId, detail.GetProperty("RestrictionIds")[0].GetGuid());
            Assert.Empty(detail.GetProperty("TreatmentIds").EnumerateArray());
        });
        Assert.Equal(HttpStatusCode.Conflict, (await s.RiderClient.PostAsJsonAsync($"/api/training/sessions/{s.SessionId}/start", new { })).StatusCode);
    }

    [Fact]
    public async Task MissingCrossHorseDuplicateAndFutureSelectionsFailWithoutCreatingFollowUp()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f); var other = await TrainingScenario.Create(f);
        var foreign = await ClubFactory.Post(other.VetClient, $"/api/horses/{other.HorseId}/medical/restrictions", other.Restriction);
        var future = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/restrictions", s.Restriction with { ValidFrom = DateTimeOffset.UtcNow.AddDays(1) });
        var request = new FollowUpRequest(s.RecordId, Exam(DateTimeOffset.UtcNow.AddSeconds(-1), HealthStatus.Fit), true, "Review");
        var foreignId = foreign.GetProperty("id").GetGuid();
        foreach (var invalid in new[] { request, request with { RestrictionIds = [foreignId] }, request with { RestrictionIds = [foreignId, foreignId] }, request with { RestrictionIds = [future.GetProperty("id").GetGuid()] } })
            Assert.Equal(HttpStatusCode.BadRequest, (await s.VetClient.PostAsJsonAsync($"/api/horses/{s.HorseId}/medical/follow-ups", invalid, ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => { Assert.Empty(await db.FollowUps.Where(x => x.HorseId == s.HorseId).ToListAsync()); Assert.False((await db.Restrictions.FindAsync(foreignId))!.Cleared); });
    }

    [Fact]
    public async Task BackdatedAssessmentFollowUpInjuryAndCorrectionDoNotOverwriteNewerIsolation()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        var pastAt = DateTimeOffset.UtcNow.AddDays(-7);
        var old = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/records", Exam(pastAt, HealthStatus.Fit));
        await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/records", Exam(DateTimeOffset.UtcNow.AddMilliseconds(-50), HealthStatus.Isolated));
        await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/records", Exam(pastAt.AddDays(1), HealthStatus.Fit));
        var oldId = old.GetProperty("id").GetGuid();
        await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/follow-ups", new FollowUpRequest(oldId, Exam(pastAt.AddDays(2), HealthStatus.Fit), false, "Historical review"));
        Assert.Equal(HttpStatusCode.OK, (await s.VetClient.PutAsJsonAsync($"/api/horses/{s.HorseId}/medical/records/{oldId}", Exam(pastAt, HealthStatus.Monitoring), ClubFactory.Json)).StatusCode);
        var injuryDate = DateOnly.FromDateTime(pastAt.UtcDateTime);
        await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/injuries", new InjuryRequest(oldId, injuryDate, InjuryType.Muscle, "Leg", InjurySeverity.Mild, "History", injuryDate.AddDays(2)));
        await WorkerTests.Read(f, async db => Assert.Equal(HealthStatus.Isolated, (await db.Horses.FindAsync(s.HorseId))!.HealthStatus));
    }

    [Fact]
    public async Task CorrectionKeepsOriginalDateAndLatestAssessmentCorrectionChangesCurrentState()
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        var at = DateTimeOffset.UtcNow.AddMilliseconds(-100);
        var record = await ClubFactory.Post(s.VetClient, $"/api/horses/{s.HorseId}/medical/records", Exam(at, HealthStatus.Isolated));
        var id = record.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await s.VetClient.PutAsJsonAsync($"/api/horses/{s.HorseId}/medical/records/{id}", Exam(at.AddDays(-1), HealthStatus.Fit), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await s.VetClient.PutAsJsonAsync($"/api/horses/{s.HorseId}/medical/records/{id}", Exam(at, HealthStatus.Monitoring), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(f, async db => Assert.Equal(HealthStatus.Monitoring, (await db.Horses.FindAsync(s.HorseId))!.HealthStatus));
    }

    [Theory]
    [InlineData(Role.ClubManager)]
    [InlineData(Role.HorseOwner)]
    [InlineData(Role.HeadTrainer)]
    [InlineData(Role.Trainer)]
    [InlineData(Role.Groom)]
    [InlineData(Role.WorkRider)]
    public async Task NonMedicalRolesCannotReadOrWriteClinicalRecords(Role role)
    {
        await using var f = new ClubFactory(); var s = await TrainingScenario.Create(f);
        using var client = role == Role.WorkRider ? await f.Client(s.Rider) : await f.Client(await f.User(role));
        foreach (var route in new[] { "records", "treatments", "follow-ups" })
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/horses/{s.HorseId}/medical/{route}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/horses/{s.HorseId}/medical/records", Exam(DateTimeOffset.UtcNow.AddSeconds(-1), HealthStatus.Fit), ClubFactory.Json)).StatusCode);
    }
}
