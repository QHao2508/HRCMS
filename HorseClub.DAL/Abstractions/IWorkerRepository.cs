namespace HorseClub.DAL.Abstractions;

public sealed record ReminderBatch(List<PreventiveCare> Preventive, ILookup<Guid, Guid> PreventiveRecipients,
    List<TrainingSession> Overdue, ILookup<Guid, Guid> OverdueRecipients, HashSet<Guid> ActiveRiders,
    List<TreatmentPlan> FollowUps, ILookup<Guid, Guid> FollowUpRecipients);

public interface IWorkerRepository
{
    Task<EmailMessage?> GetNextEmailAsync(DateTimeOffset now, int maxAttempts, CancellationToken token);
    Task<bool> IsChallengeConsumedAsync(Guid? challengeId, CancellationToken token);
    Task<ReminderBatch> GetRemindersAsync(DateTimeOffset now, DateTimeOffset overdueBefore, DateOnly today, int batchSize, CancellationToken token);
    Task<int> DeleteExpiredOwnersAsync(DateTimeOffset cutoff, int batchSize, string? email, string? userName, CancellationToken token);
}
