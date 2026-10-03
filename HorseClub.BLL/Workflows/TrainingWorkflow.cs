using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Workflows;

public static class TrainingWorkflow
{
    public static async Task<object> GetTemplates(CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { Ensure.Role(await current.Get(), Role.ClubManager, Role.HeadTrainer, Role.Trainer); return await pager.Page(db.Templates.Where(x => !x.Archived).OrderBy(x => x.Name), page, pageSize); }

    public static async Task<object> PostTemplates(TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            Ensure.Role(await current.Get(), Role.HeadTrainer);
            var template = new TrainingTemplate(); Apply(template, r); db.Templates.Add(template);
            await events.Audit(AuditAction.TrainingTemplateCreated, template.Id); await db.SaveChangesAsync(); return Results.Created($"/api/training/templates/{template.Id}", template);
        }

    public static async Task<object> PutTemplatesById(Guid id, TemplateRequest r, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            Ensure.Role(await current.Get(), Role.HeadTrainer); var template = Ensure.Found(await db.Templates.FindAsync(id));
            Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived)); Apply(template, r);
            await events.Audit(AuditAction.TrainingTemplateEdited, id); await db.SaveChangesAsync(); return template;
        }

    public static async Task<object> PostTemplatesByIdArchive(Guid id, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            Ensure.Role(await current.Get(), Role.HeadTrainer); (Ensure.Found(await db.Templates.FindAsync(id))).Archived = true;
            await events.Audit(AuditAction.TrainingTemplateArchived, id); await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> PostPlans(PlanRequest r, TrainingService service)
    { var p = await service.CreatePlan(r); return Results.Created($"/api/training/plans/{p.Id}", p); }

    public static async Task<object> GetPlans(Guid? horseId, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    {
            var horses = (await access.Horses()).Select(x => x.Id); var q = db.Plans.Where(x => horses.Contains(x.HorseId));
            if (horseId.HasValue) { await access.Horse(horseId.Value); q = q.Where(x => x.HorseId == horseId); }
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<object> GetPlansById(Guid id, ClubAccess access, ClubDbContext db, CurrentUser current)
    {
            var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Horse(p.HorseId); var u = await current.Get();
            var sessions = db.Sessions.Where(x => x.PlanId == id);
            if (u.Role == Role.WorkRider) sessions = sessions.Where(x => x.RiderId == u.Id);
            return new { plan = p, sessions = await sessions.OrderBy(x => x.ScheduledAt).Take(100).ToListAsync(),
                restrictions = await db.Restrictions.Where(x => x.HorseId == p.HorseId && !x.Cleared).ToListAsync() };
        }

    public static async Task<object> PutPlansById(Guid id, PlanRequest r, ClubAccess access, ClubDbContext db, ClubEvents events, ClubCalendar calendar)
    {
            var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Trainer(p.HorseId);
            Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Get(MessageKey.PlanCannotBeEditedInThisState), 409, "invalid_state");
            Ensure.That(r.HorseId == p.HorseId && r.TemplateId == p.TemplateId, Messages.Get(MessageKey.HorseAndTemplateCannotBeChangedAfterPlanCreation));
            Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, Messages.Get(MessageKey.InvalidDates));
            var sessions = await db.Sessions.Where(x => x.PlanId == id).ToListAsync();
            Ensure.That(sessions.All(x => calendar.DateAt(x.ScheduledAt) >= r.StartDate && calendar.DateAt(x.ScheduledAt) <= r.EndDate), Messages.Get(MessageKey.DatesWouldExcludeExistingSessions));
            p.Goal = r.Goal; p.Phase = r.Phase; p.Notes = r.Notes; p.StartDate = r.StartDate; p.EndDate = r.EndDate;
            await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanEdited, id); await db.SaveChangesAsync(); return p;
        }

    public static async Task<object> PutPlansByIdStatus(Guid id, PlanStatusRequest r, ClubAccess access, ClubDbContext db, ClubEvents events)
    {
            var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Trainer(p.HorseId);
            Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Get(MessageKey.CompletedArchivedPlansCannotBeReopened), 409, "invalid_state");
            if (r.Status != PlanStatus.Active)
                Ensure.That(!await db.Sessions.AnyAsync(x => x.PlanId == id && x.Status == SessionStatus.InProgress), Messages.Get(MessageKey.FinishActiveSessionsFirst), 409, "invalid_state");
            if (r.Status == PlanStatus.Completed)
                Ensure.That(!await db.Sessions.AnyAsync(x => x.PlanId == id && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.Planned)), Messages.Get(MessageKey.CompleteOrSkipPendingSessionsFirst), 409, "invalid_state");
            if (r.Status == PlanStatus.Archived)
            {
                foreach (var session in await db.Sessions.Where(x => x.PlanId == id && (x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned)).ToListAsync())
                {
                    session.Status = SessionStatus.Skipped;
                    if (session.RiderId.HasValue) events.Notify(session.RiderId.Value, NotificationType.SessionSkipped, MessageKey.ThePlanWasArchivedAndThisSessionWasCancelled, session.Id);
                    await events.TrainingHistory(p, session);
                }
            }
            p.Status = r.Status; await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanStatus, id, r.Status.ToString()); await db.SaveChangesAsync(); return p;
        }

    public static async Task<object> GetPlansByIdHistory(Guid id, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.HorseOwner, Role.HeadTrainer, Role.Trainer, Role.Veterinarian);
            var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Horse(p.HorseId);
            return await pager.Page(db.TrainingRevisions.Where(x => x.PlanId == id).OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<object> PostPlansByIdSessions(Guid id, SessionRequest r, TrainingService service)
    { var s = await service.CreateSession(id, r); return Results.Created($"/api/training/sessions/{s.Id}", s); }

    public static async Task<object> GetSessions(Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    {
            var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id);
            var q = db.Sessions.Where(x => horses.Contains(x.HorseId));
            if (u.Role == Role.WorkRider) q = q.Where(x => x.RiderId == u.Id);
            if (horseId.HasValue) { await access.Horse(horseId.Value); q = q.Where(x => x.HorseId == horseId); }
            if (status.HasValue) q = q.Where(x => x.Status == status);
            if (from.HasValue) q = q.Where(x => x.ScheduledAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.ScheduledAt <= to.Value);
            return await pager.Page(q.OrderBy(x => x.ScheduledAt), page, pageSize);
        }

    public static async Task<object> GetSessionsById(Guid id, ClubAccess access, CurrentUser current, ClubDbContext db)
    {
            var s = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Horse(s.HorseId); var u = await current.Get();
            Ensure.That(u.Role != Role.WorkRider || s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
            return new { session = s, result = await db.Results.SingleOrDefaultAsync(x => x.SessionId == id), evaluation = await db.Evaluations.SingleOrDefaultAsync(x => x.SessionId == id) };
        }

    public static async Task<object> PutSessionsById(Guid id, SessionRequest r, TrainingService service)
    { return await service.EditSession(id, r); }

    public static async Task<object> PostSessionsByIdAssign(Guid id, RiderRequest r, ClubDbContext db, TrainingService service)
    {
            var s = Ensure.Found(await db.Sessions.FindAsync(id));
            return await service.EditSession(id, new SessionRequest(s.ScheduledAt, s.TrainingType, s.DistanceMetres, s.Intensity, s.Surface, s.Target, s.Notes, r.RiderId));
        }

    public static async Task<object> PostSessionsByIdStart(Guid id, TrainingService service)
    { await service.Start(id); return Results.NoContent(); }

    public static async Task<object> PostSessionsByIdResults(Guid id, ResultRequest r, TrainingService service)
    { return await service.SubmitResult(id, r); }

    public static async Task<object> PostSessionsByIdSkip(Guid id, ReasonRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            var s = Ensure.Found(await db.Sessions.FindAsync(id)); var u = await current.Get();
            if (u.Role == Role.WorkRider) { await access.Horse(s.HorseId); Ensure.That(s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden"); }
            else await access.Trainer(s.HorseId);
            Ensure.That(s.Status is SessionStatus.Planned or SessionStatus.Assigned or SessionStatus.InProgress, Messages.Get(MessageKey.SessionIsAlreadyFinal), 409, "invalid_state");
            s.Status = SessionStatus.Skipped; s.Notes += Messages.Get(MessageKey.Skipped, r.Reason);
            await events.HorseStaff(s.HorseId, NotificationType.SessionSkipped, MessageKey.ASessionWasSkipped, Role.Trainer);
            await events.Audit(AuditAction.TrainingSessionSkipped, id); await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> PostSessionsByIdEvaluation(Guid id, EvaluationRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            var s = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Trainer(s.HorseId);
            Ensure.That(s.Status is SessionStatus.Completed or SessionStatus.IssueReported, Messages.Get(MessageKey.OnlyResultsCanBeEvaluated), 409, "invalid_state");
            Ensure.That(!await db.Evaluations.AnyAsync(x => x.SessionId == id), Messages.Get(MessageKey.EvaluationAlreadyExists), 409, "duplicate_evaluation");
            var e = new TrainerEvaluation { SessionId = id, TrainerId = (await current.Get()).Id, Comment = r.Comment, AdjustFutureSessions = r.AdjustFutureSessions };
            db.Evaluations.Add(e); await events.Audit(AuditAction.TrainingEvaluated, id); await db.SaveChangesAsync(); return e;
        }

    public static void Apply(TrainingTemplate t, TemplateRequest r)
    { t.Name = r.Name; t.Goal = r.Goal; t.Phase = r.Phase; t.DistanceMetres = r.DistanceMetres; t.Intensity = r.Intensity; t.Surface = r.Surface; t.FrequencyPerWeek = r.FrequencyPerWeek; t.Notes = r.Notes; }
}
