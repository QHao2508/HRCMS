namespace HorseClub.DAL.Entities;

public sealed class HorseRegistration : Entity
{
    public Guid OwnerId { get; set; }
    // Response enrichment only; owner names remain authoritative on Users.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? OwnerName { get; set; }
    public string? Name { get; set; }
    public string? Sire { get; set; }
    public string? Dam { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public HorseGender? Gender { get; set; }
    public string? Breed { get; set; }
    public string? RegistrationNumber { get; set; }
    // The legacy registrationNumber response remains an alias for the document number.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? ExternalRegistrationNumber => RegistrationNumber;
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public DateOnly? MeasurementDate { get; set; }
    public string? DeclaredHealth { get; set; }
    public string? HealthNotes { get; set; }
    public DateOnly? BoardingStart { get; set; }
    public DateOnly? BoardingEnd { get; set; }
    public Guid? PreferredHeadTrainerId { get; set; }
    public Guid? PreferredGroomId { get; set; }
    public Guid? PreferredVeterinarianId { get; set; }
    public RegistrationStatus Status { get; set; }
    public string? ReviewReason { get; set; }
    public Guid? ReviewedBy { get; set; }
}
