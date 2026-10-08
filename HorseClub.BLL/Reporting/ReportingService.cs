using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Reporting;

public sealed class ReportingService(CurrentUser current, IReportingQueries queries, IUnitOfWork unitOfWork, PageReader pager, ClubAccess access, TimeProvider clock, ClubCalendar calendar, IOptions<BusinessOptions> options) : IReportingService
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang thông báo trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="unread">Giá trị kiểu bool? dùng trong ListNotifications.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListNotifications.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListNotifications.</param>
    public async Task<PageResponse<Notification>> ListNotifications(bool? unread, int? page, int? pageSize)
    {
        var id = (await current.Get()).Id;
        return await pager.Page(page, pageSize, (p, size) => queries.ListNotificationsAsync(id, unread == true, p, size));
    }

    /// <summary>
    /// Đánh dấu Notification Read trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.</remarks>
    public async Task<OperationResult> MarkNotificationRead(Guid id)
    {
        var u = await current.Get(); var notification = Ensure.Found(await queries.GetNotificationAsync(u.Id, id));
        notification.Read = true; await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
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
        Ensure.Role(await current.Get(), Role.ClubManager);
        return await pager.Page(page, pageSize, (p, size) => queries.ListAuditAsync(referenceId, p, size));
    }

    /// <summary>
    /// Đọc chi tiết chỉ số tổng quan trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<DashboardResponse> GetDashboard()
    {
        var u = await current.Get(); var scope = await access.Scope(); var now = clock.GetUtcNow();
        var (start, end) = calendar.DayRange(now);
        var data = await queries.DashboardAsync(scope, u.Id, u.Role == Role.WorkRider ? u.Id : null, u.Role == Role.Groom ? u.Id : null, now, start, end);
        return new(data.HorseCount, data.ActivePlans, data.SessionsToday, data.OverdueSessions, data.CareTasksToday, data.RestrictedHorses, data.UnreadNotifications);
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
        var scope = await access.Scope();
        if (horseId.HasValue) await access.Horse(horseId.Value);
        var rows = await queries.ReportRowsAsync(scope, horseId, u.Role == Role.WorkRider ? u.Id : null, u.Role == Role.Groom ? u.Id : null, start, end, options.Value.MaxReportRecords);
        var sessions = rows.Sessions; var tasks = rows.Tasks;
        Ensure.That(sessions.Count <= options.Value.MaxReportRecords && tasks.Count <= options.Value.MaxReportRecords, Messages.Get(MessageKey.TooMuchReportDataNarrowHorseDateFilters), 413, "report_limit");
        var sessionIds = sessions.Select(x => x.Id).ToArray();
        var results = await queries.ResultsAsync(sessionIds);
        var completed = sessions.Count(x => x.Status is SessionStatus.Completed or SessionStatus.IssueReported);
        var medicalVisible = u.Role is Role.Veterinarian or Role.ClubManager or Role.HorseOwner;
        var medicalCount = medicalVisible ? await queries.MedicalCountAsync(scope, horseId, start, end) : 0;
        List<MedicalRecord>? clinical = null;
        if (u.Role == Role.Veterinarian)
            clinical = await queries.ClinicalAsync(scope, horseId, start, end);
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
