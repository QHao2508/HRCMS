using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Endpoints;

public static class TrainingEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Training với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapTraining.</param>
    public static void MapTraining(this RouteGroupBuilder api)
    {
        var t = api.MapGroup("/training").RequireAuthorization().WithTags("Training");
        t.AddEndpointFilter(new AllowedRolesFilter(Role.ClubManager, Role.HorseOwner, Role.HeadTrainer, Role.Trainer, Role.WorkRider, Role.Veterinarian));
<<<<<<< HEAD
=======
<<<<<<< HEAD
        t.MapGet("/templates", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetTemplates(current, db, page, pageSize, pager));
        t.MapPost("/templates", async (TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostTemplates(r, current, db, events));
        t.MapPut("/templates/{id:guid}", async (Guid id, TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PutTemplatesById(id, r, current, db, events));
        t.MapPost("/templates/{id:guid}/archive", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostTemplatesByIdArchive(id, current, db, events));
        t.MapPost("/plans", async (PlanRequest r, TrainingService service) => await TrainingWorkflow.PostPlans(r, service));
        t.MapGet("/plans", async (Guid? horseId, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetPlans(horseId, access, current, db, page, pageSize, pager));
        t.MapGet("/plans/{id:guid}", async (Guid id, ClubAccess access, ClubDbContext db, CurrentUser current) => await TrainingWorkflow.GetPlansById(id, access, db, current));
        t.MapPut("/plans/{id:guid}", async (Guid id, PlanRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, ClubCalendar calendar) => await TrainingWorkflow.PutPlansById(id, r, access, db, events, calendar));
        t.MapPut("/plans/{id:guid}/status", async (Guid id, PlanStatusRequest r, ClubAccess access, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PutPlansByIdStatus(id, r, access, db, events));
        t.MapGet("/plans/{id:guid}/history", async (Guid id, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetPlansByIdHistory(id, access, current, db, page, pageSize, pager));
        t.MapPost("/plans/{id:guid}/sessions", async (Guid id, SessionRequest r, TrainingService service) => await TrainingWorkflow.PostPlansByIdSessions(id, r, service));
        t.MapGet("/sessions", async (Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetSessions(horseId, status, from, to, access, current, db, page, pageSize, pager));
        t.MapGet("/sessions/{id:guid}", async (Guid id, ClubAccess access, CurrentUser current, ClubDbContext db) => await TrainingWorkflow.GetSessionsById(id, access, current, db));
        t.MapPut("/sessions/{id:guid}", async (Guid id, SessionRequest r, TrainingService service) => await TrainingWorkflow.PutSessionsById(id, r, service));
        t.MapPost("/sessions/{id:guid}/assign", async (Guid id, RiderRequest r, ClubDbContext db, TrainingService service) => await TrainingWorkflow.PostSessionsByIdAssign(id, r, db, service));
        t.MapPost("/sessions/{id:guid}/start", async (Guid id, TrainingService service) => await TrainingWorkflow.PostSessionsByIdStart(id, service));
        t.MapPost("/sessions/{id:guid}/results", async (Guid id, ResultRequest r, TrainingService service) => await TrainingWorkflow.PostSessionsByIdResults(id, r, service));
        t.MapPost("/sessions/{id:guid}/skip", async (Guid id, ReasonRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostSessionsByIdSkip(id, r, access, current, db, events));
        t.MapPost("/sessions/{id:guid}/evaluation", async (Guid id, EvaluationRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostSessionsByIdEvaluation(id, r, access, current, db, events));
=======
>>>>>>> 5db325c (task 22-23)
        t.MapGet("/reference-data", async (ITrainingSchedulingService service) => await service.ReferenceData()).Produces<System.Text.Json.JsonElement>(200);
        t.MapGet("/availability", async (Guid horseId, DateOnly date, int durationMinutes, TrainingType trainingType, Intensity intensity, decimal distanceMetres,
            Guid? riderId, Guid? exceptSessionId, ITrainingSchedulingService service) => await service.Availability(horseId, date, durationMinutes, trainingType, intensity, distanceMetres, riderId, exceptSessionId))
            .Produces<TrainingAvailabilityResponse>(200);
        t.MapGet("/trainer-candidates", async (Guid horseId, TrainingType trainingType, ITrainingSchedulingService service) => await service.Candidates(horseId, trainingType))
            .Produces<List<TrainerCandidateResponse>>(200);
        t.MapGet("/templates", async (int? page, int? pageSize, ITrainingTemplateService moduleService) => await moduleService.ListTemplates(page, pageSize)).Produces<PageResponse<TrainingTemplate>>(200);
        t.MapGet("/templates/defaults", async (ITrainingTemplateService service) => await service.Defaults()).Produces<List<TrainingTemplate>>(200);
        t.MapGet("/templates/{id:guid}/trainer-candidates", async (Guid id, ITrainingTemplateService service) => await service.Candidates(id)).Produces<List<TemplateTrainerCandidate>>(200);
        t.MapPost("/templates/{id:guid}/assign", async (Guid id, TemplateTrainerRequest request, ITrainingTemplateService service) => (await service.AssignTrainer(id, request)).ToHttpResult()).Produces(204);
        t.MapPost("/templates", async (TemplateRequest r, ITrainingTemplateService moduleService) => (await moduleService.CreateTemplate(r)).ToHttpResult()).Produces<TrainingTemplate>(201);
        t.MapPut("/templates/{id:guid}", async (Guid id, TemplateRequest r, ITrainingTemplateService moduleService) => await moduleService.UpdateTemplate(id, r)).Produces<TrainingTemplate>(200);
        t.MapPost("/templates/{id:guid}/archive", async (Guid id, ITrainingTemplateService moduleService) => (await moduleService.ArchiveTemplate(id)).ToHttpResult()).Produces(204);
        t.MapPost("/plans", async (PlanRequest r, ITrainingPlanService moduleService) => (await moduleService.Create(r)).ToHttpResult()).Produces<TrainingPlan>(201);
        t.MapGet("/plans", async (Guid? horseId, string? search, int? page, int? pageSize, ITrainingPlanService moduleService) => await moduleService.ListPlans(horseId, page, pageSize, search)).Produces<PageResponse<TrainingPlanListItem>>(200);
        t.MapGet("/plans/{id:guid}", async (Guid id, int? sessionPage, int? sessionPageSize, ITrainingPlanService moduleService) => await moduleService.GetPlan(id, sessionPage, sessionPageSize)).Produces<PlanDetailResponse>(200);
        t.MapPut("/plans/{id:guid}", async (Guid id, PlanRequest r, ITrainingPlanService moduleService) => await moduleService.UpdatePlan(id, r)).Produces<TrainingPlan>(200);
        t.MapPut("/plans/{id:guid}/status", async (Guid id, PlanStatusRequest r, ITrainingPlanService moduleService) => await moduleService.SetPlanStatus(id, r)).Produces<TrainingPlan>(200);
        t.MapGet("/plans/{id:guid}/history", async (Guid id, int? page, int? pageSize, ITrainingPlanService moduleService) => await moduleService.ListHistory(id, page, pageSize)).Produces<PageResponse<TrainingRevision>>(200);
        t.MapPost("/plans/{id:guid}/sessions", async (Guid id, SessionRequest r, ITrainingSessionService moduleService) => (await moduleService.Create(id, r)).ToHttpResult()).Produces<TrainingSession>(201);
        t.MapGet("/sessions", async (Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, string? search, int? page, int? pageSize, ITrainingSessionService moduleService) => await moduleService.ListSessions(horseId, status, from, to, page, pageSize, search)).Produces<PageResponse<TrainingSessionListItem>>(200);
        t.MapGet("/sessions/{id:guid}", async (Guid id, ITrainingSessionService moduleService) => await moduleService.GetSession(id)).Produces<SessionDetailResponse>(200);
        t.MapPut("/sessions/{id:guid}", async (Guid id, SessionRequest r, ITrainingSessionService moduleService) => await moduleService.Update(id, r)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/assign", async (Guid id, RiderRequest r, ITrainingSessionService moduleService) => await moduleService.AssignRider(id, r)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/start", async (Guid id, ITrainingSessionService moduleService) => (await moduleService.StartSession(id)).ToHttpResult()).Produces(204);
        t.MapPost("/sessions/{id:guid}/results", async (Guid id, ResultRequest r, ITrainingSessionService moduleService) => await moduleService.RecordResult(id, r)).Produces<SessionResult>(200);
        t.MapPost("/sessions/{id:guid}/skip", async (Guid id, ReasonRequest r, ITrainingSessionService moduleService) => (await moduleService.SkipSession(id, r)).ToHttpResult()).Produces(204);
        t.MapPost("/sessions/{id:guid}/evaluation", async (Guid id, EvaluationRequest r, ITrainingSessionService moduleService) => await moduleService.EvaluateSession(id, r)).Produces<TrainerEvaluation>(200);
<<<<<<< HEAD
=======
>>>>>>> main
>>>>>>> 5db325c (task 22-23)
    }

}
