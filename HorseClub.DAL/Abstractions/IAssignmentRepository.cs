namespace HorseClub.DAL.Abstractions;

public interface IAssignmentRepository
{
    Task<bool> IsAssignedAsync(Guid horseId, Guid staffId, Role? role = null);
    Task<List<StaffAssignment>> GetActiveAsync(Guid horseId, Role role);
    void Add(StaffAssignment assignment);
}
