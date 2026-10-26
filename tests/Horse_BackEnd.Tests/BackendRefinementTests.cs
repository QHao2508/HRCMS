using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Training;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class BackendRefinementTests
{
    [Fact]
    public async Task NotificationsReturnCodesWithHorseContextScopeAndReadState()
    {
        await using var factory = new ClubFactory();
        var owner = await factory.User(Role.HorseOwner); var user = await factory.User(Role.Groom); var other = await factory.User(Role.Groom);
        var horse = await factory.Horse(owner, user);
        var notification = HorseClub.BLL.Common.ClubEvents.Create(user.Id, NotificationType.HorseAssignment, HorseClub.BLL.Messaging.MessageKey.YouHaveBeenAssignedToAHorse, horse.Id);
        await WorkerTests.Read(factory, async db =>
        {
            db.Notifications.Add(notification);
            db.Notifications.Add(HorseClub.BLL.Common.ClubEvents.Create(other.Id, NotificationType.CareAssigned, HorseClub.BLL.Messaging.MessageKey.ACareTaskWasAssignedToYou));
            await db.SaveChangesAsync();
        });
        using var client = await factory.Client(user);
        var list = await client.GetFromJsonAsync<JsonElement>("/api/notifications");
        var item = list.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == notification.Id);
        Assert.Equal("YouHaveBeenAssignedToAHorse", item.GetProperty("messageCode").GetString());
        Assert.Equal("HorseAssignment", item.GetProperty("type").GetString());
        Assert.Equal(horse.Id, item.GetProperty("horseId").GetGuid()); Assert.Equal(horse.Name, item.GetProperty("horseName").GetString());
        Assert.False(item.TryGetProperty("message", out _)); Assert.False(item.GetProperty("read").GetBoolean());
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), x => x.GetProperty("messageCode").GetString() == "ACareTaskWasAssignedToYou");
        await ClubFactory.Post(client, $"/api/notifications/{notification.Id}/read", new { });
        var unread = await client.GetFromJsonAsync<JsonElement>("/api/notifications?unread=true");
        Assert.DoesNotContain(unread.GetProperty("items").EnumerateArray(), x => x.GetProperty("id").GetGuid() == notification.Id);
    }

    [Fact]
    public async Task RegistrationNamesPreservePagingOwnershipAndPersonalDataPrivacy()
    {
        await using var factory = new ClubFactory();
        var owner = await factory.User(Role.HorseOwner, "private.owner@example.test");
        var other = await factory.User(Role.HorseOwner);
        var horse = await factory.Horse(owner); var second = await factory.Horse(other);
        using var manager = await factory.Client(); using var client = await factory.Client(owner);
        var list = await manager.GetFromJsonAsync<JsonElement>("/api/registrations?pageSize=1");
        Assert.Equal(2, list.GetProperty("total").GetInt32()); Assert.Single(list.GetProperty("items").EnumerateArray());
        var detail = await manager.GetFromJsonAsync<JsonElement>($"/api/registrations/{horse.RegistrationId}");
        Assert.Equal(owner.FirstName + " " + owner.LastName, detail.GetProperty("ownerName").GetString());
        Assert.DoesNotContain(owner.Email, detail.GetRawText()); Assert.DoesNotContain("passwordHash", detail.GetRawText());
        var own = await client.GetFromJsonAsync<PageResponse<HorseRegistration>>("/api/registrations", ClubFactory.Json);
        Assert.Equal(horse.RegistrationId, Assert.Single(own!.Items).Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/registrations/{second.RegistrationId}")).StatusCode);
        await WorkerTests.Read(factory, db => { Assert.Null(db.Model.FindEntityType(typeof(HorseRegistration))!.FindProperty("OwnerName")); return Task.CompletedTask; });
    }

    [Fact]
    public async Task CandidateRequirementsExplainMissingAndLowSkillsAndProfileDowngradeRequiresRevocation()
    {
        await using var factory = new ClubFactory();
        var head = await factory.User(Role.HeadTrainer); var trainer = await factory.User(Role.Trainer); var missing = await factory.User(Role.Trainer);
        using var manager = await factory.Client(); using var headClient = await factory.Client(head);
        var template = DefaultTrainingTemplates.All.Single(t => t.SystemKey == "steady-canter");
        var profile = new StaffTrainingProfileRequest([new(TrainingType.Canter, 1)], [], []);
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", profile, ClubFactory.Json)).StatusCode);
        var candidates = await headClient.GetFromJsonAsync<List<TemplateTrainerCandidate>>($"/api/training/templates/{template.Id}/trainer-candidates", ClubFactory.Json);
        var low = candidates!.Single(c => c.Id == trainer.Id);
        Assert.Equal("BelowMinimumLevel", low.Eligibility); Assert.False(low.Matches);
        Assert.Equal(2, low.MinimumExpertiseLevel); Assert.Equal([TrainingType.Canter], low.RequiredTrainingTypes);
        Assert.Equal([TrainingType.Canter], low.BelowLevelSpecialties); Assert.Empty(low.MissingSpecialties);
        Assert.Equal("MissingSpecialty", candidates!.Single(c => c.Id == missing.Id).Eligibility);
        Assert.Equal(HttpStatusCode.Conflict, (await headClient.PostAsJsonAsync($"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(trainer.Id), ClubFactory.Json)).StatusCode);
        profile = profile with { Specialties = [new(TrainingType.Canter, 2)] };
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", profile, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(headClient, $"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(trainer.Id));
        var reduced = profile with { Specialties = [new(TrainingType.Canter, 1)] };
        Assert.Equal(HttpStatusCode.Conflict, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", reduced, ClubFactory.Json)).StatusCode);
        await ClubFactory.Post(headClient, $"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(trainer.Id, false));
        Assert.Equal(HttpStatusCode.OK, (await manager.PutAsJsonAsync($"/api/staff/{trainer.Id}/training-profile", reduced, ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task ManagerCannotOperateTrainingButKeepsAdministrativeAssignmentsAndRoutineCare()
    {
        await using var factory = new ClubFactory(); using var manager = await factory.Client();
        var head = await factory.User(Role.HeadTrainer); var trainer = await factory.User(Role.Trainer); var rider = await factory.User(Role.WorkRider);
        var owner = await factory.User(Role.HorseOwner); var groom = await factory.User(Role.Groom); var vet = await factory.User(Role.Veterinarian);
        var horse = await factory.Horse(owner, head, trainer, groom, vet);
        var template = DefaultTrainingTemplates.All[0];
        var request = new TemplateRequest("Giáo án riêng", "Thể lực", "Base", 1000, Intensity.Light, "Cát", 3, "");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync("/api/training/templates", request, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/training/templates/{template.Id}", request, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync($"/api/training/templates/{template.Id}/archive", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/training/templates/{template.Id}/assign", new TemplateTrainerRequest(trainer.Id), ClubFactory.Json)).StatusCode);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/horses/{horse.Id}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, today, "Reason"), ClubFactory.Json)).StatusCode);
        foreach (var staff in new[] { head, groom, vet })
            await ClubFactory.Post(manager, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(staff.Id, staff.Role, today, "Confirmed"));
        using var headClient = await factory.Client(head);
        await ClubFactory.Post(headClient, $"/api/horses/{horse.Id}/assignments", new AssignmentRequest(trainer.Id, Role.Trainer, today, "Confirmed"));
        var plan = new TrainingPlan { HorseId = horse.Id, TrainerId = trainer.Id, TemplateId = template.Id, Goal = "Thể lực", Phase = "Base", StartDate = today, EndDate = today.AddDays(2) };
        var session = new TrainingSession { HorseId = horse.Id, PlanId = plan.Id, RiderId = rider.Id, ScheduledAt = DateTimeOffset.UtcNow.AddHours(1), Status = SessionStatus.Assigned, DistanceMetres = 400, Surface = "Cát", Target = "Đi bộ" };
        await WorkerTests.Read(factory, async db => { db.Plans.Add(plan); db.Sessions.Add(session); await db.SaveChangesAsync(); });
        var planRequest = new PlanRequest(horse.Id, template.Id, "Thể lực", "Base", today, today.AddDays(2), "");
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync("/api/training/plans", planRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/training/plans/{plan.Id}", planRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/training/plans/{plan.Id}/status", new PlanStatusRequest(PlanStatus.Completed), ClubFactory.Json)).StatusCode);
        var sessionRequest = new SessionRequest(session.ScheduledAt, TrainingType.Walk, 400, Intensity.Light, "Cát", "Đi bộ", "", rider.Id, 15);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/training/plans/{plan.Id}/sessions", sessionRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PutAsJsonAsync($"/api/training/sessions/{session.Id}", sessionRequest, ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/training/sessions/{session.Id}/assign", new RiderRequest(rider.Id), ClubFactory.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsync($"/api/training/sessions/{session.Id}/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync($"/api/training/sessions/{session.Id}/skip", new ReasonRequest("Reason"), ClubFactory.Json)).StatusCode);
        foreach (var type in new[] { CareType.Feeding, CareType.Cleaning, CareType.Bathing, CareType.Grooming, CareType.WaterCheck })
            await ClubFactory.Post(manager, "/api/care/tasks", new CareRequest(horse.Id, groom.Id, null, type, DateTimeOffset.UtcNow, "Hướng dẫn", type == CareType.Feeding ? 1 : null));
        foreach (var type in new[] { CareType.Treatment, CareType.IceBath })
            Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync("/api/care/tasks", new CareRequest(horse.Id, groom.Id, null, type, DateTimeOffset.UtcNow, "Hướng dẫn", null), ClubFactory.Json)).StatusCode);
        await WorkerTests.Read(factory, async db => Assert.Equal(5, await db.CareTasks.CountAsync(t => t.HorseId == horse.Id)));
    }
}
