namespace HorseClub.DAL.Entities;

public sealed class StaffTimeOff : Entity
{
    public Guid StaffId { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}
