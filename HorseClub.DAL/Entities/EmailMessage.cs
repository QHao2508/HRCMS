namespace HorseClub.DAL.Entities;

public sealed class EmailMessage : Entity
{
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public Guid? ChallengeId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? DiscardedAt { get; set; }
}
