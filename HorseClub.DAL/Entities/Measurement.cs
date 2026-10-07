namespace HorseClub.DAL.Entities;

public sealed class Measurement : Entity
{
    public Guid HorseId { get; set; }
    public DateOnly Date { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
}
