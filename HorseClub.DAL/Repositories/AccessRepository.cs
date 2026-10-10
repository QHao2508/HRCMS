using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class AccessRepository(ClubDbContext db) : IAccessRepository
{
    public ValueTask<User?> FindUserAsync(Guid id) => db.Users.FindAsync(id);
    public ValueTask<HorseRegistration?> FindRegistrationAsync(Guid id) => db.Registrations.FindAsync(id);
    public Task<bool> HasRiderSessionAsync(Guid horseId, Guid riderId) => db.Sessions.AnyAsync(x => x.HorseId == horseId && x.RiderId == riderId && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.InProgress));
    public Task<bool> HasHistoricalAssignmentAsync(Guid horseId, Guid staffId) => db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == staffId);
}
