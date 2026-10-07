using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Training;

public sealed class TrainingSessionService(ClubDbContext db, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, IOptions<BusinessOptions> options, ClubCalendar calendar, PageReader pager)
{
    /// <summary>
    /// Kiểm tình trạng ngựa và hạn chế y tế tại thời điểm buổi tập; chặn bài tập, cường độ hoặc quãng đường không phù hợp.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="distance">Giá trị kiểu decimal dùng trong Guard.</param>
    /// <param name="intensity">Giá trị kiểu Intensity dùng trong Guard.</param>
    /// <param name="type">Giá trị kiểu TrainingType dùng trong Guard.</param>
    /// <param name="at">Thời điểm nghiệp vụ được kiểm, tính theo UTC rồi quy đổi múi giờ khi cần.</param>
    public async Task Guard(Guid horseId, decimal distance, Intensity intensity, TrainingType type, DateTimeOffset at)
    {
        var horse = Ensure.Found(await db.Horses.FindAsync(horseId));
        Ensure.That(!horse.Archived, Messages.Get(MessageKey.HorseIsArchived), 409, "horse_archived");
        if (horse.HealthStatus == HealthStatus.Isolated || horse.HealthStatus == HealthStatus.Injured && intensity == Intensity.Heavy)
            throw new ApiException(409, "medical_block", Messages.Get(MessageKey.CurrentHealthStatusPreventsThisTraining), horseId);
        var restrictions = await db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared && x.ValidFrom <= at && (x.ValidUntil == null || x.ValidUntil >= at)).ToListAsync();
        foreach (var r in restrictions)
            if (r.BlockAllTraining || r.TrainingLock && intensity == Intensity.Heavy || r.MaxIntensity.HasValue && intensity > r.MaxIntensity
                || r.MaxDistanceMetres.HasValue && distance > r.MaxDistanceMetres || r.NoSprint && type == TrainingType.Sprint)
                throw new ApiException(409, "medical_restriction", Messages.Get(MessageKey.TrainingBlocked, r.Reason), r.MedicalRecordId);
    }
    /// <summary>
    /// Tạo mới buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="planId">ID kế hoạch chứa buổi tập hoặc dữ liệu huấn luyện được thao tác.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingSession> CreateSession(Guid planId, SessionRequest r)
    {
        var plan = Ensure.Found(await db.Plans.FindAsync(planId));
        await access.Trainer(plan.HorseId);
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Get(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var session = new TrainingSession { PlanId = planId, HorseId = plan.HorseId };
        await ApplySession(session, plan, r);
        db.Sessions.Add(session);
        await events.TrainingHistory(plan, session);
        if (r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, MessageKey.ATrainingSessionWasAssignedToYou, session.Id);
        await events.Audit(AuditAction.TrainingSessionCreated, session.Id); await db.SaveChangesAsync(); return session;
    }
    /// <summary>
    /// Chỉnh sửa buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingSession> EditSession(Guid id, SessionRequest r)
    {
        var session = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Trainer(session.HorseId);
        Ensure.That(session.Status is SessionStatus.Planned or SessionStatus.Assigned, Messages.Get(MessageKey.OnlyUnstartedSessionsCanBeEdited), 409, "invalid_state");
        var plan = Ensure.Found(await db.Plans.FindAsync(session.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Get(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var oldRider = session.RiderId;
        await ApplySession(session, plan, r);
        await events.TrainingHistory(plan, session);
        if (r.RiderId != oldRider && r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, MessageKey.ATrainingSessionWasAssignedToYou, id);
        if (oldRider.HasValue && oldRider != r.RiderId) events.Notify(oldRider.Value, NotificationType.SessionUnassigned, MessageKey.ATrainingAssignmentWasRemoved, id);
        await events.Audit(AuditAction.TrainingSessionEdited, id); await db.SaveChangesAsync(); return session;
    }
    /// <summary>
    /// Kiểm thời gian trong kế hoạch/múi giờ, sức khỏe và lịch người cưỡi; áp dụng dữ liệu và chọn trạng thái Planned hoặc Assigned.
    /// </summary>
    /// <param name="s">Giá trị kiểu TrainingSession dùng trong ApplySession.</param>
    /// <param name="p">Giá trị kiểu TrainingPlan dùng trong ApplySession.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    private async Task ApplySession(TrainingSession s, TrainingPlan p, SessionRequest r)
    {
        var date = calendar.DateAt(r.ScheduledAt);
        Ensure.That(r.ScheduledAt != default && date >= p.StartDate && date <= p.EndDate, Messages.Get(MessageKey.SessionMustFallWithinPlanDatesInTheClub));
        Ensure.That(r.ScheduledAt >= clock.GetUtcNow().AddMinutes(-options.Value.ScheduleGraceMinutes), Messages.Get(MessageKey.ScheduleSessionsInTheFuture));
        await Guard(s.HorseId, r.DistanceMetres, r.Intensity, r.TrainingType, r.ScheduledAt);
        if (r.RiderId.HasValue)
        {
            await access.Staff(r.RiderId.Value, Role.WorkRider);
            Ensure.That(!await db.Sessions.AnyAsync(x => x.Id != s.Id && x.RiderId == r.RiderId && x.ScheduledAt == r.ScheduledAt && (x.Status == SessionStatus.Assigned || x.Status == SessionStatus.InProgress)), Messages.Get(MessageKey.RiderAlreadyHasASessionAtThisTime), 409, "rider_conflict");
        }
        s.ScheduledAt = r.ScheduledAt.ToUniversalTime(); s.TrainingType = r.TrainingType; s.DistanceMetres = r.DistanceMetres;
        s.Intensity = r.Intensity; s.Surface = r.Surface; s.Target = r.Target; s.Notes = r.Notes; s.RiderId = r.RiderId;
        s.Status = r.RiderId.HasValue ? SessionStatus.Assigned : SessionStatus.Planned;
    }
    /// <summary>
    /// Chỉ người cưỡi được giao mới bắt đầu; kiểm lịch, xung đột, kế hoạch hoạt động và sức khỏe ngay trước lúc chuyển InProgress.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task Start(Guid id)
    {
        var u = await current.Get(); Ensure.Role(u, Role.WorkRider);
        var s = Ensure.Found(await db.Sessions.FindAsync(id));
        Ensure.That(s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        await access.Horse(s.HorseId);
        Ensure.That(s.Status == SessionStatus.Assigned, Messages.Get(MessageKey.SessionIsNotAssignedReady), 409, "invalid_state");
        Ensure.That(s.ScheduledAt <= clock.GetUtcNow().AddMinutes(options.Value.StartEarlyMinutes), Messages.Get(MessageKey.SessionIsNotDueToStartYet), 409, "invalid_state");
        var plan = Ensure.Found(await db.Plans.FindAsync(s.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Get(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var today = calendar.Today(clock);
        Ensure.That(today >= plan.StartDate && today <= plan.EndDate, Messages.Get(MessageKey.PlanIsOutsideItsActiveDates), 409, "invalid_state");
        Ensure.That(!await db.Sessions.AnyAsync(x => x.Status == SessionStatus.InProgress && (x.RiderId == u.Id || x.HorseId == s.HorseId)), Messages.Get(MessageKey.RiderOrHorseAlreadyHasAnActiveSession), 409, "session_conflict");
        await Guard(s.HorseId, s.DistanceMetres, s.Intensity, s.TrainingType, clock.GetUtcNow());
        s.Status = SessionStatus.InProgress; s.StartedAt = clock.GetUtcNow();
        await events.TrainingHistory(plan, s);
        await events.Audit(AuditAction.TrainingSessionStarted, id); await db.SaveChangesAsync();
    }
    /// <summary>
    /// Kiểm quyền người cưỡi và buổi tập đã bắt đầu, tránh kết quả trùng; lưu kết quả đo, chuyển trạng thái và thông báo huấn luyện viên.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<SessionResult> SubmitResult(Guid id, ResultRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.WorkRider);
        var s = Ensure.Found(await db.Sessions.FindAsync(id));
        Ensure.That(s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        await access.Horse(s.HorseId);
        Ensure.That(s.Status == SessionStatus.InProgress, Messages.Get(MessageKey.StartSessionBeforeSubmittingResults), 409, "invalid_state");
        Ensure.That(!await db.Results.AnyAsync(x => x.SessionId == id), Messages.Get(MessageKey.ResultAlreadyExists), 409, "duplicate_result");
        // Always allow reporting actual activity, even if a medical lock was applied mid-session.
        // A newly conflicting restriction is recorded as an issue rather than discarding observations.
        var medicalIssue = false;
        try { await Guard(s.HorseId, r.DistanceMetres, r.Intensity, s.TrainingType, clock.GetUtcNow()); }
        catch (ApiException e) when (e.Code is "medical_block" or "medical_restriction") { medicalIssue = true; }
        var issue = r.AbnormalObservation || medicalIssue;
        var result = new SessionResult
        {
            SessionId = id,
            DistanceMetres = r.DistanceMetres,
            TimeSeconds = r.TimeSeconds,
            SpeedMetresPerSecond = decimal.Round(r.DistanceMetres / r.TimeSeconds, options.Value.SpeedDecimalPlaces),
            HeartRate = r.HeartRate,
            Intensity = r.Intensity,
            Feedback = r.Feedback,
            AbnormalObservation = issue
        };
        db.Results.Add(result); s.Status = issue ? SessionStatus.IssueReported : SessionStatus.Completed;
        await events.HorseStaff(s.HorseId, NotificationType.SessionResult, MessageKey.ARiderSubmittedASessionResult, Role.Trainer);
        if (issue)
        {
            db.Incidents.Add(new Incident { HorseId = s.HorseId, ReporterId = u.Id, SessionId = id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.TrainingObservation, Description = r.Feedback, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
            await events.HorseStaff(s.HorseId, NotificationType.IncidentReported, MessageKey.ATrainingObservationRequiresReview, Role.Veterinarian, Role.Trainer);
        }
        var plan = Ensure.Found(await db.Plans.FindAsync(s.PlanId));
        await events.TrainingHistory(plan, s, result);
        await events.Audit(AuditAction.TrainingResultSubmitted, id); await db.SaveChangesAsync(); return result;
    }

    /// <summary>
    /// Điểm vào tạo dữ liệu của TrainingSessionService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<OperationResult> Create(Guid id, SessionRequest r)
    { var s = await CreateSession(id, r); return OperationResult.Created($"/api/training/sessions/{s.Id}", s); }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="status">Trạng thái enum API, tách khỏi nhãn tiếng Việt.</param>
    /// <param name="from">Giá trị kiểu DateTimeOffset? dùng trong ListSessions.</param>
    /// <param name="to">URL nội bộ mà liên kết/redirect hướng đến.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListSessions.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListSessions.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<TrainingSession>> ListSessions(Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize)
    {
        var u = await current.Get(); var horses = (await access.Horses()).Select(x => x.Id);
        var q = db.Sessions.Where(x => horses.Contains(x.HorseId));
        if (u.Role == Role.WorkRider) q = q.Where(x => x.RiderId == u.Id);
        if (horseId.HasValue) { await access.Horse(horseId.Value); q = q.Where(x => x.HorseId == horseId); }
        if (status.HasValue) q = q.Where(x => x.Status == status);
        if (from.HasValue) q = q.Where(x => x.ScheduledAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.ScheduledAt <= to.Value);
        return await pager.Page(q.OrderBy(x => x.ScheduledAt).ThenBy(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Đọc chi tiết buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<SessionDetailResponse> GetSession(Guid id)
    {
        var s = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Horse(s.HorseId); var u = await current.Get();
        Ensure.That(u.Role != Role.WorkRider || s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        return new SessionDetailResponse(s, await db.Results.SingleOrDefaultAsync(x => x.SessionId == id), await db.Evaluations.SingleOrDefaultAsync(x => x.SessionId == id));
    }

    /// <summary>
    /// Cập nhật dữ liệu của module trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<TrainingSession> Update(Guid id, SessionRequest r)
    { return await EditSession(id, r); }

    /// <summary>
    /// Phân công Rider trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<TrainingSession> AssignRider(Guid id, RiderRequest r)
    {
        var s = Ensure.Found(await db.Sessions.FindAsync(id));
        return await EditSession(id, new SessionRequest(s.ScheduledAt, s.TrainingType, s.DistanceMetres, s.Intensity, s.Surface, s.Target, s.Notes, r.RiderId));
    }

    /// <summary>
    /// Bắt đầu buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public async Task<OperationResult> StartSession(Guid id)
    { await Start(id); return OperationResult.NoContent(); }

    /// <summary>
    /// Ghi nhận kết quả buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public async Task<SessionResult> RecordResult(Guid id, ResultRequest r)
    { return await SubmitResult(id, r); }

    /// <summary>
    /// Bỏ qua buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> SkipSession(Guid id, ReasonRequest r)
    {
        var s = Ensure.Found(await db.Sessions.FindAsync(id)); var u = await current.Get();
        if (u.Role == Role.WorkRider) { await access.Horse(s.HorseId); Ensure.That(s.RiderId == u.Id, Messages.Get(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden"); }
        else await access.Trainer(s.HorseId);
        Ensure.That(s.Status is SessionStatus.Planned or SessionStatus.Assigned or SessionStatus.InProgress, Messages.Get(MessageKey.SessionIsAlreadyFinal), 409, "invalid_state");
        s.Status = SessionStatus.Skipped; s.Notes += Messages.Get(MessageKey.Skipped, r.Reason);
        await events.TrainingHistory(Ensure.Found(await db.Plans.FindAsync(s.PlanId)), s);
        await events.HorseStaff(s.HorseId, NotificationType.SessionSkipped, MessageKey.ASessionWasSkipped, Role.Trainer);
        await events.Audit(AuditAction.TrainingSessionSkipped, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đánh giá buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainerEvaluation> EvaluateSession(Guid id, EvaluationRequest r)
    {
        var s = Ensure.Found(await db.Sessions.FindAsync(id)); await access.Trainer(s.HorseId);
        Ensure.That(s.Status is SessionStatus.Completed or SessionStatus.IssueReported, Messages.Get(MessageKey.OnlyResultsCanBeEvaluated), 409, "invalid_state");
        Ensure.That(!await db.Evaluations.AnyAsync(x => x.SessionId == id), Messages.Get(MessageKey.EvaluationAlreadyExists), 409, "duplicate_evaluation");
        var result = await db.Results.SingleOrDefaultAsync(x => x.SessionId == id);
        Ensure.That(result is not null, Messages.Get(MessageKey.OnlyResultsCanBeEvaluated), 409, "invalid_state");
        var e = new TrainerEvaluation { SessionId = id, TrainerId = (await current.Get()).Id, Comment = r.Comment, AdjustFutureSessions = r.AdjustFutureSessions };
        db.Evaluations.Add(e);
        await events.TrainingHistory(Ensure.Found(await db.Plans.FindAsync(s.PlanId)), s, result, e);
        await events.Audit(AuditAction.TrainingEvaluated, id); await db.SaveChangesAsync(); return e;
    }

}
