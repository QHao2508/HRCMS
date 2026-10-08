using HorseClub.DAL.Abstractions;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.DAL.Repositories;

public sealed class InventoryRepository(ClubDbContext db) : IInventoryRepository
{
    public Task<DataPage<InventoryItem>> ListItemsAsync(bool lowStock, int page, int size)
    {
        var query = db.Inventory.Where(x => !x.Archived);
        if (lowStock) query = query.Where(x => x.Stock <= x.MinimumStock);
        return Page(query.OrderBy(x => x.Name).ThenBy(x => x.Id), page, size);
    }
    public Task<DataPage<StockMovement>> ListMovementsAsync(Guid itemId, int page, int size)
        => Page(db.StockMovements.Where(x => x.ItemId == itemId).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    public Task<DataPage<ReplenishmentRequest>> ListReplenishmentsAsync(Guid? requesterId, int page, int size)
    {
        var query = db.Replenishments.AsQueryable();
        if (requesterId.HasValue) query = query.Where(x => x.RequestedBy == requesterId.Value);
        return Page(query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, size);
    }
    private static async Task<DataPage<T>> Page<T>(IQueryable<T> query, int page, int size) where T : class
        => new(await query.AsNoTracking().Skip((page - 1) * size).Take(size).ToListAsync(), await query.CountAsync());
    public ValueTask<InventoryItem?> FindItemAsync(Guid id) => db.Inventory.FindAsync(id);
    public ValueTask<ReplenishmentRequest?> FindReplenishmentAsync(Guid id) => db.Replenishments.FindAsync(id);
    public void AddItem(InventoryItem item) => db.Inventory.Add(item);
    public void AddMovement(StockMovement movement) => db.StockMovements.Add(movement);
    public void AddReplenishment(ReplenishmentRequest request) => db.Replenishments.Add(request);
}
