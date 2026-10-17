> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Reporting

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Reporting/ReportingService.cs

Aggregate dashboard, thông báo, audit và báo cáo có phạm vi/giới hạn dữ liệu.

### ReportingService.ListNotifications(unread, page, pageSize)

**Kết quả:** `Task<PageResponse<Notification>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang thông báo trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `unread` | Giá trị kiểu bool? dùng trong ListNotifications. |
| `page` | Giá trị kiểu int? dùng trong ListNotifications. |
| `pageSize` | Giá trị kiểu int? dùng trong ListNotifications. |

Lời gọi chính: `current.Get`, `Notifications.Where`, `q.Where`, `pager.Page`, `q.OrderByDescending`.

### ReportingService.MarkNotificationRead(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đánh dấu Notification Read trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `current.Get`, `Ensure.Found`, `Notifications.SingleOrDefaultAsync`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### ReportingService.ListAudit(referenceId, page, pageSize)

**Kết quả:** `Task<PageResponse<AuditEvent>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang nhật ký thao tác trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `referenceId` | Giá trị kiểu Guid? dùng trong ListAudit. |
| `page` | Giá trị kiểu int? dùng trong ListAudit. |
| `pageSize` | Giá trị kiểu int? dùng trong ListAudit. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `Audit.AsQueryable`, `q.Where`, `pager.Page`, `q.OrderByDescending`.

### ReportingService.GetDashboard()

**Kết quả:** `Task<DashboardResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết chỉ số tổng quan trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `access.Horses`, `clock.GetUtcNow`, `calendar.DayRange`, `Sessions.Where`, `horses.Contains`, `sessions.Where`, `CareTasks.Where`, `care.Where`, `Users.Where`, `horses.Count`, `Plans.Count`, `sessions.Count`, `care.Count`, `Restrictions.Where`, `Notifications.Count`.

### ReportingService.BuildReport(horseId, from, to, groupBy)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tổng hợp báo cáo trong ReportingService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `from` | Giá trị kiểu DateTimeOffset? dùng trong BuildReport. |
| `to` | URL nội bộ mà liên kết/redirect hướng đến. |
| `groupBy` | Giá trị kiểu ReportGrouping? dùng trong BuildReport. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `clock.GetUtcNow`, `end.AddDays`, `Ensure.That`, `Messages.Get`, `Enum.IsDefined`, `access.Horses`, `access.Horse`, `horses.Where`, `Sessions.Where`, `horses.Contains`, `sq.Where`, `CareTasks.Where`, `cq.Where`, `sq.OrderBy`, `cq.OrderBy`.

### ReportingService.Bucket(date, group, start)

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Nhóm ngày theo cách tổng hợp báo cáo đã chọn để thống kê nhất quán.

| Đầu vào | Ý nghĩa |
|---|---|
| `date` | Giá trị kiểu DateTime dùng trong Bucket. |
| `group` | Giá trị kiểu ReportGrouping dùng trong Bucket. |
| `start` | Giá trị kiểu DateTime dùng trong Bucket. |

Lời gọi chính: `d.ToString`, `d.AddDays`, `start.ToString`.
