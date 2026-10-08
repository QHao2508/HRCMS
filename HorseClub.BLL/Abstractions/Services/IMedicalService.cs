using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by MedicalService.</summary>
public interface IMedicalService
{
    Task<MedicalSummaryResponse> GetSummary(Guid horseId);
    Task<PageResponse<MedicalRecord>> ListExaminations(Guid horseId, int? page, int? pageSize);
    Task<OperationResult> CreateExamination(Guid horseId, MedicalRequest r);
    Task<PageResponse<Injury>> ListInjuries(Guid horseId, int? page, int? pageSize);
    Task<MedicalRecord> CorrectExamination(Guid horseId, Guid id, MedicalRequest r);
    Task<Injury> RecordInjury(Guid horseId, InjuryRequest r);
    Task<PageResponse<MedicalRestriction>> ListRestrictions(Guid horseId, int? page, int? pageSize);
    Task<MedicalRestriction> CreateRestriction(Guid horseId, RestrictionRequest r);
    Task<PageResponse<TreatmentPlan>> ListTreatments(Guid horseId, int? page, int? pageSize);
    Task<TreatmentPlan> CreateTreatment(Guid horseId, TreatmentRequest r);
    Task<PageResponse<MedicalFollowUp>> ListFollowUps(Guid horseId, int? page, int? pageSize);
    Task<MedicalFollowUp> RecordFollowUp(Guid horseId, FollowUpRequest r);
    Task<PageResponse<PreventiveCareSummaryResponse>> ListPreventiveCare(Guid horseId, int? page, int? pageSize);
    Task<PreventiveCare> SchedulePreventiveCare(Guid horseId, PreventiveRequest r);
    Task<OperationResult> CompletePreventiveCare(Guid horseId, Guid id, PreventiveCompletionRequest r);
}
