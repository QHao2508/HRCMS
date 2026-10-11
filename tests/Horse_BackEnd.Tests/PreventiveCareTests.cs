using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.BLL.Common;
using HorseClub.BLL.Workers;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class PreventiveCareTests
{
    [Fact]
    public async Task VetLifecycleUsesVietnamDateRejectsFutureAndDuplicateCompletionAndHidesClinicalNotes()
    {
        var clock = new WorkerClock();
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:TimeZoneId"] = "Asia/Ho_Chi_Minh" }, clock: clock);
        var owner = await f.User(Role.HorseOwner); var vet = await f.User(Role.Veterinarian); var trainer = await f.User(Role.Trainer);
        var horse = await f.Horse(owner, vet, trainer); using var vc = await f.Client(vet); using var oc = await f.Client(owner); using var tc = await f.Client(trainer);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1);
        var care = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/preventive-care", new PreventiveRequest(PreventiveCareType.Vaccination, today, "Private preventive clinical notes"));
        var id = care.GetProperty("id").GetGuid(); var route = $"/api/horses/{horse.Id}/medical/preventive-care/{id}/complete";
        Assert.Equal(HttpStatusCode.BadRequest, (await vc.PostAsJsonAsync(route, new PreventiveCompletionRequest(today.AddDays(1), "Future"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await tc.PostAsJsonAsync(route, new PreventiveCompletionRequest(today, ""))).StatusCode);
        Assert.DoesNotContain("Private preventive clinical notes", await oc.GetStringAsync($"/api/horses/{horse.Id}/medical/preventive-care"));
        await ClubFactory.Post(vc, route, new PreventiveCompletionRequest(today, "Completed"));
        Assert.Equal(HttpStatusCode.BadRequest, (await vc.PostAsJsonAsync(route, new PreventiveCompletionRequest(today, "Again"))).StatusCode);
        await WorkerTests.Read(f, async db =>
        {
            Assert.Equal(today, (await db.PreventiveCare.FindAsync(id))!.CompletedDate);
            Assert.Equal(1, await db.Audit.CountAsync(x => x.ReferenceId == id && x.Action == AuditAction.MedicalPreventiveCompleted));
        });
        using var outsider = await f.Client(await f.User(Role.Veterinarian));
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/horses/{horse.Id}/medical/preventive-care")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PostAsJsonAsync($"/api/horses/{horse.Id}/medical/preventive-care", new PreventiveRequest(PreventiveCareType.Farrier, today, ""), ClubFactory.Json)).StatusCode);
    }

    [Fact]
    public async Task ReminderDueAtVietnamMidnightOnlyNotifiesCurrentActiveVetOnceAndSkipsArchivedOrCompleted()
    {
        var clock = new WorkerClock();
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Business:TimeZoneId"] = "Asia/Ho_Chi_Minh" }, clock: clock);
        var owner = await f.User(Role.HorseOwner); var vet = await f.User(Role.Veterinarian); var inactive = await f.User(Role.Veterinarian);
        var horse = await f.Horse(owner, vet); var waitingHorse = await f.Horse(owner, inactive); var archived = await f.Horse(owner, vet);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1);
        var due = new PreventiveCare { HorseId = horse.Id, DueDate = today };
        var waiting = new PreventiveCare { HorseId = waitingHorse.Id, DueDate = today };
        await WorkerTests.Read(f, async db =>
        {
            (await db.Users.FindAsync(inactive.Id))!.Active = false; (await db.Horses.FindAsync(archived.Id))!.Archived = true;
            db.PreventiveCare.AddRange(due, waiting, new PreventiveCare { HorseId = horse.Id, DueDate = today.AddDays(1) },
                new PreventiveCare { HorseId = horse.Id, DueDate = today, CompletedDate = today }, new PreventiveCare { HorseId = archived.Id, DueDate = today });
            await db.SaveChangesAsync();
        });
        using var worker = new ReminderWorker(f.Services.GetRequiredService<IServiceScopeFactory>(), clock, NullLogger<ReminderWorker>.Instance,
            f.Services.GetRequiredService<IOptions<WorkerOptions>>(), f.Services.GetRequiredService<IOptions<BusinessOptions>>());
        Assert.Equal(1, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
        await WorkerTests.Read(f, async db =>
        {
            var notification = await db.Notifications.SingleAsync(x => x.Type == NotificationType.MedicalPreventiveDue);
            Assert.Equal(vet.Id, notification.RecipientId); Assert.Equal(due.Id, notification.ReferenceId);
            Assert.False((await db.PreventiveCare.FindAsync(waiting.Id))!.ReminderSent);
            (await db.Users.FindAsync(inactive.Id))!.Active = true; await db.SaveChangesAsync();
        });
        Assert.Equal(1, await worker.RunOnce()); Assert.Equal(0, await worker.RunOnce());
    }

    [Fact]
    public async Task PreventiveListPagesMoreThanOneHundredWithoutLeakingAnotherOwner()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var horse = await f.Horse(owner);
        var otherHorse = await f.Horse(await f.User(Role.HorseOwner)); using var client = await f.Client(owner);
        await WorkerTests.Read(f, async db =>
        {
            db.PreventiveCare.AddRange(Enumerable.Range(0, 105).Select(i => new PreventiveCare { HorseId = horse.Id, DueDate = new DateOnly(2026, 10, 10).AddDays(i), Notes = "Private clinical notes" }));
            db.PreventiveCare.Add(new PreventiveCare { HorseId = otherHorse.Id, DueDate = new DateOnly(2026, 10, 10) }); await db.SaveChangesAsync();
        });
        var first = await client.GetFromJsonAsync<JsonElement>($"/api/horses/{horse.Id}/medical/preventive-care?page=1&pageSize=100");
        var second = await client.GetFromJsonAsync<JsonElement>($"/api/horses/{horse.Id}/medical/preventive-care?page=2&pageSize=100");
        Assert.Equal(105, first.GetProperty("total").GetInt32()); Assert.Equal(100, first.GetProperty("items").GetArrayLength()); Assert.Equal(5, second.GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/horses/{otherHorse.Id}/medical/preventive-care")).StatusCode);
    }
}
