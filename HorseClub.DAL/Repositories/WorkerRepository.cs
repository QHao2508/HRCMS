using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class WorkerRepository(ClubDbContext db) : IWorkerRepository
{
    public Task<EmailMessage?> GetNextEmailAsync(DateTimeOffset now, int maxAttempts, CancellationToken token)
        => db.EmailMessages.Where(x => x.SentAt == null && x.DiscardedAt == null
            && (x.ExpiresAt <= now || db.Challenges.Any(c => c.Id == x.ChallengeId && c.Consumed)
                || x.Attempts < maxAttempts && (x.NextAttemptAt == null || x.NextAttemptAt <= now)))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
    public Task<bool> IsChallengeConsumedAsync(Guid? challengeId, CancellationToken token)
        => db.Challenges.AnyAsync(c => c.Id == challengeId && c.Consumed, token);
    public async Task<ReminderBatch> GetRemindersAsync(DateTimeOffset now, DateTimeOffset overdueBefore, DateOnly today, int batchSize, CancellationToken token)
    {
        var vets = db.Assignments.Where(x => x.Active && x.Role == Role.Veterinarian && db.Users.Any(u => u.Id == x.StaffId && u.Active && u.Role == Role.Veterinarian));
        var trainers = db.Assignments.Where(x => x.Active && x.Role == Role.Trainer && db.Users.Any(u => u.Id == x.StaffId && u.Active && u.Role == Role.Trainer));
        var preventive = await db.PreventiveCare.Where(x => x.CompletedDate == null && x.DueDate <= today && !x.ReminderSent
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
            .OrderBy(x => x.DueDate).ThenBy(x => x.Id).Take(batchSize).ToListAsync(token);
        var preventiveHorseIds = preventive.Select(x => x.HorseId).Distinct().ToArray();
        var preventiveRecipients = (await vets.Where(x => preventiveHorseIds.Contains(x.HorseId)).Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        var overdue = await db.Sessions.Where(x => x.ScheduledAt < overdueBefore && x.Status == SessionStatus.Assigned
            && db.Plans.Any(p => p.Id == x.PlanId && p.Status == PlanStatus.Active)
            && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.SessionOverdue)
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)
            && (trainers.Any(a => a.HorseId == x.HorseId) || db.Users.Any(u => u.Id == x.RiderId && u.Active && u.Role == Role.WorkRider)))
            .OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id).Take(batchSize).ToListAsync(token);
        var overdueHorseIds = overdue.Select(x => x.HorseId).Distinct().ToArray();
        var overdueRecipients = (await trainers.Where(x => overdueHorseIds.Contains(x.HorseId)).Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        var riderIds = overdue.Where(x => x.RiderId.HasValue).Select(x => x.RiderId!.Value).Distinct().ToArray();
        var activeRiders = (await db.Users.Where(x => riderIds.Contains(x.Id) && x.Active && x.Role == Role.WorkRider).Select(x => x.Id).ToListAsync(token)).ToHashSet();
        var followups = await db.Treatments.Where(x => !x.Completed && x.FollowUpDate <= today
            && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.MedicalFollowUpDue)
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
            .OrderBy(x => x.FollowUpDate).ThenBy(x => x.Id).Take(batchSize).ToListAsync(token);
        var followupHorseIds = followups.Select(x => x.HorseId).Distinct().ToArray();
        var followupRecipients = (await vets.Where(x => followupHorseIds.Contains(x.HorseId)).Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        return new(preventive, preventiveRecipients, overdue, overdueRecipients, activeRiders, followups, followupRecipients);
    }
    public async Task<int> DeleteExpiredOwnersAsync(DateTimeOffset cutoff, int batchSize, string? email, string? userName, CancellationToken token)
    {
        var accounts = await db.Users.AsNoTracking().Where(u => u.Role == Role.HorseOwner && !u.EmailVerified && u.CreatedAt <= cutoff
            && (email == null && userName == null || u.Email == email || u.UserName == userName || u.Email == userName || u.UserName == email)
            && !db.Registrations.Any(r => r.OwnerId == u.Id) && !db.Horses.Any(h => h.OwnerId == u.Id)
            && !db.Plans.Any(p => p.TrainerId == u.Id) && !db.Sessions.Any(s => s.RiderId == u.Id))
            .OrderBy(u => u.CreatedAt).ThenBy(u => u.Id).Take(batchSize).Select(u => new { u.Id, u.Email }).ToListAsync(token);
        if (accounts.Count == 0) return 0;
        var ids = accounts.Select(u => u.Id).ToArray(); var emails = accounts.Select(u => u.Email).ToArray();
        var challenges = db.Challenges.Where(c => ids.Contains(c.UserId));
        await db.EmailMessages.Where(m => emails.Contains(m.Recipient) || challenges.Any(c => c.Id == m.ChallengeId)).ExecuteDeleteAsync(token);
        await challenges.ExecuteDeleteAsync(token);
        await db.Notifications.Where(n => ids.Contains(n.RecipientId)).ExecuteDeleteAsync(token);
        await db.RealtimeOutboxMessages.Where(n => ids.Contains(n.RecipientId)).ExecuteDeleteAsync(token);
        return await db.Users.Where(u => ids.Contains(u.Id) && !u.EmailVerified && u.Role == Role.HorseOwner && u.CreatedAt <= cutoff).ExecuteDeleteAsync(token);
    }
}
