using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

/// <summary>
/// Tóm tắt y tế cho Manager, HeadTrainer, Chủ ngựa (và Vet): tình trạng, các ca bệnh, phác đồ, hạn chế đang hiệu lực.
/// Symptoms/Findings/Notes chỉ có giá trị với Vet và Manager; vai trò khác nhận null (không lộ ghi chú nội bộ).
/// </summary>
public sealed record MedicalOverviewResponse(
    HealthStatus HealthStatus,
    List<MedicalCaseSummaryResponse> Cases,
    List<TreatmentSummaryResponse> Treatments,
    List<RestrictionSummaryResponse> Restrictions,
    bool IncludesClinicalNotes);

public sealed record MedicalCaseSummaryResponse(
    Guid RecordId, string Reason, string Diagnosis, HealthStatus HealthStatus, DateTimeOffset OpenedAt, DateTimeOffset LastExaminationAt,
    bool Resolved, int FollowUps, string? Symptoms, string? Findings, string? Notes);

public sealed record TreatmentSummaryResponse(
    Guid Id, Guid MedicalRecordId, string Objective, string Instructions, string Medication, string Frequency,
    DateOnly StartDate, DateOnly? EndDate, DateOnly FollowUpDate, bool Completed);
