namespace HorseClub.DAL.Entities;

public sealed class MedicalFollowUp : Entity
{
    public Guid HorseId { get; set; }
    public Guid PreviousRecordId { get; set; }
    public Guid CurrentRecordId { get; set; }
    public bool Clearance { get; set; }
    public string Outcome { get; set; } = "";
}
