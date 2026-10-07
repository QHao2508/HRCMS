namespace HorseClub.DAL.Entities;

public sealed class Injury : Entity
{
    public Guid HorseId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public DateOnly InjuryDate { get; set; }
    public InjuryType Type { get; set; }
    public string BodyLocation { get; set; } = "";
    public InjurySeverity Severity { get; set; }
    public string Cause { get; set; } = "";
    public InjuryStatus Status { get; set; } = InjuryStatus.Active;
    public DateOnly ReviewDate { get; set; }
}
