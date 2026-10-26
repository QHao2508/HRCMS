using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Queries;

public sealed class ReportingQueries(ClubDbContext db) : IReportingQueries
{
    private static async Task<DataPage<T>> Page<T>(IQueryable<T> query, int page, int size) where T : class
        => await query.AsNoTracking().ToPageAsync(page, size);
    public Task<DataPage<NotificationListItem>> ListNotificationsAsync(Guid recipientId, bool unread, int page, int size)
    {
        var query = db.Notifications.Where(x => x.RecipientId == recipientId);
        if (unread) query = query.Where(x => !x.Read);
        // ReferenceId trỏ tới ngựa hoặc tới công việc thuộc một ngựa; suy ra ngựa khi đọc để thông báo cũ cũng có ngữ cảnh.
        var withHorse = query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Select(n => new
        {
            n,
            HorseId = db.Horses.Where(h => h.Id == n.ReferenceId).Select(h => (Guid?)h.Id).FirstOrDefault()
                ?? db.CareTasks.Where(t => t.Id == n.ReferenceId).Select(t => (Guid?)t.HorseId).FirstOrDefault()
                ?? db.Sessions.Where(t => t.Id == n.ReferenceId).Select(t => (Guid?)t.HorseId).FirstOrDefault()
                ?? db.Treatments.Where(t => t.Id == n.ReferenceId).Select(t => (Guid?)t.HorseId).FirstOrDefault()
                ?? db.PreventiveCare.Where(t => t.Id == n.ReferenceId).Select(t => (Guid?)t.HorseId).FirstOrDefault()
                ?? db.Incidents.Where(t => t.Id == n.ReferenceId).Select(t => (Guid?)t.HorseId).FirstOrDefault()
        });
        var items = withHorse.Select(x => new NotificationListItem(x.n.Id, x.n.Type, x.n.MessageCode, x.n.ReferenceId, x.HorseId,
            db.Horses.Where(h => h.Id == x.HorseId).Select(h => h.Name).FirstOrDefault(),
            db.Registrations.Where(r => r.Id == x.n.ReferenceId).Select(r => r.Name).FirstOrDefault()
                ?? db.Inventory.Where(i => i.Id == x.n.ReferenceId).Select(i => i.Name).FirstOrDefault()
                ?? db.Replenishments.Where(r => r.Id == x.n.ReferenceId).Join(db.Inventory, r => r.ItemId, i => i.Id, (r, i) => i.Name).FirstOrDefault(),
            x.n.Read, x.n.CreatedAt));
        return items.ToPageAsync(page, size);
    }
    public Task<Notification?> GetNotificationAsync(Guid recipientId, Guid id) => db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientId == recipientId);
    public Task<DataPage<AuditEvent>> ListAuditAsync(Guid? referenceId, int page, int size)
    {
        var query = db.Audit.AsQueryable();
        if (referenceId.HasValue) query = query.Where(x => x.ReferenceId == referenceId);
        return Page(query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    }
    public Task<DashboardData> DashboardAsync(HorseScope scope, Guid userId, Guid? riderId, Guid? groomId, DateTimeOffset now, DateTimeOffset start, DateTimeOffset end, DateTimeOffset overdueBefore)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        var sessions = db.Sessions.Where(x => horses.Contains(x.HorseId));
        if (riderId.HasValue) sessions = sessions.Where(x => x.RiderId == riderId);
        var care = db.CareTasks.Where(x => horses.Contains(x.HorseId));
        if (groomId.HasValue) care = care.Where(x => x.GroomId == groomId);
        return db.Users.Where(x => x.Id == userId).Select(_ => new DashboardData(
            horses.Count(), db.Plans.Count(x => horses.Contains(x.HorseId) && x.Status == PlanStatus.Active),
            sessions.Count(x => x.ScheduledAt >= start && x.ScheduledAt < end),
            sessions.Count(x => x.ScheduledAt < overdueBefore && x.Status == SessionStatus.Assigned && db.Plans.Any(p => p.Id == x.PlanId && p.Status == PlanStatus.Active)),
            care.Count(x => x.ScheduledAt >= start && x.ScheduledAt < end),
            db.Restrictions.Where(x => horses.Contains(x.HorseId) && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now)).Select(x => x.HorseId).Distinct().Count(),
            db.Notifications.Count(x => x.RecipientId == userId && !x.Read))).SingleAsync();
    }
    private IQueryable<Guid> HorseIds(HorseScope scope, Guid? horseId)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        return horseId.HasValue ? horses.Where(x => x == horseId) : horses;
    }
    public async Task<ReportRows> ReportRowsAsync(HorseScope scope, Guid? horseId, Guid? riderId, Guid? groomId, DateTimeOffset start, DateTimeOffset end, int limit)
    {
        var horses = HorseIds(scope, horseId);
        var sq = db.Sessions.Where(x => horses.Contains(x.HorseId) && x.ScheduledAt >= start && x.ScheduledAt <= end);
        if (riderId.HasValue) sq = sq.Where(x => x.RiderId == riderId);
        if (groomId.HasValue) sq = sq.Where(x => false);
        var cq = db.CareTasks.Where(x => horses.Contains(x.HorseId) && x.ScheduledAt >= start && x.ScheduledAt <= end);
        if (riderId.HasValue) cq = cq.Where(x => false);
        if (groomId.HasValue) cq = cq.Where(x => x.GroomId == groomId);
        var sessions = await sq.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id).Select(x => new ReportSessionData(x.Id, x.HorseId, x.ScheduledAt, x.Status, x.DistanceMetres, x.Target)).Take(limit + 1).ToListAsync();
        var tasks = await cq.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id).Select(x => new ReportCareData(x.Id, x.HorseId, x.Type, x.Status, x.ScheduledAt, x.ApprovedPortionKg, x.ActualPortionKg)).Take(limit + 1).ToListAsync();
        return new(sessions, tasks);
    }
    public Task<List<SessionResult>> ResultsAsync(Guid[] sessionIds) => db.Results.AsNoTracking().Where(x => sessionIds.Contains(x.SessionId)).ToListAsync();
    public Task<int> MedicalCountAsync(HorseScope scope, Guid? horseId, DateTimeOffset start, DateTimeOffset end)
    {
        var horses = HorseIds(scope, horseId);
        return db.MedicalRecords.CountAsync(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end);
    }
    public Task<List<MedicalRecord>> ClinicalAsync(HorseScope scope, Guid? horseId, DateTimeOffset start, DateTimeOffset end, int limit)
    {
        var horses = HorseIds(scope, horseId);
        return db.MedicalRecords.AsNoTracking().Where(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end).OrderByDescending(x => x.ExaminationAt).ThenByDescending(x => x.Id).Take(limit + 1).ToListAsync();
    }
}
