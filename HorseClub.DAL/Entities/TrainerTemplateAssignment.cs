namespace HorseClub.DAL.Entities;

public sealed class TrainerTemplateAssignment : Entity
{
    public Guid TemplateId { get; set; }
    public Guid TrainerId { get; set; }
    public Guid AssignedBy { get; set; }
    public bool Active { get; set; } = true;
}
