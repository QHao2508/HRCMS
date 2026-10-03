using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Endpoints;

public static class AttachmentEndpoints
{
    public static void MapAttachments(this RouteGroupBuilder api)
    {
        // Public profile images are still protected by the horse scope; staff cannot access intake medical files.
        api.MapGet("/horses/{horseId:guid}/photo", async (Guid horseId, ClubAccess access, ClubDbContext db, IConfiguration config, IWebHostEnvironment env) =>
        {
            var horse = await access.Horse(horseId);
            var attachment = Ensure.Found(await db.Attachments.Where(x => x.RegistrationId == horse.RegistrationId && x.Type == AttachmentType.HorsePhoto).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync());
            var path = Path.Combine(Storage(config, env), attachment.StorageName); Ensure.That(File.Exists(path), "Photo unavailable.", 404, "not_found");
            return Results.File(path, attachment.ContentType, attachment.FileName);
        }).RequireAuthorization().WithTags("Attachments");
        var a = api.MapGroup("/registrations/{registrationId:guid}/attachments").WithTags("Attachments").RequireAuthorization();
        a.MapGet("", async (Guid registrationId, ClubAccess access, ClubDbContext db) =>
        {
            await access.Registration(registrationId);
            return await db.Attachments.Where(x => x.RegistrationId == registrationId).Select(x => new { x.Id, x.FileName, x.ContentType, x.Length, x.Type, x.CertificateNumber, x.IssueDate, x.ExpiryDate }).ToListAsync();
        });
        a.MapPost("", async (Guid registrationId, HttpRequest request, ClubAccess access, CurrentUser current, ClubDbContext db, IConfiguration config, IWebHostEnvironment env, ClubEvents events, IOptions<StorageOptions> options) =>
        {
            var reg = await access.Registration(registrationId); var u = await current.Get();
            Ensure.That(reg.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired || u.Role == Role.ClubManager && reg.Status == RegistrationStatus.PendingReview, "Cannot add documents in this state.", 409, "invalid_state");
            Ensure.That(request.HasFormContentType, "multipart/form-data required.");
            var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
            Ensure.That(file is not null && file.Length > 0 && file.Length <= options.Value.MaxFileBytes, $"Upload one file up to {options.Value.MaxFileBytes} bytes.");
            Ensure.That(await db.Attachments.CountAsync(x => x.RegistrationId == registrationId) < options.Value.MaxAttachmentsPerRecord, "Attachment limit reached.");
            var rawType = form["type"].ToString();
            Ensure.That(Enum.TryParse<AttachmentType>(rawType, false, out var type) && Enum.IsDefined(type) && !int.TryParse(rawType, out _) && type != AttachmentType.IncidentPhoto, "Invalid attachment type.");
            var bytes = new byte[file!.Length];
            await using (var stream = file.OpenReadStream()) await stream.ReadExactlyAsync(bytes);
            var contentType = Detect(bytes);
            Ensure.That(contentType is not null && (type != AttachmentType.HorsePhoto || contentType.StartsWith("image/")), "Only valid PNG, JPEG or PDF files are accepted; horse photo must be an image.");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            Ensure.That(contentType switch { "image/png" => extension == ".png", "image/jpeg" => extension is ".jpg" or ".jpeg", "application/pdf" => extension == ".pdf", _ => false }, "File extension must match its content.");
            DateOnly? issue = ParseDate(form["issueDate"].ToString()); DateOnly? expiry = ParseDate(form["expiryDate"].ToString());
            Ensure.That(!issue.HasValue || !expiry.HasValue || expiry >= issue, "Certificate dates are invalid.");
            var attachment = new Attachment { RegistrationId = registrationId, UploadedBy = u.Id, FileName = Path.GetFileName(file.FileName), StorageName = Guid.NewGuid().ToString("N"), ContentType = contentType!, Length = file.Length, Type = type,
                CertificateNumber = form["certificateNumber"].ToString(), IssueDate = issue, ExpiryDate = expiry };
            Ensure.That(attachment.FileName.Length <= 200 && attachment.CertificateNumber.Length <= 100, "Filename or certificate number is too long.");
            Directory.CreateDirectory(Storage(config, env));
            await File.WriteAllBytesAsync(Path.Combine(Storage(config, env), attachment.StorageName), bytes);
            db.Attachments.Add(attachment); await events.Audit(AuditAction.AttachmentUploaded, attachment.Id); await db.SaveChangesAsync();
            return Results.Created($"/api/registrations/{registrationId}/attachments/{attachment.Id}", new { attachment.Id, attachment.FileName, attachment.Type, attachment.Length });
        }).RequireRateLimiting("uploads");
        a.MapGet("/{id:guid}", async (Guid registrationId, Guid id, ClubAccess access, ClubDbContext db, IConfiguration config, IWebHostEnvironment env) =>
        {
            await access.Registration(registrationId); var attachment = Ensure.Found(await db.Attachments.SingleOrDefaultAsync(x => x.Id == id && x.RegistrationId == registrationId));
            var path = Path.Combine(Storage(config, env), attachment.StorageName);
            Ensure.That(File.Exists(path), "Stored attachment is unavailable.", 404, "not_found");
            // Force download to avoid active content executing in the API origin.
            return Results.File(path, attachment.ContentType, attachment.FileName);
        });
    }
    public static string Storage(IConfiguration config, IWebHostEnvironment env) => Path.GetFullPath(config["Storage:Path"] ?? Path.Combine(env.ContentRootPath, "App_Data", "uploads"));
    internal static string? Detect(byte[] b)
    {
        if (b.Length >= 8 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (b.Length >= 3 && b[0] == 255 && b[1] == 216 && b[2] == 255) return "image/jpeg";
        if (b.Length >= 5 && System.Text.Encoding.ASCII.GetString(b, 0, 5) == "%PDF-") return "application/pdf";
        return null;
    }
    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Ensure.That(DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date), "Date must be yyyy-MM-dd."); return date;
    }
}
