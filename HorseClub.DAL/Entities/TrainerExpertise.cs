namespace HorseClub.DAL.Entities;

public sealed class TrainerExpertise : Entity
{
    public Guid StaffId { get; set; }
    public TrainingType TrainingType { get; set; }
    // 1: qualified; 2: proficient; 3: advanced. Assigned by the manager, never inferred from race results.
    public int Level { get; set; }
}
