using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Horses;

public sealed class HorseProfileService(ClubAccess access, PageReader pager, ClubDbContext db, CurrentUser current, ClubEvents events, TimeProvider clock, ClubCalendar calendar)
{

    /// <summary>
    /// Đọc danh sách có lọc/phân trang ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="search">Giá trị kiểu string? dùng trong ListHorses.</param>
    /// <param name="healthStatus">Giá trị kiểu HealthStatus? dùng trong ListHorses.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListHorses.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListHorses.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<Horse>> ListHorses(string? search, HealthStatus? healthStatus, int? page, int? pageSize)
    {
        var q = await access.Horses();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Name.Contains(search) || x.RegistrationNumber != null && x.RegistrationNumber.Contains(search));
        if (healthStatus.HasValue) q = q.Where(x => x.HealthStatus == healthStatus);
        return await pager.Page(q.OrderBy(x => x.Name).ThenBy(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Đọc chi tiết ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<HorseDetailResponse> GetHorse(Guid id)
    {
        var horse = await access.Horse(id);
        var photo = await db.Attachments.AsNoTracking().Where(x => x.RegistrationId == horse.RegistrationId && x.Type == AttachmentType.HorsePhoto)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new HorsePhotoResponse(x.Id, x.FileName, x.StorageName, x.ContentType, x.Length, $"/api/horses/{id}/photo")).FirstOrDefaultAsync();
        return new HorseDetailResponse(horse, await db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date).FirstOrDefaultAsync(), await db.Assignments.Where(x => x.HorseId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(), await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).FirstOrDefaultAsync(), await db.Registrations.Where(x => x.Id == horse.RegistrationId).Select(x => new HorsePreferencesResponse(x.PreferredHeadTrainerId, x.PreferredGroomId, x.PreferredVeterinarianId)).SingleAsync(), photo);
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang số đo thể chất trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListMeasurements.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListMeasurements.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<Measurement>> ListMeasurements(Guid id, int? page, int? pageSize)
    { await access.Horse(id); return await pager.Page(db.Measurements.Where(x => x.HorseId == id).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id), page, pageSize); }

    /// <summary>
    /// Bổ sung số đo thể chất trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Measurement> AddMeasurement(Guid id, MeasurementRequest request)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Veterinarian, Role.Groom);
        var horse = await access.Horse(id);
        Ensure.That(request.Date >= horse.DateOfBirth && request.Date <= calendar.Today(clock), Messages.Get(MessageKey.InvalidMeasurementDate));
        var m = new Measurement { HorseId = id, Date = request.Date, HeightCm = request.HeightCm, WeightKg = request.WeightKg };
        db.Measurements.Add(m); await events.Audit(AuditAction.HorseMeasurementAdded, id); await db.SaveChangesAsync(); return m;
    }

    /// <summary>
    /// Lưu trữ ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ArchiveHorse(Guid id)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var horse = await access.Horse(id);
        Ensure.That(!await db.Sessions.AnyAsync(x => x.HorseId == id && x.Status == SessionStatus.InProgress), Messages.Get(MessageKey.FinishActiveSessionsBeforeArchiving), 409, "invalid_state");
        horse.Archived = true;
        foreach (var occupancy in await db.Occupancies.Where(x => x.HorseId == id && x.EndedAt == null).ToListAsync()) occupancy.EndedAt = DateTimeOffset.UtcNow;
        await events.Audit(AuditAction.HorseArchived, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

}
