namespace HorseClub.DAL.Abstractions;
public sealed record StaffReadModel(Guid Id, string UserName, string FirstName, string LastName, string Email, Role Role, bool Active, bool EmailVerified);
public interface IAuthRepository
{
    Task<User?> GetDemoAccountAsync(string email, string userName);
    Task<User?> GetSessionUserAsync(Guid id);
    Task<bool> HasIdentityConflictAsync(string email, string name);
    Task<bool> HasRecentChallengeAsync(Guid userId, ChallengePurpose purpose, DateTimeOffset after);
    Task<List<EmailChallenge>> GetPendingChallengesAsync(Guid userId, ChallengePurpose purpose);
    Task<EmailChallenge?> GetLatestChallengeAsync(Guid userId, ChallengePurpose purpose);
    Task<User?> GetLoginUserAsync(string identifier);
    Task<User?> GetUserByEmailAsync(string email);
    ValueTask<User?> FindUserAsync(Guid id);
    void AddUser(User entity);
    void AddEmailChallenge(EmailChallenge entity);
    void AddEmailMessage(EmailMessage entity);
    Task<bool> HasManagerAsync(CancellationToken token);
    Task<bool> CanConnectAsync(CancellationToken token);
    Task<DataPage<StaffReadModel>> ListStaffAsync(Role? role, bool directory, int page, int size);
}
