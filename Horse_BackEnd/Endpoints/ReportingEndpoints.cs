using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Endpoints;

public static class ReportingEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Reporting với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapReporting.</param>
    public static void MapReporting(this RouteGroupBuilder api)
    {
        var n = api.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization();
        n.MapGet("", async (bool? unread, int? page, int? pageSize, IReportingService moduleService) => await moduleService.ListNotifications(unread, page, pageSize)).Produces<PageResponse<Notification>>(200);
        n.MapPost("/{id:guid}/read", async (Guid id, IReportingService moduleService) => (await moduleService.MarkNotificationRead(id)).ToHttpResult()).Produces(204);
        api.MapGet("/audit", async (Guid? referenceId, int? page, int? pageSize, IReportingService moduleService) => await moduleService.ListAudit(referenceId, page, pageSize)).RequireAuthorization().WithTags("Audit").Produces<PageResponse<AuditEvent>>(200);
        api.MapGet("/reports", async (Guid? horseId, DateTimeOffset? from, DateTimeOffset? to, ReportGrouping? groupBy, IReportingService moduleService) => (await moduleService.BuildReport(horseId, from, to, groupBy)).ToHttpResult()).RequireAuthorization().WithTags("Reports").Produces<ReportResponse>(200);
        api.MapGet("/dashboard", async (IReportingService moduleService) => await moduleService.GetDashboard()).RequireAuthorization().WithTags("Dashboard").Produces<DashboardResponse>(200);
    }

}
