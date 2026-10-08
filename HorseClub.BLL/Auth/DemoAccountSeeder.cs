using HorseClub.BLL.Messaging;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;

namespace HorseClub.BLL.Auth;

// Invoked only by an explicitly selected demo CLI, never exposed through an API route.
public sealed class DemoAccountSeeder(IAuthRepository repository, IUnitOfWork unitOfWork)
{
    public sealed record Account(string Email, string UserName, Role Role,
        string? FirstName = null, string? LastName = null, string? Phone = null, string? Address = null);

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
        await using var transaction = await unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
        var created = 0;
        foreach (var account in accounts)
        {
            var email = AuthenticationService.Normalize(account.Email);
            var name = AuthenticationService.Normalize(account.UserName);
            var existing = await repository.GetDemoAccountAsync(email, name);
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
                FirstName = account.FirstName?.Trim() ?? "BE02",
                LastName = account.LastName?.Trim() ?? account.Role.ToString(),
                Phone = account.Phone?.Trim() ?? "",
                Address = account.Address?.Trim() ?? "",
                Active = true,
                EmailVerified = true
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            repository.AddUser(user);
            created++;
        }
        await unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();
        return created;
    }
}
