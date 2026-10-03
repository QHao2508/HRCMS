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
        var host = settings.Host ?? throw new InvalidOperationException("Configure Email:Host before sending mail.");
        using var smtp = new SmtpClient(host, settings.Port) { EnableSsl = true };
        if (!string.IsNullOrWhiteSpace(settings.Username)) smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var message = new MailMessage(settings.From ?? throw new InvalidOperationException("Configure Email:From."), recipient, subject, body);
        await smtp.SendMailAsync(message, token);
    }
}

public sealed class EmailWorker(IServiceScopeFactory scopes, WriteGate gate, ILogger<EmailWorker> logger, TimeProvider clock, IOptions<WorkerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await gate.Semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, stoppingToken);
                    var now = clock.GetUtcNow();
                    var messages = await db.EmailMessages.Where(x => x.SentAt == null && x.Attempts < options.Value.MaxEmailAttempts && (x.NextAttemptAt == null || x.NextAttemptAt <= now)).OrderBy(x => x.CreatedAt).Take(options.Value.EmailBatchSize).ToListAsync(stoppingToken);
                    foreach (var m in messages)
                    {
                        try
                        {
                            await scope.ServiceProvider.GetRequiredService<IClubMailSender>().Send(m.Recipient, m.Subject, m.Body, stoppingToken);
                            m.SentAt = clock.GetUtcNow(); m.Body = ""; // Do not retain emailed OTP/reset codes after delivery.
                        }
                        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
                        {
                            m.Attempts++; m.NextAttemptAt = clock.GetUtcNow().AddMinutes(Math.Min(60, Math.Pow(2, m.Attempts)));
                            logger.LogWarning("Email delivery failed for message {MessageId}: {ExceptionType}", m.Id, e.GetType().Name);
                        }
                    }
                    await db.SaveChangesAsync(stoppingToken); await tx.CommitAsync(stoppingToken);
                }
                finally { gate.Semaphore.Release(); }
                await Task.Delay(TimeSpan.FromSeconds(options.Value.EmailPollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Email worker failed"); await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        }
    }
}

public sealed class ReminderWorker(IServiceScopeFactory scopes, WriteGate gate, TimeProvider clock, ILogger<ReminderWorker> logger, IOptions<WorkerOptions> options, IOptions<BusinessOptions> business) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await gate.Semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>(); var now = clock.GetUtcNow();
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, stoppingToken);
                    var today = scope.ServiceProvider.GetRequiredService<ClubCalendar>().DateAt(now);
                    var preventive = await db.PreventiveCare.Where(x => x.CompletedDate == null && x.DueDate <= today && !x.ReminderSent && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)).Take(options.Value.ReminderBatchSize).ToListAsync(stoppingToken);
                    foreach (var p in preventive)
                    {
                        foreach (var id in await db.Assignments.Where(x => x.HorseId == p.HorseId && x.Active && x.Role == Role.Veterinarian).Select(x => x.StaffId).ToListAsync(stoppingToken))
                            db.Notifications.Add(new Notification { RecipientId = id, Type = NotificationType.MedicalPreventiveDue, Message = "Preventive care is due.", ReferenceId = p.Id });
                        p.ReminderSent = true;
                    }
                    var overdue = await db.Sessions.Where(x => x.ScheduledAt < now.AddMinutes(-business.Value.OverdueAfterMinutes) && x.Status == SessionStatus.Assigned && db.Plans.Any(p => p.Id == x.PlanId && p.Status == PlanStatus.Active) && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.SessionOverdue) && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)).Take(options.Value.ReminderBatchSize).ToListAsync(stoppingToken);
                    foreach (var s in overdue)
                    {
                        if (s.RiderId.HasValue) db.Notifications.Add(new Notification { RecipientId = s.RiderId.Value, Type = NotificationType.SessionOverdue, Message = "A training session is overdue.", ReferenceId = s.Id });
                        foreach (var id in await db.Assignments.Where(x => x.HorseId == s.HorseId && x.Active && x.Role == Role.Trainer).Select(x => x.StaffId).ToListAsync(stoppingToken))
                            db.Notifications.Add(new Notification { RecipientId = id, Type = NotificationType.SessionOverdue, Message = "A training session is overdue.", ReferenceId = s.Id });
                    }
                    var followups = await db.Treatments.Where(x => !x.Completed && x.FollowUpDate <= today && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.MedicalFollowUpDue) && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)).Take(options.Value.ReminderBatchSize).ToListAsync(stoppingToken);
                    foreach (var treatment in followups)
                        foreach (var id in await db.Assignments.Where(x => x.HorseId == treatment.HorseId && x.Active && x.Role == Role.Veterinarian).Select(x => x.StaffId).ToListAsync(stoppingToken))
                            db.Notifications.Add(new Notification { RecipientId = id, Type = NotificationType.MedicalFollowUpDue, Message = "Medical follow-up is due.", ReferenceId = treatment.Id });
                    await db.SaveChangesAsync(stoppingToken); await tx.CommitAsync(stoppingToken);
                }
                finally { gate.Semaphore.Release(); }
                await Task.Delay(TimeSpan.FromSeconds(options.Value.ReminderPollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception e) { logger.LogError(e, "Reminder worker failed"); await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        }
    }
}
