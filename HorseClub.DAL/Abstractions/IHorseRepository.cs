namespace HorseClub.DAL.Abstractions;

/// <summary>Số ngựa theo tình trạng sức khỏe và lựa chọn bộ lọc (giống, chuồng đang ở) trong phạm vi người xem.</summary>
public sealed record HorseListSummary(Dictionary<HealthStatus, int> Health, List<string> Breeds, List<HorseStallOption> Stalls);
public sealed record HorseStallOption(Guid Id, string Name);

public interface IHorseRepository
{
    ValueTask<Horse?> FindAsync(Guid id);
    Task<(Horse? Horse, bool Assigned)> FindWithAssignmentAsync(Guid id, Guid staffId);
    Task<DataPage<Horse>> ListAsync(HorseScope scope, string? search, HealthStatus? healthStatus, int page, int size, DateTimeOffset? now = null, string? breed = null, Guid? stallId = null);
    Task<HorseListSummary> SummaryAsync(HorseScope scope);
    Task<Attachment?> GetPhotoAsync(Guid registrationId);
    Task<Measurement?> GetLatestMeasurementAsync(Guid horseId);
    Task<List<StaffAssignment>> GetAssignmentsAsync(Guid horseId);
    Task<StallOccupancy?> GetOccupancyAsync(Guid horseId);
    Task<HorseRegistration> GetRegistrationAsync(Guid id);
    Task<HorseDetailData> GetDetailAsync(Guid horseId, Guid registrationId);
    Task<DataPage<Measurement>> ListMeasurementsAsync(Guid horseId, int page, int size);
    void AddMeasurement(Measurement measurement);
    Task<bool> HasActiveSessionAsync(Guid horseId);
    Task<List<StallOccupancy>> GetOccupanciesAsync(Guid horseId);
    ValueTask<Stall?> FindStallAsync(Guid id);
    Task<bool> HasActiveCareAsync(Guid horseId);
    Task<List<TrainingPlan>> GetOpenPlansAsync(Guid horseId);
    Task<List<TrainingSession>> GetPendingSessionsAsync(Guid horseId);
    Task<List<CareTask>> GetPendingCareAsync(Guid horseId);
    Task<List<StaffAssignment>> GetActiveAssignmentsAsync(Guid horseId);
}
