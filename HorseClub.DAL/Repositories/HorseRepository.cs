using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Queries;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class HorseRepository(ClubDbContext db) : IHorseRepository
{
    public ValueTask<Horse?> FindAsync(Guid id) => db.Horses.FindAsync(id);
    // Tìm ngựa và kiểm tra phân công đang hoạt động trong cùng một câu SQL (entity vẫn được track như FindAsync).
    public async Task<(Horse? Horse, bool Assigned)> FindWithAssignmentAsync(Guid id, Guid staffId)
    {
        var row = await db.Horses.Where(h => h.Id == id).Select(h => new { Horse = h, Assigned = db.Assignments.Any(a => a.HorseId == id && a.StaffId == staffId && a.Active) }).SingleOrDefaultAsync();
        return (row?.Horse, row?.Assigned ?? false);
    }
    public ValueTask<Stall?> FindStallAsync(Guid id) => db.Stalls.FindAsync(id);
    public async Task<DataPage<Horse>> ListAsync(HorseScope scope, string? search, HealthStatus? healthStatus, int page, int size, DateTimeOffset? now = null)
    {
        var query = ScopedHorses.For(db, scope);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.RegistrationNumber != null && x.RegistrationNumber.Contains(search)
                || db.Registrations.Any(r => r.Id == x.RegistrationId && r.RegistrationNumber != null && r.RegistrationNumber.Contains(search)));
        }
        if (healthStatus.HasValue) query = query.Where(x => x.HealthStatus == healthStatus);
        var result = await query.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).ToPageAsync(page, size);
        var registrationIds = result.Items.Select(x => x.RegistrationId).ToArray();
        var numbers = await db.Registrations.AsNoTracking().Where(r => registrationIds.Contains(r.Id))
            .Select(r => new { r.Id, r.RegistrationNumber }).ToDictionaryAsync(r => r.Id, r => r.RegistrationNumber);
        foreach (var horse in result.Items) horse.ExternalRegistrationNumber = numbers.GetValueOrDefault(horse.RegistrationId);
        await EnrichListAsync(result.Items, now ?? DateTimeOffset.UtcNow);
        return result;
    }
    // Cột phụ cho danh sách ngựa (chuồng hiện tại, HLV phụ trách, buổi tập kế tiếp, có ảnh) trong MỘT câu SQL cho cả trang.
    private async Task EnrichListAsync(IReadOnlyCollection<Horse> horses, DateTimeOffset now)
    {
        if (horses.Count == 0) return;
        var ids = horses.Select(x => x.Id).ToArray();
        var rows = await db.Horses.AsNoTracking().Where(h => ids.Contains(h.Id)).Select(h => new
        {
            h.Id,
            Stall = (from o in db.Occupancies
                     join s in db.Stalls on o.StallId equals s.Id
                     where o.HorseId == h.Id && o.EndedAt == null
                     orderby o.CreatedAt descending
                     select s.Name).FirstOrDefault(),
            // Ưu tiên huấn luyện viên đang phụ trách; chưa có thì dùng huấn luyện viên trưởng.
            Trainer = (from a in db.Assignments
                       join u in db.Users on a.StaffId equals u.Id
                       where a.HorseId == h.Id && a.Active && (a.Role == Role.Trainer || a.Role == Role.HeadTrainer)
                       orderby a.Role == Role.Trainer ? 0 : 1, a.CreatedAt descending
                       select u.FirstName + " " + u.LastName).FirstOrDefault(),
            Next = db.Sessions.Where(x => x.HorseId == h.Id && x.ScheduledAt >= now && (x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned))
                .Min(x => (DateTimeOffset?)x.ScheduledAt),
            HasPhoto = db.Attachments.Any(x => x.RegistrationId == h.RegistrationId && x.Type == AttachmentType.HorsePhoto),
        }).ToDictionaryAsync(x => x.Id);
        foreach (var horse in horses)
        {
            if (!rows.TryGetValue(horse.Id, out var row)) continue;
            horse.StallName = row.Stall;
            horse.LeadTrainerName = string.IsNullOrWhiteSpace(row.Trainer) ? null : row.Trainer.Trim();
            horse.NextSessionAt = row.Next;
            horse.HasPhoto = row.HasPhoto;
        }
    }
    public Task<Attachment?> GetPhotoAsync(Guid registrationId) => db.Attachments.AsNoTracking().Where(x => x.RegistrationId == registrationId && x.Type == AttachmentType.HorsePhoto).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync();
    public Task<Measurement?> GetLatestMeasurementAsync(Guid horseId) => db.Measurements.Where(x => x.HorseId == horseId).OrderByDescending(x => x.Date).FirstOrDefaultAsync();
    public Task<List<StaffAssignment>> GetAssignmentsAsync(Guid horseId) => db.Assignments.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ToListAsync();
    public Task<StallOccupancy?> GetOccupancyAsync(Guid horseId) => db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).FirstOrDefaultAsync();
    public Task<HorseRegistration> GetRegistrationAsync(Guid id) => db.Registrations.AsNoTracking().SingleAsync(x => x.Id == id);
    // Một câu SQL (OUTER APPLY) thay cho 5 lượt gọi riêng: mỗi lượt tới Azure SQL tốn ~50 ms.
    public async Task<HorseDetailData> GetDetailAsync(Guid horseId, Guid registrationId)
    {
        var row = await db.Registrations.AsNoTracking().Where(r => r.Id == registrationId).Select(r => new
        {
            Registration = r,
            Photo = db.Attachments.Where(x => x.RegistrationId == registrationId && x.Type == AttachmentType.HorsePhoto).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefault(),
            Latest = db.Measurements.Where(x => x.HorseId == horseId).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault(),
            Occupancy = db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).OrderByDescending(x => x.CreatedAt).FirstOrDefault(),
            Assignments = db.Assignments.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ToList(),
            // Tên nhân sự trong lịch sử phân công, để mọi vai trò (kể cả Chủ ngựa) không cần gọi danh bạ.
            Staff = db.Users.Where(u => db.Assignments.Any(a => a.HorseId == horseId && a.StaffId == u.Id)).Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName }).ToList(),
        }).SingleAsync();
        return new HorseDetailData(row.Registration, row.Photo, row.Latest, row.Assignments, row.Occupancy, row.Staff.ToDictionary(x => x.Id, x => x.Name.Trim()));
    }
    public async Task<DataPage<Measurement>> ListMeasurementsAsync(Guid horseId, int page, int size)
    {
        var query = db.Measurements.Where(x => x.HorseId == horseId);
        return await query.AsNoTracking().OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToPageAsync(page, size);
    }
    public void AddMeasurement(Measurement measurement) => db.Measurements.Add(measurement);
    public Task<bool> HasActiveSessionAsync(Guid horseId) => db.Sessions.AnyAsync(x => x.HorseId == horseId && x.Status == SessionStatus.InProgress);
    public Task<List<StallOccupancy>> GetOccupanciesAsync(Guid horseId) => db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).ToListAsync();
    public Task<bool> HasActiveCareAsync(Guid horseId) => db.CareTasks.AnyAsync(x => x.HorseId == horseId && x.Status == CareStatus.InProgress);
    public Task<List<TrainingPlan>> GetOpenPlansAsync(Guid horseId) => db.Plans.Where(x => x.HorseId == horseId && (x.Status == PlanStatus.Active || x.Status == PlanStatus.Paused)).ToListAsync();
    public Task<List<TrainingSession>> GetPendingSessionsAsync(Guid horseId) => db.Sessions.Where(x => x.HorseId == horseId && (x.Status == SessionStatus.Planned || x.Status == SessionStatus.Assigned)).ToListAsync();
    public Task<List<CareTask>> GetPendingCareAsync(Guid horseId) => db.CareTasks.Where(x => x.HorseId == horseId && x.Status == CareStatus.Pending).ToListAsync();
    public Task<List<StaffAssignment>> GetActiveAssignmentsAsync(Guid horseId) => db.Assignments.Where(x => x.HorseId == horseId && x.Active).ToListAsync();
}
