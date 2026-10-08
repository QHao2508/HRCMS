using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Endpoints;

public static class HorseEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Horses với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapHorses.</param>
    public static void MapHorses(this RouteGroupBuilder api)
    {
        var r = api.MapGroup("/registrations").WithTags("Horse intake").RequireAuthorization();
        r.MapPost("", async (RegistrationDraftRequest request, IHorseRegistrationService moduleService) => (await moduleService.CreateDraft(request)).ToHttpResult()).Produces<HorseRegistration>(201);
        r.MapGet("", async (RegistrationStatus? status, int? page, int? pageSize, IHorseRegistrationService moduleService) => await moduleService.ListRegistrations(status, page, pageSize)).Produces<PageResponse<HorseRegistration>>(200);
        r.MapGet("/{id:guid}", async (Guid id, IHorseRegistrationService moduleService) => await moduleService.GetRegistration(id)).Produces<HorseRegistration>(200);
        r.MapPut("/{id:guid}", async (Guid id, RegistrationDraftRequest request, IHorseRegistrationService moduleService) => await moduleService.UpdateDraft(id, request)).Produces<HorseRegistration>(200);
        r.MapPost("/{id:guid}/submit", async (Guid id, IHorseRegistrationService moduleService) => (await moduleService.SubmitRegistration(id)).ToHttpResult()).Produces(204);
        r.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, IHorseRegistrationService moduleService) => await moduleService.ReviewRegistration(id, request)).Produces<RegistrationReviewResponse>(200);
        r.MapPost("/{id:guid}/cancel", async (Guid id, IHorseRegistrationService moduleService) => (await moduleService.CancelRegistration(id)).ToHttpResult()).Produces(204);

        var h = api.MapGroup("/horses").WithTags("Horses").RequireAuthorization();
        h.MapGet("", async (string? search, HealthStatus? healthStatus, int? page, int? pageSize, IHorseProfileService moduleService) => await moduleService.ListHorses(search, healthStatus, page, pageSize)).Produces<PageResponse<Horse>>(200);
        h.MapGet("/{id:guid}", async (Guid id, IHorseProfileService moduleService) => await moduleService.GetHorse(id)).Produces<HorseDetailResponse>(200);
        h.MapPost("/{id:guid}/assignments", async (Guid id, AssignmentRequest request, IHorseAssignmentService moduleService) => await moduleService.AssignStaff(id, request)).Produces<StaffAssignment>(200);
        h.MapGet("/{id:guid}/measurements", async (Guid id, int? page, int? pageSize, IHorseProfileService moduleService) => await moduleService.ListMeasurements(id, page, pageSize)).Produces<PageResponse<Measurement>>(200);
        h.MapPost("/{id:guid}/measurements", async (Guid id, MeasurementRequest request, IHorseProfileService moduleService) => await moduleService.AddMeasurement(id, request)).Produces<Measurement>(200);
        h.MapPost("/{id:guid}/archive", async (Guid id, IHorseProfileService moduleService) => (await moduleService.ArchiveHorse(id)).ToHttpResult()).Produces(204);
    }
}
