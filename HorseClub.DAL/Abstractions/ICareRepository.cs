namespace HorseClub.DAL.Abstractions;
public interface ICareRepository
{
    void AddCareTask(CareTask entity);
    ValueTask<CareTask?> FindCareTaskAsync(Guid id);
    void AddIncident(Incident entity);
    ValueTask<Incident?> FindIncidentAsync(Guid id);
    void AddStable(Stable entity);
    ValueTask<Stable?> FindStableAsync(Guid id);
    void AddStall(Stall entity);
    ValueTask<Stall?> FindStallAsync(Guid id);
    void AddStallOccupancy(StallOccupancy entity);
    ValueTask<StallOccupancy?> FindStallOccupancyAsync(Guid id);
    Task<bool> HasGroomAsync(Guid horseId, Guid groomId);
    Task<bool> HasTreatmentAsync(Guid? treatmentId, Guid horseId);
    Task<bool> IsOccupiedAsync(Guid id);
    Task<List<StallOccupancy>> GetHorseOccupanciesAsync(Guid horseId);
    Task<StallOccupancy?> GetStallOccupancyAsync(Guid id);
    Task<DataPage<Stable>> ListStablesAsync(int page, int size);
    Task<DataPage<CareTask>> ListTasksAsync(HorseScope scope, Guid? horseId, Guid? groomId, CareStatus? status, DateTimeOffset? from, DateTimeOffset? to, int page, int size);
    Task<DataPage<Incident>> ListIncidentsAsync(HorseScope scope, Guid? horseId, Guid? reporterId, Role? routedRole, int page, int size);
    Task<DataPage<Stall>> ListStallsAsync(Guid? stableId, int page, int size);
    Task<Dictionary<Guid, Guid?>> ListStallOccupanciesAsync(IEnumerable<Guid> stallIds, HorseScope scope);
}
