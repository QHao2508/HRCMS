namespace HorseClub.DAL.Abstractions;

public interface IRealtimeOutboxRepository
{
    Task<RealtimeOutboxMessage?> ClaimAsync(DateTimeOffset now, Guid lease, CancellationToken token);
    Task CompleteAsync(Guid id, Guid lease, DateTimeOffset now, bool success, CancellationToken token);
    Task<string?> GetActiveStampAsync(Guid userId, CancellationToken token);
}
