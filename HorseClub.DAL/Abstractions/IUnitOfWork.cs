using System.Data;

namespace HorseClub.DAL.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken token = default);
    Task<IWriteTransaction> BeginTransactionAsync(IsolationLevel isolation, CancellationToken token = default);
    Task<bool> TryAcquireWorkerLockAsync(string resource, CancellationToken token = default);
}

public interface IWriteTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken token = default);
    Task RollbackAsync(CancellationToken token = default);
}
