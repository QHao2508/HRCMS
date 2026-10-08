namespace HorseClub.DAL.Entities;

/// <summary>Durable invalidation signal; contains no notification content or clinical data.</summary>
public sealed class RealtimeOutboxMessage : Entity
{
    public Guid RecipientId { get; set; }
    public Guid SourceId { get; set; }
    public long SourceVersion { get; set; }
    public string EventType { get; set; } = "NotificationCreated";
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
