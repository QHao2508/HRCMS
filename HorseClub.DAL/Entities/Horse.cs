namespace HorseClub.DAL.Entities;

public sealed class Horse : Entity
{
    public Guid RegistrationId { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Sire { get; set; } = "";
    public string Dam { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public HorseGender Gender { get; set; }
    public string Breed { get; set; } = "";
    public string? RegistrationNumber { get; set; }
    public DateOnly BoardingStart { get; set; }
    public DateOnly? BoardingEnd { get; set; }
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Monitoring;
    public bool Archived { get; set; }
}
