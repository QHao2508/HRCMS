using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Horse_BackEnd.Domain;

namespace Horse_BackEnd.Contracts;

public sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(254)] string Email,
    [property: Required, StringLength(80, MinimumLength = 3)] string UserName,
    [property: Required, MaxLength(100)] string FirstName,
    [property: Required, MaxLength(100)] string LastName,
    [property: Required, MaxLength(30)] string Phone,
    [property: Required, MaxLength(500)] string Address,
    [property: Required] string Password,
    [property: Required] string ConfirmPassword, string? NationalId = null);
public sealed record LoginRequest([property: Required] string Email, [property: Required] string Password);
public sealed record RefreshRequest([property: Required] string RefreshToken);
public sealed record EmailRequest([property: Required, EmailAddress] string Email);
public sealed record VerifyRequest([property: Required, EmailAddress] string Email, [property: Required] string Code);
public sealed record ResetRequest([property: Required, EmailAddress] string Email, [property: Required] string Code,
    [property: Required] string Password, [property: Required] string ConfirmPassword);
public sealed record StaffRequest([property: Required, EmailAddress] string Email, [property: Required, StringLength(80, MinimumLength = 3)] string UserName,
    [property: Required] string FirstName, [property: Required] string LastName, [property: Required] string Phone,
    [property: Required] string Address, [property: JsonRequired] Role Role);
public sealed record ActiveRequest([property: JsonRequired] bool Active);
public sealed record ReviewRequest([property: JsonRequired] bool Approve, [property: MaxLength(2000)] string? Reason);
public sealed record ReasonRequest([property: Required, MaxLength(2000)] string Reason);

public sealed record RegistrationRequest(
    [property: Required, MaxLength(200)] string Name,
    [property: Required, MaxLength(200)] string Sire,
    [property: Required, MaxLength(200)] string Dam,
    DateOnly DateOfBirth, [property: JsonRequired] HorseGender Gender,
    [property: Required, MaxLength(100)] string Breed, [property: MaxLength(100)] string? RegistrationNumber,
    [property: Range(1, 300)] decimal HeightCm, [property: Range(1, 2000)] decimal WeightKg, DateOnly MeasurementDate,
    [property: Required, MaxLength(1000)] string DeclaredHealth, [property: MaxLength(2000)] string HealthNotes,
    DateOnly BoardingStart, DateOnly? BoardingEnd, Guid? PreferredHeadTrainerId, Guid? PreferredGroomId, Guid? PreferredVeterinarianId);
public sealed record AssignmentRequest(Guid StaffId, [property: JsonRequired] Role Role, DateOnly StartDate, [property: MaxLength(2000)] string Notes);
public sealed record MeasurementRequest(DateOnly Date, [property: Range(1, 300)] decimal HeightCm, [property: Range(1, 2000)] decimal WeightKg);
public sealed record TemplateRequest([property: Required, MaxLength(200)] string Name, [property: Required] string Goal,
    [property: Required] string Phase, [property: Range(1, 100000)] decimal DistanceMetres, [property: JsonRequired] Intensity Intensity,
    [property: Required] string Surface, [property: Range(1, 21)] int FrequencyPerWeek, string Notes);
public sealed record PlanRequest(Guid HorseId, Guid TemplateId, [property: Required] string Goal, [property: Required] string Phase,
    DateOnly StartDate, DateOnly EndDate, string Notes);
public sealed record PlanStatusRequest([property: JsonRequired] PlanStatus Status);
public sealed record SessionRequest(DateTimeOffset ScheduledAt, [property: JsonRequired] TrainingType TrainingType,
    [property: Range(1, 100000)] decimal DistanceMetres, [property: JsonRequired] Intensity Intensity, [property: Required] string Surface,
    [property: Required] string Target, string Notes, Guid? RiderId);
public sealed record RiderRequest(Guid RiderId);
public sealed record ResultRequest([property: Range(0, 100000)] decimal DistanceMetres,
    [property: Range(0.001, 86400)] decimal TimeSeconds, [property: Range(1, 300)] int? HeartRate,
    [property: JsonRequired] Intensity Intensity, [property: Required] string Feedback, bool AbnormalObservation);
public sealed record EvaluationRequest([property: Required] string Comment, bool AdjustFutureSessions);
public sealed record MedicalRequest(DateTimeOffset ExaminationAt, [property: Required] string Reason,
    [property: Required] string Symptoms, [property: Required] string Findings, [property: Required] string Diagnosis,
    [property: JsonRequired] HealthStatus HealthStatus, string Notes);
public sealed record RestrictionRequest(Guid MedicalRecordId, bool TrainingLock, bool BlockAllTraining, Intensity? MaxIntensity,
    [property: Range(0, 100000)] decimal? MaxDistanceMetres, bool NoSprint,
    DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil, [property: Required] string Reason);
public sealed record InjuryRequest(Guid MedicalRecordId, DateOnly InjuryDate, [property: JsonRequired] InjuryType Type,
    [property: Required] string BodyLocation, [property: JsonRequired] InjurySeverity Severity, [property: Required] string Cause, DateOnly ReviewDate);
public sealed record TreatmentRequest(Guid MedicalRecordId, Guid? InjuryId, DateOnly StartDate, DateOnly EndDate,
    DateOnly FollowUpDate, [property: Required] string Objective, [property: Required] string Instructions,
    string Medication, [property: Required] string Frequency);
public sealed record FollowUpRequest(Guid PreviousRecordId, [property: Required] MedicalRequest Examination, bool Clearance, [property: Required] string Outcome);
public sealed record CareRequest(Guid HorseId, Guid GroomId, Guid? TreatmentPlanId, [property: JsonRequired] CareType Type,
    DateTimeOffset ScheduledAt, [property: Required] string Instructions, [property: Range(0, 100)] decimal? ApprovedPortionKg);
public sealed record CareCompletionRequest([property: JsonRequired] CareStatus Status, [property: Range(0, 100)] decimal? ActualPortionKg, [property: Required] string Notes);
public sealed record IncidentRequest(Guid HorseId, DateTimeOffset OccurredAt, [property: JsonRequired] IncidentType Type,
    [property: Required] string Description, [property: JsonRequired] IncidentSeverity Severity, [property: JsonRequired] Role RoutedTo);
public sealed record PreventiveRequest([property: JsonRequired] PreventiveCareType Type, DateOnly DueDate, string Notes);
public sealed record PreventiveCompletionRequest(DateOnly CompletedDate, string Notes);
public sealed record NameRequest([property: Required, MaxLength(100)] string Name);
public sealed record StallRequest(Guid StableId, [property: Required, MaxLength(100)] string Name);
public sealed record OccupancyRequest(Guid HorseId);
public sealed record InventoryRequest([property: Required] string Name, [property: Required] string Category,
    [property: Required] string Unit, [property: Range(0, 1000000)] decimal MinimumStock);
public sealed record MovementRequest(decimal Quantity, [property: Required] string Reason);
public sealed record ReplenishmentRequestDto(Guid ItemId, [property: Range(0.001, 1000000)] decimal Quantity, string Notes);
public sealed record ReplenishmentReviewRequest([property: JsonRequired] bool Approve, string Notes);
