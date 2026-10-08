using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by TrainingPlanService.</summary>
public interface ITrainingPlanService
{
    Task<TrainingPlan> CreatePlan(PlanRequest r);
    Task<OperationResult> Create(PlanRequest r);
    Task<PageResponse<TrainingPlan>> ListPlans(Guid? horseId, int? page, int? pageSize);
    Task<PlanDetailResponse> GetPlan(Guid id, int? sessionPage, int? sessionPageSize);
    Task<TrainingPlan> UpdatePlan(Guid id, PlanRequest r);
    Task<TrainingPlan> SetPlanStatus(Guid id, PlanStatusRequest r);
    Task<PageResponse<TrainingRevision>> ListHistory(Guid id, int? page, int? pageSize);
}
