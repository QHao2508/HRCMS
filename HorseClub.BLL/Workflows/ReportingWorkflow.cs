using Horse_BackEnd.Contracts;
using HorseClub.BLL.Messaging;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workflows;

public static class ReportingWorkflow
{
    public static async Task<PageResponse<Notification>> GetList(CurrentUser current, ClubDbContext db, bool? unread, int? page, int? pageSize, PageReader pager)
    {
            var id = (await current.Get()).Id; var q = db.Notifications.Where(x => x.RecipientId == id);
            if (unread == true) q = q.Where(x => !x.Read); return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<IResult> PostByIdRead(Guid id, CurrentUser current, ClubDbContext db)
    {
            var u = await current.Get(); var notification = Ensure.Found(await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientId == u.Id));
            notification.Read = true; await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<PageResponse<AuditEvent>> GetAudit(CurrentUser current, ClubDbContext db, Guid? referenceId, int? page, int? pageSize, PageReader pager)
    {
            Ensure.Role(await current.Get(), Role.ClubManager); var q = db.Audit.AsQueryable();
            if (referenceId.HasValue) q = q.Where(x => x.ReferenceId == referenceId);
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<DashboardResponse> GetDashboard(ClubAccess access, CurrentUser current, ClubDbContext db, TimeProvider clock, ClubCalendar calendar)
    {
            var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id); var now = clock.GetUtcNow();
            var (start, end) = calendar.DayRange(now);
            var sessions = db.Sessions.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.WorkRider) sessions = sessions.Where(x => x.RiderId == u.Id);
            var care = db.CareTasks.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.Groom) care = care.Where(x => x.GroomId == u.Id);
            return new DashboardResponse(await horses.CountAsync(), await db.Plans.CountAsync(x => horses.Contains(x.HorseId) && x.Status == PlanStatus.Active), await sessions.CountAsync(x => x.ScheduledAt >= start && x.ScheduledAt < end), await sessions.CountAsync(x => x.ScheduledAt < now && x.Status == SessionStatus.Assigned), await care.CountAsync(x => x.ScheduledAt >= start && x.ScheduledAt < end), await db.Restrictions.Where(x => horses.Contains(x.HorseId) && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now)).Select(x => x.HorseId).Distinct().CountAsync(), await db.Notifications.CountAsync(x => x.RecipientId == u.Id && !x.Read));
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
        List<MedicalRecord>? clinical = null;
        if (u.Role == Role.Veterinarian)
            clinical = await db.MedicalRecords.Where(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end).OrderByDescending(x => x.ExaminationAt).Take(100).ToListAsync();
        var resultMap = results.ToDictionary(x => x.SessionId);
        var series = sessions.GroupBy(x => Bucket(calendar.Local(x.ScheduledAt), grouping, calendar.Local(start))).OrderBy(x => x.Key).Select(g => new ReportSeriesResponse(g.Key, g.Count(), g.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported), g.Sum(x => x.DistanceMetres), g.Sum(x => resultMap.TryGetValue(x.Id, out var r) ? r.DistanceMetres : 0)));
        var medicalVisible = u.Role is Role.Veterinarian or Role.ClubManager or Role.HorseOwner;
        return Results.Ok(new ReportResponse(start, end, grouping, sessions.Count == 0 && tasks.Count == 0 && (!medicalVisible || medicalCount == 0), new ReportKpiResponse(sessions.Count, completed, sessions.Count == 0 ? 0 : decimal.Round(100m * completed / sessions.Count, 2), results.Sum(x => x.DistanceMetres), results.Sum(x => x.TimeSeconds), tasks.Count, tasks.Count(x => x.Status == CareStatus.Completed), medicalVisible ? (int? )medicalCount : null), series, sessions.Select(x => new ReportTrainingResponse(x.Id, x.HorseId, x.ScheduledAt, x.Status, x.DistanceMetres, x.Target, resultMap.GetValueOrDefault(x.Id))), tasks.Select(x => new ReportCareResponse(x.Id, x.HorseId, x.Type, x.Status, x.ScheduledAt, x.ApprovedPortionKg, x.ActualPortionKg)), clinical));
    }
    public static string Bucket(DateTime date, ReportGrouping group, DateTime start)
    {
        var d = date;
        return group switch { ReportGrouping.Month => d.ToString("yyyy-MM"), ReportGrouping.Week => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd"), ReportGrouping.Custom => start.ToString("yyyy-MM-dd"), _ => d.ToString("yyyy-MM-dd") };
    }
}
