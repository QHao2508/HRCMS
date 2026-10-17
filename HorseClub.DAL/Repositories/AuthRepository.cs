using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
namespace HorseClub.DAL.Repositories;
public sealed class AuthRepository(ClubDbContext db) : IAuthRepository
{
    public Task<User?> GetDemoAccountAsync(string email, string userName) => db.Users.SingleOrDefaultAsync(u => u.Email == email || u.UserName == userName);
    public Task<User?> GetSessionUserAsync(Guid id) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    public Task<bool> HasIdentityConflictAsync(string email, string name) => db.Users.AnyAsync(x => x.Email == email || x.UserName == name || x.Email == name || x.UserName == email);
    public Task<bool> EmailExistsAsync(string email) => db.Users.AnyAsync(x => x.Email == email || x.UserName == email);
    public Task<bool> UsernameExistsAsync(string name) => db.Users.AnyAsync(x => x.UserName == name || x.Email == name);
    public async Task<StaffWorkloadReadModel> GetWorkloadAsync(Guid staffId)
    {
        var assignments = from a in db.Assignments join h in db.Horses on a.HorseId equals h.Id where a.Active && a.StaffId == staffId && !h.Archived select new { Kind = "Assignment", HorseId = h.Id, ReferenceId = a.Id, HorseName = h.Name, State = a.Role.ToString() };
        var sessions = from s in db.Sessions join h in db.Horses on s.HorseId equals h.Id where s.RiderId == staffId && !h.Archived && (s.Status == SessionStatus.Assigned || s.Status == SessionStatus.InProgress) select new { Kind = "Session", HorseId = h.Id, ReferenceId = s.Id, HorseName = h.Name, State = s.Status.ToString() };
        var care = from t in db.CareTasks join h in db.Horses on t.HorseId equals h.Id where t.GroomId == staffId && !h.Archived && (t.Status == CareStatus.Pending || t.Status == CareStatus.InProgress) select new { Kind = "Care", HorseId = h.Id, ReferenceId = t.Id, HorseName = h.Name, State = t.Status.ToString() };
        var preview = await assignments.OrderBy(x=>x.ReferenceId).Take(20).ToListAsync();
        preview.AddRange(await sessions.OrderBy(x=>x.ReferenceId).Take(20).ToListAsync());
        preview.AddRange(await care.OrderBy(x=>x.ReferenceId).Take(20).ToListAsync());
        return new(await assignments.CountAsync(),await sessions.CountAsync(),await care.CountAsync(),preview.Select(x=>new StaffWorkItem(x.Kind,x.HorseId,x.ReferenceId,x.HorseName,x.State)).ToList());
    }
    public Task<bool> HasRecentChallengeAsync(Guid userId, ChallengePurpose purpose, DateTimeOffset after) => db.Challenges.AnyAsync(x => x.UserId == userId && x.Purpose == purpose && x.CreatedAt > after);
    public Task<List<EmailChallenge>> GetPendingChallengesAsync(Guid userId, ChallengePurpose purpose) => db.Challenges.Where(x => x.UserId == userId && x.Purpose == purpose && !x.Consumed).ToListAsync();
    public Task<EmailChallenge?> GetLatestChallengeAsync(Guid userId, ChallengePurpose purpose) => db.Challenges.Where(x => x.UserId == userId && x.Purpose == purpose && !x.Consumed).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
    public Task<User?> GetLoginUserAsync(string identifier) => db.Users.Where(x => x.Email == identifier || x.UserName == identifier)
            .OrderByDescending(x => x.Email == identifier).FirstOrDefaultAsync();
    public Task<User?> GetUserByEmailAsync(string email) => db.Users.SingleOrDefaultAsync(x => x.Email == email);
    public ValueTask<User?> FindUserAsync(Guid id) => db.Users.FindAsync(id);
    public void AddUser(User entity) => db.Users.Add(entity);
    public void AddEmailChallenge(EmailChallenge entity) => db.Challenges.Add(entity);
    public void AddEmailMessage(EmailMessage entity) => db.EmailMessages.Add(entity);
    public Task<bool> HasManagerAsync(CancellationToken token) => db.Users.AnyAsync(x => x.Role == Role.ClubManager, token);
    public Task<bool> CanConnectAsync(CancellationToken token) => db.Database.CanConnectAsync(token);
    public async Task<DataPage<StaffReadModel>> ListStaffAsync(Role? role, bool directory, int page, int size)
    {
        var query = db.Users.Where(x => x.Role != Role.HorseOwner);
        if (directory) query = query.Where(x => x.Active && x.Role != Role.ClubManager);
        if (role.HasValue) query = query.Where(x => x.Role == role);
        return await query.OrderBy(x => x.UserName).Select(x => new StaffReadModel(x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.Role, x.Active, x.EmailVerified)).ToPageAsync(page, size);
    }
}
