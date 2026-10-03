using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Endpoints;

public static class CareEndpoints
{
    public static void MapCare(this RouteGroupBuilder api)
    {
        var c = api.MapGroup("/care").RequireAuthorization().WithTags("Care and stable");
        c.MapGet("/tasks", async (Guid? horseId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        {
            var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id); var q = db.CareTasks.Where(x => horses.Contains(x.HorseId));
            Ensure.Role(u, Role.ClubManager, Role.HorseOwner, Role.Groom, Role.Veterinarian);
            if (u.Role == Role.Groom) q = q.Where(x => x.GroomId == u.Id);
            if (horseId.HasValue) { await access.Horse(horseId.Value); q = q.Where(x => x.HorseId == horseId); }
            if (status.HasValue) q = q.Where(x => x.Status == status);
            if (from.HasValue) q = q.Where(x => x.ScheduledAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.ScheduledAt <= to.Value);
            // Only executing Groom, assigned Vet and Manager see treatment instructions.
            var clinical = u.Role is Role.Groom or Role.Veterinarian or Role.ClubManager;
            return await pager.Page(q.OrderBy(x => x.ScheduledAt).Select(x => new { x.Id, x.HorseId, x.GroomId, x.Type, x.ScheduledAt, x.Status, x.CompletedAt,
                x.ApprovedPortionKg, x.ActualPortionKg, instructions = clinical || x.Type != CareType.Treatment ? x.Instructions : null,
                notes = clinical || x.Type != CareType.Treatment ? x.Notes : null }), page, pageSize);
        });
        c.MapPost("/tasks", async (CareRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            var u = await current.Get(); await access.Horse(r.HorseId);
            Ensure.Role(u, Role.ClubManager, Role.Veterinarian);
            if (r.Type is CareType.Treatment or CareType.IceBath) Ensure.Role(u, Role.Veterinarian);
            if (u.Role == Role.Veterinarian) Ensure.That(r.Type is CareType.Treatment or CareType.IceBath, "Veterinarian assigns treatment or ice bath tasks.");
            await access.Staff(r.GroomId, Role.Groom);
            Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == r.HorseId && x.StaffId == r.GroomId && x.Active && x.Role == Role.Groom), "Groom must be assigned to this horse.");
            Ensure.That(r.ScheduledAt != default, "Schedule time is required.");
            if (r.TreatmentPlanId.HasValue) Ensure.That(await db.Treatments.AnyAsync(x => x.Id == r.TreatmentPlanId && x.HorseId == r.HorseId && !x.Completed), "Treatment must be active and belong to this horse.");
            if (r.Type == CareType.Treatment) Ensure.That(r.TreatmentPlanId.HasValue, "Treatment tasks need a treatment plan.");
            if (r.Type == CareType.Feeding) Ensure.That(r.ApprovedPortionKg.HasValue, "Feeding needs an approved portion in kg.");
            var task = new CareTask { HorseId = r.HorseId, GroomId = r.GroomId, TreatmentPlanId = r.TreatmentPlanId, Type = r.Type, ScheduledAt = r.ScheduledAt.ToUniversalTime(), Instructions = r.Instructions, ApprovedPortionKg = r.ApprovedPortionKg };
            db.CareTasks.Add(task); events.Notify(r.GroomId, NotificationType.CareAssigned, "A care task was assigned to you.", task.Id);
            await events.Audit(AuditAction.CareTaskCreated, task.Id); await db.SaveChangesAsync(); return task;
        });
        c.MapPost("/tasks/{id:guid}/record", async (Guid id, CareCompletionRequest r, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events, TimeProvider clock) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.Groom); var t = Ensure.Found(await db.CareTasks.FindAsync(id));
            Ensure.That(t.GroomId == u.Id, "Task is not assigned to you.", 403, "forbidden"); await access.Horse(t.HorseId);
            Ensure.That(t.Status is CareStatus.Pending or CareStatus.InProgress, "Task is already final.", 409, "invalid_state");
            Ensure.That(r.Status is CareStatus.InProgress or CareStatus.Completed or CareStatus.Skipped or CareStatus.IssueReported, "Unsupported status.");
            if (t.Type == CareType.Feeding && r.Status == CareStatus.Completed) Ensure.That(r.ActualPortionKg.HasValue, "Record actual feeding portion.");
            t.Status = r.Status; t.ActualPortionKg = r.ActualPortionKg; t.Notes = r.Notes;
            if (r.Status != CareStatus.InProgress) t.CompletedAt = clock.GetUtcNow();
            if (r.Status == CareStatus.IssueReported)
            {
                db.Incidents.Add(new Incident { HorseId = t.HorseId, ReporterId = u.Id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.CareObservation, Description = r.Notes, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
                await events.HorseStaff(t.HorseId, NotificationType.CareIssue, "A care task requires review.", Role.Veterinarian, Role.Trainer);
            }
            await events.Audit(AuditAction.CareTaskRecorded, id); await db.SaveChangesAsync(); return t;
        });
        c.MapGet("/incidents", async (Guid? horseId, ClubAccess access, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Veterinarian, Role.Trainer, Role.Groom, Role.WorkRider);
            var horses = (await access.Horses()).Select(x => x.Id); var q = db.Incidents.Where(x => horses.Contains(x.HorseId));
            if (u.Role is Role.Groom or Role.WorkRider) q = q.Where(x => x.ReporterId == u.Id);
            if (u.Role is Role.Trainer or Role.Veterinarian) q = q.Where(x => x.RoutedTo == u.Role || x.ReporterId == u.Id);
            if (horseId.HasValue) q = q.Where(x => x.HorseId == horseId);
            return await pager.Page(q.OrderByDescending(x => x.OccurredAt), page, pageSize);
        });
        c.MapPost("/incidents", async (IncidentRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.Groom, Role.WorkRider, Role.Trainer, Role.Veterinarian);
            await access.Horse(r.HorseId); Ensure.That(r.RoutedTo is Role.Trainer or Role.Veterinarian, "Route incident to Trainer or Veterinarian.");
            Ensure.That(r.OccurredAt != default && r.OccurredAt <= DateTimeOffset.UtcNow, "Incident time cannot be in the future.");
            var incident = new Incident { HorseId = r.HorseId, ReporterId = u.Id, OccurredAt = r.OccurredAt, Type = r.Type, Description = r.Description, Severity = r.Severity, RoutedTo = r.RoutedTo };
            db.Incidents.Add(incident); await events.HorseStaff(r.HorseId, NotificationType.IncidentReported, "A new incident requires review.", r.RoutedTo);
            await events.Audit(AuditAction.IncidentCreated, incident.Id); await db.SaveChangesAsync(); return incident;
        });
        c.MapPost("/incidents/{id:guid}/resolve", async (Guid id, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events) =>
        {
            var u = await current.Get(); var incident = Ensure.Found(await db.Incidents.FindAsync(id)); await access.Horse(incident.HorseId);
            Ensure.That(u.Role == incident.RoutedTo, "Only the routed decision role can resolve this incident.", 403, "forbidden");
            incident.Resolved = true; await events.Audit(AuditAction.IncidentResolved, id); await db.SaveChangesAsync(); return Results.NoContent();
        });
        c.MapGet("/stables", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        { Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); return await pager.Page(db.Stables.OrderBy(x => x.Name), page, pageSize); });
        c.MapPost("/stables", async (NameRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var stable = new Stable { Name = r.Name }; db.Stables.Add(stable);
            await events.Audit(AuditAction.StableCreated, stable.Id); await db.SaveChangesAsync(); return stable;
        });
        c.MapGet("/stalls", async (Guid? stableId, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); var q = db.Stalls.AsQueryable(); if (stableId.HasValue) q = q.Where(x => x.StableId == stableId);
            return await pager.Page(q.OrderBy(x => x.Name).Select(x => new { x.Id, x.StableId, x.Name, x.CleaningStatus }), page, pageSize);
        });
        c.MapPost("/stalls", async (StallRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await db.Stables.FindAsync(r.StableId));
            var stall = new Stall { StableId = r.StableId, Name = r.Name }; db.Stalls.Add(stall); await events.Audit(AuditAction.StallCreated, stall.Id); await db.SaveChangesAsync(); return stall;
        });
        c.MapPost("/stalls/{id:guid}/occupancy", async (Guid id, OccupancyRequest r, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await db.Stalls.FindAsync(id)); await access.Horse(r.HorseId);
            Ensure.That(!await db.Occupancies.AnyAsync(x => x.StallId == id && x.EndedAt == null), "Stall is occupied. Vacate it first.", 409, "stall_occupied");
            foreach (var old in await db.Occupancies.Where(x => x.HorseId == r.HorseId && x.EndedAt == null).ToListAsync()) old.EndedAt = clock.GetUtcNow();
            var o = new StallOccupancy { StallId = id, HorseId = r.HorseId }; db.Occupancies.Add(o); await events.Audit(AuditAction.StallOccupied, id); await db.SaveChangesAsync(); return o;
        });
        c.MapPost("/stalls/{id:guid}/vacate", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var occupancy = Ensure.Found(await db.Occupancies.SingleOrDefaultAsync(x => x.StallId == id && x.EndedAt == null));
            occupancy.EndedAt = clock.GetUtcNow(); await events.Audit(AuditAction.StallVacated, id); await db.SaveChangesAsync(); return Results.NoContent();
        });
        c.MapPost("/stalls/{id:guid}/cleaned", async (Guid id, CurrentUser current, ClubDbContext db, ClubAccess access, ClubEvents events) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom); var stall = Ensure.Found(await db.Stalls.FindAsync(id));
            if (u.Role == Role.Groom)
            {
                var occupancy = Ensure.Found(await db.Occupancies.SingleOrDefaultAsync(x => x.StallId == id && x.EndedAt == null));
                await access.Horse(occupancy.HorseId);
            }
            stall.CleaningStatus = CleaningStatus.Clean; await events.Audit(AuditAction.StallCleaned, id); await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
}
