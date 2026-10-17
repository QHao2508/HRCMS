namespace HorseClub.DAL.Entities;

public sealed class TreatmentPlan : Entity
{
    public Guid HorseId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public Guid? InjuryId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly FollowUpDate { get; set; }
    public string Objective { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Medication { get; set; } = "";
    public string Frequency { get; set; } = "";
    public bool Completed { get; set; }
}
