using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class Flow34CompletionTests
{
    [Fact]
    public async Task MovingAndVacatingStallsRequireCleaningByTheAssignedGroom()
    {
        await using var f = new ClubFactory(); using var manager = await f.Client();
        var groom = await f.User(Role.Groom); var other = await f.User(Role.Groom);
        var horse = await f.Horse(await f.User(Role.HorseOwner), groom);
        using var gc = await f.Client(groom); using var outsider = await f.Client(other);
        var stable = (await ClubFactory.Post(manager, "/api/care/stables", new NameRequest("Flow 4"))).GetProperty("id").GetGuid();
        async Task<Guid> Stall(string name) => (await ClubFactory.Post(manager, "/api/care/stalls", new StallRequest(stable, name))).GetProperty("id").GetGuid();
        var first = await Stall("First"); var second = await Stall("Second");
        await ClubFactory.Post(manager, $"/api/care/stalls/{first}/occupancy", new OccupancyRequest(horse.Id));
        await ClubFactory.Post(manager, $"/api/care/stalls/{second}/occupancy", new OccupancyRequest(horse.Id));
        var stalls = await gc.GetFromJsonAsync<JsonElement>($"/api/care/stalls?stableId={stable}");
        var vacated = stalls.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == first);
        Assert.False(vacated.GetProperty("occupied").GetBoolean()); Assert.True(vacated.GetProperty("canClean").GetBoolean());
        Assert.Equal("NeedsCleaning", vacated.GetProperty("cleaningStatus").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PostAsJsonAsync($"/api/care/stalls/{first}/cleaned", new {})).StatusCode);
        await ClubFactory.Post(gc, $"/api/care/stalls/{first}/cleaned", new {});
        await ClubFactory.Post(manager, $"/api/care/stalls/{second}/vacate", new {});
        await ClubFactory.Post(gc, $"/api/care/stalls/{second}/cleaned", new {});
        var history = await gc.GetFromJsonAsync<JsonElement>($"/api/care/stalls/{first}/history");
        Assert.Equal(horse.Name, history.GetProperty("items")[0].GetProperty("horseName").GetString());
        Assert.NotEqual(JsonValueKind.Null, history.GetProperty("items")[0].GetProperty("endedAt").ValueKind);
        var hidden = await outsider.GetFromJsonAsync<JsonElement>($"/api/care/stalls/{first}/history");
        Assert.Equal(0, hidden.GetProperty("total").GetInt32());
        await WorkerTests.Read(f, async db => {
            Assert.Equal(CleaningStatus.Clean, (await db.Stalls.FindAsync(first))!.CleaningStatus);
            Assert.Equal(CleaningStatus.Clean, (await db.Stalls.FindAsync(second))!.CleaningStatus);
        });
    }

    [Fact]
    public async Task DashboardUsesTheSameOverdueBoundaryAndPlanStateAsReminders()
    {
        var clock = new WorkerClock(); await using var f = new ClubFactory(clock: clock);
        await WorkerTests.SeedReminders(f, clock); using var manager = await f.Client();
        var dashboard = await manager.GetFromJsonAsync<JsonElement>("/api/dashboard");
        Assert.Equal(1, dashboard.GetProperty("overdueSessions").GetInt32());
    }

    [Fact]
    public async Task ClinicalReportReturnsMoreThan100RowsAndRejectsConfiguredOverflow()
    {
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:MaxReportRecords"] = "110" });
        var vet = await f.User(Role.Veterinarian); var owner = await f.User(Role.HorseOwner); var horse = await f.Horse(owner, vet);
        using var vc = await f.Client(vet); using var oc = await f.Client(owner);
        await WorkerTests.Read(f, async db => {
            db.MedicalRecords.AddRange(Enumerable.Range(0, 101).Select(i => new MedicalRecord {
                HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = DateTimeOffset.UtcNow.AddMinutes(-i - 1), Diagnosis = "Clinical private"
            })); await db.SaveChangesAsync();
        });
        var report = await vc.GetFromJsonAsync<JsonElement>($"/api/reports?horseId={horse.Id}");
        Assert.Equal(101, report.GetProperty("clinical").GetArrayLength());
        Assert.DoesNotContain("Clinical private", await oc.GetStringAsync($"/api/reports?horseId={horse.Id}"));
        await WorkerTests.Read(f, async db => {
            db.MedicalRecords.AddRange(Enumerable.Range(0, 10).Select(_ => new MedicalRecord { HorseId = horse.Id, VeterinarianId = vet.Id, ExaminationAt = DateTimeOffset.UtcNow.AddMinutes(-1) }));
            await db.SaveChangesAsync();
        });
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await vc.GetAsync($"/api/reports?horseId={horse.Id}")).StatusCode);
    }

    [Fact]
    public async Task TreatmentCareIssueResolutionAndClearanceKeepClinicalInformationPrivate()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner);
        var vet = await f.User(Role.Veterinarian); var groom = await f.User(Role.Groom); var horse = await f.Horse(owner, vet, groom);
        using var vc = await f.Client(vet); using var gc = await f.Client(groom); using var oc = await f.Client(owner); using var mc = await f.Client();
        var now = DateTimeOffset.UtcNow; var today = DateOnly.FromDateTime(now.UtcDateTime);
        var exam = (await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(now.AddMinutes(-2), "Check", "Symptoms", "Findings", "Private diagnosis", HealthStatus.Monitoring, "Clinical notes"))).GetProperty("id").GetGuid();
        var treatment = (await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/treatments", new TreatmentRequest(exam, null, today, today.AddDays(3), today.AddDays(1), "Recover", "Private care instructions", "Private medication", "Daily"))).GetProperty("id").GetGuid();
        var task = (await ClubFactory.Post(vc, "/api/care/tasks", new CareRequest(horse.Id, groom.Id, treatment, CareType.Treatment, now, "Private care instructions", null))).GetProperty("id").GetGuid();
        Assert.DoesNotContain("Private care instructions", await oc.GetStringAsync($"/api/care/tasks?horseId={horse.Id}"));
        Assert.DoesNotContain("Private care instructions", await mc.GetStringAsync($"/api/care/tasks?horseId={horse.Id}"));
        Assert.Contains("Private care instructions", await gc.GetStringAsync($"/api/care/tasks?horseId={horse.Id}"));
        await ClubFactory.Post(gc, $"/api/care/tasks/{task}/record", new CareCompletionRequest(CareStatus.InProgress, null, "Started"));
        await ClubFactory.Post(gc, $"/api/care/tasks/{task}/record", new CareCompletionRequest(CareStatus.IssueReported, null, "Observed swelling"));
        var incidents = await vc.GetFromJsonAsync<JsonElement>($"/api/care/incidents?horseId={horse.Id}");
        var incident = incidents.GetProperty("items")[0].GetProperty("id").GetGuid();
        await ClubFactory.Post(vc, $"/api/care/incidents/{incident}/resolve", new IncidentResolutionRequest("Reviewed and adjusted care"));
        await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/follow-ups", new FollowUpRequest(exam, new MedicalRequest(DateTimeOffset.UtcNow.AddMilliseconds(-10), "Follow up", "Normal", "Recovered", "Fit", HealthStatus.Fit, "Reviewed"), true, "Treatment complete", TreatmentIds: [treatment]));
        await WorkerTests.Read(f, async db => {
            Assert.True((await db.Treatments.FindAsync(treatment))!.Completed);
            Assert.True((await db.Incidents.FindAsync(incident))!.Resolved);
            Assert.Equal(HealthStatus.Fit, (await db.Horses.FindAsync(horse.Id))!.HealthStatus);
        });
    }
}
