namespace HorseClub.DAL.Entities;

public sealed class Stall : Entity
{
    public Guid StableId { get; set; }
    public string Name { get; set; } = "";
    public CleaningStatus CleaningStatus { get; set; } = CleaningStatus.NeedsCleaning;
}
