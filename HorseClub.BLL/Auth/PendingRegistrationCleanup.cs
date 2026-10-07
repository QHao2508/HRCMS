using HorseClub.DAL.Data;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Auth;

/// <summary>Deletes unused self-registered accounts and their verification data in the caller's transaction.</summary>
public sealed class PendingRegistrationCleanup(ClubDbContext db, TimeProvider clock, IOptions<SecurityOptions> options)
{
    /// <summary>
    /// Xóa chủ ngựa chưa xác thực quá hạn cùng challenge, email OTP và thông báo. Giữ tài khoản đã xác thực, nhân viên và dữ liệu có liên kết nghiệp vụ; caller quản lý transaction.
    /// </summary>
    /// <param name="batchSize">Giá trị kiểu int dùng trong RemoveExpired.</param>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <param name="email">Email đầu vào hoặc email chuẩn của tài khoản; không dùng để suy luận tài khoản tồn tại từ phản hồi chung.</param>
    /// <param name="userName">Giá trị kiểu string? dùng trong RemoveExpired.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<int> RemoveExpired(int batchSize, CancellationToken token = default, string? email = null, string? userName = null)
    {
        var cutoff = clock.GetUtcNow().AddHours(-options.Value.PendingRegistrationHours);
        var accounts = await db.Users.AsNoTracking()
            .Where(u => u.Role == Role.HorseOwner && !u.EmailVerified && u.CreatedAt <= cutoff
                && (email == null && userName == null || u.Email == email || u.UserName == userName || u.Email == userName || u.UserName == email)
                // Unverified registrations cannot normally own business records. Preserve legacy/inconsistent records rather than cascading into business data.
                && !db.Registrations.Any(r => r.OwnerId == u.Id) && !db.Horses.Any(h => h.OwnerId == u.Id)
                && !db.Plans.Any(p => p.TrainerId == u.Id) && !db.Sessions.Any(s => s.RiderId == u.Id))
            .OrderBy(u => u.CreatedAt).ThenBy(u => u.Id).Take(batchSize).Select(u => new { u.Id, u.Email }).ToListAsync(token);
        if (accounts.Count == 0) return 0;
        var ids = accounts.Select(u => u.Id).ToArray();
        var emails = accounts.Select(u => u.Email).ToArray();
        var challenges = db.Challenges.Where(c => ids.Contains(c.UserId));
        // EmailMessage has a restrictive challenge FK. Remove queued and delivered OTP copies first.
        await db.EmailMessages.Where(m => emails.Contains(m.Recipient) || challenges.Any(c => c.Id == m.ChallengeId)).ExecuteDeleteAsync(token);
        await challenges.ExecuteDeleteAsync(token);
        await db.Notifications.Where(n => ids.Contains(n.RecipientId)).ExecuteDeleteAsync(token);
        return await db.Users.Where(u => ids.Contains(u.Id) && !u.EmailVerified && u.Role == Role.HorseOwner && u.CreatedAt <= cutoff).ExecuteDeleteAsync(token);
    }
}
