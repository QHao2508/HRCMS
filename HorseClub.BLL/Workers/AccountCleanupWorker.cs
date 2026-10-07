using System.Data;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workers;

public sealed class AccountCleanupWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AccountCleanupWorker> logger, IOptions<WorkerOptions> options) : BackgroundService
{
    /// <summary>
    /// Khóa lượt dọn trong SQL Server, xóa một lô tài khoản tự đăng ký chưa xác thực quá hạn và commit độc lập.
    /// </summary>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <remarks>Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.</remarks>
    public async Task<int> RunOnce(CancellationToken token = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        if (!await WorkerDatabaseLock.TryAcquire(db, "HorseClub.Worker.AccountCleanup", token)) return 0;
        var removed = await scope.ServiceProvider.GetRequiredService<PendingRegistrationCleanup>().RemoveExpired(options.Value.AccountCleanupBatchSize, token);
        await tx.CommitAsync(token);
        if (removed > 0) logger.LogInformation("Removed {Count} expired unverified registrations", removed);
        return removed;
    }

    /// <summary>
    /// Vòng lặp nền của AccountCleanupWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.
    /// </summary>
    /// <param name="stoppingToken">Token dừng host; mọi vòng lặp/delay phải tôn trọng token này.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnce(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError(error, "Pending registration cleanup failed"); }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.AccountCleanupPollSeconds), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
