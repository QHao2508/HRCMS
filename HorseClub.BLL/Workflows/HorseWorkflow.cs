using HorseClub.BLL.Messaging;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Workflows;

public static class HorseWorkflow
{
    public static async Task<object> PostList(RegistrationRequest request, HorseService s)
    { var record = await s.Create(request); return Results.Created($"/api/registrations/{record.Id}", record); }

    public static async Task<object> GetList(CurrentUser current, ClubDbContext db, RegistrationStatus? status, int? page, int? pageSize, PageReader pager)
    {
            var u = await current.Get(); Ensure.Role(u, Role.HorseOwner, Role.ClubManager);
            var q = db.Registrations.AsQueryable();
            if (u.Role == Role.HorseOwner) q = q.Where(x => x.OwnerId == u.Id);
            if (status.HasValue) q = q.Where(x => x.Status == status);
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        }

    public static async Task<object> GetById(Guid id, ClubAccess access)
    { return await access.Registration(id); }

    public static async Task<object> PutById(Guid id, RegistrationRequest request, HorseService s)
    { return await s.Edit(id, request); }

    public static async Task<object> PostByIdSubmit(Guid id, HorseService s)
    { await s.Submit(id); return Results.NoContent(); }

    public static async Task<object> PostByIdReview(Guid id, ReviewRequest request, HorseService s)
    { return await s.Review(id, request); }

    public static async Task<object> PostByIdCancel(Guid id, ClubAccess access, CurrentUser current, ClubEvents events, ClubDbContext db)
    {
            Ensure.Role(await current.Get(), Role.HorseOwner);
            var record = await access.Registration(id);
            Ensure.That(record.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.OnlyDraftRevisionRegistrationsMayBeCancelled), 409, "invalid_state");
            record.Status = RegistrationStatus.Cancelled; await events.Audit(AuditAction.RegistrationCancelled, id);
            await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> GetList2(ClubAccess access, string? search, HealthStatus? healthStatus, int? page, int? pageSize, PageReader pager)
    {
            var q = await access.Horses();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.Contains(search) || x.RegistrationNumber != null && x.RegistrationNumber.Contains(search));
            if (healthStatus.HasValue) q = q.Where(x => x.HealthStatus == healthStatus);
            return await pager.Page(q.OrderBy(x => x.Name), page, pageSize);
        }

    public static async Task<object> GetById2(Guid id, ClubAccess access, ClubDbContext db)
    {
            var horse = await access.Horse(id);
            return new { horse, latestMeasurement = await db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date).FirstOrDefaultAsync(),
                assignments = await db.Assignments.Where(x => x.HorseId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(),
                currentStall = await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).FirstOrDefaultAsync(),
                preferences = await db.Registrations.Where(x => x.Id == horse.RegistrationId).Select(x => new { x.PreferredHeadTrainerId, x.PreferredGroomId, x.PreferredVeterinarianId }).SingleAsync() };
        }

    public static async Task<object> PostByIdAssignments(Guid id, AssignmentRequest request, HorseService s)
    { return await s.Assign(id, request); }

    public static async Task<object> GetByIdMeasurements(Guid id, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager)
    { await access.Horse(id); return await pager.Page(db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date), page, pageSize); }

    public static async Task<object> PostByIdMeasurements(Guid id, MeasurementRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
    {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Veterinarian, Role.Groom);
            var horse = await access.Horse(id);
            Ensure.That(request.Date >= horse.DateOfBirth && request.Date <= calendar.Today(clock), Messages.Get(MessageKey.InvalidMeasurementDate));
            var m = new Measurement { HorseId = id, Date = request.Date, HeightCm = request.HeightCm, WeightKg = request.WeightKg };
            db.Measurements.Add(m); await events.Audit(AuditAction.HorseMeasurementAdded, id); await db.SaveChangesAsync(); return m;
        }

    public static async Task<object> PostByIdArchive(Guid id, ReasonRequest request, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
    {
            Ensure.Role(await current.Get(), Role.ClubManager);
            Ensure.That(!string.IsNullOrWhiteSpace(request.Reason), Messages.Get(MessageKey.Invalid, nameof(request.Reason)));
            var horse = await access.Horse(id);
            var activeSession = await db.Sessions.AnyAsync(x => x.HorseId == id && x.Status == SessionStatus.InProgress);
            var activeCareTask = await db.CareTasks.AnyAsync(x => x.HorseId == id && x.Status == CareStatus.InProgress);
            Ensure.That(!activeSession && !activeCareTask, Messages.Get(MessageKey.FinishActiveHorseWorkBeforeArchiving), 409, "invalid_state");

            var now = clock.GetUtcNow();
            var today = calendar.DateAt(now);
            foreach (var assignment in await db.Assignments.Where(x => x.HorseId == id && x.Active).ToListAsync())
            {
                assignment.Active = false;
                assignment.EndDate = today;
            }

            var pendingSessions = await db.Sessions.Where(x => x.HorseId == id &&
                (x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned)).ToListAsync();
            foreach (var session in pendingSessions)
            {
                var plan = await db.Plans.FindAsync(session.PlanId);
                if (plan is not null) await events.TrainingHistory(plan, session);
                session.Status = SessionStatus.Skipped;
                session.Notes += Messages.Get(MessageKey.Skipped, request.Reason.Trim());
                if (session.RiderId.HasValue)
                    events.Notify(session.RiderId.Value, NotificationType.SessionSkipped, MessageKey.ThePlanWasArchivedAndThisSessionWasCancelled, session.Id);
            }

            foreach (var task in await db.CareTasks.Where(x => x.HorseId == id && x.Status == CareStatus.Pending).ToListAsync())
            {
                task.Status = CareStatus.Skipped;
                task.Notes += Messages.Get(MessageKey.Skipped, request.Reason.Trim());
            }

            foreach (var plan in await db.Plans.Where(x => x.HorseId == id &&
                (x.Status == PlanStatus.Active || x.Status == PlanStatus.Paused)).ToListAsync())
            {
                plan.Status = PlanStatus.Archived;
                await events.TrainingHistory(plan);
            }

            foreach (var occupancy in await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).ToListAsync())
                occupancy.EndedAt = now;

            horse.Archived = true;
            await events.Audit(AuditAction.HorseArchived, id, request.Reason.Trim());
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

}
