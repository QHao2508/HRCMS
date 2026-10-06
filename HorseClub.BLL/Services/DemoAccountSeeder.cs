using HorseClub.BLL.Messaging;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Services;

// Invoked only by the local demo CLI, never exposed through an API route.
public sealed class DemoAccountSeeder(ClubDbContext db)
{
    public sealed record Account(string Email, string UserName, Role Role);

    public async Task<int> Seed(IReadOnlyList<Account> accounts, string password, SecurityOptions security, bool resetDemoPassword = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var hasher = new PasswordHasher<User>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var created = 0;
        foreach (var account in accounts)
        {
            var email = AuthenticationService.Normalize(account.Email);
            var name = AuthenticationService.Normalize(account.UserName);
            var existing = await db.Users.SingleOrDefaultAsync(u => u.Email == email || u.UserName == name);
            if (existing is not null)
            {
                // Matching accounts stay unchanged unless the local CLI explicitly requests a password reset.
                Ensure.That(existing.Email == email && existing.UserName == name && existing.Role == account.Role
                    && existing.Active && existing.EmailVerified
                    && (resetDemoPassword || hasher.VerifyHashedPassword(existing, existing.PasswordHash, password) != PasswordVerificationResult.Failed),
                    Messages.Get(MessageKey.EmailOrUsernameIsAlreadyRegistered), 409, "account_exists");
                if (resetDemoPassword)
                {
                    existing.PasswordHash = hasher.HashPassword(existing, password);
                    existing.SecurityStamp = Guid.NewGuid().ToString();
                    existing.FailedLogins = 0;
                    existing.LockedUntil = null;
                }
                continue;
            }
            if (!resetDemoPassword) AuthenticationService.CheckPassword(password, security);
            var user = new User
            {
                Email = email, UserName = name, Role = account.Role,
                FirstName = "BE02", LastName = account.Role.ToString(),
                Active = true, EmailVerified = true
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            created++;
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return created;
    }
}
