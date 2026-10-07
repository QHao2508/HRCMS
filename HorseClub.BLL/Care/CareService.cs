using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Care;

public sealed class CareService(ClubAccess access, CurrentUser current, ClubDbContext db, PageReader pager, ClubEvents events, TimeProvider clock)
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="status">Trạng thái enum API, tách khỏi nhãn tiếng Việt.</param>
    /// <param name="from">Giá trị kiểu DateTimeOffset? dùng trong ListTasks.</param>
    /// <param name="to">URL nội bộ mà liên kết/redirect hướng đến.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListTasks.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListTasks.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<CareTaskSummaryResponse>> ListTasks(Guid? horseId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize)
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
        return await pager.Page(q.OrderBy(x => x.ScheduledAt).Select(x => new CareTaskSummaryResponse(x.Id, x.HorseId, x.GroomId, x.Type, x.ScheduledAt, x.Status, x.CompletedAt, x.ApprovedPortionKg, x.ActualPortionKg, clinical || x.Type != CareType.Treatment ? x.Instructions : null, clinical || x.Type != CareType.Treatment ? x.Notes : null)), page, pageSize);
    }

    /// <summary>
    /// Tạo mới công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<CareTask> CreateTask(CareRequest r)
    {
        var u = await current.Get(); await access.Horse(r.HorseId);
        Ensure.Role(u, Role.ClubManager, Role.Veterinarian);
        if (r.Type is CareType.Treatment or CareType.IceBath) Ensure.Role(u, Role.Veterinarian);
        if (u.Role == Role.Veterinarian) Ensure.That(r.Type is CareType.Treatment or CareType.IceBath, Messages.Get(MessageKey.VeterinarianAssignsTreatmentOrIceBathTasks));
        await access.Staff(r.GroomId, Role.Groom);
        Ensure.That(await db.Assignments.AnyAsync(x => x.HorseId == r.HorseId && x.StaffId == r.GroomId && x.Active && x.Role == Role.Groom), Messages.Get(MessageKey.GroomMustBeAssignedToThisHorse));
        Ensure.That(r.ScheduledAt != default, Messages.Get(MessageKey.ScheduleTimeIsRequired));
        if (r.TreatmentPlanId.HasValue) Ensure.That(await db.Treatments.AnyAsync(x => x.Id == r.TreatmentPlanId && x.HorseId == r.HorseId && !x.Completed), Messages.Get(MessageKey.TreatmentMustBeActiveAndBelongToThisHorse));
        if (r.Type == CareType.Treatment) Ensure.That(r.TreatmentPlanId.HasValue, Messages.Get(MessageKey.TreatmentTasksNeedATreatmentPlan));
        if (r.Type == CareType.Feeding) Ensure.That(r.ApprovedPortionKg.HasValue, Messages.Get(MessageKey.FeedingNeedsAnApprovedPortionInKg));
        var task = new CareTask { HorseId = r.HorseId, GroomId = r.GroomId, TreatmentPlanId = r.TreatmentPlanId, Type = r.Type, ScheduledAt = r.ScheduledAt.ToUniversalTime(), Instructions = r.Instructions, ApprovedPortionKg = r.ApprovedPortionKg };
        db.CareTasks.Add(task); events.Notify(r.GroomId, NotificationType.CareAssigned, MessageKey.ACareTaskWasAssignedToYou, task.Id);
        await events.Audit(AuditAction.CareTaskCreated, task.Id); await db.SaveChangesAsync(); return task;
    }

    /// <summary>
    /// Ghi nhận công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<CareTask> RecordTask(Guid id, CareCompletionRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.Groom); var t = Ensure.Found(await db.CareTasks.FindAsync(id));
        Ensure.That(t.GroomId == u.Id, Messages.Get(MessageKey.TaskIsNotAssignedToYou), 403, "forbidden"); await access.Horse(t.HorseId);
        Ensure.That(t.Status is CareStatus.Pending or CareStatus.InProgress, Messages.Get(MessageKey.TaskIsAlreadyFinal), 409, "invalid_state");
        Ensure.That(r.Status is CareStatus.InProgress or CareStatus.Completed or CareStatus.Skipped or CareStatus.IssueReported, Messages.Get(MessageKey.UnsupportedStatus));
        if (t.Type == CareType.Feeding && r.Status == CareStatus.Completed) Ensure.That(r.ActualPortionKg.HasValue, Messages.Get(MessageKey.RecordActualFeedingPortion));
        t.Status = r.Status; t.ActualPortionKg = r.ActualPortionKg; t.Notes = r.Notes;
        if (r.Status != CareStatus.InProgress) t.CompletedAt = clock.GetUtcNow();
        if (r.Status == CareStatus.IssueReported)
        {
            db.Incidents.Add(new Incident { HorseId = t.HorseId, ReporterId = u.Id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.CareObservation, Description = r.Notes, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
            await events.HorseStaff(t.HorseId, NotificationType.CareIssue, MessageKey.ACareTaskRequiresReview, Role.Veterinarian, Role.Trainer);
        }
        await events.Audit(AuditAction.CareTaskRecorded, id); await db.SaveChangesAsync(); return t;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListIncidents.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListIncidents.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<Incident>> ListIncidents(Guid? horseId, int? page, int? pageSize)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Veterinarian, Role.Trainer, Role.Groom, Role.WorkRider);
        var horses = (await access.Horses()).Select(x => x.Id); var q = db.Incidents.Where(x => horses.Contains(x.HorseId));
        if (u.Role is Role.Groom or Role.WorkRider) q = q.Where(x => x.ReporterId == u.Id);
        if (u.Role is Role.Trainer or Role.Veterinarian) q = q.Where(x => x.RoutedTo == u.Role || x.ReporterId == u.Id);
        if (horseId.HasValue) q = q.Where(x => x.HorseId == horseId);
        return await pager.Page(q.OrderByDescending(x => x.OccurredAt), page, pageSize);
    }

    /// <summary>
    /// Báo cáo sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Incident> ReportIncident(IncidentRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.Groom, Role.WorkRider, Role.Trainer, Role.Veterinarian);
        await access.Horse(r.HorseId); Ensure.That(r.RoutedTo is Role.Trainer or Role.Veterinarian, Messages.Get(MessageKey.RouteIncidentToTrainerOrVeterinarian));
        Ensure.That(r.OccurredAt != default && r.OccurredAt <= DateTimeOffset.UtcNow, Messages.Get(MessageKey.IncidentTimeCannotBeInTheFuture));
        var incident = new Incident { HorseId = r.HorseId, ReporterId = u.Id, OccurredAt = r.OccurredAt, Type = r.Type, Description = r.Description, Severity = r.Severity, RoutedTo = r.RoutedTo };
        db.Incidents.Add(incident); await events.HorseStaff(r.HorseId, NotificationType.IncidentReported, MessageKey.ANewIncidentRequiresReview, r.RoutedTo);
        await events.Audit(AuditAction.IncidentCreated, incident.Id); await db.SaveChangesAsync(); return incident;
    }

    /// <summary>
    /// Xử lý quyết định cho sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ResolveIncident(Guid id)
    {
        var u = await current.Get(); var incident = Ensure.Found(await db.Incidents.FindAsync(id)); await access.Horse(incident.HorseId);
        Ensure.That(u.Role == incident.RoutedTo, Messages.Get(MessageKey.OnlyTheRoutedDecisionRoleCanResolveThisIncident), 403, "forbidden");
        incident.Resolved = true; await events.Audit(AuditAction.IncidentResolved, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="page">Giá trị kiểu int? dùng trong ListStables.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListStables.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<Stable>> ListStables(int? page, int? pageSize)
    { Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); return await pager.Page(db.Stables.OrderBy(x => x.Name).ThenBy(x => x.Id), page, pageSize); }

    /// <summary>
    /// Tạo mới khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Stable> CreateStable(NameRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var stable = new Stable { Name = r.Name }; db.Stables.Add(stable);
        await events.Audit(AuditAction.StableCreated, stable.Id); await db.SaveChangesAsync(); return stable;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="stableId">Giá trị kiểu Guid? dùng trong ListStalls.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListStalls.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListStalls.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<StallSummaryResponse>> ListStalls(Guid? stableId, int? page, int? pageSize)
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); var q = db.Stalls.AsQueryable(); if (stableId.HasValue) q = q.Where(x => x.StableId == stableId);
        return await pager.Page(q.OrderBy(x => x.Name).Select(x => new StallSummaryResponse(x.Id, x.StableId, x.Name, x.CleaningStatus)), page, pageSize);
    }

    /// <summary>
    /// Tạo mới ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Stall> CreateStall(StallRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await db.Stables.FindAsync(r.StableId));
        var stall = new Stall { StableId = r.StableId, Name = r.Name }; db.Stalls.Add(stall); await events.Audit(AuditAction.StallCreated, stall.Id); await db.SaveChangesAsync(); return stall;
    }

    /// <summary>
    /// Ghi nhận sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<StallOccupancy> OccupyStall(Guid id, OccupancyRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await db.Stalls.FindAsync(id)); await access.Horse(r.HorseId);
        Ensure.That(!await db.Occupancies.AnyAsync(x => x.StallId == id && x.EndedAt == null), Messages.Get(MessageKey.StallIsOccupiedVacateItFirst), 409, "stall_occupied");
        foreach (var old in await db.Occupancies.Where(x => x.HorseId == r.HorseId && x.EndedAt == null).ToListAsync()) old.EndedAt = clock.GetUtcNow();
        var o = new StallOccupancy { StallId = id, HorseId = r.HorseId }; db.Occupancies.Add(o); await events.Audit(AuditAction.StallOccupied, id); await db.SaveChangesAsync(); return o;
    }

    /// <summary>
    /// Kết thúc sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> VacateStall(Guid id)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var occupancy = Ensure.Found(await db.Occupancies.SingleOrDefaultAsync(x => x.StallId == id && x.EndedAt == null));
        occupancy.EndedAt = clock.GetUtcNow(); await events.Audit(AuditAction.StallVacated, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đánh dấu vệ sinh ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> MarkStallClean(Guid id)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom); var stall = Ensure.Found(await db.Stalls.FindAsync(id));
        if (u.Role == Role.Groom)
        {
            var occupancy = Ensure.Found(await db.Occupancies.SingleOrDefaultAsync(x => x.StallId == id && x.EndedAt == null));
            await access.Horse(occupancy.HorseId);
        }
        stall.CleaningStatus = CleaningStatus.Clean; await events.Audit(AuditAction.StallCleaned, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

}
