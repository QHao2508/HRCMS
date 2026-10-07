namespace HorseClub.DAL.Entities;

public sealed class CareTask : Entity
{
    public Guid HorseId { get; set; }
    public Guid GroomId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public CareType Type { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public string Instructions { get; set; } = "";
    public decimal? ApprovedPortionKg { get; set; }
    public decimal? ActualPortionKg { get; set; }
    public CareStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
}
