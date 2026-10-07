using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventory(this RouteGroupBuilder api)
    {
        var inventory = api.MapGroup("/inventory").WithTags("Inventory").RequireAuthorization();
        inventory.MapGet("", async (CurrentUser current, ClubDbContext db, bool? lowStock, int? page, int? pageSize, PageReader pager) => await InventoryWorkflow.GetList(current, db, lowStock, page, pageSize, pager)).Produces<PageResponse<InventoryItem>>(200);
        inventory.MapPost("", async (InventoryRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await InventoryWorkflow.PostList(r, current, db, events)).Produces<InventoryItem>(200);
        inventory.MapGet("/{id:guid}/movements", async (Guid id, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await InventoryWorkflow.GetByIdMovements(id, current, db, page, pageSize, pager)).Produces<PageResponse<StockMovement>>(200);
        inventory.MapPost("/{id:guid}/movements", async (Guid id, MovementRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await InventoryWorkflow.PostByIdMovements(id, r, current, db, events)).Produces<StockMovementResponse>(200);
        inventory.MapPost("/{id:guid}/archive", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events) => await InventoryWorkflow.PostByIdArchive(id, current, db, events)).Produces(204);
        inventory.MapGet("/replenishments", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) => await InventoryWorkflow.GetReplenishments(current, db, page, pageSize, pager)).Produces<PageResponse<ReplenishmentRequest>>(200);
        inventory.MapPost("/replenishments", async (ReplenishmentRequestDto r, CurrentUser current, ClubDbContext db, ClubEvents events) => await InventoryWorkflow.PostReplenishments(r, current, db, events)).Produces<ReplenishmentRequest>(200);
        inventory.MapPost("/replenishments/{id:guid}/review", async (Guid id, ReplenishmentReviewRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await InventoryWorkflow.PostReplenishmentsByIdReview(id, r, current, db, events)).Produces<ReplenishmentRequest>(200);
    }
}
