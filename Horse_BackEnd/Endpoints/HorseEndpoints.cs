using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class HorseEndpoints
{
    public static void MapHorses(this RouteGroupBuilder api)
    {
        var r = api.MapGroup("/registrations").WithTags("Horse intake").RequireAuthorization();
        r.MapPost("", async (RegistrationRequest request, HorseService s) => await HorseWorkflow.PostList(request, s)).Produces<HorseRegistration>(201);
        r.MapGet("", async (CurrentUser current, ClubDbContext db, RegistrationStatus? status, int? page, int? pageSize, PageReader pager) => await HorseWorkflow.GetList(current, db, status, page, pageSize, pager)).Produces<PageResponse<HorseRegistration>>(200);
        r.MapGet("/{id:guid}", async (Guid id, ClubAccess access) => await HorseWorkflow.GetById(id, access)).Produces<HorseRegistration>(200);
        r.MapPut("/{id:guid}", async (Guid id, RegistrationRequest request, HorseService s) => await HorseWorkflow.PutById(id, request, s)).Produces<HorseRegistration>(200);
        r.MapPost("/{id:guid}/submit", async (Guid id, HorseService s) => await HorseWorkflow.PostByIdSubmit(id, s)).Produces(204);
        r.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, HorseService s) => await HorseWorkflow.PostByIdReview(id, request, s)).Produces<RegistrationReviewResponse>(200);
        r.MapPost("/{id:guid}/cancel", async (Guid id, ClubAccess access, CurrentUser current, ClubEvents events, ClubDbContext db) => await HorseWorkflow.PostByIdCancel(id, access, current, events, db)).Produces(204);

        var h = api.MapGroup("/horses").WithTags("Horses").RequireAuthorization();
        h.MapGet("", async (ClubAccess access, string? search, HealthStatus? healthStatus, int? page, int? pageSize, PageReader pager) => await HorseWorkflow.GetList2(access, search, healthStatus, page, pageSize, pager)).Produces<PageResponse<Horse>>(200);
        h.MapGet("/{id:guid}", async (Guid id, ClubAccess access, ClubDbContext db) => await HorseWorkflow.GetById2(id, access, db)).Produces<HorseDetailResponse>(200);
        h.MapPost("/{id:guid}/assignments", async (Guid id, AssignmentRequest request, HorseService s) => await HorseWorkflow.PostByIdAssignments(id, request, s)).Produces<StaffAssignment>(200);
        h.MapGet("/{id:guid}/measurements", async (Guid id, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await HorseWorkflow.GetByIdMeasurements(id, access, db, page, pageSize, pager)).Produces<PageResponse<Measurement>>(200);
        h.MapPost("/{id:guid}/measurements", async (Guid id, MeasurementRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar) => await HorseWorkflow.PostByIdMeasurements(id, request, access, current, db, events, clock, calendar)).Produces<Measurement>(200);
        h.MapPost("/{id:guid}/archive", async (Guid id, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events) => await HorseWorkflow.PostByIdArchive(id, current, access, db, events)).Produces(204);
    }
}
