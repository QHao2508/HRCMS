using Horse_BackEnd.Domain;

namespace Horse_BackEnd.Contracts;

// Named projections preserve the existing JSON contract.
public sealed record UserResponse(
    Guid Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    string Phone,
    string Address,
    Role Role,
    bool EmailVerified,
    bool Active);

public sealed record RegistrationReviewResponse(
    HorseRegistration Registration,
    Guid? HorseId);

public sealed record AttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Length,
    AttachmentType Type,
    string? CertificateNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate);

public sealed record AttachmentCreatedResponse(
    Guid Id,
    string FileName,
    AttachmentType Type,
    long Length);

public sealed record LoginErrorResponse(
    string Error);

public sealed record VerificationResponse(
    bool Verified);

public sealed record MessageResponse(
    string Message);

public sealed record StaffResponse(
    Guid Id,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    Role Role,
    bool Active,
    bool EmailVerified);

public sealed record StaffDirectoryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    Role Role);

public sealed record PasswordChangedResponse(
    bool Changed);

public sealed record CareTaskSummaryResponse(
    Guid Id,
    Guid HorseId,
    Guid GroomId,
    CareType Type,
    DateTimeOffset ScheduledAt,
    CareStatus Status,
    DateTimeOffset? CompletedAt,
    decimal? ApprovedPortionKg,
    decimal? ActualPortionKg,
    string? Instructions,
    string? Notes);

public sealed record StallSummaryResponse(
    Guid Id,
    Guid StableId,
    string Name,
    CleaningStatus CleaningStatus);

public sealed record HorseDetailResponse(
    Horse Horse,
    Measurement? LatestMeasurement,
    List<StaffAssignment> Assignments,
    StallOccupancy? CurrentStall,
    HorsePreferencesResponse Preferences);

public sealed record HorsePreferencesResponse(
    Guid? PreferredHeadTrainerId,
    Guid? PreferredGroomId,
    Guid? PreferredVeterinarianId);

public sealed record IncidentPhotoResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Length);

public sealed record IncidentPhotoCreatedResponse(
    Guid Id,
    string FileName,
    long Length);

public sealed record StockMovementResponse(
    InventoryItem Item,
    StockMovement Movement);

public sealed record MedicalSummaryResponse(
    HealthStatus HealthStatus,
    List<RestrictionSummaryResponse> Restrictions);

public sealed record RestrictionSummaryResponse(
    Guid Id,
    Guid MedicalRecordId,
    bool TrainingLock,
    bool BlockAllTraining,
    Intensity? MaxIntensity,
    decimal? MaxDistanceMetres,
    bool NoSprint,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string Reason);

public sealed record PreventiveCareSummaryResponse(
    Guid Id,
    Guid HorseId,
    PreventiveCareType Type,
    DateOnly DueDate,
    DateOnly? CompletedDate);

public sealed record DashboardResponse(
    int HorseCount,
    int ActivePlans,
    int SessionsToday,
    int OverdueSessions,
    int CareTasksToday,
    int RestrictedHorses,
    int UnreadNotifications);

public sealed record ReportSeriesResponse(
    string Period,
    int Sessions,
    int Completed,
    decimal PlannedDistanceMetres,
    decimal ActualDistanceMetres);

public sealed record ReportResponse(
    DateTimeOffset From,
    DateTimeOffset To,
    ReportGrouping GroupBy,
    bool NoData,
    ReportKpiResponse Kpi,
    IEnumerable<ReportSeriesResponse> Series,
    IEnumerable<ReportTrainingResponse> Training,
    IEnumerable<ReportCareResponse> Care,
    List<MedicalRecord>? Clinical);

public sealed record PageResponse<T>(List<T> Items, int Page, int PageSize, int Total);
public sealed record ApiErrorResponse(string Type, string Title, int Status, string Detail, Guid? ReferenceId, string TraceId);
public sealed record HealthResponse(string Status);

public sealed record ReportKpiResponse(
    int Sessions,
    int Completed,
    decimal CompletionRate,
    decimal ActualDistanceMetres,
    decimal ActualTimeSeconds,
    int CareTasks,
    int CompletedCare,
    int? MedicalExaminations);

public sealed record ReportTrainingResponse(
    Guid Id,
    Guid HorseId,
    DateTimeOffset ScheduledAt,
    SessionStatus Status,
    decimal DistanceMetres,
    string Target,
    SessionResult? Result);

public sealed record ReportCareResponse(
    Guid Id,
    Guid HorseId,
    CareType Type,
    CareStatus Status,
    DateTimeOffset ScheduledAt,
    decimal? ApprovedPortionKg,
    decimal? ActualPortionKg);

public sealed record PlanDetailResponse(
    TrainingPlan Plan,
    List<TrainingSession> Sessions,
    List<MedicalRestriction> Restrictions,
    int SessionPage,
    int SessionPageSize,
    int SessionTotal);

public sealed record SessionDetailResponse(
    TrainingSession Session,
    SessionResult? Result,
    TrainerEvaluation? Evaluation);
