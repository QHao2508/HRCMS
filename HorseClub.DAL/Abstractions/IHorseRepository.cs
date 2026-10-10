namespace HorseClub.DAL.Abstractions;

public interface IHorseRepository
{
    ValueTask<Horse?> FindAsync(Guid id);
    Task<DataPage<Horse>> ListAsync(HorseScope scope, string? search, HealthStatus? healthStatus, int page, int size);
    Task<Attachment?> GetPhotoAsync(Guid registrationId);
    Task<Measurement?> GetLatestMeasurementAsync(Guid horseId);
    Task<List<StaffAssignment>> GetAssignmentsAsync(Guid horseId);
    Task<StallOccupancy?> GetOccupancyAsync(Guid horseId);
    Task<HorseRegistration> GetRegistrationAsync(Guid id);
    Task<DataPage<Measurement>> ListMeasurementsAsync(Guid horseId, int page, int size);
    void AddMeasurement(Measurement measurement);
    Task<bool> HasActiveSessionAsync(Guid horseId);
    Task<List<StallOccupancy>> GetOccupanciesAsync(Guid horseId);
    Task<bool> HasActiveCareAsync(Guid horseId);
    Task<List<TrainingPlan>> GetOpenPlansAsync(Guid horseId);
    Task<List<TrainingSession>> GetPendingSessionsAsync(Guid horseId);
    Task<List<CareTask>> GetPendingCareAsync(Guid horseId);
    Task<List<StaffAssignment>> GetActiveAssignmentsAsync(Guid horseId);
}
