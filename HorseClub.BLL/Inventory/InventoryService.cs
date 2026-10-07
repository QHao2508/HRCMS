using HorseClub.BLL.Messaging;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;

namespace HorseClub.BLL.Inventory;

public sealed class InventoryService(CurrentUser current, ClubDbContext db, PageReader pager, ClubEvents events)
{
    /// <summary>
    /// Đọc danh sách có lọc/phân trang vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="lowStock">Giá trị kiểu bool? dùng trong ListItems.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListItems.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListItems.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<InventoryItem>> ListItems(bool? lowStock, int? page, int? pageSize)
    {
        Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); var q = db.Inventory.Where(x => !x.Archived);
        if (lowStock == true) q = q.Where(x => x.Stock <= x.MinimumStock);
        return await pager.Page(q.OrderBy(x => x.Name).ThenBy(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Tạo mới vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<InventoryItem> CreateItem(InventoryRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var item = new InventoryItem { Name = r.Name, Category = r.Category, Unit = r.Unit, MinimumStock = r.MinimumStock };
        db.Inventory.Add(item); await events.Audit(AuditAction.InventoryCreated, item.Id); await db.SaveChangesAsync(); return item;
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang biến động kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="page">Giá trị kiểu int? dùng trong ListMovements.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListMovements.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<StockMovement>> ListMovements(Guid id, int? page, int? pageSize)
    { Ensure.Role(await current.Get(), Role.ClubManager, Role.Groom); return await pager.Page(db.StockMovements.Where(x => x.ItemId == id).OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize); }

    /// <summary>
    /// Ghi nhận biến động kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<StockMovementResponse> RecordMovement(Guid id, MovementRequest r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom);
        Ensure.That(r.Quantity != 0 && Math.Abs(r.Quantity) <= 1000000, Messages.Get(MessageKey.MovementQuantityMustBeNonzeroAndWithinLimits));
        if (r.Quantity > 0) Ensure.Role(u, Role.ClubManager);
        var item = Ensure.Found(await db.Inventory.FindAsync(id)); Ensure.That(!item.Archived, Messages.Get(MessageKey.ItemIsArchived));
        Ensure.That(item.Stock + r.Quantity >= 0, Messages.Get(MessageKey.InsufficientStock), 409, "insufficient_stock");
        var oldStock = item.Stock; item.Stock += r.Quantity;
        var movement = new StockMovement { ItemId = id, ActorId = u.Id, Quantity = r.Quantity, Reason = r.Reason }; db.StockMovements.Add(movement);
        if (oldStock > item.MinimumStock && item.Stock <= item.MinimumStock) await events.Managers(NotificationType.InventoryLowStock, MessageKey.AnInventoryItemReachedItsMinimumStock, id);
        await events.Audit(AuditAction.InventoryStockMoved, id); await db.SaveChangesAsync(); return new StockMovementResponse(item, movement);
    }

    /// <summary>
    /// Lưu trữ vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<OperationResult> ArchiveItem(Guid id)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var item = Ensure.Found(await db.Inventory.FindAsync(id));
        Ensure.That(item.Stock == 0, Messages.Get(MessageKey.ConsumeAdjustRemainingStockBeforeArchiving)); item.Archived = true;
        await events.Audit(AuditAction.InventoryArchived, id); await db.SaveChangesAsync(); return OperationResult.NoContent();
    }

    /// <summary>
    /// Đọc danh sách có lọc/phân trang yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="page">Giá trị kiểu int? dùng trong ListReplenishments.</param>
    /// <param name="pageSize">Giá trị kiểu int? dùng trong ListReplenishments.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).</remarks>
    public async Task<PageResponse<ReplenishmentRequest>> ListReplenishments(int? page, int? pageSize)
    {
        var u = await current.Get(); Ensure.Role(u, Role.ClubManager, Role.Groom);
        var q = db.Replenishments.AsQueryable(); if (u.Role == Role.Groom) q = q.Where(x => x.RequestedBy == u.Id);
        return await pager.Page(q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id), page, pageSize);
    }

    /// <summary>
    /// Tạo yêu cầu yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<ReplenishmentRequest> RequestReplenishment(ReplenishmentRequestDto r)
    {
        var u = await current.Get(); Ensure.Role(u, Role.Groom, Role.ClubManager); var item = Ensure.Found(await db.Inventory.FindAsync(r.ItemId)); Ensure.That(!item.Archived, Messages.Get(MessageKey.ItemIsArchived));
        var request = new ReplenishmentRequest { ItemId = r.ItemId, RequestedBy = u.Id, Quantity = r.Quantity, Notes = r.Notes };
        db.Replenishments.Add(request); await events.Managers(NotificationType.InventoryReplenishment, MessageKey.AReplenishmentRequestNeedsReview, request.Id);
        await events.Audit(AuditAction.InventoryReplenishmentRequested, request.Id); await db.SaveChangesAsync(); return request;
    }

    /// <summary>
    /// Duyệt yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    /// <param name="r">DTO request của thao tác; các field được kiểm ở API và quy tắc BLL.</param>
    /// <remarks>Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API). Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback. Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.</remarks>
    public async Task<ReplenishmentRequest> ReviewReplenishment(Guid id, ReplenishmentReviewRequest r)
    {
        Ensure.Role(await current.Get(), Role.ClubManager); var request = Ensure.Found(await db.Replenishments.FindAsync(id));
        Ensure.That(request.Status == ReplenishmentStatus.Pending, Messages.Get(MessageKey.RequestIsAlreadyReviewed), 409, "invalid_state");
        request.Status = r.Approve ? ReplenishmentStatus.Approved : ReplenishmentStatus.Rejected; request.Notes = r.Notes;
        // Approval authorizes procurement; stock only increases after an actual receipt/movement.
        events.Notify(request.RequestedBy, NotificationType.InventoryReplenishmentReviewed, MessageKey.YourReplenishmentRequestWasReviewed, id);
        await events.Audit(AuditAction.InventoryReplenishmentReviewed, id); await db.SaveChangesAsync(); return request;
    }

}
