namespace HorseClub.DAL.Abstractions;

public interface IMedicalRepository
{
    Task<DataPage<MedicalRecord>> ListExaminationsAsync(Guid horseId, int page, int size);
    Task<DataPage<Injury>> ListInjuriesAsync(Guid horseId, int page, int size);
    Task<DataPage<MedicalRestriction>> ListRestrictionsAsync(Guid horseId, int page, int size);
    Task<DataPage<TreatmentPlan>> ListTreatmentsAsync(Guid horseId, int page, int size);
    Task<DataPage<MedicalFollowUp>> ListFollowUpsAsync(Guid horseId, int page, int size);
    Task<DataPage<PreventiveCare>> ListPreventiveCareAsync(Guid horseId, int page, int size);
    void AddMedicalRecord(MedicalRecord entity);
    void AddInjury(Injury entity);
    void AddMedicalRestriction(MedicalRestriction entity);
    void AddTreatmentPlan(TreatmentPlan entity);
    void AddMedicalFollowUp(MedicalFollowUp entity);
    void AddPreventiveCare(PreventiveCare entity);
    Task<MedicalRecord?> GetRecordAsync(Guid horseId, Guid id);
    Task<bool> HasCorrectionAsync(Guid id);
    Task<bool> HasNewerRecordAsync(Guid horseId, DateTimeOffset examinationAt);
    Task<List<Guid>> GetActiveRidersAsync(Guid horseId);
    Task<bool> HasInjuryAsync(Guid? injuryId, Guid horseId, Guid medicalRecordId);
    Task<List<MedicalRestriction>> GetActiveRestrictionsAsync(Guid horseId);
    Task<List<Injury>> GetActiveInjuriesAsync(Guid horseId);
    Task<List<TreatmentPlan>> GetActiveTreatmentsAsync(Guid horseId);
    Task<PreventiveCare?> GetPreventiveAsync(Guid horseId, Guid id);
    Task<bool> HasRecordAsync(Guid horseId, Guid id);
    Task<List<MedicalRestriction>> GetEffectiveRestrictionsAsync(Guid horseId, DateTimeOffset now);
}
