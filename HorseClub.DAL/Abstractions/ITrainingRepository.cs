namespace HorseClub.DAL.Abstractions;

public interface ITrainingRepository
{
    Task<List<TrainingNames>> PlanNamesAsync(Guid[] ids);
    Task<List<TrainingNames>> SessionNamesAsync(Guid[] ids);
    ValueTask<TrainingTemplate?> FindTemplateAsync(Guid id);
    ValueTask<TrainingPlan?> FindPlanAsync(Guid id);
    ValueTask<TrainingSession?> FindSessionAsync(Guid id);
    ValueTask<Horse?> FindHorseAsync(Guid id);
    void AddTemplate(TrainingTemplate template);
    void AddPlan(TrainingPlan plan);
    void AddSession(TrainingSession session);
    void AddResult(SessionResult result);
    void AddEvaluation(TrainerEvaluation evaluation);
    void AddIncident(Incident incident);
    Task<DataPage<TrainingTemplate>> ListTemplatesAsync(int page, int size);
    Task<DataPage<TrainingPlan>> ListPlansAsync(HorseScope scope, Guid? horseId, Guid? riderId, int page, int size, string? search = null);
    Task<DataPage<TrainingSession>> ListSessionsAsync(HorseScope scope, Guid? horseId, Guid? riderId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int size, string? search = null);
    Task<DataPage<TrainingSession>> ListPlanSessionsAsync(Guid planId, Guid? riderId, int page, int size);
    Task<DataPage<TrainingRevision>> ListHistoryAsync(Guid planId, int page, int size);
    Task<List<TrainingSession>> GetPlanSessionsAsync(Guid planId, bool pendingOnly = false);
    Task<bool> HasPlanSessionsAsync(Guid planId, params SessionStatus[] statuses);
    Task<bool> HasRiderPlanSessionsAsync(Guid planId, Guid riderId);
    Task<bool> HasRiderConflictAsync(Guid exceptSessionId, Guid? riderId, DateTimeOffset scheduledAt);
    Task<bool> HasActiveSessionAsync(Guid riderId, Guid horseId);
    Task<bool> HasResultAsync(Guid sessionId);
    Task<bool> HasEvaluationAsync(Guid sessionId);
    Task<SessionResult?> GetResultAsync(Guid sessionId);
    Task<TrainerEvaluation?> GetEvaluationAsync(Guid sessionId);
    Task<List<MedicalRestriction>> GetRestrictionsAsync(Guid horseId, DateTimeOffset? effectiveAt = null);
}
