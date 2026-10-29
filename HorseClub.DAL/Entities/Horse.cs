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
    // Legacy persisted name retained for compatibility. This is the club-issued code.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? SystemRegistrationCode => RegistrationNumber;
    // Enriched from the approved registration; never persisted as a second copy.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? ExternalRegistrationNumber { get; set; }
    // List-only enrichment for GET /api/horses (null/false on other endpoints); never persisted.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? StallName { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? LeadTrainerName { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTimeOffset? NextSessionAt { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasPhoto { get; set; }
    public DateOnly BoardingStart { get; set; }
    public DateOnly? BoardingEnd { get; set; }
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Monitoring;
    public bool Archived { get; set; }
    public HorseArchiveCategory? ArchiveCategory { get; set; }
    public string? ArchiveReason { get; set; }
    public Guid? ArchivedBy { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
