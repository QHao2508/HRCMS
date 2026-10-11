using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using HorseClub.DAL.Queries;
using Microsoft.EntityFrameworkCore;
namespace HorseClub.DAL.Repositories;
public sealed class CareRepository(ClubDbContext db) : ICareRepository
{
    private static async Task<DataPage<T>> Page<T>(IQueryable<T> query, int page, int size) where T : class
        => new(await query.AsNoTracking().Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    public void AddCareTask(CareTask entity) => db.CareTasks.Add(entity);
    public ValueTask<CareTask?> FindCareTaskAsync(Guid id) => db.CareTasks.FindAsync(id);
    public void AddIncident(Incident entity) => db.Incidents.Add(entity);
    public ValueTask<Incident?> FindIncidentAsync(Guid id) => db.Incidents.FindAsync(id);
    public void AddStable(Stable entity) => db.Stables.Add(entity);
    public ValueTask<Stable?> FindStableAsync(Guid id) => db.Stables.FindAsync(id);
    public void AddStall(Stall entity) => db.Stalls.Add(entity);
    public ValueTask<Stall?> FindStallAsync(Guid id) => db.Stalls.FindAsync(id);
    public void AddStallOccupancy(StallOccupancy entity) => db.Occupancies.Add(entity);
    public ValueTask<StallOccupancy?> FindStallOccupancyAsync(Guid id) => db.Occupancies.FindAsync(id);
    public Task<bool> HasGroomAsync(Guid horseId, Guid groomId) => db.Assignments.AnyAsync(x => x.HorseId == horseId && x.StaffId == groomId && x.Active && x.Role == Role.Groom);
    public Task<bool> HasTreatmentAsync(Guid? treatmentId, Guid horseId) => db.Treatments.AnyAsync(x => x.Id == treatmentId && x.HorseId == horseId && !x.Completed);
    public Task<bool> IsOccupiedAsync(Guid id) => db.Occupancies.AnyAsync(x => x.StallId == id && x.EndedAt == null);
    public Task<List<StallOccupancy>> GetHorseOccupanciesAsync(Guid horseId) => db.Occupancies.Where(x => x.HorseId == horseId && x.EndedAt == null).ToListAsync();
    public Task<StallOccupancy?> GetStallOccupancyAsync(Guid id) => db.Occupancies.SingleOrDefaultAsync(x => x.StallId == id && x.EndedAt == null);
    public Task<DataPage<Stable>> ListStablesAsync(int page, int size) => Page(db.Stables.OrderBy(x => x.Name).ThenBy(x => x.Id), page, size);
    public async Task<DataPage<CareTask>> ListTasksAsync(HorseScope scope, Guid? horseId, Guid? groomId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int size)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        var query = db.CareTasks.Where(x => horses.Contains(x.HorseId));
        if (horseId.HasValue) query = query.Where(x => x.HorseId == horseId);
        if (groomId.HasValue) query = query.Where(x => x.GroomId == groomId);
        if (status.HasValue) query = query.Where(x => x.Status == status);
        if (from.HasValue) query = query.Where(x => x.ScheduledAt >= from);
        if (to.HasValue) query = query.Where(x => x.ScheduledAt <= to);
        return await Page(query.OrderBy(x => x.ScheduledAt), page, size);
    }
    public async Task<DataPage<Incident>> ListIncidentsAsync(HorseScope scope, Guid? horseId, Guid? reporterId, Role? routedRole, int page, int size)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        var query = db.Incidents.Where(x => horses.Contains(x.HorseId));
        if (reporterId.HasValue) query = routedRole.HasValue ? query.Where(x => x.ReporterId == reporterId || x.RoutedTo == routedRole) : query.Where(x => x.ReporterId == reporterId);
        if (horseId.HasValue) query = query.Where(x => x.HorseId == horseId);
        return await Page(query.OrderByDescending(x => x.OccurredAt), page, size);
    }
    public async Task<DataPage<Stall>> ListStallsAsync(Guid? stableId, int page, int size)
    {
        var query = db.Stalls.AsQueryable();
        if (stableId.HasValue) query = query.Where(x => x.StableId == stableId);
        return await Page(query.OrderBy(x => x.Name), page, size);
    }
    public Task<Dictionary<Guid, Guid?>> ListStallOccupanciesAsync(IEnumerable<Guid> stallIds, HorseScope scope)
    {
        var horses = ScopedHorses.For(db, scope).Select(x => x.Id);
        return db.Occupancies.AsNoTracking().Where(x => stallIds.Contains(x.StallId) && x.EndedAt == null)
            .Select(x => new { x.StallId, HorseId = horses.Contains(x.HorseId) ? (Guid?)x.HorseId : null })
            .ToDictionaryAsync(x => x.StallId, x => x.HorseId);
    }
}
