using System.Data;
using HorseClub.DAL.Abstractions;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workers;

public sealed class EmailWorker(IServiceScopeFactory scopes, ILogger<EmailWorker> logger, TimeProvider clock, IOptions<WorkerOptions> options) : BackgroundService
{
    /// <summary>
    /// Lấy lô email dưới khóa SQL, bỏ mã hết hạn/đã dùng, gửi với timeout và lưu trạng thái từng thư; lên lịch retry khi lỗi, không log OTP/credential.
    /// </summary>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.</remarks>
    public async Task<int> RunOnce(CancellationToken token = default)
    {
        var processed = 0;
        // Commit each message independently: cancellation/failure on a later message
        // must not roll back delivery records already persisted for earlier messages.
        for (var index = 0; index < options.Value.EmailBatchSize; index++)
        {
            using var scope = scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await using var tx = await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, token);
            if (!await unitOfWork.TryAcquireWorkerLockAsync("HorseClub.Worker.Email", token)) break;
            var now = clock.GetUtcNow();
            var message = await repository.GetNextEmailAsync(now, options.Value.MaxEmailAttempts, token);
            if (message is null) break;
            if (message.ExpiresAt <= now || await repository.IsChallengeConsumedAsync(message.ChallengeId, token))
            {
                message.DiscardedAt = now; message.Body = ""; message.NextAttemptAt = null;
                await unitOfWork.SaveChangesAsync(token); await tx.CommitAsync(token);
                processed++;
                continue;
            }
            using var sendTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            sendTimeout.CancelAfter(TimeSpan.FromSeconds(options.Value.EmailSendTimeoutSeconds));
            try
            {
                await scope.ServiceProvider.GetRequiredService<IClubMailSender>().Send(message.Recipient, message.Subject, message.Body, sendTimeout.Token);
                message.SentAt = clock.GetUtcNow(); message.Body = ""; message.NextAttemptAt = null;
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                message.Attempts++;
                message.NextAttemptAt = clock.GetUtcNow().AddMinutes(Math.Min(60, Math.Pow(2, message.Attempts)));
                // Never log recipient, body, SMTP credentials or exception message containing them.
                logger.LogWarning("Email delivery failed for message {MessageId} (attempt {Attempt}): {ExceptionType}", message.Id, message.Attempts, error.GetType().Name);
                if (message.Attempts >= options.Value.MaxEmailAttempts)
                    logger.LogError("Email delivery exhausted retries for message {MessageId}", message.Id);
            }
            await unitOfWork.SaveChangesAsync(token); await tx.CommitAsync(token);
            processed++;
        }
        return processed;
    }

    /// <summary>
    /// Vòng lặp nền của EmailWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.
    /// </summary>
    /// <param name="stoppingToken">Token dừng host; mọi vòng lặp/delay phải tôn trọng token này.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = options.Value.EmailPollSeconds;
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Email worker failed"); delay = 10; }
            try { await Task.Delay(TimeSpan.FromSeconds(delay), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
