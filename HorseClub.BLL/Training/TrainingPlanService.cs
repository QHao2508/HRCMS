using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.BLL.Training;

public sealed class TrainingPlanService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, ClubCalendar calendar, PageReader pager)
{
    /// <summary>
    /// Tạo mới kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> CreatePlan(PlanRequest r)
    {
        await access.Trainer(r.HorseId); var u = await current.Get();
        var template = Ensure.Found(await db.Templates.FindAsync(r.TemplateId));
        Ensure.That(!template.Archived, Messages.Get(MessageKey.TemplateIsArchived));
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, Messages.Get(MessageKey.InvalidPlanDates));
        var p = new TrainingPlan { HorseId = r.HorseId, TrainerId = u.Id, TemplateId = r.TemplateId, Goal = r.Goal, Phase = r.Phase, StartDate = r.StartDate, EndDate = r.EndDate, Notes = r.Notes };
        db.Plans.Add(p); await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanCreated, p.Id); await db.SaveChangesAsync(); return p;
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
    public async Task<PageResponse<TrainingPlan>> ListPlans(Guid? horseId, int? page, int? pageSize)
    {
        var horses = (await access.Horses()).Select(x => x.Id); var q = db.Plans.Where(x => horses.Contains(x.HorseId));
        var user = await current.Get();
        if (user.Role == Role.WorkRider) q = q.Where(x => db.Sessions.Any(s => s.PlanId == x.Id && s.RiderId == user.Id));
        if (horseId.HasValue) { await access.Horse(horseId.Value); q = q.Where(x => x.HorseId == horseId); }
        return await pager.Page(q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
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
        var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Horse(p.HorseId); var u = await current.Get();
        var sessions = db.Sessions.Where(x => x.PlanId == id);
        if (u.Role == Role.WorkRider)
        {
            sessions = sessions.Where(x => x.RiderId == u.Id);
            Ensure.That(await sessions.AnyAsync(), Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
        }
        var page = await pager.Page(sessions.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id), sessionPage, sessionPageSize);
        return new PlanDetailResponse(p, page.Items, await db.Restrictions.Where(x => x.HorseId == p.HorseId && !x.Cleared).ToListAsync(), page.Page, page.PageSize, page.Total);
    }

    /// <summary>
    /// Cập nhật kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> UpdatePlan(Guid id, PlanRequest r)
    {
        var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Trainer(p.HorseId);
        Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Get(MessageKey.PlanCannotBeEditedInThisState), 409, "invalid_state");
        Ensure.That(r.HorseId == p.HorseId && r.TemplateId == p.TemplateId, Messages.Get(MessageKey.HorseAndTemplateCannotBeChangedAfterPlanCreation));
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate, Messages.Get(MessageKey.InvalidDates));
        var sessions = await db.Sessions.Where(x => x.PlanId == id).ToListAsync();
        Ensure.That(sessions.All(x => calendar.DateAt(x.ScheduledAt) >= r.StartDate && calendar.DateAt(x.ScheduledAt) <= r.EndDate), Messages.Get(MessageKey.DatesWouldExcludeExistingSessions));
        p.Goal = r.Goal; p.Phase = r.Phase; p.Notes = r.Notes; p.StartDate = r.StartDate; p.EndDate = r.EndDate;
        await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanEdited, id); await db.SaveChangesAsync(); return p;
    }

    /// <summary>
    /// Thiết lập trạng thái kế hoạch trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingPlan> SetPlanStatus(Guid id, PlanStatusRequest r)
    {
        var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Trainer(p.HorseId);
        Ensure.That(p.Status is PlanStatus.Active or PlanStatus.Paused, Messages.Get(MessageKey.CompletedArchivedPlansCannotBeReopened), 409, "invalid_state");
        if (r.Status != PlanStatus.Active)
            Ensure.That(!await db.Sessions.AnyAsync(x => x.PlanId == id && x.Status == SessionStatus.InProgress), Messages.Get(MessageKey.FinishActiveSessionsFirst), 409, "invalid_state");
        if (r.Status == PlanStatus.Completed)
            Ensure.That(!await db.Sessions.AnyAsync(x => x.PlanId == id && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.Planned)), Messages.Get(MessageKey.CompleteOrSkipPendingSessionsFirst), 409, "invalid_state");
        if (r.Status == PlanStatus.Archived)
        {
            foreach (var session in await db.Sessions.Where(x => x.PlanId == id && (x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned)).ToListAsync())
            {
                session.Status = SessionStatus.Skipped;
                if (session.RiderId.HasValue) events.Notify(session.RiderId.Value, NotificationType.SessionSkipped, MessageKey.ThePlanWasArchivedAndThisSessionWasCancelled, session.Id);
                await events.TrainingHistory(p, session);
            }
        }
        p.Status = r.Status; await events.TrainingHistory(p); await events.Audit(AuditAction.TrainingPlanStatus, id, r.Status.ToString()); await db.SaveChangesAsync(); return p;
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
        var p = Ensure.Found(await db.Plans.FindAsync(id)); await access.Horse(p.HorseId);
        return await pager.Page(db.TrainingRevisions.Where(x => x.PlanId == id).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
    }

}
