namespace HorseClub.DAL.Abstractions;

public sealed record DataPage<T>(List<T> Items, int Total);

/// <summary>Inventory persistence only; authorization and stock policy belong to BLL.</summary>
public interface IInventoryRepository
{
    Task<DataPage<InventoryItem>> ListItemsAsync(bool lowStock, int page, int size);
    Task<DataPage<StockMovement>> ListMovementsAsync(Guid itemId, int page, int size);
    Task<DataPage<ReplenishmentRequest>> ListReplenishmentsAsync(Guid? requesterId, int page, int size);
    ValueTask<InventoryItem?> FindItemAsync(Guid id);
    ValueTask<ReplenishmentRequest?> FindReplenishmentAsync(Guid id);
    void AddItem(InventoryItem item);
    void AddMovement(StockMovement movement);
    void AddReplenishment(ReplenishmentRequest request);
}
