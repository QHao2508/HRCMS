using HorseClub.BLL.Messaging;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Workflows;

public static class IncidentPhotoWorkflow
{
    public static async Task<object> GetList(Guid incidentId, ClubDbContext db, ClubAccess access, CurrentUser current)
    {
            await Check(incidentId, db, access, current);
            return await db.IncidentPhotos.Where(x => x.IncidentId == incidentId).Select(x => new { x.Id, x.FileName, x.ContentType, x.Length }).ToListAsync();
        }

    public static async Task<object> Handle02(Guid incidentId, HttpRequest request, ClubDbContext db, ClubAccess access, CurrentUser current, ClubEvents events,
            IOptions<StorageOptions> limits, IConfiguration config, IWebHostEnvironment env)
    {
            var incident = await Check(incidentId, db, access, current);
            var user = await current.Get(); Ensure.That(incident.ReporterId == user.Id, Messages.Get(MessageKey.OnlyTheIncidentReporterMayUploadPhotos), 403, "forbidden");
            Ensure.That(!incident.Resolved, Messages.Get(MessageKey.IncidentIsResolved), 409, "invalid_state"); Ensure.That(request.HasFormContentType, Messages.Get(MessageKey.MultipartFormDataRequired));
            var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
            Ensure.That(file is not null && file.Length > 0 && file.Length <= limits.Value.MaxFileBytes, Messages.Get(MessageKey.InvalidFileOrSizeLimitExceeded));
            Ensure.That(await db.IncidentPhotos.CountAsync(x => x.IncidentId == incidentId) < limits.Value.MaxAttachmentsPerRecord, Messages.Get(MessageKey.AttachmentLimitReached));
            var bytes = new byte[file!.Length]; await using (var stream = file.OpenReadStream()) await stream.ReadExactlyAsync(bytes);
            var contentType = AttachmentWorkflow.Detect(bytes); var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            Ensure.That(contentType == "image/png" && extension == ".png" || contentType == "image/jpeg" && extension is ".jpg" or ".jpeg", Messages.Get(MessageKey.UploadAPNGOrJPEGImage));
            var photo = new IncidentPhoto { IncidentId = incidentId, UploadedBy = user.Id, FileName = Path.GetFileName(file.FileName), StorageName = Guid.NewGuid().ToString("N"), ContentType = contentType!, Length = file.Length };
            Ensure.That(photo.FileName.Length <= 200, Messages.Get(MessageKey.FilenameIsTooLong));
            var storage = AttachmentWorkflow.Storage(config, env); Directory.CreateDirectory(storage); await File.WriteAllBytesAsync(Path.Combine(storage, photo.StorageName), bytes);
            db.IncidentPhotos.Add(photo); await events.Audit(AuditAction.AttachmentUploaded, photo.Id); await db.SaveChangesAsync();
            return Results.Created($"/api/care/incidents/{incidentId}/photos/{photo.Id}", new { photo.Id, photo.FileName, photo.Length });
        }

    public static async Task<object> GetById(Guid incidentId, Guid id, ClubDbContext db, ClubAccess access, CurrentUser current, IConfiguration config, IWebHostEnvironment env)
    {
            await Check(incidentId, db, access, current); var photo = Ensure.Found(await db.IncidentPhotos.SingleOrDefaultAsync(x => x.Id == id && x.IncidentId == incidentId));
            var file = Path.Combine(AttachmentWorkflow.Storage(config, env), photo.StorageName); Ensure.That(File.Exists(file), Messages.Get(MessageKey.PhotoUnavailable), 404, "not_found");
            return Results.File(file, photo.ContentType, photo.FileName);
        }

    public static async Task<Incident> Check(Guid id, ClubDbContext db, ClubAccess access, CurrentUser current)
    {
        var incident = Ensure.Found(await db.Incidents.FindAsync(id)); await access.Horse(incident.HorseId); var user = await current.Get();
        Ensure.That(user.Role == Role.ClubManager || user.Id == incident.ReporterId || user.Role == incident.RoutedTo, Messages.Get(MessageKey.IncidentIsOutsideYourScope), 403, "forbidden");
        return incident;
    }
}
