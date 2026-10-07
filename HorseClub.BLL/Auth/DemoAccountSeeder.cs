using HorseClub.BLL.Messaging;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Auth;

// Invoked only by the local demo CLI, never exposed through an API route.
public sealed class DemoAccountSeeder(ClubDbContext db)
{
    public sealed record Account(string Email, string UserName, Role Role);

    /// <summary>
    /// Tạo bộ dữ liệu demo theo vai trò/phân công; chỉ chạy công cụ demo được gọi rõ ràng, không thuộc request thường.
    /// </summary>
    /// <param name="accounts">Giá trị kiểu IReadOnlyList&lt;Account&gt; dùng trong Seed.</param>
    /// <param name="password">Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response.</param>
    /// <param name="security">Giá trị kiểu SecurityOptions dùng trong Seed.</param>
    /// <param name="resetDemoPassword">Giá trị kiểu bool dùng trong Seed.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.</remarks>
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
                Email = email,
                UserName = name,
                Role = account.Role,
                FirstName = "BE02",
                LastName = account.Role.ToString(),
                Active = true,
                EmailVerified = true
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
