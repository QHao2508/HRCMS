using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Endpoints;

public static class IncidentPhotoEndpoints
{
    public static void MapIncidentPhotos(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/care/incidents/{incidentId:guid}/photos").RequireAuthorization().WithTags("Incident photos");
        group.MapGet("", async (Guid incidentId, ClubDbContext db, ClubAccess access, CurrentUser current) =>
        {
            await Check(incidentId, db, access, current);
            return await db.IncidentPhotos.Where(x => x.IncidentId == incidentId).Select(x => new { x.Id, x.FileName, x.ContentType, x.Length }).ToListAsync();
        });
        group.MapPost("", async (Guid incidentId, HttpRequest request, ClubDbContext db, ClubAccess access, CurrentUser current, ClubEvents events,
            IOptions<StorageOptions> limits, IConfiguration config, IWebHostEnvironment env) =>
        {
            var incident = await Check(incidentId, db, access, current);
            var user = await current.Get(); Ensure.That(incident.ReporterId == user.Id, "Only the incident reporter may upload photos.", 403, "forbidden");
            Ensure.That(!incident.Resolved, "Incident is resolved.", 409, "invalid_state"); Ensure.That(request.HasFormContentType, "multipart/form-data required.");
            var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
            Ensure.That(file is not null && file.Length > 0 && file.Length <= limits.Value.MaxFileBytes, "Invalid file or size limit exceeded.");
            Ensure.That(await db.IncidentPhotos.CountAsync(x => x.IncidentId == incidentId) < limits.Value.MaxAttachmentsPerRecord, "Attachment limit reached.");
            var bytes = new byte[file!.Length]; await using (var stream = file.OpenReadStream()) await stream.ReadExactlyAsync(bytes);
            var contentType = AttachmentEndpoints.Detect(bytes); var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            Ensure.That(contentType == "image/png" && extension == ".png" || contentType == "image/jpeg" && extension is ".jpg" or ".jpeg", "Upload a PNG or JPEG image.");
            var photo = new IncidentPhoto { IncidentId = incidentId, UploadedBy = user.Id, FileName = Path.GetFileName(file.FileName), StorageName = Guid.NewGuid().ToString("N"), ContentType = contentType!, Length = file.Length };
            Ensure.That(photo.FileName.Length <= 200, "Filename is too long.");
            var storage = AttachmentEndpoints.Storage(config, env); Directory.CreateDirectory(storage); await File.WriteAllBytesAsync(Path.Combine(storage, photo.StorageName), bytes);
            db.IncidentPhotos.Add(photo); await events.Audit(AuditAction.AttachmentUploaded, photo.Id); await db.SaveChangesAsync();
            return Results.Created($"/api/care/incidents/{incidentId}/photos/{photo.Id}", new { photo.Id, photo.FileName, photo.Length });
        }).RequireRateLimiting("uploads");
        group.MapGet("/{id:guid}", async (Guid incidentId, Guid id, ClubDbContext db, ClubAccess access, CurrentUser current, IConfiguration config, IWebHostEnvironment env) =>
        {
            await Check(incidentId, db, access, current); var photo = Ensure.Found(await db.IncidentPhotos.SingleOrDefaultAsync(x => x.Id == id && x.IncidentId == incidentId));
            var file = Path.Combine(AttachmentEndpoints.Storage(config, env), photo.StorageName); Ensure.That(File.Exists(file), "Photo unavailable.", 404, "not_found");
            return Results.File(file, photo.ContentType, photo.FileName);
        });
    }
    private static async Task<Incident> Check(Guid id, ClubDbContext db, ClubAccess access, CurrentUser current)
    {
        var incident = Ensure.Found(await db.Incidents.FindAsync(id)); await access.Horse(incident.HorseId); var user = await current.Get();
        Ensure.That(user.Role == Role.ClubManager || user.Id == incident.ReporterId || user.Role == incident.RoutedTo, "Incident is outside your scope.", 403, "forbidden");
        return incident;
    }
}
