namespace HorseClub.DAL.Entities;

public sealed class TrainingPlan : Entity
{
    public Guid HorseId { get; set; }
    public Guid TrainerId { get; set; }
    public Guid TemplateId { get; set; }
    public string Goal { get; set; } = "";
    public string Phase { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Notes { get; set; } = "";
    public PlanStatus Status { get; set; } = PlanStatus.Active;
}
