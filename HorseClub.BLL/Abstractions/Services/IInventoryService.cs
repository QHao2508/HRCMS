using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Abstractions.Services;

/// <summary>Application operations implemented by InventoryService.</summary>
public interface IInventoryService
{
    Task<PageResponse<InventoryItem>> ListItems(bool? lowStock, int? page, int? pageSize);
    Task<InventoryItem> CreateItem(InventoryRequest r);
    Task<PageResponse<StockMovement>> ListMovements(Guid id, int? page, int? pageSize);
    Task<StockMovementResponse> RecordMovement(Guid id, MovementRequest r);
    Task<OperationResult> ArchiveItem(Guid id);
    Task<PageResponse<ReplenishmentRequest>> ListReplenishments(int? page, int? pageSize);
    Task<ReplenishmentRequest> RequestReplenishment(ReplenishmentRequestDto r);
    Task<ReplenishmentRequest> ReviewReplenishment(Guid id, ReplenishmentReviewRequest r);
}
