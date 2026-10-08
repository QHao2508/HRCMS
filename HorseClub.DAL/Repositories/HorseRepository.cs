using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Queries;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class HorseRepository(ClubDbContext db) : IHorseRepository
{
    public ValueTask<Horse?> FindAsync(Guid id) => db.Horses.FindAsync(id);
    public async Task<DataPage<Horse>> ListAsync(HorseScope scope, string? search, HealthStatus? healthStatus, int page, int size)
    {
        var query = ScopedHorses.For(db, scope);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.RegistrationNumber != null && x.RegistrationNumber.Contains(search));
        if (healthStatus.HasValue) query = query.Where(x => x.HealthStatus == healthStatus);
        return new(await query.AsNoTracking().OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    }
    public Task<Attachment?> GetPhotoAsync(Guid registrationId) => db.Attachments.AsNoTracking().Where(x => x.RegistrationId == registrationId && x.Type == AttachmentType.HorsePhoto).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync();
    public Task<Measurement?> GetLatestMeasurementAsync(Guid horseId) => db.Measurements.Where(x => x.HorseId == horseId).OrderByDescending(x => x.Date).FirstOrDefaultAsync();
    public Task<List<StaffAssignment>> GetAssignmentsAsync(Guid horseId) => db.Assignments.Where(x => x.HorseId == horseId).OrderByDescending(x => x.CreatedAt).ToListAsync();
    public Task<StallOccupancy?> GetOccupancyAsync(Guid horseId) => db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).FirstOrDefaultAsync();
    public Task<HorseRegistration> GetRegistrationAsync(Guid id) => db.Registrations.AsNoTracking().SingleAsync(x => x.Id == id);
    public async Task<DataPage<Measurement>> ListMeasurementsAsync(Guid horseId, int page, int size)
    {
        var query = db.Measurements.Where(x => x.HorseId == horseId);
        return new(await query.AsNoTracking().OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    }
    public void AddMeasurement(Measurement measurement) => db.Measurements.Add(measurement);
    public Task<bool> HasActiveSessionAsync(Guid horseId) => db.Sessions.AnyAsync(x => x.HorseId == horseId && x.Status == SessionStatus.InProgress);
    public Task<List<StallOccupancy>> GetOccupanciesAsync(Guid horseId) => db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).ToListAsync();
}
