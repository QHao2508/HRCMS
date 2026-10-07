using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;

namespace Horse_BackEnd.Endpoints;

public static class InventoryEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Inventory với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapInventory.</param>
    public static void MapInventory(this RouteGroupBuilder api)
    {
        var inventory = api.MapGroup("/inventory").WithTags("Inventory").RequireAuthorization();
        inventory.MapGet("", async (bool? lowStock, int? page, int? pageSize, InventoryService moduleService) => await moduleService.ListItems(lowStock, page, pageSize)).Produces<PageResponse<InventoryItem>>(200);
        inventory.MapPost("", async (InventoryRequest r, InventoryService moduleService) => await moduleService.CreateItem(r)).Produces<InventoryItem>(200);
        inventory.MapGet("/{id:guid}/movements", async (Guid id, int? page, int? pageSize, InventoryService moduleService) => await moduleService.ListMovements(id, page, pageSize)).Produces<PageResponse<StockMovement>>(200);
        inventory.MapPost("/{id:guid}/movements", async (Guid id, MovementRequest r, InventoryService moduleService) => await moduleService.RecordMovement(id, r)).Produces<StockMovementResponse>(200);
        inventory.MapPost("/{id:guid}/archive", async (Guid id, InventoryService moduleService) => (await moduleService.ArchiveItem(id)).ToHttpResult()).Produces(204);
        inventory.MapGet("/replenishments", async (int? page, int? pageSize, InventoryService moduleService) => await moduleService.ListReplenishments(page, pageSize)).Produces<PageResponse<ReplenishmentRequest>>(200);
        inventory.MapPost("/replenishments", async (ReplenishmentRequestDto r, InventoryService moduleService) => await moduleService.RequestReplenishment(r)).Produces<ReplenishmentRequest>(200);
        inventory.MapPost("/replenishments/{id:guid}/review", async (Guid id, ReplenishmentReviewRequest r, InventoryService moduleService) => await moduleService.ReviewReplenishment(id, r)).Produces<ReplenishmentRequest>(200);
    }
}
