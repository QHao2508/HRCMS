namespace HorseClub.DAL.Entities;

public sealed class MedicalRestriction : Entity
{
    public Guid HorseId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public bool TrainingLock { get; set; }
    public bool BlockAllTraining { get; set; }
    public Intensity? MaxIntensity { get; set; }
    public decimal? MaxDistanceMetres { get; set; }
    public bool NoSprint { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public string Reason { get; set; } = "";
    public bool Cleared { get; set; }
}
