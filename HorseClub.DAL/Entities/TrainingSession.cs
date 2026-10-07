namespace HorseClub.DAL.Entities;

public sealed class TrainingSession : Entity
{
    public Guid HorseId { get; set; }
    public Guid PlanId { get; set; }
    public Guid? RiderId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public TrainingType TrainingType { get; set; }
    public decimal DistanceMetres { get; set; }
    public Intensity Intensity { get; set; }
    public string Surface { get; set; } = "";
    public string Target { get; set; } = "";
    public string Notes { get; set; } = "";
    public SessionStatus Status { get; set; } = SessionStatus.Planned;
    public DateTimeOffset? StartedAt { get; set; }
}
