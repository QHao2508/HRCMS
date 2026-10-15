using HorseClub.BLL.Contracts;
using HorseClub.BLL.Training;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class DefaultTrainingCurriculumTests
{
    [Fact]
    public void DefaultsHaveUniqueStableIdsAndValidatedIllustrativeChoices()
    {
        var defaults = DefaultTrainingTemplates.All;
        Assert.Equal(5, defaults.Count);
        Assert.Equal(5, defaults.Select(x => x.Id).Distinct().Count());
        foreach (var template in defaults)
        {
            Assert.True(template.IsSystemDefault);
            Assert.Contains("minh họa", template.Notes);
            TemplateCurriculumPolicy.Validate(Request(template));
            Assert.NotEmpty(TemplateCurriculumPolicy.Read(template)!.SessionOptions);
        }
    }
    [Fact]
    public async Task EveryHeadTrainerSeesSharedDefaultsAndCannotArchiveOrEditThem()
    {
        await using var factory = new ClubFactory();
        var one = await factory.User(Role.HeadTrainer); var two = await factory.User(Role.HeadTrainer);
        using var first = await factory.Client(one); using var second = await factory.Client(two);
        var defaults = await first.GetFromJsonAsync<List<TrainingTemplate>>("/api/training/templates/defaults", ClubFactory.Json);
        Assert.Equal(5, defaults!.Count);
        var secondDefaults = await second.GetFromJsonAsync<List<TrainingTemplate>>("/api/training/templates/defaults", ClubFactory.Json);
        Assert.Equal(defaults.Select(t => t.Id), secondDefaults!.Select(t => t.Id));
        var template = defaults[0];
        Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsJsonAsync($"/api/training/templates/{template.Id}/archive", new { }, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await first.PutAsJsonAsync($"/api/training/templates/{template.Id}", Request(template), ClubFactory.Json)).StatusCode);
        var copy = await second.PostAsJsonAsync("/api/training/templates", Request(template) with { Name = "Giáo án thể lực riêng" }, ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Created, copy.StatusCode);
        Assert.False((await copy.Content.ReadFromJsonAsync<TrainingTemplate>(ClubFactory.Json))!.IsSystemDefault);
    }
    [Fact]
    public async Task AssignmentRequiresAllSpecialtiesAndPlansAndSessionsUseExplicitChoices()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var head = await factory.User(Role.HeadTrainer); var trainer = await factory.User(Role.Trainer); var low = await factory.User(Role.Trainer);
        var owner = await factory.User(Role.HorseOwner); var horse = await factory.Horse(owner, head, trainer);
        using var headClient = await factory.Client(head); using var trainerClient = await factory.Client(trainer);
        var template = DefaultTrainingTemplates.All[0]; var curriculum = TemplateCurriculumPolicy.Read(template)!;
        var profile = new StaffTrainingProfileRequest([new(TrainingType.Walk, 1), new(TrainingType.Trot, 1)], [], []);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", profile, ClubFactory.Json)).StatusCode);
        var option = curriculum.PlanOptions[0]; var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var planRequest = new PlanRequest(horse.Id, template.Id, option.Goal, option.Phase, today, today.AddDays(7), "", option.Key);
        Assert.Equal(HttpStatusCode.Conflict, (await trainerClient.PostAsJsonAsync("/api/training/plans", planRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await headClient.PostAsJsonAsync($"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(low.Id), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await headClient.PostAsJsonAsync($"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(trainer.Id), ClubFactory.Json)).StatusCode);
        var listed = await trainerClient.GetFromJsonAsync<PageResponse<TrainingTemplate>>("/api/training/templates", ClubFactory.Json);
        Assert.Contains(listed!.Items, t => t.Id == template.Id);
        Assert.DoesNotContain(listed.Items, t => t.SystemKey == "speed-intervals");
        var response = await trainerClient.PostAsJsonAsync("/api/training/plans", planRequest, ClubFactory.Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var plan = (await response.Content.ReadFromJsonAsync<TrainingPlan>(ClubFactory.Json))!;
        var exercise = curriculum.SessionOptions[0];
        var sessionRequest = new SessionRequest(DateTimeOffset.UtcNow.AddHours(1), exercise.TrainingType, exercise.DistanceMetres, exercise.Intensity,
            exercise.Surface, exercise.Target, "", null, exercise.DurationMinutes, exercise.Key);
        Assert.Equal(HttpStatusCode.BadRequest, (await trainerClient.PostAsJsonAsync($"/api/training/plans/{plan.Id}/sessions", sessionRequest with { DistanceMetres = 999 }, ClubFactory.Json)).StatusCode);
        var custom = await trainerClient.PostAsJsonAsync($"/api/training/plans/{plan.Id}/sessions", sessionRequest with { DistanceMetres = 999, TemplateExerciseKey = null, UseCustomExercise = true }, ClubFactory.Json);
        Assert.True(custom.StatusCode == HttpStatusCode.Created, $"{(int)custom.StatusCode}: {await custom.Content.ReadAsStringAsync()}");
        var session = (await custom.Content.ReadFromJsonAsync<TrainingSession>(ClubFactory.Json))!;
        var rider = await factory.User(Role.WorkRider);
        Assert.Equal(HttpStatusCode.OK, (await trainerClient.PostAsJsonAsync($"/api/training/sessions/{session.Id}/assign", new RiderRequest(rider.Id), ClubFactory.Json)).StatusCode);
    }
    private static TemplateRequest Request(TrainingTemplate template) => new(template.Name, template.Goal, template.Phase, template.DistanceMetres,
        template.Intensity, template.Surface, template.FrequencyPerWeek, template.Notes, TemplateCurriculumPolicy.RequiredTypes(template), template.MinimumExpertiseLevel, TemplateCurriculumPolicy.Read(template));
}
