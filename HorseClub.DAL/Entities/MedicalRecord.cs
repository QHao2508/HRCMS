namespace HorseClub.DAL.Entities;

public sealed class MedicalRecord : Entity
{
    public Guid? SupersedesRecordId { get; set; }
    public Guid HorseId { get; set; }
    public Guid VeterinarianId { get; set; }
    public DateTimeOffset ExaminationAt { get; set; }
    public string Reason { get; set; } = "";
    public string Symptoms { get; set; } = "";
    public string Findings { get; set; } = "";
    public string Diagnosis { get; set; } = "";
    public HealthStatus HealthStatus { get; set; }
    public string Notes { get; set; } = "";
}
