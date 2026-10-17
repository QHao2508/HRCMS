> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Care

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Care/CareService.cs

Chuồng, công việc chăm sóc và sự cố; phân công, ghi nhận và duyệt đúng vai trò.

### CareService.ListTasks(horseId, status, from, to, page, pageSize)

**Kết quả:** `Task<PageResponse<CareTaskSummaryResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `from` | Giá trị kiểu DateTimeOffset? dùng trong ListTasks. |
| `to` | URL nội bộ mà liên kết/redirect hướng đến. |
| `page` | Giá trị kiểu int? dùng trong ListTasks. |
| `pageSize` | Giá trị kiểu int? dùng trong ListTasks. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `access.Horses`, `CareTasks.Where`, `horses.Contains`, `Ensure.Role`, `q.Where`, `access.Horse`, `pager.Page`, `q.OrderBy`.

### CareService.CreateTask(r)

**Kết quả:** `Task<CareTask>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `access.Horse`, `Ensure.Role`, `Ensure.That`, `Messages.Get`, `access.Staff`, `Assignments.AnyAsync`, `Treatments.AnyAsync`, `ScheduledAt.ToUniversalTime`, `CareTasks.Add`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.RecordTask(id, r)

**Kết quả:** `Task<CareTask>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận công việc chăm sóc trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `CareTasks.FindAsync`, `Ensure.That`, `Messages.Get`, `access.Horse`, `clock.GetUtcNow`, `Incidents.Add`, `events.HorseStaff`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.ListIncidents(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<Incident>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListIncidents. |
| `pageSize` | Giá trị kiểu int? dùng trong ListIncidents. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `Ensure.Role`, `access.Horses`, `Incidents.Where`, `horses.Contains`, `q.Where`, `pager.Page`, `q.OrderByDescending`.

### CareService.ReportIncident(r)

**Kết quả:** `Task<Incident>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Báo cáo sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `access.Horse`, `Ensure.That`, `Messages.Get`, `Incidents.Add`, `events.HorseStaff`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.ResolveIncident(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Xử lý quyết định cho sự cố trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Found`, `Incidents.FindAsync`, `access.Horse`, `Ensure.That`, `Messages.Get`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### CareService.ListStables(page, pageSize)

**Kết quả:** `Task<PageResponse<Stable>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `page` | Giá trị kiểu int? dùng trong ListStables. |
| `pageSize` | Giá trị kiểu int? dùng trong ListStables. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `pager.Page`, `Stables.OrderBy`.

### CareService.CreateStable(r)

**Kết quả:** `Task<Stable>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới khu chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Stables.Add`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.ListStalls(stableId, page, pageSize)

**Kết quả:** `Task<PageResponse<StallSummaryResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `stableId` | Giá trị kiểu Guid? dùng trong ListStalls. |
| `page` | Giá trị kiểu int? dùng trong ListStalls. |
| `pageSize` | Giá trị kiểu int? dùng trong ListStalls. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `Stalls.AsQueryable`, `q.Where`, `pager.Page`, `q.OrderBy`.

### CareService.CreateStall(r)

**Kết quả:** `Task<Stall>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Stables.FindAsync`, `Stalls.Add`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.OccupyStall(id, r)

**Kết quả:** `Task<StallOccupancy>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Stalls.FindAsync`, `access.Horse`, `Ensure.That`, `Occupancies.AnyAsync`, `Messages.Get`, `Occupancies.Where`, `clock.GetUtcNow`, `Occupancies.Add`, `events.Audit`, `db.SaveChangesAsync`.

### CareService.VacateStall(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kết thúc sử dụng ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Occupancies.SingleOrDefaultAsync`, `clock.GetUtcNow`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### CareService.MarkStallClean(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đánh dấu vệ sinh ô chuồng trong CareService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `Stalls.FindAsync`, `Occupancies.SingleOrDefaultAsync`, `access.Horse`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

## HorseClub.BLL/Care/IncidentPhotoService.cs

Chuồng, công việc chăm sóc và sự cố; phân công, ghi nhận và duyệt đúng vai trò.

### IncidentPhotoService.ListPhotos(incidentId)

**Kết quả:** `Task<List<IncidentPhotoResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `incidentId` | Giá trị kiểu Guid dùng trong ListPhotos. |

Lời gọi chính: `IncidentPhotos.Where`.

### IncidentPhotoService.UploadPhoto(incidentId, request)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tải lên ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `incidentId` | Giá trị kiểu Guid dùng trong UploadPhoto. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.That`, `Messages.Get`, `request.ReadFormAsync`, `Files.GetFile`, `IncidentPhotos.CountAsync`, `file.OpenReadStream`, `stream.ReadExactlyAsync`, `AttachmentService.Detect`, `Path.GetExtension`, `Path.GetFileName`, `Guid.NewGuid`, `storage.Write`, `IncidentPhotos.Add`, `events.Audit`, `db.SaveChangesAsync`.

### IncidentPhotoService.DownloadPhoto(incidentId, id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tải nội dung ảnh trong IncidentPhotoService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `incidentId` | Giá trị kiểu Guid dùng trong DownloadPhoto. |
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `Ensure.Found`, `IncidentPhotos.SingleOrDefaultAsync`, `OperationResult.File`, `storage.OpenRead`.

### IncidentPhotoService.Check(id, db, access, current)

**Kết quả:** `Task<Incident>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm quyền truy cập sự cố và ảnh theo người báo/phạm vi trước upload hoặc download.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `db` | Giá trị kiểu ClubDbContext dùng trong Check. |
| `access` | Giá trị kiểu ClubAccess dùng trong Check. |
| `current` | Giá trị kiểu CurrentUser dùng trong Check. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `Ensure.Found`, `Incidents.FindAsync`, `access.Horse`, `current.Get`, `Ensure.That`, `Messages.Get`.
