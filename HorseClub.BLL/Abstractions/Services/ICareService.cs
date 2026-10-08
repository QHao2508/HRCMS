using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by CareService.</summary>
public interface ICareService
{
    Task<PageResponse<CareTaskSummaryResponse>> ListTasks(Guid? horseId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize);
    Task<CareTask> CreateTask(CareRequest r);
    Task<CareTask> RecordTask(Guid id, CareCompletionRequest r);
    Task<PageResponse<Incident>> ListIncidents(Guid? horseId, int? page, int? pageSize);
    Task<Incident> ReportIncident(IncidentRequest r);
    Task<OperationResult> ResolveIncident(Guid id);
    Task<PageResponse<Stable>> ListStables(int? page, int? pageSize);
    Task<Stable> CreateStable(NameRequest r);
    Task<PageResponse<StallSummaryResponse>> ListStalls(Guid? stableId, int? page, int? pageSize);
    Task<Stall> CreateStall(StallRequest r);
    Task<StallOccupancy> OccupyStall(Guid id, OccupancyRequest r);
    Task<OperationResult> VacateStall(Guid id);
    Task<OperationResult> MarkStallClean(Guid id);
}
