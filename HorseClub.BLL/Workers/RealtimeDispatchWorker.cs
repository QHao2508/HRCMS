using HorseClub.BLL.Realtime;
using HorseClub.DAL.Abstractions;

namespace HorseClub.BLL.Workers;

/// <summary>Claims in a short SQL transaction; never holds a database transaction during network delivery.</summary>
public sealed class RealtimeDispatchWorker(IServiceScopeFactory scopes, IConfiguration configuration, TimeProvider clock, ILogger<RealtimeDispatchWorker> logger) : BackgroundService
{
    public async Task<int> RunOnce(CancellationToken token = default)
    {
        var count = 0;
        for (; count < 50 && !token.IsCancellationRequested; count++)
        {
            using var scope = scopes.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRealtimeOutboxRepository>();
            var lease = Guid.NewGuid();
            var message = await repository.ClaimAsync(clock.GetUtcNow(), lease, token);
            if (message is null) break;
            var success = false;
            try
            {
                var stamp = await repository.GetActiveStampAsync(message.RecipientId, token);
                if (stamp is not null)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    await scope.ServiceProvider.GetRequiredService<IRealtimePublisher>().PublishAsync(message.RecipientId, stamp,
                        new(message.Id, 1, message.EventType == "DataChanged" ? null : message.SourceId, message.CreatedAt, message.EventType, message.SourceVersion), timeout.Token);
                }
                success = true; // Offline users recover their durable notifications through REST.
            }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                logger.LogWarning("Realtime dispatch failed for {EventId} attempt {Attempt}: {ErrorType}", message.Id, message.Attempts, error.GetType().Name);
            }
            await repository.CompleteAsync(message.Id, lease, clock.GetUtcNow(), success, token);
        }
        return count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("Realtime:Enabled")) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnce(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogError("Realtime worker failed: {ErrorType}", error.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
