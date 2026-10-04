using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class TrainingEndpoints
{
    public static void MapTraining(this RouteGroupBuilder api)
    {
        var t = api.MapGroup("/training").RequireAuthorization().WithTags("Training");
        t.AddEndpointFilter(new AllowedRolesFilter(Role.ClubManager, Role.HorseOwner, Role.HeadTrainer, Role.Trainer, Role.WorkRider, Role.Veterinarian));
        t.MapGet("/templates", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetTemplates(current, db, page, pageSize, pager)).Produces<PageResponse<TrainingTemplate>>(200);
        t.MapPost("/templates", async (TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostTemplates(r, current, db, events)).Produces<TrainingTemplate>(201);
        t.MapPut("/templates/{id:guid}", async (Guid id, TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PutTemplatesById(id, r, current, db, events)).Produces<TrainingTemplate>(200);
        t.MapPost("/templates/{id:guid}/archive", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostTemplatesByIdArchive(id, current, db, events)).Produces(204);
        t.MapPost("/plans", async (PlanRequest r, TrainingService service) => await TrainingWorkflow.PostPlans(r, service)).Produces<TrainingPlan>(201);
        t.MapGet("/plans", async (Guid? horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetPlans(horseId, access, db, page, pageSize, pager)).Produces<PageResponse<TrainingPlan>>(200);
        t.MapGet("/plans/{id:guid}", async (Guid id, ClubAccess access, ClubDbContext db, CurrentUser current) => await TrainingWorkflow.GetPlansById(id, access, db, current)).Produces<PlanDetailResponse>(200);
        t.MapPut("/plans/{id:guid}", async (Guid id, PlanRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, ClubCalendar calendar) => await TrainingWorkflow.PutPlansById(id, r, access, db, events, calendar)).Produces<TrainingPlan>(200);
        t.MapPut("/plans/{id:guid}/status", async (Guid id, PlanStatusRequest r, ClubAccess access, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PutPlansByIdStatus(id, r, access, db, events)).Produces<TrainingPlan>(200);
        t.MapGet("/plans/{id:guid}/history", async (Guid id, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetPlansByIdHistory(id, access, current, db, page, pageSize, pager)).Produces<PageResponse<TrainingRevision>>(200);
        t.MapPost("/plans/{id:guid}/sessions", async (Guid id, SessionRequest r, TrainingService service) => await TrainingWorkflow.PostPlansByIdSessions(id, r, service)).Produces<TrainingSession>(201);
        t.MapGet("/sessions", async (Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await TrainingWorkflow.GetSessions(horseId, status, from, to, access, current, db, page, pageSize, pager)).Produces<PageResponse<TrainingSession>>(200);
        t.MapGet("/sessions/{id:guid}", async (Guid id, ClubAccess access, CurrentUser current, ClubDbContext db) => await TrainingWorkflow.GetSessionsById(id, access, current, db)).Produces<SessionDetailResponse>(200);
        t.MapPut("/sessions/{id:guid}", async (Guid id, SessionRequest r, TrainingService service) => await TrainingWorkflow.PutSessionsById(id, r, service)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/assign", async (Guid id, RiderRequest r, ClubDbContext db, TrainingService service) => await TrainingWorkflow.PostSessionsByIdAssign(id, r, db, service)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/start", async (Guid id, TrainingService service) => await TrainingWorkflow.PostSessionsByIdStart(id, service)).Produces(204);
        t.MapPost("/sessions/{id:guid}/results", async (Guid id, ResultRequest r, TrainingService service) => await TrainingWorkflow.PostSessionsByIdResults(id, r, service)).Produces<SessionResult>(200);
        t.MapPost("/sessions/{id:guid}/skip", async (Guid id, ReasonRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostSessionsByIdSkip(id, r, access, current, db, events)).Produces(204);
        t.MapPost("/sessions/{id:guid}/evaluation", async (Guid id, EvaluationRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) => await TrainingWorkflow.PostSessionsByIdEvaluation(id, r, access, current, db, events)).Produces<TrainerEvaluation>(200);
    }

}
