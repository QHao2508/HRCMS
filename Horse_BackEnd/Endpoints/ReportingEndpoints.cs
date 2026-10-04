using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.Extensions.Options;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class ReportingEndpoints
{
    public static void MapReporting(this RouteGroupBuilder api)
    {
        var n = api.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization();
        n.MapGet("", async (CurrentUser current, ClubDbContext db, bool? unread, int? page, int? pageSize, PageReader pager) => await ReportingWorkflow.GetList(current, db, unread, page, pageSize, pager)).Produces<PageResponse<Notification>>(200);
        n.MapPost("/{id:guid}/read", async (Guid id, CurrentUser current, ClubDbContext db) => await ReportingWorkflow.PostByIdRead(id, current, db)).Produces(204);
        api.MapGet("/audit", async (CurrentUser current, ClubDbContext db, Guid? referenceId, int? page, int? pageSize, PageReader pager) => await ReportingWorkflow.GetAudit(current, db, referenceId, page, pageSize, pager)).RequireAuthorization().WithTags("Audit").Produces<PageResponse<AuditEvent>>(200);
        api.MapGet("/reports", async (Guid? horseId, DateTimeOffset? from, DateTimeOffset? to, ReportGrouping? groupBy, ClubAccess access, CurrentUser current, ClubDbContext db, TimeProvider clock, IOptions<BusinessOptions> options, ClubCalendar calendar) => await ReportingWorkflow.BuildReport(horseId, from, to, groupBy, access, current, db, clock, options, calendar)).RequireAuthorization().WithTags("Reports").Produces<ReportResponse>(200);
        api.MapGet("/dashboard", async (ClubAccess access, CurrentUser current, ClubDbContext db, TimeProvider clock, ClubCalendar calendar) => await ReportingWorkflow.GetDashboard(access, current, db, clock, calendar)).RequireAuthorization().WithTags("Dashboard").Produces<DashboardResponse>(200);
    }

}
