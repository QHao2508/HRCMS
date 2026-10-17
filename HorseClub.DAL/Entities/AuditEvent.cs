namespace HorseClub.DAL.Entities;

public sealed class AuditEvent : Entity
{
    public Guid ActorId { get; set; }
    public AuditAction Action { get; set; }
    public Guid ReferenceId { get; set; }
    public string Detail { get; set; } = "";
}
