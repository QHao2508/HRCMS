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
        t.MapGet("/templates", async (int? page, int? pageSize, ITrainingTemplateService moduleService) => await moduleService.ListTemplates(page, pageSize)).Produces<PageResponse<TrainingTemplate>>(200);
        t.MapPost("/templates", async (TemplateRequest r, ITrainingTemplateService moduleService) => (await moduleService.CreateTemplate(r)).ToHttpResult()).Produces<TrainingTemplate>(201);
        t.MapPut("/templates/{id:guid}", async (Guid id, TemplateRequest r, ITrainingTemplateService moduleService) => await moduleService.UpdateTemplate(id, r)).Produces<TrainingTemplate>(200);
        t.MapPost("/templates/{id:guid}/archive", async (Guid id, ITrainingTemplateService moduleService) => (await moduleService.ArchiveTemplate(id)).ToHttpResult()).Produces(204);
        t.MapPost("/plans", async (PlanRequest r, ITrainingPlanService moduleService) => (await moduleService.Create(r)).ToHttpResult()).Produces<TrainingPlan>(201);
        t.MapGet("/plans", async (Guid? horseId, int? page, int? pageSize, ITrainingPlanService moduleService) => await moduleService.ListPlans(horseId, page, pageSize)).Produces<PageResponse<TrainingPlan>>(200);
        t.MapGet("/plans/{id:guid}", async (Guid id, int? sessionPage, int? sessionPageSize, ITrainingPlanService moduleService) => await moduleService.GetPlan(id, sessionPage, sessionPageSize)).Produces<PlanDetailResponse>(200);
        t.MapPut("/plans/{id:guid}", async (Guid id, PlanRequest r, ITrainingPlanService moduleService) => await moduleService.UpdatePlan(id, r)).Produces<TrainingPlan>(200);
        t.MapPut("/plans/{id:guid}/status", async (Guid id, PlanStatusRequest r, ITrainingPlanService moduleService) => await moduleService.SetPlanStatus(id, r)).Produces<TrainingPlan>(200);
        t.MapGet("/plans/{id:guid}/history", async (Guid id, int? page, int? pageSize, ITrainingPlanService moduleService) => await moduleService.ListHistory(id, page, pageSize)).Produces<PageResponse<TrainingRevision>>(200);
        t.MapPost("/plans/{id:guid}/sessions", async (Guid id, SessionRequest r, ITrainingSessionService moduleService) => (await moduleService.Create(id, r)).ToHttpResult()).Produces<TrainingSession>(201);
        t.MapGet("/sessions", async (Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize, ITrainingSessionService moduleService) => await moduleService.ListSessions(horseId, status, from, to, page, pageSize)).Produces<PageResponse<TrainingSession>>(200);
        t.MapGet("/sessions/{id:guid}", async (Guid id, ITrainingSessionService moduleService) => await moduleService.GetSession(id)).Produces<SessionDetailResponse>(200);
        t.MapPut("/sessions/{id:guid}", async (Guid id, SessionRequest r, ITrainingSessionService moduleService) => await moduleService.Update(id, r)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/assign", async (Guid id, RiderRequest r, ITrainingSessionService moduleService) => await moduleService.AssignRider(id, r)).Produces<TrainingSession>(200);
        t.MapPost("/sessions/{id:guid}/start", async (Guid id, ITrainingSessionService moduleService) => (await moduleService.StartSession(id)).ToHttpResult()).Produces(204);
        t.MapPost("/sessions/{id:guid}/results", async (Guid id, ResultRequest r, ITrainingSessionService moduleService) => await moduleService.RecordResult(id, r)).Produces<SessionResult>(200);
        t.MapPost("/sessions/{id:guid}/skip", async (Guid id, ReasonRequest r, ITrainingSessionService moduleService) => (await moduleService.SkipSession(id, r)).ToHttpResult()).Produces(204);
        t.MapPost("/sessions/{id:guid}/evaluation", async (Guid id, EvaluationRequest r, ITrainingSessionService moduleService) => await moduleService.EvaluateSession(id, r)).Produces<TrainerEvaluation>(200);
    }

}
