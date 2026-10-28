using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using HorseClub.DAL.Entities;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class MedicalCareUsabilityTests
{
    [Fact]
    public async Task TreatmentNeedsOnlyStartAndFollowUpAndAllScopedRolesReadCareInstructionsAndNotes()
    {
        await using var f = new ClubFactory();
        var users = new Dictionary<Role, HorseClub.DAL.Entities.User>();
        foreach (var role in Enum.GetValues<Role>()) users[role] = await f.User(role);
        var horse = await f.Horse(users[Role.HorseOwner], users.Values.Where(x => x.Role is not (Role.HorseOwner or Role.ClubManager)).ToArray());
        await WorkerTests.Read(f, async db =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var template = new TrainingTemplate { Name = "Care visibility" };
            var plan = new TrainingPlan { HorseId = horse.Id, TrainerId = users[Role.Trainer].Id, TemplateId = template.Id, StartDate = today, EndDate = today.AddDays(1) };
            db.Templates.Add(template); db.Plans.Add(plan);
            db.Sessions.Add(new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, RiderId = users[Role.WorkRider].Id,
                ScheduledAt = DateTimeOffset.UtcNow, Status = SessionStatus.Assigned });
            await db.SaveChangesAsync();
        });
        using var vet = await f.Client(users[Role.Veterinarian]);
        using var groom = await f.Client(users[Role.Groom]);
        var record = await ClubFactory.Post(vet, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(DateTimeOffset.UtcNow.AddMinutes(-1), "Check", "Signs", "Findings", "Diagnosis", HealthStatus.Monitoring, "Internal exam notes"));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new { medicalRecordId = record.GetProperty("id").GetGuid(), startDate = today,
            followUpDate = today.AddDays(2), objective = "Recover", instructions = "Apply cold pack", medication = "", frequency = "1" };
        var treatment = await ClubFactory.Post(vet, $"/api/horses/{horse.Id}/medical/treatments", request);
        Assert.Equal(JsonValueKind.Null, treatment.GetProperty("endDate").ValueKind);
        Assert.Equal("1 lần/ngày", treatment.GetProperty("frequency").GetString());
        var invalid = await vet.PostAsJsonAsync($"/api/horses/{horse.Id}/medical/treatments", request with { followUpDate = today.AddDays(-1) });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        foreach (var type in new[] { CareType.Treatment, CareType.IceBath })
        {
            var task = await ClubFactory.Post(vet, "/api/care/tasks", new CareRequest(horse.Id, users[Role.Groom].Id,
                treatment.GetProperty("id").GetGuid(), type, DateTimeOffset.UtcNow, "Apply cold pack · 1 lần/ngày", null));
            await ClubFactory.Post(groom, $"/api/care/tasks/{task.GetProperty("id").GetGuid()}/record", new CareCompletionRequest(CareStatus.Completed, null, "Horse tolerated care well"));
        }
        foreach (var user in users.Values)
        {
            using var client = await f.Client(user);
            var response = await client.GetFromJsonAsync<JsonElement>($"/api/care/tasks?horseId={horse.Id}");
            var tasks = response.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(2, tasks.Count);
            Assert.All(tasks, task => {
                Assert.Equal("Apply cold pack · 1 lần/ngày", task.GetProperty("instructions").GetString());
                Assert.Equal("Horse tolerated care well", task.GetProperty("notes").GetString());
            });
        }
        using var outsider = await f.Client(await f.User(Role.WorkRider));
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/care/tasks?horseId={horse.Id}")).StatusCode);
        var scoped = await outsider.GetFromJsonAsync<JsonElement>("/api/care/tasks");
        Assert.Empty(scoped.GetProperty("items").EnumerateArray());
    }
}
