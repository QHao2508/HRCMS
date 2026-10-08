using System.Data;
using HorseClub.DAL.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HorseClub.DAL.Data;

/// <summary>Shares the request DbContext with all repositories; saving does not commit its transaction.</summary>
public sealed class EfUnitOfWork(ClubDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken token = default) => db.SaveChangesAsync(token);
    public Task<bool> TryAcquireWorkerLockAsync(string resource, CancellationToken token = default) => WorkerDatabaseLock.TryAcquire(db, resource, token);
    public async Task<IWriteTransaction> BeginTransactionAsync(IsolationLevel isolation, CancellationToken token = default)
        => new WriteTransaction(await db.Database.BeginTransactionAsync(isolation, token));

    private sealed class WriteTransaction(IDbContextTransaction transaction) : IWriteTransaction
    {
        public Task CommitAsync(CancellationToken token = default) => transaction.CommitAsync(token);
        public Task RollbackAsync(CancellationToken token = default) => transaction.RollbackAsync(token);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
