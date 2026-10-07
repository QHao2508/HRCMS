namespace HorseClub.DAL.Entities;

public sealed class StaffAssignment : Entity
{
    public Guid HorseId { get; set; }
    public Guid StaffId { get; set; }
    public Role Role { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}
