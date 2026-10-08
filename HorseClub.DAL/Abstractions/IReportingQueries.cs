namespace HorseClub.DAL.Abstractions;

public sealed record DashboardData(int HorseCount, int ActivePlans, int SessionsToday, int OverdueSessions, int CareTasksToday, int RestrictedHorses, int UnreadNotifications);
public sealed record ReportSessionData(Guid Id, Guid HorseId, DateTimeOffset ScheduledAt, SessionStatus Status, decimal DistanceMetres, string Target);
public sealed record ReportCareData(Guid Id, Guid HorseId, CareType Type, CareStatus Status, DateTimeOffset ScheduledAt, decimal? ApprovedPortionKg, decimal? ActualPortionKg);
public sealed record ReportRows(List<ReportSessionData> Sessions, List<ReportCareData> Tasks);

public interface IReportingQueries
{
    Task<DataPage<Notification>> ListNotificationsAsync(Guid recipientId, bool unread, int page, int size);
    Task<Notification?> GetNotificationAsync(Guid recipientId, Guid id);
    Task<DataPage<AuditEvent>> ListAuditAsync(Guid? referenceId, int page, int size);
    Task<DashboardData> DashboardAsync(HorseScope scope, Guid userId, Guid? riderId, Guid? groomId, DateTimeOffset now, DateTimeOffset start, DateTimeOffset end);
    Task<ReportRows> ReportRowsAsync(HorseScope scope, Guid? horseId, Guid? riderId, Guid? groomId, DateTimeOffset start, DateTimeOffset end, int limit);
    Task<List<SessionResult>> ResultsAsync(Guid[] sessionIds);
    Task<int> MedicalCountAsync(HorseScope scope, Guid? horseId, DateTimeOffset start, DateTimeOffset end);
    Task<List<MedicalRecord>> ClinicalAsync(HorseScope scope, Guid? horseId, DateTimeOffset start, DateTimeOffset end);
}
