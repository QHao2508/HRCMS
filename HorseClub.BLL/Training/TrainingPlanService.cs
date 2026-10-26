using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Training;

public sealed class TrainingPlanService(ITrainingRepository repository, IUnitOfWork unitOfWork, CurrentUser current, ClubAccess access, ClubEvents events, ClubCalendar calendar, PageReader pager, ITrainingSchedulingService scheduling, ITrainingTemplateService templates) : ITrainingPlanService
{
    /// <summary>
    /// Tạo mới kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> CreatePlan(PlanRequest r)
    {
        await access.Trainer(r.HorseId); var u = await current.Get();
        var template = Ensure.Found(await repository.FindTemplateAsync(r.TemplateId));
        Ensure.That(!template.Archived, Messages.Text(MessageKey.TemplateIsArchived));
        await templates.ValidateTrainer(template.Id, u.Id); TemplateCurriculumPolicy.ValidatePlan(template, r);
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, Messages.Text(MessageKey.InvalidPlanDates));
        var p = new TrainingPlan { HorseId = r.HorseId, TrainerId = u.Id, TemplateId = r.TemplateId, Goal = r.Goal, Phase = r.Phase, StartDate = r.StartDate, EndDate = r.EndDate, Notes = r.Notes, TemplatePlanOptionKey = r.TemplatePlanOptionKey, UseCustomPlan = r.UseCustomPlan };
        repository.AddPlan(p); await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanCreated, p.Id); await events.RefreshHorse(p.HorseId); await unitOfWork.SaveChangesAsync(); return p;
    }

    /// <summary>
    /// Điểm vào tạo dữ liệu của TrainingPlanService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<OperationResult> Create(PlanRequest r)
    { var p = await CreatePlan(r); return OperationResult.Created($"/api/training/plans/{p.Id}", p); }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListPlans.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListPlans.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<TrainingPlanListItem>> ListPlans(Guid? horseId, int? page, int? pageSize, string? search = null)
    {
        Ensure.That(search is null || search.Length <= 100, Messages.Text(MessageKey.InvalidRequestBody));
        var user = await current.Get();
        var scope = await access.Scope();
        if (horseId.HasValue && user.Role != Role.WorkRider) await access.Horse(horseId.Value, allowArchived: true);
        var (p, size) = pager.Read(page, pageSize);
        var data = await repository.ListPlansAsync(scope, horseId, user.Role == Role.WorkRider ? user.Id : null, p, size, search?.Trim());
        var names = data.Items.Count == 0 ? new Dictionary<Guid, HorseClub.DAL.Abstractions.TrainingNames>() : (await repository.PlanNamesAsync(data.Items.Select(x=>x.Id).ToArray())).ToDictionary(x=>x.Id);
        return new(data.Items.Select(x=>TrainingPlanListItem.From(x,names[x.Id])).ToList(),p,size,data.Total);
    }

    /// <summary>
    /// Đọc chi tiết kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="sessionPage">Giá trị kiểu int? dùng trong GetPlan.</param>
    /// <param name="sessionPageSize">Giá trị kiểu int? dùng trong GetPlan.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PlanDetailResponse> GetPlan(Guid id, int? sessionPage, int? sessionPageSize)
    {
        var p = Ensure.Found(await repository.FindPlanAsync(id)); var u = await current.Get();
        var riderId = u.Role == Role.WorkRider ? (Guid?)u.Id : null;
        if (riderId.HasValue) Ensure.That(await repository.HasRiderPlanSessionsAsync(id, riderId.Value), Messages.Text(MessageKey.PermissionDenied), 403, "forbidden");
        else await access.TrainingHistoryHorse(p.HorseId, p.TrainerId);
        var (page, size) = pager.Read(sessionPage, sessionPageSize);
        var data = await repository.ListPlanSessionsAsync(id, riderId, page, size);
        var names = (await repository.PlanNamesAsync([id])).Single();
        return new PlanDetailResponse(p, data.Items, await access.IsCurrentHorseReader(p.HorseId) ? await repository.GetRestrictionsAsync(p.HorseId) : [], page, size, data.Total, names.HorseName, names.TrainerName,
            await repository.SessionNamesAsync(data.Items.Select(x=>x.Id).ToArray()), Ensure.Found(await repository.FindHorseAsync(p.HorseId)).Archived, await repository.FindTemplateAsync(p.TemplateId));
    }

    /// <summary>
    /// Cập nhật kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> UpdatePlan(Guid id, PlanRequest r)
    {
        var p = Ensure.Found(await repository.FindPlanAsync(id)); await access.Trainer(p.HorseId);
        Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Text(MessageKey.PlanCannotBeEditedInThisState), 409, "invalid_state");
        Ensure.That(r.HorseId == p.HorseId && r.TemplateId == p.TemplateId, Messages.Text(MessageKey.HorseAndTemplateCannotBeChangedAfterPlanCreation));
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, Messages.Text(MessageKey.InvalidDates));
        var sessions = await repository.GetPlanSessionsAsync(id);
        Ensure.That(sessions.All(x => calendar.DateAt(x.ScheduledAt) >= r.StartDate && calendar.DateAt(x.ScheduledAt) <= r.EndDate), Messages.Text(MessageKey.DatesWouldExcludeExistingSessions));
        var template = Ensure.Found(await repository.FindTemplateAsync(p.TemplateId));
        await templates.ValidateTrainer(template.Id, (await current.Get()).Id); TemplateCurriculumPolicy.ValidatePlan(template, r);
        p.TemplatePlanOptionKey = r.TemplatePlanOptionKey; p.UseCustomPlan = r.UseCustomPlan;
        p.Goal = r.Goal; p.Phase = r.Phase; p.Notes = r.Notes; p.StartDate = r.StartDate; p.EndDate = r.EndDate;
        await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanEdited, id); await events.RefreshHorse(p.HorseId); await unitOfWork.SaveChangesAsync(); return p;
    }

    /// <summary>
    /// Thiết lập trạng thái kế hoạch trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> SetPlanStatus(Guid id, PlanStatusRequest r)
    {
        var p = Ensure.Found(await repository.FindPlanAsync(id)); await access.Trainer(p.HorseId);
        Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Text(MessageKey.CompletedArchivedPlansCannotBeReopened), 409, "invalid_state");
        if (r.Status == PlanStatus.Active && p.Status == PlanStatus.Paused)
            foreach (var session in await repository.GetPlanSessionsAsync(id, pendingOnly: true))
                await scheduling.ValidateSession(session.HorseId, session.RiderId, session.TrainingType, session.ScheduledAt, session.ScheduledEndAt, session.Id);
        if (r.Status != PlanStatus.Active)
            Ensure.That(!await repository.HasPlanSessionsAsync(id, SessionStatus.InProgress), Messages.Text(MessageKey.FinishActiveSessionsFirst), 409, "invalid_state");
        if (r.Status == PlanStatus.Completed)
            Ensure.That(!await repository.HasPlanSessionsAsync(id, SessionStatus.Assigned, SessionStatus.Planned), Messages.Text(MessageKey.CompleteOrSkipPendingSessionsFirst), 409, "invalid_state");
        if (r.Status == PlanStatus.Archived)
        {
            foreach (var session in await repository.GetPlanSessionsAsync(id, pendingOnly: true))
            {
                session.Status = SessionStatus.Skipped;
                if (session.RiderId.HasValue) events.Notify(session.RiderId.Value, NotificationType.SessionSkipped, MessageKey.ThePlanWasArchivedAndThisSessionWasCancelled, session.Id);
                await events.TrainingHistory(p, session);
            }
        }
        p.Status = r.Status; await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanStatus, id, r.Status.ToString()); await events.RefreshHorse(p.HorseId); await unitOfWork.SaveChangesAsync(); return p;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang lịch sử huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListHistory.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListHistory.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<TrainingRevision>> ListHistory(Guid id, int? page, int? pageSize)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.HorseOwner, Role.HeadTrainer, Role.Trainer, Role.Veterinarian);
        var p = Ensure.Found(await repository.FindPlanAsync(id)); await access.TrainingHistoryHorse(p.HorseId, p.TrainerId);
        var (number, size) = pager.Read(page, pageSize);
        var data = await repository.ListHistoryAsync(id, number, size);
        return new(data.Items, number, size, data.Total);
    }

}
