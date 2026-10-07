using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Reporting;

public sealed class ReportingService(CurrentUser current, ClubDbContext db, PageReader pager, ClubAccess access, TimeProvider clock, ClubCalendar calendar, IOptions<BusinessOptions> options)
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang thông báo trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="unread">Giá trị kiểu bool? dùng trong ListNotifications.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListNotifications.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListNotifications.</param>
    public async Task<PageResponse<Notification>> ListNotifications(bool? unread, int? page, int? pageSize)
    {
        var id = (await current.Get()).Id; var q = db.Notifications.Where(x => x.RecipientId == id);
        if (unread == true) q = q.Where(x => !x.Read); return await pager.Page(q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Đánh dấu Notification Read trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> MarkNotificationRead(Guid id)
    {
        var u = await current.Get(); var notification = Ensure.Found(await db.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.RecipientId == u.Id));
        notification.Read = true; await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang nhật ký thao tác trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="referenceId">Giá trị kiểu Guid? dùng trong ListAudit.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListAudit.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListAudit.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<AuditEvent>> ListAudit(Guid? referenceId, int? page, int? pageSize)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var q = db.Audit.AsQueryable();
        if (referenceId.HasValue) q = q.Where(x => x.ReferenceId == referenceId);
        return await pager.Page(q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Đọc chi tiết chỉ số tổng quan trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<DashboardResponse> GetDashboard()
    {
        var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id); var now = clock.GetUtcNow();
        var (start, end) = calendar.DayRange(now);
        var sessions = db.Sessions.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.WorkRider) sessions = sessions.Where(x => x.RiderId == u.Id);
        var care = db.CareTasks.Where(x => horses.Contains(x.HorseId)); if (u.Role == Role.Groom) care = care.Where(x => x.GroomId == u.Id);
        // Correlated counts are translated into one SQL command, avoiding seven network round trips.
        return await db.Users.Where(x => x.Id == u.Id).Select(_ => new DashboardResponse(
            horses.Count(),
            db.Plans.Count(x => horses.Contains(x.HorseId) && x.Status == PlanStatus.Active),
            sessions.Count(x => x.ScheduledAt >= start && x.ScheduledAt < end),
            sessions.Count(x => x.ScheduledAt < now && x.Status == SessionStatus.Assigned),
            care.Count(x => x.ScheduledAt >= start && x.ScheduledAt < end),
            db.Restrictions.Where(x => horses.Contains(x.HorseId) && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now)).Select(x => x.HorseId).Distinct().Count(),
            db.Notifications.Count(x => x.RecipientId == u.Id && !x.Read))).SingleAsync();
    }

    /// <summary>
    /// Tổng hợp báo cáo trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="from">Giá trị kiểu DateTimeOffset? dùng trong BuildReport.</param>
    /// <param name="to">URL nội bộ mà liên kết/redirect hướng đến.</param>
    /// <param name="groupBy">Giá trị kiểu ReportGrouping? dùng trong BuildReport.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<OperationResult> BuildReport(Guid? horseId, DateTimeOffset? from, DateTimeOffset? to, ReportGrouping? groupBy)
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
        // Bound report materialization and fetch only the columns used in its response/aggregates.
        var sessions = await sq.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.HorseId, x.ScheduledAt, x.Status, x.DistanceMetres, x.Target })
            .Take(options.Value.MaxReportRecords + 1).ToListAsync();
        var tasks = await cq.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.HorseId, x.Type, x.Status, x.ScheduledAt, x.ApprovedPortionKg, x.ActualPortionKg })
            .Take(options.Value.MaxReportRecords + 1).ToListAsync();
        Ensure.That(sessions.Count <= options.Value.MaxReportRecords && tasks.Count <= options.Value.MaxReportRecords, Messages.Get(MessageKey.TooMuchReportDataNarrowHorseDateFilters), 413, "report_limit");
        var sessionIds = sessions.Select(x => x.Id).ToArray();
        var results = await db.Results.AsNoTracking().Where(x => sessionIds.Contains(x.SessionId)).ToListAsync();
        var completed = sessions.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported);
        var medicalVisible = u.Role is Role.Veterinarian or Role.ClubManager or Role.HorseOwner;
        var medicalCount = medicalVisible ? await db.MedicalRecords.CountAsync(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end) : 0;
        List<MedicalRecord>? clinical = null;
        if (u.Role == Role.Veterinarian)
            clinical = await db.MedicalRecords.AsNoTracking().Where(x => horses.Contains(x.HorseId) && x.ExaminationAt >= start && x.ExaminationAt <= end).OrderByDescending(x => x.ExaminationAt).ThenByDescending(x => x.Id).Take(100).ToListAsync();
        var resultMap = results.ToDictionary(x => x.SessionId);
        var series = sessions.GroupBy(x => Bucket(calendar.Local(x.ScheduledAt), grouping, calendar.Local(start))).OrderBy(x => x.Key).Select(g => new ReportSeriesResponse(g.Key, g.Count(), g.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported), g.Sum(x => x.DistanceMetres), g.Sum(x => resultMap.TryGetValue(x.Id, out var r) ? r.DistanceMetres : 0)));
        return OperationResult.Ok(new ReportResponse(start, end, grouping, sessions.Count == 0 && tasks.Count == 0 && (!medicalVisible || medicalCount == 0), new ReportKpiResponse(sessions.Count, completed, sessions.Count == 0 ? 0 : decimal.Round(100m * completed / sessions.Count, 2), results.Sum(x => x.DistanceMetres), results.Sum(x => x.TimeSeconds), tasks.Count, tasks.Count(x => x.Status == CareStatus.Completed), medicalVisible ? (int?)medicalCount : null), series, sessions.Select(x => new ReportTrainingResponse(x.Id, x.HorseId, x.ScheduledAt, x.Status, x.DistanceMetres, x.Target, resultMap.GetValueOrDefault(x.Id))), tasks.Select(x => new ReportCareResponse(x.Id, x.HorseId, x.Type, x.Status, x.ScheduledAt, x.ApprovedPortionKg, x.ActualPortionKg)), clinical));
    }
    /// <summary>
    /// Nhóm ngày theo cách tổng hợp báo cáo đã chọn để thống kê nhất quán.
    /// </summary>
    /// <param name="date">Giá trị kiểu DateTime dùng trong Bucket.</param>
    /// <param name="group">Giá trị kiểu ReportGrouping dùng trong Bucket.</param>
    /// <param name="start">Giá trị kiểu DateTime dùng trong Bucket.</param>
    public static string Bucket(DateTime date, ReportGrouping group, DateTime start)
    {
        var d = date;
        return group switch { ReportGrouping.Month => d.ToString("yyyy-MM"), ReportGrouping.Week => d.AddDays(-(((int)d.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd"), ReportGrouping.Custom => start.ToString("yyyy-MM-dd"), _ => d.ToString("yyyy-MM-dd") };
    }

}
