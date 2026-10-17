using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Auth;

/// <summary>Deletes unused self-registered accounts and their verification data in the caller's transaction.</summary>
public sealed class PendingRegistrationCleanup(IWorkerRepository repository, TimeProvider clock, IOptions<SecurityOptions> options)
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
        return await repository.DeleteExpiredOwnersAsync(cutoff, batchSize, email, userName, token);
    }
}
