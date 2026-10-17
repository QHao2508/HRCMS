using System.Data;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class RealtimeOutboxRepository(ClubDbContext db) : IRealtimeOutboxRepository
{
    public async Task<RealtimeOutboxMessage?> ClaimAsync(DateTimeOffset now, Guid lease, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        if (!await WorkerDatabaseLock.TryAcquire(db, "HorseClub.Worker.Realtime", token)) return null;
        var message = await db.RealtimeOutboxMessages.Where(x => x.SentAt == null && x.Attempts < 8
            && (x.NextAttemptAt == null || x.NextAttemptAt <= now) && (x.LockedUntil == null || x.LockedUntil <= now))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
        if (message is null) return null;
        message.LeaseOwner = lease; message.LockedUntil = now.AddMinutes(1); message.Attempts++;
        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        db.Entry(message).State = EntityState.Detached;
        return message;
    }
    public async Task CompleteAsync(Guid id, Guid lease, DateTimeOffset now, bool success, CancellationToken token)
    {
        var query = db.RealtimeOutboxMessages.Where(x => x.Id == id && x.LeaseOwner == lease && x.SentAt == null);
        if (success)
            await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.SentAt, now).SetProperty(x => x.LockedUntil, (DateTimeOffset?)null).SetProperty(x => x.LeaseOwner, (Guid?)null).SetProperty(x => x.Version, x => x.Version + 1), token);
        else
            await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, now.AddSeconds(30)).SetProperty(x => x.LockedUntil, (DateTimeOffset?)null).SetProperty(x => x.LeaseOwner, (Guid?)null).SetProperty(x => x.Version, x => x.Version + 1), token);
    }
    public Task<string?> GetActiveStampAsync(Guid userId, CancellationToken token)
        => db.Users.Where(x => x.Id == userId && x.Active && x.EmailVerified).Select(x => x.SecurityStamp).SingleOrDefaultAsync(token);
}
