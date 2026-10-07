using HorseClub.BLL.Messaging;
using System.Data;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workers;

public sealed class ReminderWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<ReminderWorker> logger, IOptions<WorkerOptions> options, IOptions<BusinessOptions> business) : BackgroundService
{
    /// <summary>
    /// Quét công việc phòng bệnh, buổi tập quá hạn và follow-up y tế; gửi thông báo đúng nhân viên hiện được phân công, tránh tạo trùng.
    /// </summary>
    /// <param name="token">Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.</remarks>
    public async Task<int> RunOnce(CancellationToken token = default)
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
        // Tạo thông báo từ MessageKey cho người nhận trong lô công việc, tăng bộ đếm và chờ transaction commit.
        void Notify(Guid recipient, NotificationType type, MessageKey message, Guid reference)
        {
            db.Notifications.Add(new Notification { RecipientId = recipient, Type = type, Message = Messages.Get(message), ReferenceId = reference });
            notificationCount++;
        }

        // Filter eligible recipients before Take, so unassigned records cannot starve a batch.
        var preventive = await db.PreventiveCare.Where(x => x.CompletedDate == null && x.DueDate <= today && !x.ReminderSent
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
            .OrderBy(x => x.DueDate).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
        var preventiveHorseIds = preventive.Select(x => x.HorseId).Distinct().ToArray();
        var preventiveRecipients = (await vets.Where(x => preventiveHorseIds.Contains(x.HorseId))
            .Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        foreach (var item in preventive)
        {
            foreach (var recipient in preventiveRecipients[item.HorseId])
                Notify(recipient, NotificationType.MedicalPreventiveDue, MessageKey.PreventiveCareIsDue, item.Id);
            item.ReminderSent = true;
        }
        var overdue = await db.Sessions.Where(x => x.ScheduledAt < now.AddMinutes(-business.Value.OverdueAfterMinutes)
            && x.Status == SessionStatus.Assigned && db.Plans.Any(p => p.Id == x.PlanId && p.Status == PlanStatus.Active)
            && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.SessionOverdue)
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived)
            && (trainers.Any(a => a.HorseId == x.HorseId) || db.Users.Any(u => u.Id == x.RiderId && u.Active && u.Role == Role.WorkRider)))
            .OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
        var overdueHorseIds = overdue.Select(x => x.HorseId).Distinct().ToArray();
        var overdueRecipients = (await trainers.Where(x => overdueHorseIds.Contains(x.HorseId))
            .Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        var overdueRiderIds = overdue.Where(x => x.RiderId.HasValue).Select(x => x.RiderId!.Value).Distinct().ToArray();
        var activeRiders = (await db.Users.Where(x => overdueRiderIds.Contains(x.Id) && x.Active && x.Role == Role.WorkRider)
            .Select(x => x.Id).ToListAsync(token)).ToHashSet();
        foreach (var session in overdue)
        {
            var recipients = overdueRecipients[session.HorseId].ToList();
            if (session.RiderId.HasValue && activeRiders.Contains(session.RiderId.Value))
                recipients.Add(session.RiderId.Value);
            foreach (var recipient in recipients.Distinct())
                Notify(recipient, NotificationType.SessionOverdue, MessageKey.ATrainingSessionIsOverdue, session.Id);
        }
        var followups = await db.Treatments.Where(x => !x.Completed && x.FollowUpDate <= today
            && !db.Notifications.Any(n => n.ReferenceId == x.Id && n.Type == NotificationType.MedicalFollowUpDue)
            && db.Horses.Any(h => h.Id == x.HorseId && !h.Archived) && vets.Any(a => a.HorseId == x.HorseId))
            .OrderBy(x => x.FollowUpDate).ThenBy(x => x.Id).Take(options.Value.ReminderBatchSize).ToListAsync(token);
        var followupHorseIds = followups.Select(x => x.HorseId).Distinct().ToArray();
        var followupRecipients = (await vets.Where(x => followupHorseIds.Contains(x.HorseId))
            .Select(x => new { x.HorseId, x.StaffId }).Distinct().ToListAsync(token)).ToLookup(x => x.HorseId, x => x.StaffId);
        foreach (var treatment in followups)
            foreach (var recipient in followupRecipients[treatment.HorseId])
                Notify(recipient, NotificationType.MedicalFollowUpDue, MessageKey.MedicalFollowUpIsDue, treatment.Id);

        await db.SaveChangesAsync(token); await tx.CommitAsync(token);
        return notificationCount;
    }

    /// <summary>
    /// Vòng lặp nền của ReminderWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.
    /// </summary>
    /// <param name="stoppingToken">Token dừng host; mọi vòng lặp/delay phải tôn trọng token này.</param>
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
