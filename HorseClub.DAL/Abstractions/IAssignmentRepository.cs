namespace HorseClub.DAL.Abstractions;

public interface IAssignmentRepository
{
    Task<bool> IsAssignedAsync(Guid horseId, Guid staffId, Role? role = null);
    Task<List<StaffAssignment>> GetActiveAsync(Guid horseId, Role role);
    Task<bool> HasInProgressCareAsync(Guid horseId, Guid groomId);
    Task<List<CareTask>> GetInProgressCareAsync(Guid horseId, Guid groomId);
    Task<List<CareTask>> GetPendingCareAsync(Guid horseId, Guid groomId);
    void Add(StaffAssignment assignment);
}
