using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HorseClub.BLL.Horses;

public sealed class HorseRegistrationService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, ClubCalendar calendar, IOptions<BusinessOptions> options, PageReader pager)
{
    /// <summary>
    /// Điểm vào tạo dữ liệu của HorseRegistrationService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<HorseRegistration> Create(RegistrationDraftRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.HorseOwner);
        var registration = new HorseRegistration { OwnerId = u.Id };
        await Apply(registration, r);
        db.Registrations.Add(registration);
        await events.Audit(AuditAction.RegistrationDraftCreated, registration.Id);
        await db.SaveChangesAsync(); return registration;
    }
    /// <summary>
    /// Chỉnh sửa dữ liệu của module trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<HorseRegistration> Edit(Guid id, RegistrationDraftRequest r)
    {
        var reg = await access.Registration(id); var u = await current.Get();
        if (u.Role == Role.ClubManager)
        {
            Ensure.That(reg.Status == RegistrationStatus.PendingReview, Messages.Get(MessageKey.RegistrationCannotBeEditedInThisState), 409, "invalid_state");
            Ensure.That((r.Sire is null || r.Sire == reg.Sire) && (r.Dam is null || r.Dam == reg.Dam)
                && (!r.DateOfBirth.HasValue || r.DateOfBirth == reg.DateOfBirth) && (!r.Gender.HasValue || r.Gender == reg.Gender)
                && (r.Breed is null || r.Breed == reg.Breed) && (!r.HeightCm.HasValue || r.HeightCm == reg.HeightCm)
                && (!r.WeightKg.HasValue || r.WeightKg == reg.WeightKg) && (!r.MeasurementDate.HasValue || r.MeasurementDate == reg.MeasurementDate)
                && (r.DeclaredHealth is null || r.DeclaredHealth == reg.DeclaredHealth) && (r.HealthNotes is null || r.HealthNotes == reg.HealthNotes)
                && (!r.PreferredHeadTrainerId.HasValue || r.PreferredHeadTrainerId == reg.PreferredHeadTrainerId)
                && (!r.PreferredGroomId.HasValue || r.PreferredGroomId == reg.PreferredGroomId)
                && (!r.PreferredVeterinarianId.HasValue || r.PreferredVeterinarianId == reg.PreferredVeterinarianId),
                Messages.Get(MessageKey.ManagerMayOnlyEditAdministrativeRegistrationFields), 403, "forbidden");
            var before = new { reg.Name, reg.RegistrationNumber, reg.BoardingStart, reg.BoardingEnd };
            var administrative = Draft(reg) with
            {
                Name = r.Name ?? reg.Name,
                RegistrationNumber = r.RegistrationNumber ?? reg.RegistrationNumber,
                BoardingStart = r.BoardingStart ?? reg.BoardingStart,
                BoardingEnd = r.BoardingEnd ?? reg.BoardingEnd
            };
            await Apply(reg, administrative);
            await ValidateReady(reg);
            await events.Audit(AuditAction.RegistrationEdited, reg.Id, JsonSerializer.Serialize(new
            { Before = before, After = new { reg.Name, reg.RegistrationNumber, reg.BoardingStart, reg.BoardingEnd } }));
        }
        else
        {
            Ensure.Role(u, Role.HorseOwner);
            Ensure.That(reg.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.RegistrationCannotBeEditedInThisState), 409, "invalid_state");
            await Apply(reg, r); await events.Audit(AuditAction.RegistrationEdited, reg.Id);
        }
        await db.SaveChangesAsync(); return reg;
    }
    /// <summary>
    /// Gửi dữ liệu của module trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task Submit(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HorseOwner);
        var r = await access.Registration(id);
        Ensure.That(r.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.OnlyDraftsOrRevisionsMayBeSubmitted), 409, "invalid_state");
        await ValidateReady(r);
        r.Status = RegistrationStatus.PendingReview; r.ReviewReason = null;
        await events.Managers(NotificationType.RegistrationReview, MessageKey.AHorseRegistrationRequiresReview, id);
        await events.Audit(AuditAction.RegistrationSubmitted, id);
        await db.SaveChangesAsync();
    }
    /// <summary>
    /// Cho quản lý duyệt hoặc yêu cầu chỉnh sửa; khi duyệt tạo hồ sơ ngựa chính thức, khi từ chối giữ hồ sơ cùng lý do.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<RegistrationReviewResponse> Review(Guid id, ReviewRequest request)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager);
        var r = await access.Registration(id);
        Ensure.That(r.Status == RegistrationStatus.PendingReview, Messages.Get(MessageKey.RegistrationIsNotPendingReview), 409, "invalid_state");
        r.ReviewedBy = u.Id;
        if (!request.Approve)
        {
            Ensure.That(!string.IsNullOrWhiteSpace(request.Reason), Messages.Get(MessageKey.ARevisionReasonIsRequired));
            r.Status = RegistrationStatus.RevisionRequired; r.ReviewReason = request.Reason;
            events.Notify(r.OwnerId, NotificationType.RegistrationRevision, MessageKey.YourHorseRegistrationRequiresRevision, id);
            await events.Audit(AuditAction.RegistrationRevisionRequested, id);
            await db.SaveChangesAsync(); return new RegistrationReviewResponse(r, (Guid?)null);
        }
        await ValidateReady(r);
        r.Status = RegistrationStatus.Approved; r.ReviewReason = null;
        var horse = new Horse
        {
            RegistrationId = id,
            OwnerId = r.OwnerId,
            Name = r.Name!,
            Sire = r.Sire!,
            Dam = r.Dam!,
            DateOfBirth = r.DateOfBirth!.Value,
            Gender = r.Gender!.Value,
            Breed = r.Breed!,
            RegistrationNumber = r.RegistrationNumber,
            BoardingStart = r.BoardingStart!.Value,
            BoardingEnd = r.BoardingEnd
        };
        // Owner health is a declaration, not medical clearance; official status starts Monitoring.
        db.Horses.Add(horse);
        db.Measurements.Add(new Measurement { HorseId = horse.Id, Date = r.MeasurementDate!.Value, HeightCm = r.HeightCm!.Value, WeightKg = r.WeightKg!.Value });
        events.Notify(r.OwnerId, NotificationType.RegistrationApproved, MessageKey.YourHorseRegistrationWasApproved, horse.Id);
        await events.Audit(AuditAction.RegistrationApproved, id);
        await db.SaveChangesAsync(); return new RegistrationReviewResponse(r, (Guid?)horse.Id);
    }
    /// <summary>
    /// Áp dụng các trường intake được phép lên hồ sơ, kiểm ngày/giới hạn thể chất và giữ quy tắc riêng cho chỉnh sửa hành chính.
    /// </summary>
    /// <param name="entity">Giá trị kiểu HorseRegistration dùng trong Apply.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    private async Task Apply(HorseRegistration entity, RegistrationDraftRequest r)
    {
        Ensure.Validate(r);
        var today = calendar.Today(clock);
        Ensure.That(!r.DateOfBirth.HasValue || (r.DateOfBirth.Value != default && r.DateOfBirth <= today), Messages.Get(MessageKey.DateOfBirthMustBeValidAndNotIn));
        Ensure.That(!r.MeasurementDate.HasValue || (r.MeasurementDate.Value != default && r.MeasurementDate <= today && (!r.DateOfBirth.HasValue || r.MeasurementDate >= r.DateOfBirth)), Messages.Get(MessageKey.MeasurementDateIsInvalid));
        Ensure.That((!r.BoardingStart.HasValue || r.BoardingStart.Value != default) && (!r.BoardingEnd.HasValue || (r.BoardingEnd.Value != default && (!r.BoardingStart.HasValue || r.BoardingEnd >= r.BoardingStart))), Messages.Get(MessageKey.BoardingDatesAreInvalid));
        Ensure.That((!r.HeightCm.HasValue || r.HeightCm >= options.Value.MinHorseHeightCm && r.HeightCm <= options.Value.MaxHorseHeightCm)
            && (!r.WeightKg.HasValue || r.WeightKg >= options.Value.MinHorseWeightKg && r.WeightKg <= options.Value.MaxHorseWeightKg), Messages.Get(MessageKey.HorseMeasurementsOutsideConfiguredLimits));
        if (r.PreferredHeadTrainerId is Guid head) await access.Staff(head, Role.HeadTrainer);
        if (r.PreferredGroomId is Guid groom) await access.Staff(groom, Role.Groom);
        if (r.PreferredVeterinarianId is Guid vet) await access.Staff(vet, Role.Veterinarian);
        entity.Name = r.Name?.Trim(); entity.Sire = r.Sire?.Trim(); entity.Dam = r.Dam?.Trim(); entity.DateOfBirth = r.DateOfBirth;
        entity.Gender = r.Gender; entity.Breed = r.Breed; entity.RegistrationNumber = r.RegistrationNumber;
        entity.HeightCm = r.HeightCm; entity.WeightKg = r.WeightKg; entity.MeasurementDate = r.MeasurementDate;
        entity.DeclaredHealth = r.DeclaredHealth; entity.HealthNotes = r.HealthNotes;
        entity.BoardingStart = r.BoardingStart; entity.BoardingEnd = r.BoardingEnd;
        entity.PreferredHeadTrainerId = r.PreferredHeadTrainerId; entity.PreferredGroomId = r.PreferredGroomId; entity.PreferredVeterinarianId = r.PreferredVeterinarianId;
    }

    /// <summary>
    /// Kiểm đủ thông tin intake bắt buộc và điều kiện để gửi/duyệt; không cho dữ liệu nháp thiếu vào hồ sơ ngựa chính thức.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    private async Task ValidateReady(HorseRegistration r)
    {
        var missing = new List<string>();
        foreach (var field in new[] { (nameof(r.Name), r.Name), (nameof(r.Sire), r.Sire), (nameof(r.Dam), r.Dam),
            (nameof(r.Breed), r.Breed), (nameof(r.DeclaredHealth), r.DeclaredHealth) })
            if (string.IsNullOrWhiteSpace(field.Item2)) missing.Add(field.Item1);
        foreach (var field in new (string Name, object? Value)[] { (nameof(r.DateOfBirth), r.DateOfBirth), (nameof(r.Gender), r.Gender),
            (nameof(r.HeightCm), r.HeightCm), (nameof(r.WeightKg), r.WeightKg), (nameof(r.MeasurementDate), r.MeasurementDate), (nameof(r.BoardingStart), r.BoardingStart) })
            if (field.Value is null) missing.Add(field.Name);
        Ensure.That(missing.Count == 0, Messages.Get(MessageKey.RegistrationMissingFields, string.Join(", ", missing)));
        await Apply(r, Draft(r));
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == r.Id && x.Type == AttachmentType.HorsePhoto), Messages.Get(MessageKey.AddAHorsePhotoBeforeSubmission));
        Ensure.That(await db.Attachments.AnyAsync(x => x.RegistrationId == r.Id && x.Type == AttachmentType.Certificate), Messages.Get(MessageKey.AddACertificateBeforeSubmission));
    }

    /// <summary>
    /// Chuyển hồ sơ intake sang response nháp với các trường được phép xem/sửa.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    private static RegistrationDraftRequest Draft(HorseRegistration r) => new(r.Name, r.Sire, r.Dam, r.DateOfBirth, r.Gender,
        r.Breed, r.RegistrationNumber, r.HeightCm, r.WeightKg, r.MeasurementDate, r.DeclaredHealth, r.HealthNotes,
        r.BoardingStart, r.BoardingEnd, r.PreferredHeadTrainerId, r.PreferredGroomId, r.PreferredVeterinarianId);
    /// <summary>
    /// Tạo mới bản nháp trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public async Task<OperationResult> CreateDraft(RegistrationDraftRequest request)
    { var record = await Create(request); return OperationResult.Created($"/api/registrations/{record.Id}", record); }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="status">Trạng thái enum API, tách khỏi nhãn tiếng Việt.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListRegistrations.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListRegistrations.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<HorseRegistration>> ListRegistrations(RegistrationStatus? status, int? page, int? pageSize)
    {
        var u = await current.Get(); Ensure.Role(u, Role.HorseOwner, Role.ClubManager);
        var q = db.Registrations.AsQueryable();
        if (u.Role == Role.HorseOwner) q = q.Where(x => x.OwnerId == u.Id);
        if (status.HasValue) q = q.Where(x => x.Status == status);
        return await pager.Page(q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Đọc chi tiết hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<HorseRegistration> GetRegistration(Guid id)
    { return await access.Registration(id); }

    /// <summary>
    /// Cập nhật bản nháp trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public async Task<HorseRegistration> UpdateDraft(Guid id, RegistrationDraftRequest request)
    { return await Edit(id, request); }

    /// <summary>
    /// Gửi hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public async Task<OperationResult> SubmitRegistration(Guid id)
    { await Submit(id); return OperationResult.NoContent(); }

    /// <summary>
    /// Duyệt hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    public async Task<RegistrationReviewResponse> ReviewRegistration(Guid id, ReviewRequest request)
    { return await Review(id, request); }

    /// <summary>
    /// Hủy hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> CancelRegistration(Guid id)
    {
        Ensure.Role(await current.Get(), Role.HorseOwner);
        var record = await access.Registration(id);
        Ensure.That(record.Status is RegistrationStatus.Draft or RegistrationStatus.RevisionRequired, Messages.Get(MessageKey.OnlyDraftRevisionRegistrationsMayBeCancelled), 409, "invalid_state");
        record.Status = RegistrationStatus.Cancelled; await events.Audit(AuditAction.RegistrationCancelled, id);
        await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

}
