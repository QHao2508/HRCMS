namespace HorseClub.DAL.Entities;

public sealed class Notification : Entity
{
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public bool Read { get; set; }
}
