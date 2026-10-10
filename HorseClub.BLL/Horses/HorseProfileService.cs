using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Horses;

public sealed class HorseProfileService(ClubAccess access, PageReader pager, IHorseRepository repository, IUnitOfWork unitOfWork, CurrentUser current, ClubEvents events, TimeProvider clock, ClubCalendar calendar) : IHorseProfileService
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
        var scope = await access.Scope(); var (p, size) = pager.Read(page, pageSize);
        var data = await repository.ListAsync(scope, search, healthStatus, p, size);
        return new(data.Items, p, size, data.Total);
    }

    /// <summary>
    /// Đọc chi tiết ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<HorseDetailResponse> GetHorse(Guid id)
    {
        var horse = await access.Horse(id, allowArchived: true);
        var attachment = await repository.GetPhotoAsync(horse.RegistrationId);
        var photo = attachment is null ? null : new HorsePhotoResponse(attachment.Id, attachment.FileName, attachment.StorageName, attachment.ContentType, attachment.Length, $"/api/horses/{id}/photo");
        var registration = await repository.GetRegistrationAsync(horse.RegistrationId);
        return new HorseDetailResponse(horse, await repository.GetLatestMeasurementAsync(id), await repository.GetAssignmentsAsync(id), await repository.GetOccupancyAsync(id), new HorsePreferencesResponse(registration.PreferredHeadTrainerId, registration.PreferredGroomId, registration.PreferredVeterinarianId), photo);
    }
    public async Task<HorseAssignmentHistoryResponse> GetAssignmentHistory(Guid id)
    {
        var horse = await access.AssignmentHistoryHorse(id);
        var user = await current.Get();
        var assignments = await repository.GetAssignmentsAsync(id);
        if (user.Role is not (Role.ClubManager or Role.HorseOwner)) assignments = assignments.Where(x => x.StaffId == user.Id).ToList();
        return new(horse.Id, horse.Name, horse.Archived, assignments);
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang số đo thể chất trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListMeasurements.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListMeasurements.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<Measurement>> ListMeasurements(Guid id, int? page, int? pageSize)
    { await access.Horse(id, allowArchived: true); var (p, size) = pager.Read(page, pageSize); var data = await repository.ListMeasurementsAsync(id, p, size); return new(data.Items, p, size, data.Total); }

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
        repository.AddMeasurement(m); await events.Audit(AuditAction.HorseMeasurementAdded, id); await events.RefreshHorse(id); await unitOfWork.SaveChangesAsync(); return m;
    }

    /// <summary>
    /// Lưu trữ ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ArchiveHorse(Guid id)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var horse = await access.Horse(id);
        Ensure.That(!await repository.HasActiveSessionAsync(id), Messages.Get(MessageKey.FinishActiveSessionsBeforeArchiving), 409, "invalid_state");
        Ensure.That(!await repository.HasActiveCareAsync(id), Messages.Get(MessageKey.FinishActiveCareBeforeArchiving), 409, "invalid_state");
        var now = clock.GetUtcNow();
        var assignments = await repository.GetActiveAssignmentsAsync(id);
        var plans = await repository.GetOpenPlansAsync(id);
        // Capture recipients before ending assignments; all changes share the request transaction.
        await events.RefreshHorse(id);
        foreach (var session in await repository.GetPendingSessionsAsync(id))
        {
            session.Status = SessionStatus.Skipped;
            session.Notes += Messages.Get(MessageKey.Skipped, "HorseArchived");
            var plan = plans.SingleOrDefault(x => x.Id == session.PlanId);
            if (plan is not null) await events.TrainingHistory(plan, session);
            if (session.RiderId.HasValue) events.Notify(session.RiderId.Value, NotificationType.SessionSkipped, MessageKey.ThePlanWasArchivedAndThisSessionWasCancelled, session.Id);
        }
        foreach (var care in await repository.GetPendingCareAsync(id))
        {
            care.Status = CareStatus.Skipped;
            care.Notes += Messages.Get(MessageKey.Skipped, "HorseArchived");
            events.Notify(care.GroomId, NotificationType.CareIssue, MessageKey.HorseIsArchived, care.Id);
        }
        foreach (var plan in plans) { plan.Status = PlanStatus.Archived; await events.TrainingHistory(plan); }
        foreach (var assignment in assignments)
        {
            events.Notify(assignment.StaffId, NotificationType.HorseAssignment, MessageKey.HorseIsArchived, id);
            assignment.Active = false; assignment.EndDate = calendar.DateAt(now);
        }
        events.Notify(horse.OwnerId, NotificationType.HorseAssignment, MessageKey.HorseIsArchived, id);
        horse.Archived = true;
        foreach (var occupancy in await repository.GetOccupanciesAsync(id)) occupancy.EndedAt = now;
        await events.Audit(AuditAction.HorseArchived, id, "HorseArchived"); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

}
