using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class TrainingSchedulingRepository(ClubDbContext db) : ITrainingSchedulingRepository
{
    public ValueTask<User?> FindStaffAsync(Guid id) => db.Users.FindAsync(id);
    public Task<Guid?> CurrentTrainerAsync(Guid horseId) => db.Assignments.Where(a => a.HorseId == horseId && a.Active && a.Role == Role.Trainer)
        .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Select(a => (Guid?)a.StaffId).FirstOrDefaultAsync();
    public Task<bool> IsHeadTrainerAsync(Guid horseId, Guid staffId) => db.Assignments.AnyAsync(a => a.HorseId == horseId && a.StaffId == staffId && a.Active && a.Role == Role.HeadTrainer);
    public Task<List<TrainerCandidateData>> TrainersAsync() => db.Users.AsNoTracking().Where(u => u.Role == Role.Trainer && u.Active && u.EmailVerified)
        .Select(u => new TrainerCandidateData(u, db.Assignments.Count(a => a.Active && a.Role == Role.Trainer && a.StaffId == u.Id && db.Horses.Any(h => h.Id == a.HorseId && !h.Archived)))).ToListAsync();
    public Task<List<TrainerExpertise>> ExpertiseAsync(Guid[] staffIds) => db.TrainerExpertise.AsNoTracking().Where(x => staffIds.Contains(x.StaffId)).ToListAsync();
    public Task<List<TrainingType>> RequiredTypesAsync(Guid staffId) => db.Assignments.Where(x => x.Active && x.Role == Role.Trainer && x.StaffId == staffId && x.RequiredTrainingType != null)
        .Select(x => x.RequiredTrainingType!.Value).Distinct().ToListAsync();
    public Task<List<StaffWorkingHours>> HoursAsync(Guid staffId) => db.StaffWorkingHours.AsNoTracking().Where(x => x.StaffId == staffId).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartMinute).ToListAsync();
    public Task<List<StaffTimeOff>> TimeOffAsync(Guid staffId, DateTimeOffset from, DateTimeOffset to) => db.StaffTimeOff.AsNoTracking().Where(x => x.StaffId == staffId && x.Start < to && x.End > from).OrderBy(x => x.Start).ToListAsync();

    private IQueryable<TrainingSession> OpenSessions() => db.Sessions.AsNoTracking().Where(s => s.Status == SessionStatus.InProgress
        || db.Plans.Any(p => p.Id == s.PlanId && p.Status == PlanStatus.Active) && (s.Status == SessionStatus.Planned || s.Status == SessionStatus.Assigned));

    public Task<List<ScheduledResource>> ReservationsAsync(Guid horseId, Guid? trainerId, Guid? riderId, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, Guid? exceptSessionId)
        => (from s in OpenSessions()
            let trainer = db.Assignments.Where(a => a.HorseId == s.HorseId && a.Active && a.Role == Role.Trainer)
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Select(a => (Guid?)a.StaffId).FirstOrDefault()
            where s.Id != exceptSessionId && (s.HorseId == horseId || trainerId != null && trainer == trainerId || riderId != null && s.RiderId == riderId)
                && (s.Status == SessionStatus.InProgress || s.ScheduledAt < rangeEnd && (s.ScheduledEndAt == null ? s.ScheduledAt >= rangeStart : s.ScheduledEndAt > rangeStart))
            select new ScheduledResource(s.Id, s.HorseId, trainer, s.RiderId, s.ScheduledAt, s.ScheduledEndAt, s.Status == SessionStatus.InProgress, s.TrainingType)).ToListAsync();
    public Task<List<ScheduledResource>> PendingHorseSessionsAsync(Guid horseId) => OpenSessions().Where(s => s.HorseId == horseId)
        .Select(s => new ScheduledResource(s.Id, s.HorseId, null, s.RiderId, s.ScheduledAt, s.ScheduledEndAt, s.Status == SessionStatus.InProgress, s.TrainingType)).ToListAsync();

    public async Task ReplaceProfileAsync(Guid staffId, List<TrainerExpertise> expertise, List<StaffWorkingHours> hours, List<StaffTimeOff> timeOff)
    {
        db.TrainerExpertise.RemoveRange(await db.TrainerExpertise.Where(x => x.StaffId == staffId).ToListAsync());
        db.StaffWorkingHours.RemoveRange(await db.StaffWorkingHours.Where(x => x.StaffId == staffId).ToListAsync());
        db.StaffTimeOff.RemoveRange(await db.StaffTimeOff.Where(x => x.StaffId == staffId).ToListAsync());
        // Delete first to release unique keys; the request transaction rolls this back if anything fails.
        await db.SaveChangesAsync();
        db.TrainerExpertise.AddRange(expertise); db.StaffWorkingHours.AddRange(hours); db.StaffTimeOff.AddRange(timeOff);
    }
}
