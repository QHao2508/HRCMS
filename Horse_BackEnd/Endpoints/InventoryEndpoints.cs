using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Horse_BackEnd.Endpoints;

public static class InventoryEndpoints
{
    public static void MapInventory(this RouteGroupBuilder api)
    {
        var inventory = api.MapGroup("/inventory").WithTags("Inventory").RequireAuthorization();
        inventory.MapGet("", async (CurrentUser current, ClubDbContext db, bool? lowStock, int? page, int? pageSize, PageReader pager) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); var q = db.Inventory.Where(x => !x.Archived);
            // SQLite decimal comparisons are translated by EF provider; do not aggregate decimals in SQL.
            if (lowStock == true) q = q.Where(x => x.Stock <= x.MinimumStock);
            return await pager.Page(q.OrderBy(x => x.Name), page, pageSize);
        });
        inventory.MapPost("", async (InventoryRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var item = new InventoryItem { Name = r.Name, Category = r.Category, Unit = r.Unit, MinimumStock = r.MinimumStock };
            db.Inventory.Add(item); await events.Audit(AuditAction.InventoryCreated, item.Id); await db.SaveChangesAsync(); return item;
        });
        inventory.MapGet("/{id:guid}/movements", async (Guid id, CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        { Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); return await pager.Page(db.StockMovements.Where(x => x.ItemId == id).OrderByDescending(x => x.CreatedAt), page, pageSize); });
        inventory.MapPost("/{id:guid}/movements", async (Guid id, MovementRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom);
            Ensure.That(r.Quantity != 0 && Math.Abs(r.Quantity) <= 1000000, "Movement quantity must be nonzero and within limits.");
            if (r.Quantity > 0) Ensure.Role(u, Role.ClubManager);
            var item = Ensure.Found(await db.Inventory.FindAsync(id)); Ensure.That(!item.Archived, "Item is archived.");
            Ensure.That(item.Stock + r.Quantity >= 0, "Insufficient stock.", 409, "insufficient_stock");
            var oldStock = item.Stock; item.Stock += r.Quantity;
            var movement = new StockMovement { ItemId = id, ActorId = u.Id, Quantity = r.Quantity, Reason = r.Reason }; db.StockMovements.Add(movement);
            if (oldStock > item.MinimumStock && item.Stock <= item.MinimumStock) await events.Managers(NotificationType.InventoryLowStock, "An inventory item reached its minimum stock.", id);
            await events.Audit(AuditAction.InventoryStockMoved, id); await db.SaveChangesAsync(); return new { item, movement };
        });
        inventory.MapPost("/{id:guid}/archive", async (Guid id, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var item = Ensure.Found(await db.Inventory.FindAsync(id));
            Ensure.That(item.Stock == 0, "Consume/adjust remaining stock before archiving."); item.Archived = true;
            await events.Audit(AuditAction.InventoryArchived, id); await db.SaveChangesAsync(); return Results.NoContent();
        });
        inventory.MapGet("/replenishments", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, PageReader pager) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom);
            var q = db.Replenishments.AsQueryable(); if (u.Role == Role.Groom) q = q.Where(x => x.RequestedBy == u.Id);
            return await pager.Page(q.OrderByDescending(x => x.CreatedAt), page, pageSize);
        });
        inventory.MapPost("/replenishments", async (ReplenishmentRequestDto r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            var u = await current.Get(); Ensure.Role(u, Role.Groom, Role.ClubManager); var item = Ensure.Found(await db.Inventory.FindAsync(r.ItemId)); Ensure.That(!item.Archived, "Item is archived.");
            var request = new ReplenishmentRequest { ItemId = r.ItemId, RequestedBy = u.Id, Quantity = r.Quantity, Notes = r.Notes };
            db.Replenishments.Add(request); await events.Managers(NotificationType.InventoryReplenishment, "A replenishment request needs review.", request.Id);
            await events.Audit(AuditAction.InventoryReplenishmentRequested, request.Id); await db.SaveChangesAsync(); return request;
        });
        inventory.MapPost("/replenishments/{id:guid}/review", async (Guid id, ReplenishmentReviewRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager); var request = Ensure.Found(await db.Replenishments.FindAsync(id));
            Ensure.That(request.Status == ReplenishmentStatus.Pending, "Request is already reviewed.", 409, "invalid_state");
            request.Status = r.Approve ? ReplenishmentStatus.Approved : ReplenishmentStatus.Rejected; request.Notes = r.Notes;
            // Approval authorizes procurement; stock only increases after an actual receipt/movement.
            events.Notify(request.RequestedBy, NotificationType.InventoryReplenishmentReviewed, "Your replenishment request was reviewed.", id);
            await events.Audit(AuditAction.InventoryReplenishmentReviewed, id); await db.SaveChangesAsync(); return request;
        });
    }
}
