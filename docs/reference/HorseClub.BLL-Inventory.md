# Giải thích function: HorseClub.BLL-Inventory

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Inventory/InventoryService.cs

Vật tư, nhập/xuất/điều chỉnh kho và yêu cầu bổ sung có duyệt.

### InventoryService.ListItems(lowStock, page, pageSize)

**Kết quả:** `Task<PageResponse<InventoryItem>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `lowStock` | Giá trị kiểu bool? dùng trong ListItems. |
| `page` | Giá trị kiểu int? dùng trong ListItems. |
| `pageSize` | Giá trị kiểu int? dùng trong ListItems. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `Inventory.Where`, `q.Where`, `pager.Page`, `q.OrderBy`.

### InventoryService.CreateItem(r)

**Kết quả:** `Task<InventoryItem>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Inventory.Add`, `events.Audit`, `db.SaveChangesAsync`.

### InventoryService.ListMovements(id, page, pageSize)

**Kết quả:** `Task<PageResponse<StockMovement>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang biến động kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `page` | Giá trị kiểu int? dùng trong ListMovements. |
| `pageSize` | Giá trị kiểu int? dùng trong ListMovements. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `pager.Page`, `StockMovements.Where`.

### InventoryService.RecordMovement(id, r)

**Kết quả:** `Task<StockMovementResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận biến động kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.That`, `Math.Abs`, `Messages.Get`, `Ensure.Found`, `Inventory.FindAsync`, `StockMovements.Add`, `events.Managers`, `events.Audit`, `db.SaveChangesAsync`.

### InventoryService.ArchiveItem(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lưu trữ vật tư trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Inventory.FindAsync`, `Ensure.That`, `Messages.Get`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### InventoryService.ListReplenishments(page, pageSize)

**Kết quả:** `Task<PageResponse<ReplenishmentRequest>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `page` | Giá trị kiểu int? dùng trong ListReplenishments. |
| `pageSize` | Giá trị kiểu int? dùng trong ListReplenishments. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `current.Get`, `Ensure.Role`, `Replenishments.AsQueryable`, `q.Where`, `pager.Page`, `q.OrderByDescending`.

### InventoryService.RequestReplenishment(r)

**Kết quả:** `Task<ReplenishmentRequest>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo yêu cầu yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `Inventory.FindAsync`, `Ensure.That`, `Messages.Get`, `Replenishments.Add`, `events.Managers`, `events.Audit`, `db.SaveChangesAsync`.

### InventoryService.ReviewReplenishment(id, r)

**Kết quả:** `Task<ReplenishmentRequest>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Duyệt yêu cầu bổ sung kho trong InventoryService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Replenishments.FindAsync`, `Ensure.That`, `Messages.Get`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`.
