using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Training;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class TrainingSchedulingTests
{
    private static readonly DateOnly Day = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
    private static DateTimeOffset At(int hour) => new(Day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);
    private static StaffTrainingProfileRequest Profile(int level = 2, params TimeOffInput[] timeOff) => new(
        [new(TrainingType.Trot, level)], [new(Day.DayOfWeek, 360, 1080)], timeOff.ToList());
    private static SessionRequest Session(int hour, Guid? riderId = null) => new(At(hour), TrainingType.Trot, 1000, Intensity.Light, "Sand", "Endurance", "", riderId, 60);
    private static string AvailabilityUrl(Guid horseId, Guid? riderId = null, Guid? exceptSessionId = null) =>
        $"/api/training/availability?horseId={horseId}&date={Day:yyyy-MM-dd}&durationMinutes=60&trainingType=Trot&intensity=Light&distanceMetres=1000"
        + (riderId.HasValue ? $"&riderId={riderId}" : "") + (exceptSessionId.HasValue ? $"&exceptSessionId={exceptSessionId}" : "");

    private static async Task<Guid> Plan(ClubFactory factory, Horse horse, User trainer)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var template = new TrainingTemplate { Name = "Schedule test" };
        var plan = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id, Goal = "Endurance", Phase = "Preparation", StartDate = Day, EndDate = Day.AddDays(5) };
        db.Templates.Add(template); db.Plans.Add(plan); await db.SaveChangesAsync(); return plan.Id;
    }

    [Fact]
    public void IntervalSubtractionMergesOverlappingBlocksAndKeepsAdjacentBookingsLegal()
    {
        var free = AvailabilityRanges.Subtract([new(At(6), At(18))], [new(At(8), At(10), "session"), new(At(9), At(11), "session"), new(At(14), At(15), "time_off")], 60);
        Assert.Equal(new[] { new TimeRange(At(6), At(8)), new TimeRange(At(11), At(14)), new TimeRange(At(15), At(18)) }, free);
        Assert.Equal(new[] { new TimeRange(At(7), At(8)), new TimeRange(At(11), At(12)) }, AvailabilityRanges.Intersect(free, [new(At(7), At(12))], 60));
        Assert.Empty(AvailabilityRanges.Subtract([new(At(6), At(18))], [new(At(6), At(18), "duration_unknown")], 60));
        Assert.Equal(new[] { new TimeRange(At(10).AddMinutes(1), At(11)) }, AvailabilityRanges.ForMinuteInput([new(At(10).AddTicks(1), At(11))], 30));
        Assert.Empty(AvailabilityRanges.ForMinuteInput([new(At(10).AddTicks(1), At(11))], 60));
    }

    [Fact]
    public async Task ManagerProfilesValidateShiftsAndCandidatesRequireScopedHeadTrainerAndSpecialty()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var head = await factory.User(Role.HeadTrainer); var trainer = await factory.User(Role.Trainer); var other = await factory.User(Role.Trainer);
        var owner = await factory.User(Role.HorseOwner); var horse = await factory.Horse(owner, head);
        using var headClient = await factory.Client(head); using var trainerClient = await factory.Client(trainer);
        var profilePath = $"/api/staff/{trainer.Id}/training-profile";
        Assert.Equal(HttpStatusCode.Forbidden, (await headClient.PutAsJsonAsync(profilePath, Profile(), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync(profilePath, Profile(), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync(profilePath, Profile(3), ClubFactory.Json)).StatusCode);
        var overlap = Profile() with { WorkingHours = [new(Day.DayOfWeek, 360, 800), new(Day.DayOfWeek, 700, 1080)] };
        Assert.Equal(HttpStatusCode.BadRequest, (await manager.PutAsJsonAsync(profilePath, overlap, ClubFactory.Json)).StatusCode);
        var candidatesPath = $"/api/training/trainer-candidates?horseId={horse.Id}&trainingType=Trot";
        var candidates = await headClient.GetFromJsonAsync<List<TrainerCandidateResponse>>(candidatesPath, ClubFactory.Json);
        Assert.Equal(trainer.Id, candidates![0].Id); Assert.True(candidates[0].Matches); Assert.Equal(3, candidates[0].MatchLevel);
        Assert.False(candidates.Single(x => x.Id == other.Id).Matches);
        Assert.Equal(HttpStatusCode.Forbidden, (await trainerClient.GetAsync(candidatesPath)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await headClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(other.Id, Role.Trainer, DateOnly.FromDateTime(DateTime.UtcNow), "", RequiredTrainingType: TrainingType.Trot), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(headClient, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, DateOnly.FromDateTime(DateTime.UtcNow), "", RequiredTrainingType: TrainingType.Trot));
        Assert.Equal(HttpStatusCode.Conflict, (await headClient.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(other.Id, Role.Trainer, DateOnly.FromDateTime(DateTime.UtcNow), ""), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync(profilePath, Profile() with { Specialties = [] }, ClubFactory.Json)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Equal(TrainingType.Trot, (await db.Assignments.SingleAsync(x => x.HorseId == horse.Id && x.Role == Role.Trainer && x.Active)).RequiredTrainingType);
        Assert.Single(await db.TrainerExpertise.Where(x => x.StaffId == trainer.Id).ToListAsync());
    }

    [Fact]
    public async Task AvailabilityCombinesHorseTrainerRiderAndTimeOffWithoutLeakingOtherHorseDetails()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var trainer = await factory.User(Role.Trainer); var rider = await factory.User(Role.WorkRider); var owner = await factory.User(Role.HorseOwner);
        var horse = await factory.Horse(owner, trainer); var otherHorse = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var plan = await Plan(factory, horse, trainer); var otherPlan = await Plan(factory, otherHorse, trainer);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", Profile(2, new TimeOffInput(At(12), At(13))), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{rider.Id}/training-profile", Profile() with { Specialties = [], WorkingHours = [new(Day.DayOfWeek, 480, 1020)] }, ClubFactory.Json)).StatusCode);
        using var trainerClient = await factory.Client(trainer);
        var first = await ClubFactory.Post(trainerClient, $"/api/training/plans/{plan}/sessions", Session(8, rider.Id));
        await ClubFactory.Post(trainerClient, $"/api/training/plans/{otherPlan}/sessions", Session(10));
        var result = await trainerClient.GetFromJsonAsync<TrainingAvailabilityResponse>(AvailabilityUrl(horse.Id, rider.Id), ClubFactory.Json);
        Assert.Equal(new[] { new TimeRange(At(9), At(10)), new TimeRange(At(11), At(12)), new TimeRange(At(13), At(17)) }, result!.CommonFree);
        Assert.Contains(result.Trainer.Busy, x => x.Start == At(10) && x.End == At(11));
        var body = await trainerClient.GetStringAsync(AvailabilityUrl(horse.Id, rider.Id));
        Assert.DoesNotContain(otherHorse.Id.ToString(), body); Assert.DoesNotContain(otherPlan.ToString(), body);
        var edited = await trainerClient.GetFromJsonAsync<TrainingAvailabilityResponse>(AvailabilityUrl(horse.Id, rider.Id, first.GetProperty("id").GetGuid()), ClubFactory.Json);
        Assert.Contains(edited!.CommonFree, x => x.Start == At(8) && x.End == At(10));
        using var outsider = await factory.Client(await factory.User(Role.Trainer));
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync(AvailabilityUrl(horse.Id))).StatusCode);
        using var ownerClient = await factory.Client(owner);
        Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync(AvailabilityUrl(horse.Id))).StatusCode);
    }

    [Fact]
    public async Task ServerBlocksTrainerOverlapTimeOffWrongSpecialtyAndProfileChangesButAllowsAdjacency()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var trainer = await factory.User(Role.Trainer); var horse = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var second = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var plan = await Plan(factory, horse, trainer); var secondPlan = await Plan(factory, second, trainer);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", Profile(2, new TimeOffInput(At(12), At(13))), ClubFactory.Json)).StatusCode);
        using var client = await factory.Client(trainer);
        await ClubFactory.Post(client, $"/api/training/plans/{plan}/sessions", Session(8));
        var overlap = await client.PostAsJsonAsync($"/api/training/plans/{secondPlan}/sessions", Session(8), ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode); Assert.Equal("trainer_schedule_overlap", (await overlap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("title").GetString());
        await ClubFactory.Post(client, $"/api/training/plans/{secondPlan}/sessions", Session(9));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/training/plans/{plan}/sessions", Session(12), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/training/plans/{plan}/sessions", Session(19), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync($"/api/training/plans/{plan}/sessions", Session(14) with { TrainingType = TrainingType.Sprint }, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", Profile() with { Specialties = [] }, ClubFactory.Json)).StatusCode);
        var unchanged = await manager.GetFromJsonAsync<StaffTrainingProfileResponse>($"/api/staff/{trainer.Id}/training-profile", ClubFactory.Json);
        Assert.Single(unchanged!.Specialties); Assert.Single(unchanged.TimeOff);
    }

    [Fact]
    public async Task UnknownDurationAndMissingWorkingHoursNeverProduceConfirmedFreeTime()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var trainer = await factory.User(Role.Trainer); var horse = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var plan = await Plan(factory, horse, trainer); using var client = await factory.Client(trainer);
        var before = await client.GetFromJsonAsync<TrainingAvailabilityResponse>(AvailabilityUrl(horse.Id), ClubFactory.Json);
        Assert.Equal("SpecialtyMissing", before!.Trainer.State); Assert.Empty(before.CommonFree);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            db.TrainerExpertise.Add(new TrainerExpertise { StaffId = trainer.Id, TrainingType = TrainingType.Trot, Level = 2 });
            await db.SaveChangesAsync();
        }
        var noHours = await client.GetFromJsonAsync<TrainingAvailabilityResponse>(AvailabilityUrl(horse.Id), ClubFactory.Json);
        Assert.Equal("HoursMissing", noHours!.Trainer.State); Assert.Empty(noHours.CommonFree);
        await ClubFactory.Post(client, $"/api/training/plans/{plan}/sessions", Session(8) with { DurationMinutes = null });
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", Profile(), ClubFactory.Json)).StatusCode);
        var result = await client.GetFromJsonAsync<TrainingAvailabilityResponse>(AvailabilityUrl(horse.Id), ClubFactory.Json);
        Assert.Equal("Unknown", result!.Horse.State); Assert.Empty(result.Horse.Free); Assert.Empty(result.CommonFree);
    }

    [Fact]
    public async Task PausedPlansCannotResumeOverAnotherHorseBookingForTheSameTrainer()
    {
        await using var factory = new ClubFactory();
        var trainer = await factory.User(Role.Trainer); var first = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var second = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var firstPlan = await Plan(factory, first, trainer); var secondPlan = await Plan(factory, second, trainer);
        using var client = await factory.Client(trainer);
        await ClubFactory.Post(client, $"/api/training/plans/{firstPlan}/sessions", Session(8));
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/training/plans/{firstPlan}/status", new PlanStatusRequest(PlanStatus.Paused), ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(client, $"/api/training/plans/{secondPlan}/sessions", Session(8));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/training/plans/{firstPlan}/status", new PlanStatusRequest(PlanStatus.Active), ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task SeparateHostsCannotDoubleBookATrainerAcrossDifferentHorses()
    {
        await using var factory = new ClubFactory();
        var trainer = await factory.User(Role.Trainer); var first = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var second = await factory.Horse(await factory.User(Role.HorseOwner), trainer);
        var firstPlan = await Plan(factory, first, trainer); var secondPlan = await Plan(factory, second, trainer);
        await using var replica = factory.Replica(); using var firstClient = await factory.Client(trainer); using var secondClient = await replica.Client(trainer);
        var responses = await Task.WhenAll(firstClient.PostAsJsonAsync($"/api/training/plans/{firstPlan}/sessions", Session(8), ClubFactory.Json),
            secondClient.PostAsJsonAsync($"/api/training/plans/{secondPlan}/sessions", Session(8), ClubFactory.Json));
        Assert.Single(responses, x => x.IsSuccessStatusCode); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.Single(await db.Sessions.Where(x => x.PlanId == firstPlan || x.PlanId == secondPlan).ToListAsync());
    }

    [Fact]
    public void CollectedReferenceProfilesHaveSourcesAndDoNotInventOperationalFacts()
    {
        var data = PublicRacingReference.Read();
        Assert.Equal(6, data.GetProperty("horses").GetArrayLength()); Assert.Equal(5, data.GetProperty("trainers").GetArrayLength());
        foreach (var horse in data.GetProperty("horses").EnumerateArray())
        {
            Assert.StartsWith("https://racing.hkjc.com/", horse.GetProperty("sourceUrl").GetString());
            Assert.Equal(JsonValueKind.Null, horse.GetProperty("healthStatus").ValueKind);
            Assert.Equal(JsonValueKind.Null, horse.GetProperty("dateOfBirth").ValueKind);
        }
        foreach (var trainer in data.GetProperty("trainers").EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Null, trainer.GetProperty("confirmedSpecialties").ValueKind);
            Assert.Equal(JsonValueKind.Null, trainer.GetProperty("workingHours").ValueKind);
        }
    }
}
