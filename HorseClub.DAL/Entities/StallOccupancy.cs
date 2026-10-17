namespace HorseClub.DAL.Entities;

public sealed class StallOccupancy : Entity
{
    public Guid StallId { get; set; }
    public Guid HorseId { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
