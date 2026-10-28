using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Training;

public sealed class TrainingSessionService(ITrainingRepository repository, IUnitOfWork unitOfWork, CurrentUser current, ClubAccess access, ClubEvents events, TimeProvider clock, IOptions<BusinessOptions> options, ClubCalendar calendar, PageReader pager, ITrainingSchedulingService scheduling, ITrainingTemplateService templates) : ITrainingSessionService
{
    /// <summary>
    /// Kiểm tình trạng ngựa và hạn chế y tế tại thời điểm buổi tập; chặn bài tập, cường độ hoặc quãng đường không phù hợp.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="distance">Giá trị kiểu decimal dùng trong Guard.</param>
    /// <param name="intensity">Giá trị kiểu Intensity dùng trong Guard.</param>
    /// <param name="type">Giá trị kiểu TrainingType dùng trong Guard.</param>
    /// <param name="at">Thời điểm nghiệp vụ được kiểm, tính theo UTC rồi quy đổi múi giờ khi cần.</param>
    public async Task Guard(Guid horseId, decimal distance, Intensity intensity, TrainingType type, DateTimeOffset at, DateTimeOffset? until = null)
    {
        var horse = Ensure.Found(await repository.FindHorseAsync(horseId));
        Ensure.That(!horse.Archived, Messages.Text(MessageKey.HorseIsArchived), 409, "horse_archived");
        if (horse.HealthStatus == HealthStatus.Isolated || intensity == Intensity.Heavy && (horse.HealthStatus == HealthStatus.Injured
            || await repository.HasUnrecoveredInjuryAsync(horseId, calendar.DateAt(at))))
            throw new ApiException(409, "medical_block", Messages.Text(MessageKey.CurrentHealthStatusPreventsThisTraining), horseId);
        var restrictions = await repository.GetRestrictionsAsync(horseId, at, until);
        foreach (var r in restrictions)
            if (r.BlockAllTraining || r.TrainingLock && intensity == Intensity.Heavy || r.MaxIntensity.HasValue && intensity > r.MaxIntensity
                || r.MaxDistanceMetres.HasValue && distance > r.MaxDistanceMetres || r.NoSprint && type == TrainingType.Sprint)
                throw new ApiException(409, "medical_restriction", Messages.Text(MessageKey.TrainingBlocked, r.Reason), r.MedicalRecordId);
    }
    /// <summary>
    /// Tạo mới buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="planId">ID kế hoạch chứa buổi tập hoặc dữ liệu huấn luyện được thao tác.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingSession> CreateSession(Guid planId, SessionRequest r)
    {
        var plan = Ensure.Found(await repository.FindPlanAsync(planId));
        await access.Trainer(plan.HorseId);
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Text(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var session = new TrainingSession { PlanId = planId, HorseId = plan.HorseId };
        await ApplySession(session, plan, r);
        repository.AddSession(session);
        await events.TrainingHistory(plan, session);
        if (r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, MessageKey.ATrainingSessionWasAssignedToYou, session.Id);
        await events.Audit(AuditAction.TrainingSessionCreated, session.Id); await events.RefreshHorse(session.HorseId); await unitOfWork.SaveChangesAsync(); return session;
    }
    /// <summary>
    /// Chỉnh sửa buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainingSession> EditSession(Guid id, SessionRequest r)
    {
        var session = Ensure.Found(await repository.FindSessionAsync(id)); await access.Trainer(session.HorseId);
        Ensure.That(session.Status is SessionStatus.Planned or SessionStatus.Assigned, Messages.Text(MessageKey.OnlyUnstartedSessionsCanBeEdited), 409, "invalid_state");
        var plan = Ensure.Found(await repository.FindPlanAsync(session.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Text(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var oldRider = session.RiderId;
        await ApplySession(session, plan, r);
        await events.TrainingHistory(plan, session);
        if (r.RiderId != oldRider && r.RiderId.HasValue) events.Notify(r.RiderId.Value, NotificationType.SessionAssigned, MessageKey.ATrainingSessionWasAssignedToYou, id);
        if (oldRider.HasValue && oldRider != r.RiderId) events.Notify(oldRider.Value, NotificationType.SessionUnassigned, MessageKey.ATrainingAssignmentWasRemoved, id);
        await events.Audit(AuditAction.TrainingSessionEdited, id); await events.RefreshHorse(session.HorseId); await unitOfWork.SaveChangesAsync(); return session;
    }
    /// <summary>
    /// Kiểm thời gian trong kế hoạch/múi giờ, sức khỏe và lịch người cưỡi; áp dụng dữ liệu và chọn trạng thái Planned hoặc Assigned.
    /// </summary>
    /// <param name="s">Giá trị kiểu TrainingSession dùng trong ApplySession.</param>
    /// <param name="p">Giá trị kiểu TrainingPlan dùng trong ApplySession.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    private async Task ApplySession(TrainingSession s, TrainingPlan p, SessionRequest r)
    {
        var template = Ensure.Found(await repository.FindTemplateAsync(p.TemplateId));
        await templates.ValidateTrainer(template.Id, (await current.Get()).Id); TemplateCurriculumPolicy.ValidateSession(template, r);
        var date = calendar.DateAt(r.ScheduledAt);
        Ensure.That(r.ScheduledAt != default && date >= p.StartDate && date <= p.EndDate, Messages.Text(MessageKey.SessionMustFallWithinPlanDatesInTheClub));
        Ensure.That(r.ScheduledAt >= clock.GetUtcNow().AddMinutes(-options.Value.ScheduleGraceMinutes), Messages.Text(MessageKey.ScheduleSessionsInTheFuture));
        Ensure.That(!r.DurationMinutes.HasValue || r.DurationMinutes is >= 1 and <= 480, Messages.Text(MessageKey.InvalidSessionDuration));
        var end = r.DurationMinutes.HasValue ? (DateTimeOffset?)r.ScheduledAt.AddMinutes(r.DurationMinutes.Value) : null;
        Ensure.That(!end.HasValue || calendar.DateAt(end.Value.AddTicks(-1)) <= p.EndDate, Messages.Text(MessageKey.InvalidSessionDuration));
        await Guard(s.HorseId, r.DistanceMetres, r.Intensity, r.TrainingType, r.ScheduledAt, end);
        await scheduling.ValidateSession(s.HorseId, r.RiderId, r.TrainingType, r.ScheduledAt, end, s.Id, p.TemplateId);
        Ensure.That(!await repository.HasScheduleConflictAsync(s.Id,s.HorseId,r.RiderId,r.ScheduledAt,end), Messages.Text(MessageKey.SessionScheduleOverlap),409,"schedule_overlap");
        if (r.RiderId.HasValue)
        {
            await access.Staff(r.RiderId.Value, Role.WorkRider);
            Ensure.That(!await repository.HasRiderConflictAsync(s.Id, r.RiderId, r.ScheduledAt), Messages.Text(MessageKey.RiderAlreadyHasASessionAtThisTime), 409, "rider_conflict");
        }
        s.DurationMinutes = r.DurationMinutes; s.ScheduledEndAt = end?.ToUniversalTime();
        s.ScheduledAt = r.ScheduledAt.ToUniversalTime(); s.TrainingType = r.TrainingType; s.DistanceMetres = r.DistanceMetres;
        s.Intensity = r.Intensity; s.Surface = r.Surface; s.Target = r.Target; s.Notes = r.Notes; s.RiderId = r.RiderId;
        s.TemplateExerciseKey = r.TemplateExerciseKey; s.UseCustomExercise = r.UseCustomExercise;
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
        var s = Ensure.Found(await repository.FindSessionAsync(id));
        Ensure.That(s.RiderId == u.Id, Messages.Text(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        Ensure.That(s.Status == SessionStatus.Assigned, Messages.Text(MessageKey.SessionIsNotAssignedReady), 409, "invalid_state");
        await access.Horse(s.HorseId);
        Ensure.That(s.ScheduledAt <= clock.GetUtcNow().AddMinutes(options.Value.StartEarlyMinutes), Messages.Text(MessageKey.SessionIsNotDueToStartYet), 409, "invalid_state");
        var plan = Ensure.Found(await repository.FindPlanAsync(s.PlanId));
        Ensure.That(plan.Status == PlanStatus.Active, Messages.Text(MessageKey.PlanIsNotActive), 409, "invalid_state");
        var today = calendar.Today(clock);
        Ensure.That(today >= plan.StartDate && today <= plan.EndDate, Messages.Text(MessageKey.PlanIsOutsideItsActiveDates), 409, "invalid_state");
        Ensure.That(!await repository.HasActiveSessionAsync(u.Id, s.HorseId), Messages.Text(MessageKey.RiderOrHorseAlreadyHasAnActiveSession), 409, "session_conflict");
        var startedAt = clock.GetUtcNow();
        var end = s.DurationMinutes.HasValue ? (DateTimeOffset?)startedAt.AddMinutes(s.DurationMinutes.Value) : null;
        await Guard(s.HorseId, s.DistanceMetres, s.Intensity, s.TrainingType, startedAt, end);
        await scheduling.ValidateSession(s.HorseId, s.RiderId, s.TrainingType, startedAt, end, s.Id, plan.TemplateId, startingNow: true);
        s.Status = SessionStatus.InProgress; s.StartedAt = startedAt;
        await events.TrainingHistory(plan, s);
        await events.Audit(AuditAction.TrainingSessionStarted, id); await events.RefreshHorse(s.HorseId); await unitOfWork.SaveChangesAsync();
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
        var s = Ensure.Found(await repository.FindSessionAsync(id));
        Ensure.That(s.RiderId == u.Id, Messages.Text(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        Ensure.That(s.Status == SessionStatus.InProgress, Messages.Text(MessageKey.StartSessionBeforeSubmittingResults), 409, "invalid_state");
        await access.Horse(s.HorseId);
        Ensure.That(!await repository.HasResultAsync(id), Messages.Text(MessageKey.ResultAlreadyExists), 409, "duplicate_result");
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
        repository.AddResult(result); s.Status = issue ? SessionStatus.IssueReported : SessionStatus.Completed;
        await events.HorseStaff(s.HorseId, NotificationType.SessionResult, MessageKey.ARiderSubmittedASessionResult, Role.Trainer);
        if (issue)
        {
            repository.AddIncident(new Incident { HorseId = s.HorseId, ReporterId = u.Id, SessionId = id, OccurredAt = clock.GetUtcNow(), Type = IncidentType.TrainingObservation, Description = r.Feedback, Severity = IncidentSeverity.NeedsReview, RoutedTo = Role.Veterinarian });
            await events.HorseStaff(s.HorseId, NotificationType.IncidentReported, MessageKey.ATrainingObservationRequiresReview, Role.Veterinarian, Role.Trainer);
        }
        var plan = Ensure.Found(await repository.FindPlanAsync(s.PlanId));
        await events.TrainingHistory(plan, s, result);
        await events.Audit(AuditAction.TrainingResultSubmitted, id); await events.RefreshHorse(s.HorseId); await unitOfWork.SaveChangesAsync(); return result;
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
    public async Task<PageResponse<TrainingSessionListItem>> ListSessions(Guid? horseId, SessionStatus? status, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize, string? search = null)
    {
        Ensure.That(search is null || search.Length <= 100, Messages.Text(MessageKey.InvalidRequestBody));
        var u = await current.Get(); var scope = await access.Scope();
        if (horseId.HasValue && u.Role != Role.WorkRider) await access.Horse(horseId.Value, allowArchived: true);
        var (p, size) = pager.Read(page, pageSize);
        var data = await repository.ListSessionsAsync(scope, horseId, u.Role == Role.WorkRider ? u.Id : null, status, from, to, p, size, search?.Trim());
        var names=data.Items.Count==0 ? new Dictionary<Guid, HorseClub.DAL.Abstractions.TrainingNames>() : (await repository.SessionNamesAsync(data.Items.Select(x=>x.Id).ToArray())).ToDictionary(x=>x.Id);
        return new(data.Items.Select(x=>TrainingSessionListItem.From(x,names[x.Id])).ToList(),p,size,data.Total);
    }

    /// <summary>
    /// Đọc chi tiết buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<SessionDetailResponse> GetSession(Guid id)
    {
        var s = Ensure.Found(await repository.FindSessionAsync(id)); var u = await current.Get();
        Ensure.That(u.Role != Role.WorkRider || s.RiderId == u.Id, Messages.Text(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        if (u.Role != Role.WorkRider) await access.TrainingHistoryHorse(s.HorseId, Ensure.Found(await repository.FindPlanAsync(s.PlanId)).TrainerId);
        var names=(await repository.SessionNamesAsync([id])).Single();
        return new SessionDetailResponse(s, await repository.GetResultAsync(id), await repository.GetEvaluationAsync(id),names.HorseName,names.TrainerName,names.RiderName, Ensure.Found(await repository.FindHorseAsync(s.HorseId)).Archived, await repository.FindTemplateAsync(Ensure.Found(await repository.FindPlanAsync(s.PlanId)).TemplateId));
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
        var s = Ensure.Found(await repository.FindSessionAsync(id));
        return await EditSession(id, new SessionRequest(s.ScheduledAt, s.TrainingType, s.DistanceMetres, s.Intensity, s.Surface, s.Target, s.Notes, r.RiderId, s.DurationMinutes, s.TemplateExerciseKey, s.UseCustomExercise));
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
        var s = Ensure.Found(await repository.FindSessionAsync(id)); var u = await current.Get();
        if (u.Role == Role.WorkRider) Ensure.That(s.RiderId == u.Id, Messages.Text(MessageKey.SessionIsNotAssignedToYou), 403, "forbidden");
        else await access.Trainer(s.HorseId);
        Ensure.That(s.Status is SessionStatus.Planned or SessionStatus.Assigned or SessionStatus.InProgress, Messages.Text(MessageKey.SessionIsAlreadyFinal), 409, "invalid_state");
        if (u.Role == Role.WorkRider) await access.Horse(s.HorseId);
        s.Status = SessionStatus.Skipped; s.Notes += Messages.Get(MessageKey.Skipped, r.Reason);
        await events.TrainingHistory(Ensure.Found(await repository.FindPlanAsync(s.PlanId)), s);
        await events.HorseStaff(s.HorseId, NotificationType.SessionSkipped, MessageKey.ASessionWasSkipped, Role.Trainer);
        await events.Audit(AuditAction.TrainingSessionSkipped, id); await events.RefreshHorse(s.HorseId); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đánh giá buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TrainerEvaluation> EvaluateSession(Guid id, EvaluationRequest r)
    {
        var s = Ensure.Found(await repository.FindSessionAsync(id)); await access.Trainer(s.HorseId);
        Ensure.That(s.Status is SessionStatus.Completed or SessionStatus.IssueReported, Messages.Text(MessageKey.OnlyResultsCanBeEvaluated), 409, "invalid_state");
        Ensure.That(!await repository.HasEvaluationAsync(id), Messages.Text(MessageKey.EvaluationAlreadyExists), 409, "duplicate_evaluation");
        var result = await repository.GetResultAsync(id);
        Ensure.That(result is not null, Messages.Text(MessageKey.OnlyResultsCanBeEvaluated), 409, "invalid_state");
        var e = new TrainerEvaluation { SessionId = id, TrainerId = (await current.Get()).Id, Comment = r.Comment, AdjustFutureSessions = r.AdjustFutureSessions };
        repository.AddEvaluation(e);
        await events.TrainingHistory(Ensure.Found(await repository.FindPlanAsync(s.PlanId)), s, result, e);
        await events.Audit(AuditAction.TrainingEvaluated, id); await events.RefreshHorse(s.HorseId); await unitOfWork.SaveChangesAsync(); return e;
    }

}
