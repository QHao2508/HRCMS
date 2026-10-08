using System.Collections.Concurrent;
using System.Security.Claims;
using HorseClub.BLL.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Horse_BackEnd.Realtime;

public interface IClubClient
{
    Task NotificationCreated(RealtimeEvent message);
    Task NotificationsChanged(RealtimeEvent message);
    Task DataChanged(RealtimeEvent message);
}

/// <summary>Single-host routing registry; revoked stamps cannot receive future publications.</summary>
public sealed class RealtimeConnections
{
    private readonly ConcurrentDictionary<string, Session> sessions = new();
    public void Add(string connectionId, Guid userId, string stamp, DateTimeOffset expiresAt) => sessions[connectionId] = new(userId, stamp, expiresAt);
    public void Remove(string connectionId) => sessions.TryRemove(connectionId, out _);
    public string[] Find(Guid userId, string currentStamp, DateTimeOffset now)
        => sessions.Where(x => x.Value.UserId == userId && x.Value.Stamp == currentStamp && x.Value.ExpiresAt > now).Select(x => x.Key).ToArray();
    private sealed record Session(Guid UserId, string Stamp, DateTimeOffset ExpiresAt);
}

[Authorize]
public sealed class ClubHub(IAuthenticationService auth, RealtimeConnections connections) : Hub<IClubClient>
{
    public override async Task OnConnectedAsync()
    {
        var principal = Context.User;
        var stamp = principal?.FindFirstValue("stamp");
        var expiration = Context.Features.Get<Microsoft.AspNetCore.Authentication.IAuthenticateResultFeature>()?.AuthenticateResult?.Properties?.ExpiresUtc;
        // Hub features may not expose the HTTP authentication result directly on every transport.
        expiration ??= Context.GetHttpContext()?.Features.Get<Microsoft.AspNetCore.Authentication.IAuthenticateResultFeature>()?.AuthenticateResult?.Properties?.ExpiresUtc;
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || string.IsNullOrEmpty(stamp)
            || expiration is null || await auth.ValidateSession(id, stamp) is null)
        { Context.Abort(); return; }
        connections.Add(Context.ConnectionId, id, stamp, expiration.Value);
        await base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    { connections.Remove(Context.ConnectionId); return base.OnDisconnectedAsync(exception); }
}

public sealed class SignalRRealtimePublisher(IHubContext<ClubHub, IClubClient> hub, RealtimeConnections connections, TimeProvider clock) : IRealtimePublisher
{
    public async Task PublishAsync(Guid recipientId, string currentStamp, RealtimeEvent message, CancellationToken token)
    {
        var ids = connections.Find(recipientId, currentStamp, clock.GetUtcNow());
        if (ids.Length > 0)
        {
            var client = hub.Clients.Clients(ids);
            var send = message.EventType switch
            {
                "NotificationsChanged" => client.NotificationsChanged(message),
                "DataChanged" => client.DataChanged(message),
                _ => client.NotificationCreated(message)
            };
            await send.WaitAsync(token);
        }
    }
}
