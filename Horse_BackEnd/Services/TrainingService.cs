using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Services;

public sealed class TrainingService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, IOptions<BusinessOptions> options, ClubCalendar calendar)
{
    public async Task Guard(Guid horseId, decimal distance, Intensity intensity, TrainingType type, DateTimeOffset at)
    {
        var horse = Ensure.Found(await db.Horses.FindAsync(horseId));
        Ensure.That(!horse.Archived, "Horse is archived.", 409, "horse_archived");
        if (horse.HealthStatus == HealthStatus.Isolated || horse.HealthStatus == HealthStatus.Injured && intensity == Intensity.Heavy)
            throw new ApiException(409, "medical_block", "Current health status prevents this training.", horseId);
        var restrictions = await db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared && x.ValidFrom <= at && (x.ValidUntil == null || x.ValidUntil >= at)).ToListAsync();
        foreach (var r in restrictions)
            if (r.BlockAllTraining || r.TrainingLock && intensity == Intensity.Heavy || r.MaxIntensity.HasValue && intensity > r.MaxIntensity
                || r.MaxDistanceMetres.HasValue && distance > r.MaxDistanceMetres || r.NoSprint && type == TrainingType.Sprint)
                throw new ApiException(409, "medical_restriction", $"Training blocked: {r.Reason}", r.MedicalRecordId);
    }
    public async Task<TrainingPlan> CreatePlan(PlanRequest r)
    {
        await access.Trainer(r.HorseId); var u = await current.Get();
        var template = Ensure.Found(await db.Templates.FindAsync(r.TemplateId));
        Ensure.That(!template.Archived, "Template is archived.");
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, "Invalid plan dates.");
        var p = new TrainingPlan { HorseId = r.HorseId, TrainerId = u.Id, TemplateId = r.TemplateId, Goal = r.Goal, Phase = r.Phase, StartDate = r.StartDate, EndDate = r.EndDate, Notes = r.Notes };
        db.Plans.Add(p); await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanCreated, p.Id); await db.SaveChangesAsync(); return p;
    }
    public async Task<TrainingSession> CreateSession(Guid planId, SessionRequest r)
    {
        var plan = Ensure.Found(await db.Plans.FindAsync(planId));
        await access.Trainer(plan.HorseId);
        Ensure.That(plan.Status == PlanStatus.Active, "Plan is not active.", 409, "invalid_state");
        var session = new TrainingSession { PlanId = planId, HorseId = plan.HorseId };
        await ApplySession(session, plan, r);
        db.Sessions.Add(session);
        await events.TrainingHistory(plan, session);
        if (r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, "A training session was assigned to you.", session.Id);
        await events.Audit(AuditAction.TrainingSessionCreated, session.Id); await db.SaveChangesAsync(); return session;
    }
    public async Task<TrainingSession> EditSession(Guid id, SessionRequest r)
    {
        var session = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Trainer(session.HorseId);
        Ensure.That(session.Status is SessionStatus.Planned or SessionStatus.Assigned, "Only unstarted sessions can be edited.", 409, "invalid_state");
        var plan = Ensure.Found(await db.Plans.FindAsync(session.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, "Plan is not active.", 409, "invalid_state");
        var oldRider = session.RiderId;
        await ApplySession(session, plan, r);
        await events.TrainingHistory(plan, session);
        if (r.RiderId != oldRider && r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, "A training session was assigned to you.", id);
        if (oldRider.HasValue && oldRider != r.RiderId) events.Notify(oldRider.Value, NotificationType.SessionUnassigned, "A training assignment was removed.", id);
        await events.Audit(AuditAction.TrainingSessionEdited, id); await db.SaveChangesAsync(); return session;
    }
    private async Task ApplySession(TrainingSession s, TrainingPlan p, SessionRequest r)
    {
        var date = calendar.DateAt(r.ScheduledAt);
        Ensure.That(r.ScheduledAt != default && date >= p.StartDate && date <= p.EndDate, "Session must fall within plan dates in the club time zone.");
        Ensure.That(r.ScheduledAt >= clock.GetUtcNow().AddMinutes(-options.Value.ScheduleGraceMinutes), "Schedule sessions in the future.");
        await Guard(s.HorseId, r.DistanceMetres, r.Intensity, r.TrainingType, r.ScheduledAt);
        if (r.RiderId.HasValue)
        {
            await access.Staff(r.RiderId.Value, Role.WorkRider);
            Ensure.That(!await db.Sessions.AnyAsync(x => x.Id != s.Id && x.RiderId == r.RiderId && x.ScheduledAt == r.ScheduledAt && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.InProgress)), "Rider already has a session at this time.", 409, "rider_conflict");
        }
        s.ScheduledAt = r.ScheduledAt.ToUniversalTime(); s.TrainingType = r.TrainingType; s.DistanceMetres = r.DistanceMetres;
        s.Intensity = r.Intensity; s.Surface = r.Surface; s.Target = r.Target; s.Notes = r.Notes; s.RiderId = r.RiderId;
        s.Status = r.RiderId.HasValue ? SessionStatus.Assigned : SessionStatus.Planned;
    }
    public async Task Start(Guid id)
    {
        var u = await current.Get(); Ensure.Role(u, Role.WorkRider);
        var s = Ensure.Found(await db.Sessions.FindAsync(id));
        Ensure.That(s.RiderId == u.Id, "Session is not assigned to you.", 403, "forbidden");
        await access.Horse(s.HorseId);
        Ensure.That(s.Status == SessionStatus.Assigned, "Session is not assigned/ready.", 409, "invalid_state");
        Ensure.That(s.ScheduledAt <= clock.GetUtcNow().AddMinutes(options.Value.StartEarlyMinutes), "Session is not due to start yet.", 409, "invalid_state");
        var plan = Ensure.Found(await db.Plans.FindAsync(s.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, "Plan is not active.", 409, "invalid_state");
        var today = calendar.Today(clock);
        Ensure.That(today >= plan.StartDate && today <= plan.EndDate, "Plan is outside its active dates.", 409, "invalid_state");
        Ensure.That(!await db.Sessions.AnyAsync(x => x.Status == SessionStatus.InProgress && (x.RiderId == u.Id || x.HorseId == s.HorseId)), "Rider or horse already has an active session.", 409, "session_conflict");
        await Guard(s.HorseId, s.DistanceMetres, s.Intensity, s.TrainingType, clock.GetUtcNow());
        s.Status = SessionStatus.InProgress; s.StartedAt = clock.GetUtcNow();
        await events.Audit(AuditAction.TrainingSessionStarted, id); await db.SaveChangesAsync();
    }
    public async Task<SessionResult> SubmitResult(Guid id, ResultRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.WorkRider);
        var s = Ensure.Found(await db.Sessions.FindAsync(id));
        Ensure.That(s.RiderId == u.Id, "Session is not assigned to you.", 403, "forbidden");
        await access.Horse(s.HorseId);
        Ensure.That(s.Status == SessionStatus.InProgress, "Start session before submitting results.", 409, "invalid_state");
        Ensure.That(!await db.Results.AnyAsync(x => x.SessionId == id), "Result already exists.", 409, "duplicate_result");
        // Always allow reporting actual activity, even if a medical lock was applied mid-session.
        // A newly conflicting restriction is recorded as an issue rather than discarding observations.
        var medicalIssue = false;
        try { await Guard(s.HorseId, r.DistanceMetres, r.Intensity, s.TrainingType, clock.GetUtcNow()); }
        catch (ApiException e) when (e.Code is "medical_block" or "medical_restriction") { medicalIssue = true; }
        var issue = r.AbnormalObservation || medicalIssue;
        var result = new SessionResult { SessionId = id, DistanceMetres = r.DistanceMetres, TimeSeconds = r.TimeSeconds,
            SpeedMetresPerSecond = decimal.Round(r.DistanceMetres / r.TimeSeconds, 3), HeartRate = r.HeartRate,
            Intensity = r.Intensity, Feedback = r.Feedback, AbnormalObservation = issue };
        db.Results.Add(result); s.Status = issue ? SessionStatus.IssueReported : SessionStatus.Completed;
        await events.HorseStaff(s.HorseId, NotificationType.SessionResult, "A rider submitted a session result.", Role.Trainer);
        if (issue)
        {
            db.Incidents.Add(new Incident { HorseId = s.HorseId, ReporterId = u.Id, SessionId = id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.TrainingObservation, Description = r.Feedback, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
            await events.HorseStaff(s.HorseId, NotificationType.IncidentReported, "A training observation requires review.", Role.Veterinarian, Role.Trainer);
        }
        await events.Audit(AuditAction.TrainingResultSubmitted, id); await db.SaveChangesAsync(); return result;
    }
}
