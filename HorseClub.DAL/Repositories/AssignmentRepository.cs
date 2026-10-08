using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class AssignmentRepository(ClubDbContext db) : IAssignmentRepository
{
    public Task<bool> IsAssignedAsync(Guid horseId, Guid staffId, Role? role = null)
        => db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == staffId && x.Active && (!role.HasValue || x.Role == role));
    public Task<List<StaffAssignment>> GetActiveAsync(Guid horseId, Role role)
        => db.Assignments.Where(x => x.HorseId == horseId && x.Role == role && x.Active).ToListAsync();
    public void Add(StaffAssignment assignment) => db.Assignments.Add(assignment);
}
