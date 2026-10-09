using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Queries;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class TrainingRepository(ClubDbContext db) : ITrainingRepository
{
    public Task<List<TrainingNames>> PlanNamesAsync(Guid[] ids) => (from p in db.Plans.AsNoTracking() join h in db.Horses on p.HorseId equals h.Id join u in db.Users on p.TrainerId equals u.Id where ids.Contains(p.Id) select new TrainingNames(p.Id,h.Name,u.FirstName+" "+u.LastName,null)).ToListAsync();
    public Task<List<TrainingNames>> SessionNamesAsync(Guid[] ids) => (from s in db.Sessions.AsNoTracking() join p in db.Plans on s.PlanId equals p.Id join h in db.Horses on s.HorseId equals h.Id join u in db.Users on p.TrainerId equals u.Id join rider in db.Users on s.RiderId equals rider.Id into riders from rider in riders.DefaultIfEmpty() where ids.Contains(s.Id) select new TrainingNames(s.Id,h.Name,u.FirstName+" "+u.LastName,rider==null?null:rider.FirstName+" "+rider.LastName)).ToListAsync();
    public ValueTask<TrainingTemplate?> FindTemplateAsync(Guid id) => db.Templates.FindAsync(id);
    public ValueTask<TrainingPlan?> FindPlanAsync(Guid id) => db.Plans.FindAsync(id);
    public ValueTask<TrainingSession?> FindSessionAsync(Guid id) => db.Sessions.FindAsync(id);
    public ValueTask<Horse?> FindHorseAsync(Guid id) => db.Horses.FindAsync(id);
    public void AddTemplate(TrainingTemplate template) => db.Templates.Add(template);
    public void AddPlan(TrainingPlan plan) => db.Plans.Add(plan);
    public void AddSession(TrainingSession session) => db.Sessions.Add(session);
    public void AddResult(SessionResult result) => db.Results.Add(result);
    public void AddEvaluation(TrainerEvaluation evaluation) => db.Evaluations.Add(evaluation);
    public void AddIncident(Incident incident) => db.Incidents.Add(incident);
    private static async Task<DataPage<T>> Page<T>(IQueryable<T> query, int page, int size) where T : class
        => new(await query.AsNoTracking().Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    public Task<DataPage<TrainingTemplate>> ListTemplatesAsync(int page, int size)
        => Page(db.Templates.Where(x => !x.Archived).OrderBy(x => x.Name).ThenBy(x => x.Id), page, size);
    public Task<DataPage<TrainingPlan>> ListPlansAsync(HorseScope scope, Guid? horseId, Guid? riderId, int page, int size, string? search = null)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        var query = db.Plans.Where(x => horses.Contains(x.HorseId));
        if (horseId.HasValue) query = query.Where(x => x.HorseId == horseId);
        if (riderId.HasValue) query = query.Where(x => db.Sessions.Any(s => s.PlanId == x.Id && s.RiderId == riderId));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => db.Horses.Any(h=>h.Id==x.HorseId && h.Name.Contains(search)) || db.Users.Any(u=>u.Id==x.TrainerId && (u.FirstName+" "+u.LastName).Contains(search)));
        return Page(query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    }
    public Task<DataPage<TrainingSession>> ListSessionsAsync(HorseScope scope, Guid? horseId, Guid? riderId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int size, string? search = null)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        var query = db.Sessions.Where(x => horses.Contains(x.HorseId));
        if (horseId.HasValue) query = query.Where(x => x.HorseId == horseId);
        if (riderId.HasValue) query = query.Where(x => x.RiderId == riderId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (from.HasValue) query = query.Where(x => x.ScheduledAt >= from);
        if (to.HasValue) query = query.Where(x => x.ScheduledAt <= to);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x=>db.Horses.Any(h=>h.Id==x.HorseId && h.Name.Contains(search)) || db.Users.Any(u=>u.Id==x.RiderId && (u.FirstName+" "+u.LastName).Contains(search)) || db.Plans.Any(p=>p.Id==x.PlanId && db.Users.Any(u=>u.Id==p.TrainerId && (u.FirstName+" "+u.LastName).Contains(search))));
        return Page(query.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id), page, size);
    }
    public Task<DataPage<TrainingSession>> ListPlanSessionsAsync(Guid planId, Guid? riderId, int page, int size)
    {
        var query = db.Sessions.Where(x => x.PlanId == planId);
        if (riderId.HasValue) query = query.Where(x => x.RiderId == riderId);
        return Page(query.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id), page, size);
    }
    public Task<DataPage<TrainingRevision>> ListHistoryAsync(Guid planId, int page, int size)
        => Page(db.TrainingRevisions.Where(x => x.PlanId == planId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    public Task<List<TrainingSession>> GetPlanSessionsAsync(Guid planId, bool pendingOnly = false)
    {
        var query = db.Sessions.Where(x => x.PlanId == planId);
        if (pendingOnly) query = query.Where(x => x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned);
        return query.ToListAsync();
    }
    public Task<bool> HasPlanSessionsAsync(Guid planId, params SessionStatus[] statuses)
        => db.Sessions.AnyAsync(x => x.PlanId == planId && statuses.Contains(x.Status));
    public Task<bool> HasRiderPlanSessionsAsync(Guid planId, Guid riderId) => db.Sessions.AnyAsync(x => x.PlanId == planId && x.RiderId == riderId);
    public Task<bool> HasRiderConflictAsync(Guid exceptSessionId, Guid? riderId, DateTimeOffset scheduledAt)
        => db.Sessions.AnyAsync(x => x.Id != exceptSessionId && x.RiderId == riderId && x.ScheduledAt == scheduledAt && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.InProgress));
    public Task<bool> HasActiveSessionAsync(Guid riderId, Guid horseId)
        => db.Sessions.AnyAsync(x => x.Status == SessionStatus.InProgress && (x.RiderId == riderId || x.HorseId == horseId));
    public Task<bool> HasResultAsync(Guid sessionId) => db.Results.AnyAsync(x => x.SessionId == sessionId);
    public Task<bool> HasEvaluationAsync(Guid sessionId) => db.Evaluations.AnyAsync(x => x.SessionId == sessionId);
    public Task<SessionResult?> GetResultAsync(Guid sessionId) => db.Results.SingleOrDefaultAsync(x => x.SessionId == sessionId);
    public Task<TrainerEvaluation?> GetEvaluationAsync(Guid sessionId) => db.Evaluations.SingleOrDefaultAsync(x => x.SessionId == sessionId);
    public Task<List<MedicalRestriction>> GetRestrictionsAsync(Guid horseId, DateTimeOffset? effectiveAt = null)
    {
        var query = db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared);
        if (effectiveAt.HasValue) query = query.Where(x => x.ValidFrom <= effectiveAt && (x.ValidUntil == null || x.ValidUntil >= effectiveAt));
        return query.ToListAsync();
    }
}
