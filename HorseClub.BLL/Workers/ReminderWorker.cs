using HorseClub.BLL.Messaging;
using System.Data;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
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
        var repository = scope.ServiceProvider.GetRequiredService<IWorkerRepository>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await using var tx = await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, token);
        if (!await unitOfWork.TryAcquireWorkerLockAsync("HorseClub.Worker.Reminder", token)) return 0;
        var now = clock.GetUtcNow();
        var today = scope.ServiceProvider.GetRequiredService<ClubCalendar>().DateAt(now);
        var batch = await repository.GetRemindersAsync(now, now.AddMinutes(-business.Value.OverdueAfterMinutes), today, options.Value.ReminderBatchSize, token);
        var notificationCount = 0;
        // Tạo thông báo từ MessageKey cho người nhận trong lô công việc, tăng bộ đếm và chờ transaction commit.
        void Notify(Guid recipient, NotificationType type, MessageKey message, Guid reference)
        {
            eventRepository.AddNotification(new Notification { RecipientId = recipient, Type = type, Message = Messages.Get(message), ReferenceId = reference });
            notificationCount++;
        }

        // Filter eligible recipients before Take, so unassigned records cannot starve a batch.
        var preventive = batch.Preventive; var preventiveRecipients = batch.PreventiveRecipients;
        foreach (var item in preventive)
        {
            foreach (var recipient in preventiveRecipients[item.HorseId])
                Notify(recipient, NotificationType.MedicalPreventiveDue, MessageKey.PreventiveCareIsDue, item.Id);
            item.ReminderSent = true;
        }
        var overdue = batch.Overdue; var overdueRecipients = batch.OverdueRecipients; var activeRiders = batch.ActiveRiders;
        foreach (var session in overdue)
        {
            var recipients = overdueRecipients[session.HorseId].ToList();
            if (session.RiderId.HasValue && activeRiders.Contains(session.RiderId.Value))
                recipients.Add(session.RiderId.Value);
            foreach (var recipient in recipients.Distinct())
                Notify(recipient, NotificationType.SessionOverdue, MessageKey.ATrainingSessionIsOverdue, session.Id);
        }
        var followups = batch.FollowUps; var followupRecipients = batch.FollowUpRecipients;
        foreach (var treatment in followups)
            foreach (var recipient in followupRecipients[treatment.HorseId])
                Notify(recipient, NotificationType.MedicalFollowUpDue, MessageKey.MedicalFollowUpIsDue, treatment.Id);

        await unitOfWork.SaveChangesAsync(token); await tx.CommitAsync(token);
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
