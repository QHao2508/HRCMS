using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using System.Text.Json;

namespace HorseClub.BLL.Medical;

public sealed partial class MedicalService(ClubAccess access, IMedicalRepository repository, IUnitOfWork unitOfWork, TimeProvider clock, PageReader pager, CurrentUser current, ClubEvents events, ClubCalendar calendar) : IMedicalService
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
    /// <summary>
    /// Tóm tắt y tế theo ca bệnh cho Manager, HeadTrainer, Chủ ngựa và Vet; ghi chú lâm sàng chỉ trả cho Vet/Manager.
    /// </summary>
    /// <param name="horseId">ID ngựa trong phạm vi người xem.</param>
    public async Task<MedicalOverviewResponse> GetOverview(Guid horseId)
    {
        var u = await current.Get(); Ensure.Role(u, RolePolicy.MedicalOverviewReaders);
        var horse = await access.Horse(horseId, allowArchived: true);
        var clinical = RolePolicy.SeesClinicalNotes(u.Role);
        var records = await repository.GetRecordsAsync(horseId);
        var followUps = await repository.GetFollowUpsAsync(horseId);
        var superseded = records.Where(x => x.SupersedesRecordId.HasValue).Select(x => x.SupersedesRecordId!.Value).ToHashSet();
        var seen = new HashSet<Guid>(); var cases = new List<MedicalCaseSummaryResponse>();
        foreach (var record in records.Where(x => !superseded.Contains(x.Id)).OrderByDescending(x => x.ExaminationAt))
        {
            if (seen.Contains(record.Id)) continue;
            var ids = CaseIds(record.Id, records, followUps); seen.UnionWith(ids);
            var first = records.Where(x => ids.Contains(x.Id)).OrderBy(x => x.ExaminationAt).First();
            var visits = followUps.Where(x => ids.Contains(x.PreviousRecordId) || ids.Contains(x.CurrentRecordId)).ToList();
            cases.Add(new MedicalCaseSummaryResponse(record.Id, first.Reason, record.Diagnosis, record.HealthStatus, first.ExaminationAt, record.ExaminationAt,
                visits.Any(x => x.Clearance), visits.Count, clinical ? record.Symptoms : null, clinical ? record.Findings : null, clinical ? record.Notes : null));
        }
        var treatments = (await repository.ListTreatmentsAsync(horseId, 1, 200)).Items.Select(x => new TreatmentSummaryResponse(x.Id, x.MedicalRecordId, x.Objective, x.Instructions, x.Medication, x.Frequency, x.StartDate, x.EndDate, x.FollowUpDate, x.Completed)).ToList();
        var restrictions = (await repository.GetEffectiveRestrictionsAsync(horseId, clock.GetUtcNow())).Select(x => new RestrictionSummaryResponse(x.Id, x.MedicalRecordId, x.TrainingLock, x.BlockAllTraining, x.MaxIntensity, x.MaxDistanceMetres, x.NoSprint, x.ValidFrom, x.ValidUntil, x.Reason)).ToList();
        return new MedicalOverviewResponse(horse.HealthStatus, cases, treatments, restrictions, clinical);
    }

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
        record.InitialSnapshotJson = SnapshotJson(record);
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
        Ensure.That(r.ExaminationAt.ToUniversalTime() == previous.ExaminationAt, Messages.Text(MessageKey.CorrectionMustKeepAssessmentDate));
        Ensure.That(!await repository.HasCorrectionAsync(id), Messages.Text(MessageKey.ACorrectionAlreadyExistsEditTheLatestRevision), 409, "invalid_state");
        var correction = Record(horseId, (await current.Get()).Id, r); correction.SupersedesRecordId = previous.Id;
        correction.CaseRootId = previous.CaseRootId;
        correction.InitialSnapshotJson = SnapshotJson(correction);
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
        Ensure.That(r.InjuryDate != default && r.InjuryDate <= calendar.Today(clock) && r.ReviewDate >= r.InjuryDate, Messages.Text(MessageKey.InvalidInjuryReviewDates));
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
        Ensure.That(r.ValidFrom != default && (!r.ValidUntil.HasValue || r.ValidUntil >= r.ValidFrom), Messages.Text(MessageKey.InvalidRestrictionDates));
        Ensure.That(r.TrainingLock || r.BlockAllTraining || r.MaxIntensity.HasValue || r.MaxDistanceMetres.HasValue || r.NoSprint, Messages.Text(MessageKey.SetAtLeastOneRestriction));
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
        Ensure.That(r.StartDate != default && (!r.EndDate.HasValue || r.EndDate >= r.StartDate) && r.FollowUpDate >= r.StartDate, Messages.Text(MessageKey.InvalidTreatmentDates));
        if (r.InjuryId.HasValue) Ensure.That(await repository.HasInjuryAsync(r.InjuryId, horseId, r.MedicalRecordId), Messages.Text(MessageKey.InjuryDoesNotBelongToThisMedicalRecord));
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
            Frequency = System.Text.RegularExpressions.Regex.IsMatch(r.Frequency.Trim(), @"^\d+\s*(/\s*ngày)?$")
                ? r.Frequency.Trim().Split('/')[0].Trim() + " lần/ngày" : r.Frequency.Trim()
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
        // Tái khám CẬP NHẬT hồ sơ (ca bệnh) đang mở thay vì tạo hồ sơ mới: một vấn đề = một hồ sơ.
        // Lần khám trước được lưu snapshot trong audit; MedicalFollowUp giữ lịch sử từng lần tái khám.
        await access.Vet(horseId); await MedicalRecordFor(repository, horseId, r.PreviousRecordId);
        var record = Ensure.Found(await repository.GetRecordAsync(horseId, r.PreviousRecordId));
        Ensure.That(!await repository.HasCorrectionAsync(record.Id), Messages.Text(MessageKey.MedicalCaseMustUseLatestVersion), 409, "invalid_state");
        Ensure.Validate(r.Examination); ValidateExamination(r.Examination, clock);
        var examinationAt = r.Examination.ExaminationAt.ToUniversalTime();
        Ensure.That(examinationAt >= record.ExaminationAt, Messages.Text(MessageKey.InvalidDates));
        var followUps = await repository.GetFollowUpsAsync(horseId);
        var caseIds = CaseIds(record.Id, await repository.GetRecordsAsync(horseId), followUps);
        var caseRecords = await repository.GetRecordsAsync(horseId);
        var head = CaseHead(caseIds, caseRecords);
        Ensure.That(head.Id == record.Id, Messages.Text(MessageKey.MedicalCaseMustUseLatestVersion), 409, "invalid_state");
        Ensure.That(!followUps.Any(x => x.Clearance && (caseIds.Contains(x.PreviousRecordId) || caseIds.Contains(x.CurrentRecordId))),
            Messages.Text(MessageKey.MedicalCaseAlreadyResolved), 409, "invalid_state");
        var restrictionIds = r.RestrictionIds ?? [];
        var injuryIds = r.InjuryIds ?? [];
        var treatmentIds = r.TreatmentIds ?? [];
        var selectedCount = restrictionIds.Length + injuryIds.Length + treatmentIds.Length;
        Ensure.That(r.Clearance || selectedCount == 0, Messages.Text(MessageKey.ClearanceRequiresExplicitSelection));
        Ensure.That(restrictionIds.Distinct().Count() == restrictionIds.Length
            && injuryIds.Distinct().Count() == injuryIds.Length && treatmentIds.Distinct().Count() == treatmentIds.Length,
            Messages.Text(MessageKey.InvalidClearanceSelection));
        var examDate = calendar.DateAt(examinationAt);
        var activeRestrictions = (await repository.GetActiveRestrictionsAsync(horseId)).Where(x => x.ValidFrom <= examinationAt).ToList();
        var activeInjuries = (await repository.GetActiveInjuriesAsync(horseId)).Where(x => x.InjuryDate <= examDate).ToList();
        var activeTreatments = (await repository.GetActiveTreatmentsAsync(horseId)).Where(x => x.StartDate <= examDate).ToList();
        var restrictions = activeRestrictions.Where(x => restrictionIds.Contains(x.Id)).ToList();
        var injuries = activeInjuries.Where(x => injuryIds.Contains(x.Id)).ToList();
        var treatments = activeTreatments.Where(x => treatmentIds.Contains(x.Id)).ToList();
        Ensure.That(restrictions.Count == restrictionIds.Length && injuries.Count == injuryIds.Length && treatments.Count == treatmentIds.Length,
            Messages.Text(MessageKey.InvalidClearanceSelection));
        var previousSnapshot = SnapshotJson(record);
        if (string.IsNullOrEmpty(record.InitialSnapshotJson))
            record.InitialSnapshotJson = await RecoverInitialSnapshot(record, followUps);
        record.ExaminationAt = examinationAt;
        record.VeterinarianId = (await current.Get()).Id;
        record.Symptoms = r.Examination.Symptoms; record.Findings = r.Examination.Findings; record.Diagnosis = r.Examination.Diagnosis;
        record.HealthStatus = r.Examination.HealthStatus; record.Notes = r.Examination.Notes ?? "";
        var latestOther = await repository.GetLatestAssessmentAsync(horseId, record.Id);
        if (latestOther is null || record.ExaminationAt >= latestOther.ExaminationAt)
            (await access.Horse(horseId)).HealthStatus = record.HealthStatus;
        var follow = new MedicalFollowUp { HorseId = horseId, PreviousRecordId = record.Id, CurrentRecordId = record.Id, Clearance = r.Clearance, Outcome = r.Outcome,
            PreviousSnapshotJson = previousSnapshot, CurrentSnapshotJson = SnapshotJson(record) };
        repository.AddMedicalFollowUp(follow);
        // Hồ sơ liên quan (hạn chế, chấn thương, phác đồ gắn với ca này) tự cập nhật trạng thái.
        var linkedRestrictions = activeRestrictions.Where(x => caseIds.Contains(x.MedicalRecordId)).ToList();
        var linkedInjuries = activeInjuries.Where(x => caseIds.Contains(x.MedicalRecordId)).ToList();
        var linkedTreatments = activeTreatments.Where(x => caseIds.Contains(x.MedicalRecordId)).ToList();
        if (r.Clearance)
        {
            restrictions = restrictions.Union(linkedRestrictions).ToList();
            injuries = injuries.Union(linkedInjuries).ToList();
            treatments = treatments.Union(linkedTreatments).ToList();
            foreach (var restriction in restrictions) restriction.Cleared = true;
            foreach (var injury in injuries) injury.Status = InjuryStatus.Recovered;
            foreach (var treatment in treatments) treatment.Completed = true;
        }
        else if (record.HealthStatus is HealthStatus.Fit or HealthStatus.Monitoring)
        {
            injuries = linkedInjuries.Where(x => x.Status == InjuryStatus.Active).ToList();
            foreach (var injury in injuries) injury.Status = InjuryStatus.Recovering;
        }
        await events.HorseStaff(horseId, NotificationType.MedicalFollowUp, r.Clearance ? MessageKey.MedicalClearanceIssuedReviewTheTrainingPlanBeforeResuming : MessageKey.MedicalFollowUpCompletedReviewCurrentRestrictions, Role.Trainer, Role.HeadTrainer);
        await events.Audit(r.Clearance ? AuditAction.MedicalClearanceIssued : AuditAction.MedicalFollowUp, follow.Id,
            JsonSerializer.Serialize(new { RecordId = record.Id, SnapshotStored = true, RestrictionIds = restrictions.Select(x => x.Id), InjuryIds = injuries.Select(x => x.Id), TreatmentIds = treatments.Select(x => x.Id) }));
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return follow;
    }

    /// <summary>
    /// Bác sĩ thú y cập nhật trạng thái phác đồ (ví dụ sau khi groom hoàn thành công việc điều trị được giao).
    /// </summary>
    /// <param name="horseId">ID ngựa.</param>
    /// <param name="id">ID phác đồ.</param>
    /// <param name="r">Trạng thái mới.</param>
    public async Task<TreatmentPlan> UpdateTreatmentStatus(Guid horseId, Guid id, TreatmentStatusRequest r)
    {
        await access.Vet(horseId); var plan = Ensure.Found(await repository.GetTreatmentAsync(horseId, id));
        if (plan.Completed == r.Completed) return plan;
        plan.Completed = r.Completed;
        await events.Audit(AuditAction.MedicalTreatmentCreated, plan.Id, JsonSerializer.Serialize(new { r.Completed }));
        await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return plan;
    }

    /// <summary>
    /// Gom các hồ sơ cùng một ca bệnh: bản sửa (SupersedesRecordId) và chuỗi tái khám cũ (Previous/Current) được xem là một hồ sơ.
    /// </summary>
    private static HashSet<Guid> CaseIds(Guid start, List<MedicalRecord> records, List<MedicalFollowUp> followUps)
    {
        var edges = new Dictionary<Guid, List<Guid>>();
        void Add(Guid a, Guid b) { if (!edges.TryGetValue(a, out var list)) edges[a] = list = new List<Guid>(); list.Add(b); }
        foreach (var x in records.Where(x => x.SupersedesRecordId.HasValue)) { Add(x.Id, x.SupersedesRecordId!.Value); Add(x.SupersedesRecordId!.Value, x.Id); }
        foreach (var x in records.Where(x => x.CaseRootId.HasValue)) { Add(x.Id, x.CaseRootId!.Value); Add(x.CaseRootId!.Value, x.Id); }
        foreach (var x in followUps.Where(x => x.PreviousRecordId != x.CurrentRecordId)) { Add(x.PreviousRecordId, x.CurrentRecordId); Add(x.CurrentRecordId, x.PreviousRecordId); }
        var seen = new HashSet<Guid> { start }; var queue = new Queue<Guid>(); queue.Enqueue(start);
        while (queue.Count > 0)
            foreach (var next in edges.GetValueOrDefault(queue.Dequeue()) ?? new List<Guid>())
                if (seen.Add(next)) queue.Enqueue(next);
        return seen;
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
        await access.Vet(horseId); Ensure.That(Enum.IsDefined(r.Type), Messages.Text(MessageKey.UnknownPreventiveCareType)); Ensure.That(r.DueDate != default, Messages.Text(MessageKey.DueDateIsRequired));
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
        Ensure.That(!p.CompletedDate.HasValue && r.CompletedDate != default && r.CompletedDate <= calendar.Today(clock), Messages.Text(MessageKey.InvalidCompletion));
        p.CompletedDate = r.CompletedDate; p.Notes = r.Notes; await events.Audit(AuditAction.MedicalPreventiveCompleted, id); await events.RefreshHorse(horseId); await unitOfWork.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Xác nhận bản ghi y tế thuộc đúng ngựa trước khi tạo liên kết chấn thương/hạn chế/điều trị.
    /// </summary>
    /// <param name="repository">Repository kiểm quan hệ hồ sơ khám và ngựa.</param>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public static async Task MedicalRecordFor(IMedicalRepository repository, Guid horseId, Guid id)
        => Ensure.That(await repository.HasRecordAsync(horseId, id), Messages.Text(MessageKey.MedicalRecordDoesNotBelongToThisHorse));
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
        => Ensure.That(r.ExaminationAt != default && r.ExaminationAt <= clock.GetUtcNow(), Messages.Text(MessageKey.ExaminationDateCannotBeInTheFuture));
    /// <summary>
    /// Ghi dữ liệu khám/theo dõi vào MedicalService sau khi kiểm quyền và quan hệ ngựa/hồ sơ y tế.
    /// </summary>
    /// <param name="horseId">ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ.</param>
    /// <param name="vetId">Giá trị kiểu Guid dùng trong Record.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    public static MedicalRecord Record(Guid horseId, Guid vetId, MedicalRequest r)
        => new() { HorseId = horseId, VeterinarianId = vetId, ExaminationAt = r.ExaminationAt.ToUniversalTime(), Reason = r.Reason, Symptoms = r.Symptoms, Findings = r.Findings, Diagnosis = r.Diagnosis, HealthStatus = r.HealthStatus, Notes = r.Notes };

}
