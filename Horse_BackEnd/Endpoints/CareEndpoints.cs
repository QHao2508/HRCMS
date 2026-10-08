using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Endpoints;

public static class CareEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Care với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapCare.</param>
    public static void MapCare(this RouteGroupBuilder api)
    {
        var c = api.MapGroup("/care").RequireAuthorization().WithTags("Care and stable");
        c.MapGet("/tasks", async (Guid? horseId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize, ICareService moduleService) => await moduleService.ListTasks(horseId, status, from, to, page, pageSize)).Produces<PageResponse<CareTaskSummaryResponse>>(200);
        c.MapPost("/tasks", async (CareRequest r, ICareService moduleService) => await moduleService.CreateTask(r)).Produces<CareTask>(200);
        c.MapPost("/tasks/{id:guid}/record", async (Guid id, CareCompletionRequest r, ICareService moduleService) => await moduleService.RecordTask(id, r)).Produces<CareTask>(200);
        c.MapGet("/incidents", async (Guid? horseId, int? page, int? pageSize, ICareService moduleService) => await moduleService.ListIncidents(horseId, page, pageSize)).Produces<PageResponse<Incident>>(200);
        c.MapPost("/incidents", async (IncidentRequest r, ICareService moduleService) => await moduleService.ReportIncident(r)).Produces<Incident>(200);
        c.MapPost("/incidents/{id:guid}/resolve", async (Guid id, ICareService moduleService) => (await moduleService.ResolveIncident(id)).ToHttpResult()).Produces(204);
        c.MapGet("/stables", async (int? page, int? pageSize, ICareService moduleService) => await moduleService.ListStables(page, pageSize)).Produces<PageResponse<Stable>>(200);
        c.MapPost("/stables", async (NameRequest r, ICareService moduleService) => await moduleService.CreateStable(r)).Produces<Stable>(200);
        c.MapGet("/stalls", async (Guid? stableId, int? page, int? pageSize, ICareService moduleService) => await moduleService.ListStalls(stableId, page, pageSize)).Produces<PageResponse<StallSummaryResponse>>(200);
        c.MapPost("/stalls", async (StallRequest r, ICareService moduleService) => await moduleService.CreateStall(r)).Produces<Stall>(200);
        c.MapPost("/stalls/{id:guid}/occupancy", async (Guid id, OccupancyRequest r, ICareService moduleService) => await moduleService.OccupyStall(id, r)).Produces<StallOccupancy>(200);
        c.MapPost("/stalls/{id:guid}/vacate", async (Guid id, ICareService moduleService) => (await moduleService.VacateStall(id)).ToHttpResult()).Produces(204);
        c.MapPost("/stalls/{id:guid}/cleaned", async (Guid id, ICareService moduleService) => (await moduleService.MarkStallClean(id)).ToHttpResult()).Produces(204);
    }
}
