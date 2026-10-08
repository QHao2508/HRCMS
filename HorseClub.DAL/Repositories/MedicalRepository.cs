using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class MedicalRepository(ClubDbContext db) : IMedicalRepository
{
    private static async Task<DataPage<T>> Page<T>(IQueryable<T> query, int page, int size) where T : class
        => new(await query.AsNoTracking().Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    public Task<DataPage<MedicalRecord>> ListExaminationsAsync(Guid horseId, int page, int size) => Page(db.MedicalRecords.Where(x => x.HorseId == horseId).OrderByDescending(x => x.ExaminationAt).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<Injury>> ListInjuriesAsync(Guid horseId, int page, int size) => Page(db.Injuries.Where(x => x.HorseId == horseId).OrderByDescending(x => x.InjuryDate).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<MedicalRestriction>> ListRestrictionsAsync(Guid horseId, int page, int size) => Page(db.Restrictions.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<TreatmentPlan>> ListTreatmentsAsync(Guid horseId, int page, int size) => Page(db.Treatments.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<MedicalFollowUp>> ListFollowUpsAsync(Guid horseId, int page, int size) => Page(db.FollowUps.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<PreventiveCare>> ListPreventiveCareAsync(Guid horseId, int page, int size) => Page(db.PreventiveCare.Where(x => x.HorseId == horseId).OrderBy(x => x.DueDate), page, size);
    public void AddMedicalRecord(MedicalRecord entity) => db.MedicalRecords.Add(entity);
    public void AddInjury(Injury entity) => db.Injuries.Add(entity);
    public void AddMedicalRestriction(MedicalRestriction entity) => db.Restrictions.Add(entity);
    public void AddTreatmentPlan(TreatmentPlan entity) => db.Treatments.Add(entity);
    public void AddMedicalFollowUp(MedicalFollowUp entity) => db.FollowUps.Add(entity);
    public void AddPreventiveCare(PreventiveCare entity) => db.PreventiveCare.Add(entity);
    public Task<MedicalRecord?> GetRecordAsync(Guid horseId, Guid id) => db.MedicalRecords.SingleOrDefaultAsync(x => x.Id == id && x.HorseId == horseId);
    public Task<bool> HasCorrectionAsync(Guid id) => db.MedicalRecords.AnyAsync(x => x.SupersedesRecordId == id);
    public Task<bool> HasNewerRecordAsync(Guid horseId, DateTimeOffset examinationAt) => db.MedicalRecords.AnyAsync(x => x.HorseId == horseId && x.ExaminationAt > examinationAt);
    public Task<List<Guid>> GetActiveRidersAsync(Guid horseId) => db.Sessions.Where(x => x.HorseId == horseId && x.Status == SessionStatus.InProgress && x.RiderId != null).Select(x => x.RiderId!.Value).Distinct().ToListAsync();
    public Task<bool> HasInjuryAsync(Guid? injuryId, Guid horseId, Guid medicalRecordId) => db.Injuries.AnyAsync(x => x.Id == injuryId && x.HorseId == horseId && x.MedicalRecordId == medicalRecordId);
    public Task<List<MedicalRestriction>> GetActiveRestrictionsAsync(Guid horseId) => db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared).ToListAsync();
    public Task<List<Injury>> GetActiveInjuriesAsync(Guid horseId) => db.Injuries.Where(x => x.HorseId == horseId && x.Status == InjuryStatus.Active).ToListAsync();
    public Task<List<TreatmentPlan>> GetActiveTreatmentsAsync(Guid horseId) => db.Treatments.Where(x => x.HorseId == horseId && !x.Completed).ToListAsync();
    public Task<PreventiveCare?> GetPreventiveAsync(Guid horseId, Guid id) => db.PreventiveCare.SingleOrDefaultAsync(x => x.Id == id && x.HorseId == horseId);
    public Task<bool> HasRecordAsync(Guid horseId, Guid id) => db.MedicalRecords.AnyAsync(x => x.Id == id && x.HorseId == horseId);
    public Task<List<MedicalRestriction>> GetEffectiveRestrictionsAsync(Guid horseId, DateTimeOffset now) => db.Restrictions.Where(x => x.HorseId == horseId && !x.Cleared && x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil >= now)).ToListAsync();
}
