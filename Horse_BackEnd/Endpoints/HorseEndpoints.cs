using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Endpoints;

public static class HorseEndpoints
{
    public static void MapHorses(this RouteGroupBuilder api)
    {
        var r = api.MapGroup("/registrations").WithTags("Horse intake").RequireAuthorization();
        r.MapPost("", async (RegistrationRequest request, HorseService s) =>
        { var record = await s.Create(request); return Results.Created($"/api/registrations/{record.Id}", record); });
        r.MapGet("", async (CurrentUser current, ClubDbContext db, RegistrationStatus? status, int? page, int? pageSize, PageReader pager) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.HorseOwner, Role.ClubManager);
            var q = db.Registrations.AsQueryable();
            if (u.Role == Role.HorseOwner) q = q.Where(x => x.OwnerId == u.Id);
            if (status.HasValue) q = q.Where(x => x.Status == status);
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        });
        r.MapGet("/{id:guid}", async (Guid id, ClubAccess access) => await access.Registration(id));
        r.MapPut("/{id:guid}", async (Guid id, RegistrationRequest request, HorseService s) => await s.Edit(id, request));
        r.MapPost("/{id:guid}/submit", async (Guid id, HorseService s) => { await s.Submit(id); return Results.NoContent(); });
        r.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, HorseService s) => await s.Review(id, request));
        r.MapPost("/{id:guid}/cancel", async (Guid id, ClubAccess access, CurrentUser current, ClubEvents events, ClubDbContext db) =>
        {
            Ensure.Role(await current.Get(), Role.HorseOwner);
            var record = await access.Registration(id);
            Ensure.That(record.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, "Only draft/revision registrations may be cancelled.", 409, "invalid_state");
            record.Status = RegistrationStatus.Cancelled; await events.Audit(AuditAction.RegistrationCancelled, id);
            await db.SaveChangesAsync(); return Results.NoContent();
        });

        var h = api.MapGroup("/horses").WithTags("Horses").RequireAuthorization();
        h.MapGet("", async (ClubAccess access, string? search, HealthStatus? healthStatus, int? page, int? pageSize, PageReader pager) =>
        {
            var q = await access.Horses();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.Contains(search) || x.RegistrationNumber != null && x.RegistrationNumber.Contains(search));
            if (healthStatus.HasValue) q = q.Where(x => x.HealthStatus == healthStatus);
            return await pager.Page(q.OrderBy(x => x.Name), page, pageSize);
        });
        h.MapGet("/{id:guid}", async (Guid id, ClubAccess access, ClubDbContext db) =>
        {
            var horse = await access.Horse(id);
            return new { horse, latestMeasurement = await db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date).FirstOrDefaultAsync(),
                assignments = await db.Assignments.Where(x => x.HorseId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(),
                currentStall = await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).FirstOrDefaultAsync(),
                preferences = await db.Registrations.Where(x => x.Id == horse.RegistrationId).Select(x => new { x.PreferredHeadTrainerId, x.PreferredGroomId, x.PreferredVeterinarianId }).SingleAsync() };
        });
        h.MapPost("/{id:guid}/assignments", async (Guid id, AssignmentRequest request, HorseService s) => await s.Assign(id, request));
        h.MapGet("/{id:guid}/measurements", async (Guid id, ClubAccess access, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        { await access.Horse(id); return await pager.Page(db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date), page, pageSize); });
        h.MapPost("/{id:guid}/measurements", async (Guid id, MeasurementRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, ClubEvents events, TimeProvider clock, ClubCalendar calendar) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Veterinarian, Role.Groom);
            var horse = await access.Horse(id);
            Ensure.That(request.Date >= horse.DateOfBirth && request.Date <= calendar.Today(clock), "Invalid measurement date.");
            var m = new Measurement { HorseId = id, Date = request.Date, HeightCm = request.HeightCm, WeightKg = request.WeightKg };
            db.Measurements.Add(m); await events.Audit(AuditAction.HorseMeasurementAdded, id); await db.SaveChangesAsync(); return m;
        });
        h.MapPost("/{id:guid}/archive", async (Guid id, CurrentUser current, ClubAccess access, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var horse = await access.Horse(id);
            Ensure.That(!await db.Sessions.AnyAsync(x => x.HorseId == id && x.Status == SessionStatus.InProgress), "Finish active sessions before archiving.", 409, "invalid_state");
            horse.Archived = true;
            foreach (var occupancy in await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).ToListAsync()) occupancy.EndedAt = DateTimeOffset.UtcNow;
            await events.Audit(AuditAction.HorseArchived, id); await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
}
