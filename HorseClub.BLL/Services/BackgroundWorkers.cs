using HorseClub.BLL.Messaging;
using System.Data;
using System.Net;
using System.Net.Mail;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Services;

public interface IClubMailSender { Task Send(string recipient, string subject, string body, CancellationToken token); }
public sealed class ClubMailSender(IOptions<EmailOptions> options, IWebHostEnvironment env) : IClubMailSender
{
    public async Task Send(string recipient, string subject, string body, CancellationToken token)
    {
        var settings = options.Value;
        if (settings.Mode == EmailDeliveryMode.DevelopmentFile && env.IsDevelopment())
        {
            var path = Path.Combine(env.ContentRootPath, "App_Data", "mail"); Directory.CreateDirectory(path);
            await File.WriteAllTextAsync(Path.Combine(path, $"{Guid.NewGuid():N}.eml"), $"To: {recipient}\nSubject: {subject}\n\n{body}", token);
            return;
        }
        var host = settings.Host ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureEmailHostBeforeSendingMail));
        using var smtp = new SmtpClient(host, settings.Port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(settings.Username)) smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var message = new MailMessage(settings.From ?? throw new InvalidOperationException(Messages.Get(MessageKey.ConfigureEmailFrom)), recipient, subject, body);
        await smtp.SendMailAsync(message, token);
    }
}

public sealed class EmailWorker(IServiceScopeFactory scopes, WriteGate gate, ILogger<EmailWorker> logger, TimeProvider clock, IOptions<WorkerOptions> options) : BackgroundService
{
    public async Task<int> RunOnce(CancellationToken token = default)
    {
        var processed = 0;
        await gate.Semaphore.WaitAsync(token);
        try
        {
            // Commit each message independently: cancellation/failure on a later message
            // must not roll back delivery records already persisted for earlier messages.
            for (var index = 0; index < options.Value.EmailBatchSize; index++)
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
                await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
                if (!await WorkerDatabaseLock.TryAcquire(db, "HorseClub.Worker.Email", token)) break;
                var now = clock.GetUtcNow();
                var message = await db.EmailMessages.Where(x => x.SentAt == null && x.DiscardedAt == null
                    && (x.ExpiresAt <= now || db.Challenges.Any(c => c.Id == x.ChallengeId && c.Consumed)
                        || x.Attempts < options.Value.MaxEmailAttempts && (x.NextAttemptAt == null || x.NextAttemptAt <= now)))
                    .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
                if (message is null) break;
                if (message.ExpiresAt <= now || await db.Challenges.AnyAsync(c => c.Id == message.ChallengeId && c.Consumed, token))
                {
                    message.DiscardedAt = now; message.Body = ""; message.NextAttemptAt = null;
                    await db.SaveChangesAsync(token); await tx.CommitAsync(token);
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
                await db.SaveChangesAsync(token); await tx.CommitAsync(token);
                processed++;
            }
        }
        finally { gate.Semaphore.Release(); }
        return processed;
    }

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

public sealed class ReminderWorker(IServiceScopeFactory scopes, WriteGate gate, TimeProvider clock, ILogger<ReminderWorker> logger, IOptions<WorkerOptions> options, IOptions<BusinessOptions> business) : BackgroundService
{
    public async Task<int> RunOnce(CancellationToken token = default)
    {
        await gate.Semaphore.WaitAsync(token);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            if (!await WorkerDatabaseLock.TryAcquire(db, "HorseClub.Worker.Reminder", token)) return 0;
            var now = clock.GetUtcNow();
            var today = scope.ServiceProvider.GetRequiredService<ClubCalendar>().DateAt(now);
            var vets = db.Assignments.Where(x => x.Active && x.Role == Role.Veterinarian
                && db.Users.Any(u => u.Id == x.StaffId && u.Active && u.Role == Role.Veterinarian));
            var trainers = db.Assignments.Where(x => x.Active && x.Role == Role.Trainer
                && db.Users.Any(u => u.Id == x.StaffId && u.Active && u.Role == Role.Trainer));
            var notificationCount = 0;
            void Notify(Guid recipient, NotificationType type, MessageKey message, Guid reference)
            {
                db.Notifications.Add(new Notification { RecipientId = recipient, Type = type, Message = Messages.Get(message), ReferenceId = reference });
                notificationCount++;
            }

            // Filter eligible recipients before Take, so unassigned records cannot starve a batch.
            var preventive = await db.PreventiveCare.Where(x => x.CompletedDate == null && x.DueDate <= today && !x.ReminderSent
                && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
                .OrderBy(x => x.DueDate).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
            foreach (var item in preventive)
            {
                foreach (var recipient in await vets.Where(x => x.HorseId == item.HorseId).Select(x => x.StaffId).Distinct().ToListAsync(token))
                    Notify(recipient, NotificationType.MedicalPreventiveDue, MessageKey.PreventiveCareIsDue, item.Id);
                item.ReminderSent = true;
            }
            var overdue = await db.Sessions.Where(x => x.ScheduledAt < now.AddMinutes(-business.Value.OverdueAfterMinutes)
                && x.Status == SessionStatus.Assigned && db.Plans.Any(p => p.Id == x.PlanId && p.Status == PlanStatus.Active)
                && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.SessionOverdue)
                && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)
                && (trainers.Any(a => a.HorseId == x.HorseId) || db.Users.Any(u => u.Id == x.RiderId && u.Active && u.Role == Role.WorkRider)))
                .OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
            foreach (var session in overdue)
            {
                var recipients = await trainers.Where(x => x.HorseId == session.HorseId).Select(x => x.StaffId).Distinct().ToListAsync(token);
                if (session.RiderId.HasValue && await db.Users.AnyAsync(u => u.Id == session.RiderId && u.Active && u.Role == Role.WorkRider, token))
                    recipients.Add(session.RiderId.Value);
                foreach (var recipient in recipients.Distinct())
                    Notify(recipient, NotificationType.SessionOverdue, MessageKey.ATrainingSessionIsOverdue, session.Id);
            }
            var followups = await db.Treatments.Where(x => !x.Completed && x.FollowUpDate <= today
                && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.MedicalFollowUpDue)
                && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
                .OrderBy(x => x.FollowUpDate).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
            foreach (var treatment in followups)
                foreach (var recipient in await vets.Where(x => x.HorseId == treatment.HorseId).Select(x => x.StaffId).Distinct().ToListAsync(token))
                    Notify(recipient, NotificationType.MedicalFollowUpDue, MessageKey.MedicalFollowUpIsDue, treatment.Id);

            await db.SaveChangesAsync(token); await tx.CommitAsync(token);
            return notificationCount;
        }
        finally { gate.Semaphore.Release(); }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = options.Value.ReminderPollSeconds;
            try
            {
                await RunOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Reminder worker failed"); delay = 10; }
            try { await Task.Delay(TimeSpan.FromSeconds(delay), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
