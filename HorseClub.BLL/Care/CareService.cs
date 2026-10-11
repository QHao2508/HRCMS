using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Care;

public sealed class CareService(ClubAccess access, CurrentUser current, ICareRepository repository, IUnitOfWork unitOfWork, PageReader pager, ClubEvents events, TimeProvider clock) : ICareService
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
        var u = await current.Get(); var scope = await access.Scope();
        Ensure.Role(u, Role.ClubManager, Role.HorseOwner, Role.Groom, Role.Veterinarian);
        if (horseId.HasValue) await access.Horse(horseId.Value);
        var clinical = u.Role is Role.Groom or Role.Veterinarian;
        var data = await pager.Page(page, pageSize, (p, size) => repository.ListTasksAsync(scope, horseId, u.Role == Role.Groom ? u.Id : null, status, from, to, p, size));
        return PageReader.Map(data, x => new CareTaskSummaryResponse(x.Id, x.HorseId, x.GroomId, x.Type, x.ScheduledAt, x.Status, x.CompletedAt, x.ApprovedPortionKg, x.ActualPortionKg, clinical || x.Type is not (CareType.Treatment or CareType.IceBath) ? x.Instructions : null, clinical || x.Type is not (CareType.Treatment or CareType.IceBath) ? x.Notes : null));
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
        Ensure.That(await repository.HasGroomAsync(r.HorseId, r.GroomId), Messages.Get(MessageKey.GroomMustBeAssignedToThisHorse));
        Ensure.That(r.ScheduledAt != default, Messages.Get(MessageKey.ScheduleTimeIsRequired));
        if (r.TreatmentPlanId.HasValue) Ensure.That(await repository.HasTreatmentAsync(r.TreatmentPlanId, r.HorseId), Messages.Get(MessageKey.TreatmentMustBeActiveAndBelongToThisHorse));
        if (r.Type == CareType.Treatment) Ensure.That(r.TreatmentPlanId.HasValue, Messages.Get(MessageKey.TreatmentTasksNeedATreatmentPlan));
        if (r.Type == CareType.Feeding) Ensure.That(r.ApprovedPortionKg.HasValue, Messages.Get(MessageKey.FeedingNeedsAnApprovedPortionInKg));
        var task = new CareTask { HorseId = r.HorseId, GroomId = r.GroomId, TreatmentPlanId = r.TreatmentPlanId, Type = r.Type, ScheduledAt = r.ScheduledAt.ToUniversalTime(), Instructions = r.Instructions, ApprovedPortionKg = r.ApprovedPortionKg };
        repository.AddCareTask(task); events.Notify(r.GroomId, NotificationType.CareAssigned, MessageKey.ACareTaskWasAssignedToYou, task.Id);
        await events.Audit(AuditAction.CareTaskCreated, task.Id); await events.RefreshHorse(r.HorseId); await unitOfWork.SaveChangesAsync(); return task;
    }

    /// <summary>
    /// Ghi nhận công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<CareTask> RecordTask(Guid id, CareCompletionRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.Groom); var t = Ensure.Found(await repository.FindCareTaskAsync(id));
        Ensure.That(t.GroomId == u.Id, Messages.Get(MessageKey.TaskIsNotAssignedToYou), 403, "forbidden"); await access.Horse(t.HorseId);
        Ensure.That(t.Status is CareStatus.Pending or CareStatus.InProgress, Messages.Get(MessageKey.TaskIsAlreadyFinal), 409, "invalid_state");
        Ensure.That(r.Status is CareStatus.InProgress or CareStatus.Completed or CareStatus.Skipped or CareStatus.IssueReported, Messages.Get(MessageKey.UnsupportedStatus));
        if (t.Type == CareType.Feeding && r.Status == CareStatus.Completed) Ensure.That(r.ActualPortionKg.HasValue, Messages.Get(MessageKey.RecordActualFeedingPortion));
        t.Status = r.Status; t.ActualPortionKg = r.ActualPortionKg; t.Notes = r.Notes;
        if (r.Status != CareStatus.InProgress) t.CompletedAt = clock.GetUtcNow();
        if (r.Status == CareStatus.IssueReported)
        {
            repository.AddIncident(new Incident { HorseId = t.HorseId, ReporterId = u.Id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.CareObservation, Description = r.Notes, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
            await events.HorseStaff(t.HorseId, NotificationType.CareIssue, MessageKey.ACareTaskRequiresReview, Role.Veterinarian, Role.Trainer);
        }
        await events.Audit(AuditAction.CareTaskRecorded, id); await events.RefreshHorse(t.HorseId); await unitOfWork.SaveChangesAsync(); return t;
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
        var scope = await access.Scope();
        var reporter = u.Role == Role.ClubManager ? (Guid?)null : u.Id;
        var routedRole = u.Role is Role.Trainer or Role.Veterinarian ? (Role?)u.Role : null;
        return await pager.Page(page, pageSize, (p, size) => repository.ListIncidentsAsync(scope, horseId, reporter, routedRole, p, size));
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
        repository.AddIncident(incident); await events.HorseStaff(r.HorseId, NotificationType.IncidentReported, MessageKey.ANewIncidentRequiresReview, r.RoutedTo);
        await events.Audit(AuditAction.IncidentCreated, incident.Id); await events.RefreshHorse(r.HorseId); await unitOfWork.SaveChangesAsync(); return incident;
    }

    /// <summary>
    /// Xử lý quyết định cho sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ResolveIncident(Guid id)
    {
        var u = await current.Get(); var incident = Ensure.Found(await repository.FindIncidentAsync(id)); await access.Horse(incident.HorseId);
        Ensure.That(u.Role == incident.RoutedTo, Messages.Get(MessageKey.OnlyTheRoutedDecisionRoleCanResolveThisIncident), 403, "forbidden");
        incident.Resolved = true; await events.Audit(AuditAction.IncidentResolved, id); await events.RefreshHorse(incident.HorseId); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="page">Giá trị kiểu int? dùng trong ListStables.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListStables.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<Stable>> ListStables(int? page, int? pageSize)
    { Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); return await pager.Page(page, pageSize, repository.ListStablesAsync); }

    /// <summary>
    /// Tạo mới khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Stable> CreateStable(NameRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var stable = new Stable { Name = r.Name }; repository.AddStable(stable);
        await events.Audit(AuditAction.StableCreated, stable.Id); await events.RefreshRoles(Role.ClubManager, Role.Groom); await unitOfWork.SaveChangesAsync(); return stable;
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
        Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom);
        var data = await pager.Page(page, pageSize, (p, size) => repository.ListStallsAsync(stableId, p, size));
        var occupancy = await repository.ListStallOccupanciesAsync(data.Items.Select(x => x.Id), await access.Scope());
        return PageReader.Map(data, x => new StallSummaryResponse(x.Id, x.StableId, x.Name, x.CleaningStatus, occupancy.ContainsKey(x.Id), occupancy.GetValueOrDefault(x.Id)));
    }

    /// <summary>
    /// Tạo mới ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Stall> CreateStall(StallRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await repository.FindStableAsync(r.StableId));
        var stall = new Stall { StableId = r.StableId, Name = r.Name }; repository.AddStall(stall); await events.Audit(AuditAction.StallCreated, stall.Id); await events.RefreshRoles(Role.ClubManager, Role.Groom); await unitOfWork.SaveChangesAsync(); return stall;
    }

    /// <summary>
    /// Ghi nhận sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<StallOccupancy> OccupyStall(Guid id, OccupancyRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); Ensure.Found(await repository.FindStallAsync(id)); await access.Horse(r.HorseId);
        Ensure.That(!await repository.IsOccupiedAsync(id), Messages.Get(MessageKey.StallIsOccupiedVacateItFirst), 409, "stall_occupied");
        foreach (var old in await repository.GetHorseOccupanciesAsync(r.HorseId)) old.EndedAt = clock.GetUtcNow();
        var o = new StallOccupancy { StallId = id, HorseId = r.HorseId }; repository.AddStallOccupancy(o); await events.Audit(AuditAction.StallOccupied, id); await events.RefreshRoles(Role.ClubManager, Role.Groom); await unitOfWork.SaveChangesAsync(); return o;
    }

    /// <summary>
    /// Kết thúc sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> VacateStall(Guid id)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var occupancy = Ensure.Found(await repository.GetStallOccupancyAsync(id));
        occupancy.EndedAt = clock.GetUtcNow(); await events.Audit(AuditAction.StallVacated, id); await events.RefreshRoles(Role.ClubManager, Role.Groom); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đánh dấu vệ sinh ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> MarkStallClean(Guid id)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom); var stall = Ensure.Found(await repository.FindStallAsync(id));
        if (u.Role == Role.Groom)
        {
            var occupancy = Ensure.Found(await repository.GetStallOccupancyAsync(id));
            await access.Horse(occupancy.HorseId);
        }
        stall.CleaningStatus = CleaningStatus.Clean; await events.Audit(AuditAction.StallCleaned, id); await events.RefreshRoles(Role.ClubManager, Role.Groom); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

}
