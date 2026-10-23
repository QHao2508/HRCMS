namespace HorseClub.DAL.Entities;

public sealed class Notification : Entity
{
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? MessageCode { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? DisplayMessage { get; set; }
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public bool Read { get; set; }
}
