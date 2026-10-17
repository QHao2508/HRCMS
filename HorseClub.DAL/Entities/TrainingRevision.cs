namespace HorseClub.DAL.Entities;

public sealed class TrainingRevision : Entity
{
    public Guid PlanId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid ActorId { get; set; }
    public string Snapshot { get; set; } = "";
}
