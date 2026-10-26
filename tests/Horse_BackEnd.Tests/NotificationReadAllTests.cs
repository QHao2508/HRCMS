using System.Net;
using System.Net.Http.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class NotificationReadAllTests
{
    [Theory]
    [InlineData(Role.HorseOwner)]
    [InlineData(Role.Groom)]
    public async Task ReadAllCoversEveryPageIsRecipientScopedAndRepeatable(Role role)
    {
        await using var factory = new ClubFactory();
        var user = await factory.User(role); var other = await factory.User(Role.Trainer);
        var seen = new Notification { RecipientId = user.Id, Read = true };
        await WorkerTests.Read(factory, async db =>
        {
            db.Notifications.Add(seen);
            db.Notifications.AddRange(Enumerable.Range(0, 31).Select(_ => new Notification { RecipientId = user.Id }));
            db.Notifications.AddRange(Enumerable.Range(0, 3).Select(_ => new Notification { RecipientId = other.Id }));
            await db.SaveChangesAsync();
        });
        using var client = await factory.Client(user);
        // Query parameters cannot redirect the operation to another recipient.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/read-all?recipientId={other.Id}", null)).StatusCode);
        var unread = await client.GetFromJsonAsync<PageResponse<NotificationResponse>>("/api/notifications?unread=true&pageSize=1", ClubFactory.Json);
        Assert.Equal(0, unread!.Total);
        await WorkerTests.Read(factory, async db =>
        {
            Assert.Equal(32, await db.Notifications.CountAsync(n => n.RecipientId == user.Id && n.Read));
            Assert.Equal(3, await db.Notifications.CountAsync(n => n.RecipientId == other.Id && !n.Read));
            Assert.Equal(0, (await db.Notifications.FindAsync(seen.Id))!.Version);
            Assert.Equal(31, await db.RealtimeOutboxMessages.CountAsync(m => m.RecipientId == user.Id && m.EventType == "NotificationsChanged"));
        });
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/notifications/read-all", null)).StatusCode);
        await WorkerTests.Read(factory, async db =>
        {
            Assert.Equal(31, await db.RealtimeOutboxMessages.CountAsync(m => m.RecipientId == user.Id && m.EventType == "NotificationsChanged"));
            db.Notifications.Add(new() { RecipientId = user.Id }); await db.SaveChangesAsync();
        });
        unread = await client.GetFromJsonAsync<PageResponse<NotificationResponse>>("/api/notifications?unread=true&pageSize=1", ClubFactory.Json);
        Assert.Equal(1, unread!.Total); // A later notification remains new until another explicit action.
    }

    [Fact]
    public async Task ReadAllRequiresAuthentication()
    {
        await using var factory = new ClubFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/notifications/read-all", null)).StatusCode);
    }
}
