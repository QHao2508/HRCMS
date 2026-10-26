using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by ReportingService.</summary>
public interface IReportingService
{
    Task<PageResponse<NotificationResponse>> ListNotifications(bool? unread, int? page, int? pageSize);
    Task<OperationResult> MarkNotificationRead(Guid id);
    Task<OperationResult> MarkAllNotificationsRead();
    Task<PageResponse<AuditEvent>> ListAudit(Guid? referenceId, int? page, int? pageSize);
    Task<DashboardResponse> GetDashboard();
    Task<OperationResult> BuildReport(Guid? horseId, DateTimeOffset? from, DateTimeOffset? to, ReportGrouping? groupBy);
}
