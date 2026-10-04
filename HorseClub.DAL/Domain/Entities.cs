namespace Horse_BackEnd.Domain;

public enum Role { HorseOwner = 0, ClubManager = 1, HeadTrainer = 2, Trainer = 3, WorkRider = 4, Veterinarian = 5, Groom = 6 }
public enum RegistrationStatus { Draft = 0, PendingReview = 1, RevisionRequired = 2, Approved = 3, Cancelled = 4 }
public enum HealthStatus { Fit = 0, Monitoring = 1, Injured = 2, Isolated = 3 }
public enum PlanStatus { Active = 0, Paused = 1, Completed = 2, Archived = 3 }
public enum SessionStatus { Planned = 0, Assigned = 1, InProgress = 2, Completed = 3, Skipped = 4, IssueReported = 5 }
public enum CareStatus { Pending = 0, InProgress = 1, Completed = 2, Skipped = 3, IssueReported = 4 }
public enum Intensity { Light = 0, Moderate = 1, Heavy = 2 }
public enum CareType { Feeding = 0, Cleaning = 1, Bathing = 2, Grooming = 3, WaterCheck = 4, IceBath = 5, Treatment = 6 }

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public long Version { get; set; }
}

public sealed class User : Entity
{
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? NationalIdProtected { get; set; }
    public Role Role { get; set; }
    public bool EmailVerified { get; set; }
    public bool Active { get; set; } = true;
    public int FailedLogins { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
}

public sealed class EmailChallenge : Entity
{
    public Guid UserId { get; set; }
    public ChallengePurpose Purpose { get; set; }
    public string CodeHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool Consumed { get; set; }
}

public sealed class EmailMessage : Entity
{
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public Guid? ChallengeId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? DiscardedAt { get; set; }
}

public sealed class HorseRegistration : Entity
{
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Sire { get; set; } = "";
    public string Dam { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public HorseGender Gender { get; set; }
    public string Breed { get; set; } = "";
    public string? RegistrationNumber { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public DateOnly MeasurementDate { get; set; }
    public string DeclaredHealth { get; set; } = "";
    public string HealthNotes { get; set; } = "";
    public DateOnly BoardingStart { get; set; }
    public DateOnly? BoardingEnd { get; set; }
    public Guid? PreferredHeadTrainerId { get; set; }
    public Guid? PreferredGroomId { get; set; }
    public Guid? PreferredVeterinarianId { get; set; }
    public RegistrationStatus Status { get; set; }
    public string? ReviewReason { get; set; }
    public Guid? ReviewedBy { get; set; }
}

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

public sealed class Measurement : Entity
{
    public Guid HorseId { get; set; }
    public DateOnly Date { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
}

public sealed class StaffAssignment : Entity
{
    public Guid HorseId { get; set; }
    public Guid StaffId { get; set; }
    public Role Role { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Notes { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class TrainingTemplate : Entity
{
    public string Name { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Phase { get; set; } = "";
    public decimal DistanceMetres { get; set; }
    public Intensity Intensity { get; set; }
    public string Surface { get; set; } = "";
    public int FrequencyPerWeek { get; set; }
    public string Notes { get; set; } = "";
    public bool Archived { get; set; }
}

public sealed class TrainingPlan : Entity
{
    public Guid HorseId { get; set; }
    public Guid TrainerId { get; set; }
    public Guid TemplateId { get; set; }
    public string Goal { get; set; } = "";
    public string Phase { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Notes { get; set; } = "";
    public PlanStatus Status { get; set; } = PlanStatus.Active;
}

public sealed class TrainingSession : Entity
{
    public Guid HorseId { get; set; }
    public Guid PlanId { get; set; }
    public Guid? RiderId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public TrainingType TrainingType { get; set; }
    public decimal DistanceMetres { get; set; }
    public Intensity Intensity { get; set; }
    public string Surface { get; set; } = "";
    public string Target { get; set; } = "";
    public string Notes { get; set; } = "";
    public SessionStatus Status { get; set; } = SessionStatus.Planned;
    public DateTimeOffset? StartedAt { get; set; }
}

public sealed class SessionResult : Entity
{
    public Guid SessionId { get; set; }
    public decimal DistanceMetres { get; set; }
    public decimal TimeSeconds { get; set; }
    public decimal SpeedMetresPerSecond { get; set; }
    public int? HeartRate { get; set; }
    public Intensity Intensity { get; set; }
    public string Feedback { get; set; } = "";
    public bool AbnormalObservation { get; set; }
}

public sealed class TrainerEvaluation : Entity
{
    public Guid SessionId { get; set; }
    public Guid TrainerId { get; set; }
    public string Comment { get; set; } = "";
    public bool AdjustFutureSessions { get; set; }
}

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

public sealed class MedicalFollowUp : Entity
{
    public Guid HorseId { get; set; }
    public Guid PreviousRecordId { get; set; }
    public Guid CurrentRecordId { get; set; }
    public bool Clearance { get; set; }
    public string Outcome { get; set; } = "";
}

public sealed class Stable : Entity { public string Name { get; set; } = ""; }
public sealed class Stall : Entity
{
    public Guid StableId { get; set; }
    public string Name { get; set; } = "";
    public CleaningStatus CleaningStatus { get; set; } = CleaningStatus.NeedsCleaning;
}
public sealed class StallOccupancy : Entity
{
    public Guid StallId { get; set; }
    public Guid HorseId { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
}
public sealed class CareTask : Entity
{
    public Guid HorseId { get; set; }
    public Guid GroomId { get; set; }
    public Guid? TreatmentPlanId { get; set; }
    public CareType Type { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public string Instructions { get; set; } = "";
    public decimal? ApprovedPortionKg { get; set; }
    public decimal? ActualPortionKg { get; set; }
    public CareStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Notes { get; set; } = "";
}
public sealed class Incident : Entity
{
    public Guid HorseId { get; set; }
    public Guid ReporterId { get; set; }
    public Guid? SessionId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public IncidentType Type { get; set; }
    public string Description { get; set; } = "";
    public IncidentSeverity Severity { get; set; }
    public Role RoutedTo { get; set; }
    public bool Resolved { get; set; }
}
public sealed class PreventiveCare : Entity
{
    public Guid HorseId { get; set; }
    public PreventiveCareType Type { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string Notes { get; set; } = "";
    public bool ReminderSent { get; set; }
}
public sealed class InventoryItem : Entity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
    public bool Archived { get; set; }
}
public sealed class StockMovement : Entity
{
    public Guid ItemId { get; set; }
    public Guid ActorId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
}
public sealed class ReplenishmentRequest : Entity
{
    public Guid ItemId { get; set; }
    public Guid RequestedBy { get; set; }
    public decimal Quantity { get; set; }
    public ReplenishmentStatus Status { get; set; } = ReplenishmentStatus.Pending;
    public string Notes { get; set; } = "";
}
public sealed class Attachment : Entity
{
    public Guid RegistrationId { get; set; }
    public Guid UploadedBy { get; set; }
    public string FileName { get; set; } = "";
    public string StorageName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public AttachmentType Type { get; set; }
    public string? CertificateNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
public sealed class IncidentPhoto : Entity
{
    public Guid IncidentId { get; set; }
    public Guid UploadedBy { get; set; }
    public string FileName { get; set; } = "";
    public string StorageName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
}
public sealed class TrainingRevision : Entity
{
    public Guid PlanId { get; set; }
    public Guid? SessionId { get; set; }
    public Guid ActorId { get; set; }
    public string Snapshot { get; set; } = "";
}
public sealed class Notification : Entity
{
    public Guid RecipientId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public bool Read { get; set; }
}
public sealed class AuditEvent : Entity
{
    public Guid ActorId { get; set; }
    public AuditAction Action { get; set; }
    public Guid ReferenceId { get; set; }
    public string Detail { get; set; } = "";
}
