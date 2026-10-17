using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class MedicalCaseHistoryTests
{
    private static MedicalRequest Exam(DateTimeOffset at, string diagnosis) => new(at, "Leg injury", "Swelling", "Tender tendon", diagnosis, HealthStatus.Monitoring, "Clinical notes");

    [Fact]
    public async Task LegacyAuditSnapshotsRemainVisibleAfterMergeAndLaterCorrection()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, vet); using var client = await factory.Client(vet);
        var at = DateTimeOffset.UtcNow.AddHours(-4); var path = $"/api/horses/{horse.Id}/medical";
        Guid id, duplicate;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var record = new MedicalRecord { HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = at.AddHours(2), Reason = "Leg injury", Symptoms = "Final symptoms", Findings = "Final findings", Diagnosis = "Third", HealthStatus = HealthStatus.Monitoring };
            var other = new MedicalRecord { HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = at.AddHours(-1), Reason = "Leg injury", Symptoms = "Other", Findings = "Other", Diagnosis = "Duplicate", HealthStatus = HealthStatus.Monitoring };
            id = record.Id; duplicate = other.Id; db.MedicalRecords.AddRange(record, other);
            for (var i = 0; i < 2; i++)
            {
                var visit = new MedicalFollowUp { HorseId = horse.Id, PreviousRecordId = id, CurrentRecordId = id, CreatedAt = at.AddHours(i + 1), Outcome = "Legacy review " + i };
                var before = JsonSerializer.Serialize(new { ExaminationAt = at.AddHours(i), Symptoms = "Symptoms " + i, Findings = "Findings " + i, Diagnosis = i == 0 ? "First" : "Second", HealthStatus = "Monitoring", Notes = "Notes " + i });
                db.FollowUps.Add(visit); db.Audit.Add(new AuditEvent { ActorId = vet.Id, Action = AuditAction.MedicalFollowUp, ReferenceId = visit.Id, Detail = JsonSerializer.Serialize(new { Before = before }) });
            }
            await db.SaveChangesAsync();
        }
        await ClubFactory.Post(client, path + $"/records/{id}/merge", new MergeMedicalCaseRequest([duplicate], "Same episode"));
        var correctionResponse = await client.PutAsJsonAsync(path + $"/records/{id}", Exam(at.AddHours(2), "Corrected"), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.OK, correctionResponse.StatusCode);
        var corrected = (await correctionResponse.Content.ReadFromJsonAsync<MedicalRecord>(ClubFactory.Json))!;
        var history = await client.GetFromJsonAsync<MedicalCaseHistoryResponse>(path + $"/records/{corrected.Id}/history", ClubFactory.Json);
        Assert.Equal(new[] { "Duplicate", "First", "Second", "Third", "Corrected" }, history!.Visits.Select(x => x.Examination.Diagnosis));
        Assert.Equal("Symptoms 0", history.Visits[1].Examination.Symptoms);
        Assert.Equal("Notes 1", history.Visits[2].Examination.Notes);
        Assert.Equal(3, history.RecordIds.Length);
    }

    [Fact]
    public async Task FollowUpsKeepEveryAssessmentAndCorrectionInOneCase()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, vet); using var client = await factory.Client(vet);
        var at = DateTimeOffset.UtcNow.AddHours(-4); var path = $"/api/horses/{horse.Id}/medical";
        var initial = await ClubFactory.Post(client, path + "/records", Exam(at, "First assessment")); var id = initial.GetProperty("id").GetGuid();
        await ClubFactory.Post(client, path + "/follow-ups", new FollowUpRequest(id, Exam(at.AddHours(1), "Improving"), false, "Review one"));
        await ClubFactory.Post(client, path + "/follow-ups", new FollowUpRequest(id, Exam(at.AddHours(2), "Recovered") with { Notes = new string('N', 3900) }, true, "Resolved"));
        var history = await client.GetFromJsonAsync<MedicalCaseHistoryResponse>(path + $"/records/{id}/history", ClubFactory.Json);
        Assert.Equal(new[] { "First assessment", "Improving", "Recovered" }, history!.Visits.Select(x => x.Examination.Diagnosis));
        Assert.Equal(at, history.Visits[0].Examination.ExaminationAt);
        Assert.Equal(3900, history.Visits[2].Examination.Notes.Length); Assert.True(history.Visits[2].Clearance);
        await WorkerTests.Read(factory, async db => Assert.Single(await db.MedicalRecords.Where(x => x.HorseId == horse.Id).ToListAsync()));
        using var ownerClient = await factory.Client(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync(path + $"/records/{id}/history")).StatusCode);
    }

    [Fact]
    public async Task ExplicitMergePreservesRecordsLinksAndClosesAllLinkedStateOnClearance()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, vet); using var client = await factory.Client(vet);
        var at = DateTimeOffset.UtcNow.AddHours(-4); var path = $"/api/horses/{horse.Id}/medical";
        var a = (await ClubFactory.Post(client, path + "/records", Exam(at, "First"))).GetProperty("id").GetGuid();
        var b = (await ClubFactory.Post(client, path + "/records", Exam(at.AddHours(1), "Duplicate"))).GetProperty("id").GetGuid();
        Guid restrictionId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var restriction = new MedicalRestriction { HorseId = horse.Id, MedicalRecordId = a, ValidFrom = at, TrainingLock = true, Reason = "Rest" };
            restrictionId = restriction.Id; db.Restrictions.Add(restriction); await db.SaveChangesAsync();
        }
        var merged = await ClubFactory.Post(client, path + $"/records/{a}/merge", new MergeMedicalCaseRequest([b], "Duplicate entry for the same episode"));
        Assert.Equal(b, merged.GetProperty("id").GetGuid());
        var history = await client.GetFromJsonAsync<MedicalCaseHistoryResponse>(path + $"/records/{a}/history", ClubFactory.Json);
        Assert.Equal(2, history!.RecordIds.Length); Assert.Equal(2, history.Visits.Length);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path + "/follow-ups", new FollowUpRequest(a, Exam(at.AddHours(2), "Review"), false, "Review"), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(client, path + "/follow-ups", new FollowUpRequest(b, Exam(at.AddHours(2), "Resolved"), true, "Same case recovered"));
        await WorkerTests.Read(factory, async db => { Assert.Equal(2, await db.MedicalRecords.CountAsync(x => x.HorseId == horse.Id)); Assert.True((await db.Restrictions.FindAsync(restrictionId))!.Cleared); });
    }

    [Fact]
    public async Task MergeRejectsOtherHorsesNonVetsAndDifferentEpisodeStatesWithoutChangingData()
    {
        await using var factory = new ClubFactory(); var owner = await factory.User(Role.HorseOwner); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, vet); var other = await factory.Horse(owner, vet);
        using var client = await factory.Client(vet); using var manager = await factory.Client();
        var at = DateTimeOffset.UtcNow.AddHours(-4); var path = $"/api/horses/{horse.Id}/medical";
        var a = (await ClubFactory.Post(client, path + "/records", Exam(at, "Open"))).GetProperty("id").GetGuid();
        var b = (await ClubFactory.Post(client, path + "/records", Exam(at.AddHours(1), "Closed"))).GetProperty("id").GetGuid();
        var foreign = (await ClubFactory.Post(client, $"/api/horses/{other.Id}/medical/records", Exam(at, "Foreign"))).GetProperty("id").GetGuid();
        var url = path + $"/records/{a}/merge";
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync(url, new MergeMedicalCaseRequest([b], "Merge"), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new MergeMedicalCaseRequest([foreign], "Merge"), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(client, path + "/follow-ups", new FollowUpRequest(b, Exam(at.AddHours(2), "Resolved"), true, "Resolved"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new MergeMedicalCaseRequest([b], "Merge"), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(factory, async db => Assert.All(await db.MedicalRecords.Where(x => x.HorseId == horse.Id).ToListAsync(), record => Assert.Null(record.CaseRootId)));
    }
}
