using System.ComponentModel.DataAnnotations;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Contracts;

public sealed record MergeMedicalCaseRequest([property: Required] Guid[] RecordIds, [property: Required, MaxLength(2000)] string Reason);
public sealed record MedicalVisitSnapshot(DateTimeOffset ExaminationAt, string Reason, string Symptoms, string Findings,
    string Diagnosis, HealthStatus HealthStatus, string Notes, Guid VeterinarianId);
public sealed record MedicalCaseVisitResponse(Guid Id, Guid RecordId, string Kind, MedicalVisitSnapshot Examination,
    string Outcome, bool Clearance, DateTimeOffset RecordedAt);
public sealed record MedicalCaseHistoryResponse(Guid Id, Guid[] RecordIds, MedicalCaseVisitResponse[] Visits);
