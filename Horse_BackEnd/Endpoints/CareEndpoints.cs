using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class CareEndpoints
{
    public static void MapCare(this RouteGroupBuilder api)
    {
        var c = api.MapGroup("/care").RequireAuthorization().WithTags("Care and stable");
        c.MapGet("/tasks", async (Guid? horseId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await CareWorkflow.GetTasks(horseId, status, from, to, access, current, db, page, pageSize, pager));
        c.MapPost("/tasks", async (CareRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await CareWorkflow.PostTasks(r, access, current, db, events));
        c.MapPost("/tasks/{id:guid}/record", async (Guid id, CareCompletionRequest r, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock) => await CareWorkflow.PostTasksByIdRecord(id, r, current, access, db, events, clock));
        c.MapGet("/incidents", async (Guid? horseId, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await CareWorkflow.GetIncidents(horseId, access, current, db, page, pageSize, pager));
        c.MapPost("/incidents", async (IncidentRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await CareWorkflow.PostIncidents(r, access, current, db, events));
        c.MapPost("/incidents/{id:guid}/resolve", async (Guid id, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events) => await CareWorkflow.PostIncidentsByIdResolve(id, current, access, db, events));
        c.MapGet("/stables", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await CareWorkflow.GetStables(current, db, page, pageSize, pager));
        c.MapPost("/stables", async (NameRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await CareWorkflow.PostStables(r, current, db, events));
        c.MapGet("/stalls", async (Guid? stableId, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await CareWorkflow.GetStalls(stableId, current, db, page, pageSize, pager));
        c.MapPost("/stalls", async (StallRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await CareWorkflow.PostStalls(r, current, db, events));
        c.MapPost("/stalls/{id:guid}/occupancy", async (Guid id, OccupancyRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock) => await CareWorkflow.PostStallsByIdOccupancy(id, r, access, current, db, events, clock));
        c.MapPost("/stalls/{id:guid}/vacate", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock) => await CareWorkflow.PostStallsByIdVacate(id, current, db, events, clock));
        c.MapPost("/stalls/{id:guid}/cleaned", async (Guid id, CurrentUser current, ClubDbContext db, ClubAccess access, ClubEvents events) => await CareWorkflow.PostStallsByIdCleaned(id, current, db, access, events));
    }
}
