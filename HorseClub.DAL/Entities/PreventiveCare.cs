namespace HorseClub.DAL.Entities;

public sealed class PreventiveCare : Entity
{
    public Guid HorseId { get; set; }
    public PreventiveCareType Type { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string Notes { get; set; } = "";
    public bool ReminderSent { get; set; }
}
