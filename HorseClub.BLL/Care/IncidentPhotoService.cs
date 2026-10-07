using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Care;

public sealed class IncidentPhotoService(ClubDbContext db, ClubAccess access, CurrentUser current, ClubEvents events, IOptions<StorageOptions> limits, UploadStorage storage)
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="incidentId">Giá trị kiểu Guid dùng trong ListPhotos.</param>
    public async Task<List<IncidentPhotoResponse>> ListPhotos(Guid incidentId)
    {
        await Check(incidentId, db, access, current);
        return await db.IncidentPhotos.Where(x => x.IncidentId == incidentId).Select(x => new IncidentPhotoResponse(x.Id, x.FileName, x.ContentType, x.Length)).ToListAsync();
    }

    /// <summary>
    /// Tải lên ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="incidentId">Giá trị kiểu Guid dùng trong UploadPhoto.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <remarks>Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> UploadPhoto(Guid incidentId, UploadRequest request)
    {
        var incident = await Check(incidentId, db, access, current);
        var user = await current.Get(); Ensure.That(incident.ReporterId == user.Id, Messages.Get(MessageKey.OnlyTheIncidentReporterMayUploadPhotos), 403, "forbidden");
        Ensure.That(!incident.Resolved, Messages.Get(MessageKey.IncidentIsResolved), 409, "invalid_state"); Ensure.That(request.HasFormContentType, Messages.Get(MessageKey.MultipartFormDataRequired));
        var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
        Ensure.That(file is not null && file.Length > 0 && file.Length <= limits.Value.MaxFileBytes, Messages.Get(MessageKey.InvalidFileOrSizeLimitExceeded));
        Ensure.That(await db.IncidentPhotos.CountAsync(x => x.IncidentId == incidentId) < limits.Value.MaxAttachmentsPerRecord, Messages.Get(MessageKey.AttachmentLimitReached));
        var bytes = new byte[file!.Length]; await using (var stream = file.OpenReadStream()) await stream.ReadExactlyAsync(bytes);
        var contentType = AttachmentService.Detect(bytes); var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        Ensure.That(contentType == "image/png" && extension == ".png" || contentType == "image/jpeg" && extension is ".jpg" or ".jpeg", Messages.Get(MessageKey.UploadAPNGOrJPEGImage));
        var photo = new IncidentPhoto { IncidentId = incidentId, UploadedBy = user.Id, FileName = Path.GetFileName(file.FileName), StorageName = Guid.NewGuid().ToString("N"), ContentType = contentType!, Length = file.Length };
        Ensure.That(photo.FileName.Length <= 200, Messages.Get(MessageKey.FilenameIsTooLong));
        await storage.Write(photo.StorageName, bytes, request.CancellationToken);
        db.IncidentPhotos.Add(photo); await events.Audit(AuditAction.AttachmentUploaded, photo.Id); await db.SaveChangesAsync();
        return OperationResult.Created($"/api/care/incidents/{incidentId}/photos/{photo.Id}", new IncidentPhotoCreatedResponse(photo.Id, photo.FileName, photo.Length));
    }

    /// <summary>
    /// Tải nội dung ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="incidentId">Giá trị kiểu Guid dùng trong DownloadPhoto.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public async Task<OperationResult> DownloadPhoto(Guid incidentId, Guid id)
    {
        await Check(incidentId, db, access, current); var photo = Ensure.Found(await db.IncidentPhotos.SingleOrDefaultAsync(x => x.Id == id && x.IncidentId == incidentId));
        return OperationResult.File(await storage.OpenRead(photo.StorageName), photo.ContentType, photo.FileName);
    }

    /// <summary>
    /// Kiểm quyền truy cập sự cố và ảnh theo người báo/phạm vi trước upload hoặc download.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="db">Giá trị kiểu ClubDbContext dùng trong Check.</param>
    /// <param name="access">Giá trị kiểu ClubAccess dùng trong Check.</param>
    /// <param name="current">Giá trị kiểu CurrentUser dùng trong Check.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public static async Task<Incident> Check(Guid id, ClubDbContext db, ClubAccess access, CurrentUser current)
    {
        var incident = Ensure.Found(await db.Incidents.FindAsync(id)); await access.Horse(incident.HorseId); var user = await current.Get();
        Ensure.That(user.Role == Role.ClubManager || user.Id == incident.ReporterId || user.Role == incident.RoutedTo, Messages.Get(MessageKey.IncidentIsOutsideYourScope), 403, "forbidden");
        return incident;
    }

}
