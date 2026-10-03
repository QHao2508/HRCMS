using HorseClub.BLL.Messaging;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workflows;

public static class ReportingWorkflow
{
    public static async Task<object> GetList(CurrentUser current, ClubDbContext db, bool? unread, int? page, int? pageSize, PageReader pager)
    {
            var id = (await current.Get()).Id; var q = db.Notifications.Where(x => x.RecipientId == id);
            if (unread == true) q = q.Where(x => !x.Read); return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<object> PostByIdRead(Guid id, CurrentUser current, ClubDbContext db)
    {
            var u = await current.Get(); var notification = Ensure.Found(await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientId == u.Id));
            notification.Read = true; await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> GetAudit(CurrentUser current, ClubDbContext db, Guid? referenceId, int? page, int? pageSize, PageReader pager)
    {
            Ensure.Role(await current.Get(), Role.ClubManager); var q = db.Audit.AsQueryable();
            if (referenceId.HasValue) q = q.Where(x => x.ReferenceId == referenceId);
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<object> GetDashboard(ClubAccess access, CurrentUser current, ClubDbContext db, TimeProvider clock, ClubCalendar calendar)
    {
            var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id); var now = clock.GetUtcNow();
            var (start, end) = calendar.DayRange(now);
            var sessions = db.Sessions.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.WorkRider) sessions = sessions.Where(x => x.RiderId == u.Id);
            var care = db.CareTasks.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.Groom) care = care.Where(x => x.GroomId == u.Id);
            return new { horseCount = await horses.CountAsync(), activePlans = await db.Plans.CountAsync(x => horses.Contains(x.HorseId) && x.Status == PlanStatus.Active),
                sessionsToday = await sessions.CountAsync(x => x.ScheduledAt >= start && x.ScheduledAt < end),
                overdueSessions = await sessions.CountAsync(x => x.ScheduledAt < now && x.Status == SessionStatus.Assigned),
                careTasksToday = await care.CountAsync(x => x.ScheduledAt >= start && x.ScheduledAt < end),
                restrictedHorses = await db.Restrictions.Where(x => horses.Contains(x.HorseId) && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now)).Select(x => x.HorseId).Distinct().CountAsync(),
                unreadNotifications = await db.Notifications.CountAsync(x => x.RecipientId == u.Id && !x.Read) };
        }

    public static async Task<IResult> BuildReport(Guid? horseId, DateTimeOffset? from, DateTimeOffset? to, ReportGrouping? groupBy,
        ClubAccess access, CurrentUser current, ClubDbContext db, TimeProvider clock, IOptions<BusinessOptions> options, ClubCalendar calendar)
    {
        var u = await current.Get(); var end = to ?? clock.GetUtcNow(); var start = from ?? end.AddDays(-options.Value.DefaultReportDays);
        Ensure.That(end >= start && (end - start).TotalDays <= options.Value.MaxReportDays, Messages.Get(MessageKey.ReportRangeExceedsConfiguredLimits));
        var grouping = groupBy ?? ReportGrouping.Day; Ensure.That(Enum.IsDefined(grouping), Messages.Get(MessageKey.InvalidReportGrouping));
        var horses = (await access.Horses()).Select(x => x.Id);
        if (horseId.HasValue) { await access.Horse(horseId.Value); horses = horses.Where(x => x == horseId); }
        var sq = db.Sessions.Where(x => horses.Contains(x.HorseId) && x.ScheduledAt >= start && x.ScheduledAt <= end);
        if (u.Role == Role.WorkRider) sq = sq.Where(x => x.RiderId == u.Id);
        var cq = db.CareTasks.Where(x => horses.Contains(x.HorseId) && x.ScheduledAt >= start && x.ScheduledAt <= end);
        if (u.Role == Role.WorkRider) cq = cq.Where(x => false);
        if (u.Role == Role.Groom) sq = sq.Where(x => false);
        if (u.Role == Role.Groom) cq = cq.Where(x => x.GroomId == u.Id);
        // Bound materialization before decimal aggregation/grouping, portable across both providers.
        var sessions = await sq.OrderBy(x => x.ScheduledAt).Take(options.Value.MaxReportRecords + 1).ToListAsync();
        var tasks = await cq.OrderBy(x => x.ScheduledAt).Take(options.Value.MaxReportRecords + 1).ToListAsync();
        Ensure.That(sessions.Count <= options.Value.MaxReportRecords && tasks.Count <= options.Value.MaxReportRecords, Messages.Get(MessageKey.TooMuchReportDataNarrowHorseDateFilters), 413, "report_limit");
        var sessionIds = sessions.Select(x => x.Id).ToArray();
        var results = await db.Results.Where(x => sessionIds.Contains(x.SessionId)).ToListAsync();
        var completed = sessions.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported);
        var medicalCount = await db.MedicalRecords.CountAsync(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end);
        object? clinical = null;
        if (u.Role == Role.Veterinarian)
            clinical = await db.MedicalRecords.Where(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end).OrderByDescending(x => x.ExaminationAt).Take(100).ToListAsync();
        var resultMap = results.ToDictionary(x => x.SessionId);
        var series = sessions.GroupBy(x => Bucket(calendar.Local(x.ScheduledAt), grouping, calendar.Local(start))).OrderBy(x => x.Key).Select(g => new { period = g.Key,
            sessions = g.Count(), completed = g.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported),
            plannedDistanceMetres = g.Sum(x => x.DistanceMetres), actualDistanceMetres = g.Sum(x => resultMap.TryGetValue(x.Id, out var r) ? r.DistanceMetres : 0) });
        var medicalVisible = u.Role is Role.Veterinarian or Role.ClubManager or Role.HorseOwner;
        return Results.Ok(new { from = start, to = end, groupBy = grouping, noData = sessions.Count == 0 && tasks.Count == 0 && (!medicalVisible || medicalCount == 0),
            kpi = new { sessions = sessions.Count, completed, completionRate = sessions.Count == 0 ? 0 : decimal.Round(100m * completed / sessions.Count, 2),
                actualDistanceMetres = results.Sum(x => x.DistanceMetres), actualTimeSeconds = results.Sum(x => x.TimeSeconds),
                careTasks = tasks.Count, completedCare = tasks.Count(x => x.Status == CareStatus.Completed), medicalExaminations = medicalVisible ? (int?)medicalCount : null },
            series, training = sessions.Select(x => new { x.Id, x.HorseId, x.ScheduledAt, x.Status, x.DistanceMetres, x.Target, result = resultMap.GetValueOrDefault(x.Id) }),
            care = tasks.Select(x => new { x.Id, x.HorseId, x.Type, x.Status, x.ScheduledAt, x.ApprovedPortionKg, x.ActualPortionKg }), clinical });
    }
    public static string Bucket(DateTime date, ReportGrouping group, DateTime start)
    {
        var d = date;
        return group switch { ReportGrouping.Month => d.ToString("yyyy-MM"), ReportGrouping.Week => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd"), ReportGrouping.Custom => start.ToString("yyyy-MM-dd"), _ => d.ToString("yyyy-MM-dd") };
    }
}
