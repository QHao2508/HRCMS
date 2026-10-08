using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
namespace HorseClub.DAL.Repositories;
public sealed class AuthRepository(ClubDbContext db) : IAuthRepository
{
    public Task<User?> GetDemoAccountAsync(string email, string userName) => db.Users.SingleOrDefaultAsync(u => u.Email == email || u.UserName == userName);
    public Task<User?> GetSessionUserAsync(Guid id) => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    public Task<bool> HasIdentityConflictAsync(string email, string name) => db.Users.AnyAsync(x => x.Email == email || x.UserName == name || x.Email == name || x.UserName == email);
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
        var items = await query.OrderBy(x => x.UserName).Select(x => new StaffReadModel(x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.Role, x.Active, x.EmailVerified)).Skip((page - 1) * size).Take(size).ToListAsync();
        return new(items, await query.CountAsync());
    }
}
