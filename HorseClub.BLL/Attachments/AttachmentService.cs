using HorseClub.BLL.Contracts;
using HorseClub.BLL.Messaging;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Attachments;

public sealed class AttachmentService(ClubAccess access, IFileRepository repository, IUnitOfWork unitOfWork, UploadStorage storage, CurrentUser current, ClubEvents events, IOptions<StorageOptions> options) : IAttachmentService
{
    /// <summary>
    /// Kiểm quyền xem ngựa, chọn ảnh HorsePhoto mới nhất từ hồ sơ đăng ký rồi stream nội dung storage; không công khai blob riêng.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<OperationResult> GetHorsePhoto(Guid horseId)
    {
        var horse = await access.Horse(horseId, allowArchived: true);
        var attachment = Ensure.Found(await repository.GetHorsePhotoAsync(horse.RegistrationId));
        return OperationResult.File(await storage.OpenRead(attachment.StorageName), attachment.ContentType, attachment.FileName);
    }

    /// <summary>
    /// Trả metadata tệp của hồ sơ theo thời gian, không trả khóa Azure hoặc nội dung file.
    /// </summary>
    /// <param name="registrationId">ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<List<AttachmentResponse>> ListAttachments(Guid registrationId)
    {
        await access.Registration(registrationId);
        return (await repository.ListAttachmentsAsync(registrationId)).Select(x => new AttachmentResponse(x.Id, x.FileName, x.ContentType, x.Length, x.Type, x.CertificateNumber, x.IssueDate, x.ExpiryDate)).ToList();
    }

    /// <summary>
    /// Kiểm quyền/trạng thái hồ sơ, giới hạn tệp, chữ ký PNG/JPEG/PDF và metadata; upload storage rồi lưu Attachment/audit. Transaction middleware dọn blob nếu ghi DB thất bại.
    /// </summary>
    /// <param name="registrationId">ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> UploadAttachment(Guid registrationId, UploadRequest request)
    {
        var reg = await access.Registration(registrationId); var u = await current.Get();
        Ensure.That(reg.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired || u.Role == Role.ClubManager && reg.Status == RegistrationStatus.PendingReview, Messages.Get(MessageKey.CannotAddDocumentsInThisState), 409, "invalid_state");
        Ensure.That(request.HasFormContentType, Messages.Get(MessageKey.MultipartFormDataRequired));
        var form = await request.ReadFormAsync(); var file = form.Files.GetFile("file");
        Ensure.That(file is not null && file.Length > 0 && file.Length <= options.Value.MaxFileBytes, Messages.Get(MessageKey.UploadOneFileUpToBytes, options.Value.MaxFileBytes));
        Ensure.That(await repository.CountAttachmentsAsync(registrationId) < options.Value.MaxAttachmentsPerRecord, Messages.Get(MessageKey.AttachmentLimitReached));
        var rawType = form["type"].ToString();
        Ensure.That(Enum.TryParse<AttachmentType>(rawType, false, out var type) && Enum.IsDefined(type) && !int.TryParse(rawType, out _) && type != AttachmentType.IncidentPhoto, Messages.Get(MessageKey.InvalidAttachmentType));
        var bytes = new byte[file!.Length];
        await using (var stream = file.OpenReadStream()) await stream.ReadExactlyAsync(bytes);
        var contentType = Detect(bytes);
        Ensure.That(contentType is not null && (type != AttachmentType.HorsePhoto || contentType.StartsWith("image/")), Messages.Get(MessageKey.OnlyValidPNGJPEGOrPDFFilesAreAccepted));
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        Ensure.That(contentType switch { "image/png" => extension == ".png", "image/jpeg" => extension is ".jpg" or ".jpeg", "application/pdf" => extension == ".pdf", _ => false }, Messages.Get(MessageKey.FileExtensionMustMatchItsContent));
        DateOnly? issue = ParseDate(form["issueDate"].ToString()); DateOnly? expiry = ParseDate(form["expiryDate"].ToString());
        Ensure.That(!issue.HasValue || !expiry.HasValue || expiry >= issue, Messages.Get(MessageKey.CertificateDatesAreInvalid));
        var attachment = new Attachment
        {
            RegistrationId = registrationId,
            UploadedBy = u.Id,
            FileName = Path.GetFileName(file.FileName),
            StorageName = Guid.NewGuid().ToString("N"),
            ContentType = contentType!,
            Length = file.Length,
            Type = type,
            CertificateNumber = form["certificateNumber"].ToString(),
            IssueDate = issue,
            ExpiryDate = expiry
        };
        Ensure.That(attachment.FileName.Length <= 200 && attachment.CertificateNumber.Length <= 100, Messages.Get(MessageKey.FilenameOrCertificateNumberIsTooLong));

        await storage.Write(attachment.StorageName, bytes, request.CancellationToken, contentType!);
        repository.AddAttachment(attachment); await events.Audit(AuditAction.AttachmentUploaded, attachment.Id); await events.RefreshRegistration(reg); await unitOfWork.SaveChangesAsync();
        return OperationResult.Created($"/api/registrations/{registrationId}/attachments/{attachment.Id}", new AttachmentCreatedResponse(attachment.Id, attachment.FileName, attachment.Type, attachment.Length));
    }

    /// <summary>
    /// Kiểm quyền hồ sơ và attachment thuộc hồ sơ đó rồi stream file với tên/content type đã lưu.
    /// </summary>
    /// <param name="registrationId">ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<OperationResult> DownloadAttachment(Guid registrationId, Guid id)
    {
        await access.Registration(registrationId); var attachment = Ensure.Found(await repository.FindAttachmentAsync(registrationId, id));
        // Force download to avoid active content executing in the API origin.
        return OperationResult.File(await storage.OpenRead(attachment.StorageName), attachment.ContentType, attachment.FileName);
    }

    /// <summary>
    /// Đọc ngày metadata theo yyyy-MM-dd, cho phép rỗng và từ chối ngày sai định dạng.
    /// </summary>
    /// <param name="value">Giá trị kiểu string dùng trong ParseDate.</param>
    public static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Ensure.That(DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date), Messages.Get(MessageKey.DateMustBeYyyyMMDd)); return date;
    }
    /// <summary>
    /// Nhận diện PNG/JPEG/PDF bằng chữ ký bytes; không tin tên file hoặc MIME client gửi.
    /// </summary>
    /// <param name="b">Giá trị kiểu byte[] dùng trong Detect.</param>
    public static string? Detect(byte[] b)
    {
        if (b.Length >= 8 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (b.Length >= 3 && b[0] == 255 && b[1] == 216 && b[2] == 255) return "image/jpeg";
        if (b.Length >= 5 && System.Text.Encoding.ASCII.GetString(b, 0, 5) == "%PDF-") return "application/pdf";
        return null;
    }

}
