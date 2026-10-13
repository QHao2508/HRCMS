namespace HorseClub.DAL.Entities;

public sealed class StaffWorkingHours : Entity
{
    public Guid StaffId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
}
