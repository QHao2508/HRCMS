using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using System.Text.Json;

namespace HorseClub.BLL.Medical;

public sealed class MedicalService(ClubAccess access, IMedicalRepository repository, IUnitOfWork unitOfWork, TimeProvider clock, PageReader pager, CurrentUser current, ClubEvents events, ClubCalendar calendar) : IMedicalService
{
    /// <summary>
    /// Đọc chi tiết tóm tắt y tế trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<MedicalSummaryResponse> GetSummary(Guid horseId)
    {
        var horse = await access.Horse(horseId); var now = clock.GetUtcNow();
        return new MedicalSummaryResponse(horse.HealthStatus, (await repository.GetEffectiveRestrictionsAsync(horseId, now)).Select(x => new RestrictionSummaryResponse(x.Id, x.MedicalRecordId, x.TrainingLock, x.BlockAllTraining, x.MaxIntensity, x.MaxDistanceMetres, x.NoSprint, x.ValidFrom, x.ValidUntil, x.Reason)).ToList());
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListExaminations.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListExaminations.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<MedicalRecord>> ListExaminations(Guid horseId, int? page, int? pageSize)
    { await access.MedicalDetails(horseId); return await pager.Page(page, pageSize, (p, size) => repository.ListExaminationsAsync(horseId, p, size)); }

    /// <summary>
    /// Tạo mới lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> CreateExamination(Guid horseId, MedicalRequest r)
    {
        await access.Vet(horseId); ValidateExamination(r, clock);
        var record = Record(horseId, (await current.Get()).Id, r);
        repository.AddMedicalRecord(record); await ApplyAssessment(record);
        await events.Audit(AuditAction.MedicalExaminationCreated, record.Id);
        await events.HorseStaff(horseId, NotificationType.MedicalHealthChanged, MessageKey.HorseHealthStatusChangedReviewCurrentTrainingLimits, Role.Trainer, Role.HeadTrainer);
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return OperationResult.Created($"/api/horses/{horseId}/medical/records", record);
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang chấn thương trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListInjuries.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListInjuries.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<Injury>> ListInjuries(Guid horseId, int? page, int? pageSize)
    { await access.MedicalDetails(horseId); return await pager.Page(page, pageSize, (p, size) => repository.ListInjuriesAsync(horseId, p, size)); }

    /// <summary>
    /// Tạo bản đính chính lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<MedicalRecord> CorrectExamination(Guid horseId, Guid id, MedicalRequest r)
    {
        await access.Vet(horseId); ValidateExamination(r, clock);
        var previous = Ensure.Found(await repository.GetRecordAsync(horseId, id));
        Ensure.That(r.ExaminationAt.ToUniversalTime() == previous.ExaminationAt, Messages.Get(MessageKey.CorrectionMustKeepAssessmentDate));
        Ensure.That(!await repository.HasCorrectionAsync(id), Messages.Get(MessageKey.ACorrectionAlreadyExistsEditTheLatestRevision), 409, "invalid_state");
        var correction = Record(horseId, (await current.Get()).Id, r); correction.SupersedesRecordId = previous.Id;
        repository.AddMedicalRecord(correction);
        await ApplyAssessment(correction);
        await events.Audit(AuditAction.MedicalRecordCorrected, correction.Id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return correction;
    }

    /// <summary>
    /// Ghi nhận chấn thương trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<Injury> RecordInjury(Guid horseId, InjuryRequest r)
    {
        await access.Vet(horseId); await MedicalRecordFor(repository, horseId, r.MedicalRecordId);
        Ensure.That(r.InjuryDate != default && r.InjuryDate <= calendar.Today(clock) && r.ReviewDate >= r.InjuryDate, Messages.Get(MessageKey.InvalidInjuryReviewDates));
        var injury = new Injury { HorseId = horseId, MedicalRecordId = r.MedicalRecordId, InjuryDate = r.InjuryDate, Type = r.Type, BodyLocation = r.BodyLocation, Severity = r.Severity, Cause = r.Cause, ReviewDate = r.ReviewDate };
        repository.AddInjury(injury);
        var latest = await repository.GetLatestAssessmentAsync(horseId);
        var horse = await access.Horse(horseId);
        if ((latest is null || calendar.DateAt(latest.ExaminationAt) <= r.InjuryDate) && horse.HealthStatus != HealthStatus.Isolated)
            horse.HealthStatus = HealthStatus.Injured;
        await events.Audit(AuditAction.MedicalInjuryCreated, injury.Id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return injury;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang hạn chế vận động trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListRestrictions.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListRestrictions.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<MedicalRestriction>> ListRestrictions(Guid horseId, int? page, int? pageSize)
    { await access.Horse(horseId); return await pager.Page(page, pageSize, (p, size) => repository.ListRestrictionsAsync(horseId, p, size)); }

    /// <summary>
    /// Tạo mới hạn chế vận động trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<MedicalRestriction> CreateRestriction(Guid horseId, RestrictionRequest r)
    {
        await access.Vet(horseId); await MedicalRecordFor(repository, horseId, r.MedicalRecordId);
        Ensure.That(r.ValidFrom != default && (!r.ValidUntil.HasValue || r.ValidUntil >= r.ValidFrom), Messages.Get(MessageKey.InvalidRestrictionDates));
        Ensure.That(r.TrainingLock || r.BlockAllTraining || r.MaxIntensity.HasValue || r.MaxDistanceMetres.HasValue || r.NoSprint, Messages.Get(MessageKey.SetAtLeastOneRestriction));
        var restriction = new MedicalRestriction
        {
            HorseId = horseId,
            MedicalRecordId = r.MedicalRecordId,
            TrainingLock = r.TrainingLock,
            BlockAllTraining = r.BlockAllTraining,
            MaxIntensity = r.MaxIntensity,
            MaxDistanceMetres = r.MaxDistanceMetres,
            NoSprint = r.NoSprint,
            ValidFrom = r.ValidFrom.ToUniversalTime(),
            ValidUntil = r.ValidUntil?.ToUniversalTime(),
            Reason = r.Reason
        };
        repository.AddMedicalRestriction(restriction);
        // The reason is an operational instruction visible to training roles; keep clinical diagnosis in MedicalRecord.
        await events.HorseStaff(horseId, NotificationType.MedicalRestrictionCreated, MessageKey.TrainingRestrictionsChangedReviewPlannedSessions, Role.Trainer, Role.HeadTrainer);
        var riders = await repository.GetActiveRidersAsync(horseId);
        foreach (var id in riders) events.Notify(id, NotificationType.MedicalRestrictionCreated, MessageKey.AMedicalRestrictionChangedDuringYourSessionStopIncompatible, horseId);
        await events.Audit(AuditAction.MedicalRestrictionCreated, restriction.Id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return restriction;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang phác đồ điều trị trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListTreatments.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListTreatments.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<TreatmentPlan>> ListTreatments(Guid horseId, int? page, int? pageSize)
    { await access.MedicalDetails(horseId); return await pager.Page(page, pageSize, (p, size) => repository.ListTreatmentsAsync(horseId, p, size)); }

    /// <summary>
    /// Tạo mới phác đồ điều trị trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<TreatmentPlan> CreateTreatment(Guid horseId, TreatmentRequest r)
    {
        await access.Vet(horseId); await MedicalRecordFor(repository, horseId, r.MedicalRecordId);
        Ensure.That(r.StartDate != default && r.EndDate >= r.StartDate && r.FollowUpDate >= r.StartDate, Messages.Get(MessageKey.InvalidTreatmentDates));
        if (r.InjuryId.HasValue) Ensure.That(await repository.HasInjuryAsync(r.InjuryId, horseId, r.MedicalRecordId), Messages.Get(MessageKey.InjuryDoesNotBelongToThisMedicalRecord));
        var treatment = new TreatmentPlan
        {
            HorseId = horseId,
            MedicalRecordId = r.MedicalRecordId,
            InjuryId = r.InjuryId,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            FollowUpDate = r.FollowUpDate,
            Objective = r.Objective,
            Instructions = r.Instructions,
            Medication = r.Medication,
            Frequency = r.Frequency
        };
        repository.AddTreatmentPlan(treatment); await events.Audit(AuditAction.MedicalTreatmentCreated, treatment.Id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return treatment;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang tái khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListFollowUps.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListFollowUps.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<MedicalFollowUp>> ListFollowUps(Guid horseId, int? page, int? pageSize)
    { await access.MedicalDetails(horseId); return await pager.Page(page, pageSize, (p, size) => repository.ListFollowUpsAsync(horseId, p, size)); }

    /// <summary>
    /// Ghi nhận tái khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<MedicalFollowUp> RecordFollowUp(Guid horseId, FollowUpRequest r)
    {
        await access.Vet(horseId); await MedicalRecordFor(repository, horseId, r.PreviousRecordId);
        var previous = Ensure.Found(await repository.GetRecordAsync(horseId, r.PreviousRecordId));
        Ensure.Validate(r.Examination); ValidateExamination(r.Examination, clock);
        Ensure.That(r.Examination.ExaminationAt >= previous.ExaminationAt, Messages.Get(MessageKey.InvalidDates));
        var restrictionIds = r.RestrictionIds ?? [];
        var injuryIds = r.InjuryIds ?? [];
        var treatmentIds = r.TreatmentIds ?? [];
        var selectedCount = restrictionIds.Length + injuryIds.Length + treatmentIds.Length;
        Ensure.That(r.Clearance ? selectedCount > 0 : selectedCount == 0, Messages.Get(MessageKey.ClearanceRequiresExplicitSelection));
        Ensure.That(restrictionIds.Distinct().Count() == restrictionIds.Length
            && injuryIds.Distinct().Count() == injuryIds.Length && treatmentIds.Distinct().Count() == treatmentIds.Length,
            Messages.Get(MessageKey.InvalidClearanceSelection));
        var restrictions = (await repository.GetActiveRestrictionsAsync(horseId)).Where(x => restrictionIds.Contains(x.Id)).ToList();
        var injuries = (await repository.GetActiveInjuriesAsync(horseId)).Where(x => injuryIds.Contains(x.Id)).ToList();
        var treatments = (await repository.GetActiveTreatmentsAsync(horseId)).Where(x => treatmentIds.Contains(x.Id)).ToList();
        Ensure.That(restrictions.Count == restrictionIds.Length && injuries.Count == injuryIds.Length && treatments.Count == treatmentIds.Length,
            Messages.Get(MessageKey.InvalidClearanceSelection));
        Ensure.That(restrictions.All(x => x.ValidFrom <= r.Examination.ExaminationAt)
            && injuries.All(x => x.InjuryDate <= calendar.DateAt(r.Examination.ExaminationAt))
            && treatments.All(x => x.StartDate <= calendar.DateAt(r.Examination.ExaminationAt)), Messages.Get(MessageKey.InvalidClearanceSelection));
        var record = Record(horseId, (await current.Get()).Id, r.Examination); repository.AddMedicalRecord(record);
        await ApplyAssessment(record);
        var follow = new MedicalFollowUp { HorseId = horseId, PreviousRecordId = r.PreviousRecordId, CurrentRecordId = record.Id, Clearance = r.Clearance, Outcome = r.Outcome };
        repository.AddMedicalFollowUp(follow);
        if (r.Clearance)
        {
            foreach (var restriction in restrictions) restriction.Cleared = true;
            foreach (var injury in injuries) injury.Status = InjuryStatus.Recovered;
            foreach (var treatment in treatments) treatment.Completed = true;
        }
        await events.HorseStaff(horseId, NotificationType.MedicalFollowUp, r.Clearance ? MessageKey.MedicalClearanceIssuedReviewTheTrainingPlanBeforeResuming : MessageKey.MedicalFollowUpCompletedReviewCurrentRestrictions, Role.Trainer, Role.HeadTrainer);
        await events.Audit(r.Clearance ? AuditAction.MedicalClearanceIssued : AuditAction.MedicalFollowUp, follow.Id,
            JsonSerializer.Serialize(new { r.PreviousRecordId, CurrentRecordId = record.Id, RestrictionIds = restrictionIds, InjuryIds = injuryIds, TreatmentIds = treatmentIds }));
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return follow;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListPreventiveCare.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListPreventiveCare.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.</remarks>
    public async Task<PageResponse<PreventiveCareSummaryResponse>> ListPreventiveCare(Guid horseId, int? page, int? pageSize)
    { await access.Horse(horseId); return PageReader.Map(await pager.Page(page, pageSize, (p, size) => repository.ListPreventiveCareAsync(horseId, p, size)), x => new PreventiveCareSummaryResponse(x.Id, x.HorseId, x.Type, x.DueDate, x.CompletedDate)); }

    /// <summary>
    /// Lên lịch chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<PreventiveCare> SchedulePreventiveCare(Guid horseId, PreventiveRequest r)
    {
        await access.Vet(horseId); Ensure.That(Enum.IsDefined(r.Type), Messages.Get(MessageKey.UnknownPreventiveCareType)); Ensure.That(r.DueDate != default, Messages.Get(MessageKey.DueDateIsRequired));
        var p = new PreventiveCare { HorseId = horseId, Type = r.Type, DueDate = r.DueDate, Notes = r.Notes }; repository.AddPreventiveCare(p);
        await events.Audit(AuditAction.MedicalPreventiveScheduled, p.Id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return p;
    }

    /// <summary>
    /// Hoàn tất chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi. Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> CompletePreventiveCare(Guid horseId, Guid id, PreventiveCompletionRequest r)
    {
        await access.Vet(horseId); var p = Ensure.Found(await repository.GetPreventiveAsync(horseId, id));
        Ensure.That(!p.CompletedDate.HasValue && r.CompletedDate != default && r.CompletedDate <= calendar.Today(clock), Messages.Get(MessageKey.InvalidCompletion));
        p.CompletedDate = r.CompletedDate; p.Notes = r.Notes; await events.Audit(AuditAction.MedicalPreventiveCompleted, id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Xác nhận bản ghi y tế thuộc đúng ngựa trước khi tạo liên kết chấn thương/hạn chế/điều trị.
    /// </summary>
    /// <param name="repository">Repository kiểm quan hệ hồ sơ khám và ngựa.</param>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public static async Task MedicalRecordFor(IMedicalRepository repository, Guid horseId, Guid id)
        => Ensure.That(await repository.HasRecordAsync(horseId, id), Messages.Get(MessageKey.MedicalRecordDoesNotBelongToThisHorse));
    private async Task ApplyAssessment(MedicalRecord record)
    {
        if (await repository.IsLatestAssessmentAsync(record))
            (await access.Horse(record.HorseId)).HealthStatus = record.HealthStatus;
    }
    /// <summary>
    /// Kiểm thời gian và các trường khám y tế bắt buộc trước lưu, tránh dữ liệu không hợp lệ liên kết với ngựa.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <param name="clock">Giá trị kiểu TimeProvider dùng trong ValidateExamination.</param>
    public static void ValidateExamination(MedicalRequest r, TimeProvider clock)
        => Ensure.That(r.ExaminationAt != default && r.ExaminationAt <= clock.GetUtcNow(), Messages.Get(MessageKey.ExaminationDateCannotBeInTheFuture));
    /// <summary>
    /// Ghi dữ liệu khám/theo dõi vào MedicalService sau khi kiểm quyền và quan hệ ngựa/hồ sơ y tế.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="vetId">Giá trị kiểu Guid dùng trong Record.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public static MedicalRecord Record(Guid horseId, Guid vetId, MedicalRequest r)
        => new() { HorseId = horseId, VeterinarianId = vetId, ExaminationAt = r.ExaminationAt.ToUniversalTime(), Reason = r.Reason, Symptoms = r.Symptoms, Findings = r.Findings, Diagnosis = r.Diagnosis, HealthStatus = r.HealthStatus, Notes = r.Notes };

}
