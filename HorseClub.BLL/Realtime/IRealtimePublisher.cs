namespace HorseClub.BLL.Realtime;

public sealed record RealtimeEvent(Guid EventId, int SchemaVersion, Guid? NotificationId, DateTimeOffset OccurredAtUtc, string EventType, long ResourceVersion);

public interface IRealtimePublisher
{
    Task PublishAsync(Guid recipientId, string currentStamp, RealtimeEvent message, CancellationToken token);
}
