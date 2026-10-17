using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HorseClub.BLL.Abstractions.Services;
using HorseClub.BLL.Inventory;
using HorseClub.BLL.Workers;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class RepositoryRealtimeTests
{
    [SqlServerFact]
    public async Task InventoryInterfaceAndRepositoryShareRequestUnitOfWork()
    {
        await using var factory = new ClubFactory(); using var client = await factory.Client();
        using var scope = factory.Services.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<InventoryService>(), scope.ServiceProvider.GetRequiredService<IInventoryService>());
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using var transaction = await unit.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var item = new InventoryItem { Name = "Rollback feed", Category = "Feed", Unit = "kg" };
        repository.AddItem(item); await unit.SaveChangesAsync();
        Assert.Same(item, await repository.FindItemAsync(item.Id));
        await transaction.RollbackAsync(); db.ChangeTracker.Clear();
        Assert.Null(await repository.FindItemAsync(item.Id));
    }

    [SqlServerFact]
    public async Task NotificationOutboxRollsBackAndConcurrentClaimHasOneWinner()
    {
        await using var factory = new ClubFactory(); var user = await factory.User(Role.HorseOwner);
        Guid rolledBack;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync();
            var notification = new Notification { RecipientId = user.Id, Message = "Rollback" }; rolledBack = notification.Id;
            db.Notifications.Add(notification); await db.SaveChangesAsync();
            Assert.True(await db.RealtimeOutboxMessages.AnyAsync(x => x.SourceId == rolledBack));
            await tx.RollbackAsync();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            Assert.False(await db.RealtimeOutboxMessages.AnyAsync(x => x.SourceId == rolledBack));
            db.Notifications.Add(new Notification { RecipientId = user.Id, Message = "Committed" }); await db.SaveChangesAsync();
        }
        using var first = factory.Services.CreateScope(); using var second = factory.Services.CreateScope();
        var claims = await Task.WhenAll(first.ServiceProvider.GetRequiredService<IRealtimeOutboxRepository>().ClaimAsync(DateTimeOffset.UtcNow, Guid.NewGuid(), default),
            second.ServiceProvider.GetRequiredService<IRealtimeOutboxRepository>().ClaimAsync(DateTimeOffset.UtcNow, Guid.NewGuid(), default));
        Assert.Single(claims, x => x is not null);
    }

    [SqlServerFact]
    public async Task SignalRUsesExistingBearerTokenAndPublishesOnlyToValidSession()
    {
        await using var factory = new ClubFactory(); var user = await factory.User(Role.HorseOwner);
        using var client = await factory.Client(user);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/hubs/club/negotiate?negotiateVersion=1", null)).StatusCode);
        using var originRequest = new HttpRequestMessage(HttpMethod.Post, "/hubs/club/negotiate?negotiateVersion=1");
        originRequest.Headers.Add("Origin", "https://untrusted.example");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(originRequest)).StatusCode);

        using var socket = await factory.Server.CreateWebSocketClient().ConnectAsync(new Uri("ws://localhost/hubs/club?access_token=" + Uri.EscapeDataString(token)), default);
        await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"), WebSocketMessageType.Text, true, default);
        Assert.Contains("{}", await Receive(socket));
        Guid notificationId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var notification = new Notification { RecipientId = user.Id, Message = "Private content must not travel in realtime" };
            notificationId = notification.Id; db.Notifications.Add(notification); await db.SaveChangesAsync();
        }
        var worker = ActivatorUtilities.CreateInstance<RealtimeDispatchWorker>(factory.Services);
        Assert.Equal(1, await worker.RunOnce());
        var payload = await Receive(socket);
        Assert.Contains("NotificationCreated", payload); Assert.Contains(notificationId.ToString(), payload);
        Assert.DoesNotContain("Private content", payload);
        Assert.True((await client.PostAsync($"/api/notifications/{notificationId}/read", null)).IsSuccessStatusCode);
        Assert.Equal(1, await worker.RunOnce());
        var readPayload = await Receive(socket);
        Assert.Contains("NotificationsChanged", readPayload);
        Assert.Contains("\"resourceVersion\":1", readPayload);
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IEventRepository>().AddDataSignal(user.Id);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }
        Assert.Equal(1, await worker.RunOnce());
        var changedPayload = await Receive(socket);
        Assert.Contains("DataChanged", changedPayload);
        Assert.Contains("\"notificationId\":null", changedPayload);
        Assert.DoesNotContain("Private content", changedPayload);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            var stored = await db.Users.FindAsync(user.Id); stored!.SecurityStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync();
            var connections = scope.ServiceProvider.GetRequiredService<Horse_BackEnd.Realtime.RealtimeConnections>();
            Assert.Empty(connections.Find(user.Id, stored.SecurityStamp, DateTimeOffset.UtcNow));
        }
        socket.Abort();
    }

    private static async Task<string> Receive(WebSocket socket)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var buffer = new byte[8192];
        var result = await socket.ReceiveAsync(buffer, timeout.Token);
        Assert.NotEqual(WebSocketMessageType.Close, result.MessageType);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
