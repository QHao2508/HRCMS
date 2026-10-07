# Giải thích function: HorseClub.BLL-Horses

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Horses/HorseAssignmentService.cs

Intake bản nháp/gửi/duyệt, hồ sơ ngựa chính thức, số đo và lịch sử phân công nhân sự.

### HorseAssignmentService.Assign(horseId, r)

**Kết quả:** `Task<StaffAssignment>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm vai trò người phân công, nhân sự được chọn và ngày bắt đầu; kết thúc phân công cũ, ghi phân công mới cùng audit/thông báo.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `access.Horse`, `Ensure.That`, `calendar.Today`, `Messages.Get`, `Ensure.Role`, `Assignments.AnyAsync`, `access.Staff`, `Assignments.Where`, `Assignments.Add`, `events.Notify`, `events.Audit`, `Role.ToString`, `db.SaveChangesAsync`.

### HorseAssignmentService.AssignStaff(id, request)

**Kết quả:** `Task<StaffAssignment>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Phân công nhân viên trong HorseAssignmentService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

## HorseClub.BLL/Horses/HorseProfileService.cs

Intake bản nháp/gửi/duyệt, hồ sơ ngựa chính thức, số đo và lịch sử phân công nhân sự.

### HorseProfileService.ListHorses(search, healthStatus, page, pageSize)

**Kết quả:** `Task<PageResponse<Horse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `search` | Giá trị kiểu string? dùng trong ListHorses. |
| `healthStatus` | Giá trị kiểu HealthStatus? dùng trong ListHorses. |
| `page` | Giá trị kiểu int? dùng trong ListHorses. |
| `pageSize` | Giá trị kiểu int? dùng trong ListHorses. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horses`, `string.IsNullOrWhiteSpace`, `q.Where`, `Name.Contains`, `RegistrationNumber.Contains`, `pager.Page`, `q.OrderBy`.

### HorseProfileService.GetHorse(id)

**Kết quả:** `Task<HorseDetailResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `Attachments.AsNoTracking`, `Measurements.Where`, `Assignments.Where`, `Occupancies.Where`, `Registrations.Where`.

### HorseProfileService.ListMeasurements(id, page, pageSize)

**Kết quả:** `Task<PageResponse<Measurement>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang số đo thể chất trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `page` | Giá trị kiểu int? dùng trong ListMeasurements. |
| `pageSize` | Giá trị kiểu int? dùng trong ListMeasurements. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `pager.Page`, `Measurements.Where`.

### HorseProfileService.AddMeasurement(id, request)

**Kết quả:** `Task<Measurement>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Bổ sung số đo thể chất trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `access.Horse`, `Ensure.That`, `calendar.Today`, `Messages.Get`, `Measurements.Add`, `events.Audit`, `db.SaveChangesAsync`.

### HorseProfileService.ArchiveHorse(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lưu trữ ngựa trong HorseProfileService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `access.Horse`, `Ensure.That`, `Sessions.AnyAsync`, `Messages.Get`, `Occupancies.Where`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

## HorseClub.BLL/Horses/HorseRegistrationService.cs

Intake bản nháp/gửi/duyệt, hồ sơ ngựa chính thức, số đo và lịch sử phân công nhân sự.

### HorseRegistrationService.Create(r)

**Kết quả:** `Task<HorseRegistration>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Điểm vào tạo dữ liệu của HorseRegistrationService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Registrations.Add`, `events.Audit`, `db.SaveChangesAsync`.

### HorseRegistrationService.Edit(id, r)

**Kết quả:** `Task<HorseRegistration>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chỉnh sửa dữ liệu của module trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Registration`, `current.Get`, `Ensure.That`, `Messages.Get`, `events.Audit`, `JsonSerializer.Serialize`, `Ensure.Role`, `db.SaveChangesAsync`.

### HorseRegistrationService.Submit(id)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Gửi dữ liệu của module trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `access.Registration`, `Ensure.That`, `Messages.Get`, `events.Managers`, `events.Audit`, `db.SaveChangesAsync`.

### HorseRegistrationService.Review(id, request)

**Kết quả:** `Task<RegistrationReviewResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cho quản lý duyệt hoặc yêu cầu chỉnh sửa; khi duyệt tạo hồ sơ ngựa chính thức, khi từ chối giữ hồ sơ cùng lý do.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `access.Registration`, `Ensure.That`, `Messages.Get`, `string.IsNullOrWhiteSpace`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`, `Horses.Add`, `Measurements.Add`.

### HorseRegistrationService.Apply(entity, r)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Áp dụng các trường intake được phép lên hồ sơ, kiểm ngày/giới hạn thể chất và giữ quy tắc riêng cho chỉnh sửa hành chính.

| Đầu vào | Ý nghĩa |
|---|---|
| `entity` | Giá trị kiểu HorseRegistration dùng trong Apply. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `Ensure.Validate`, `calendar.Today`, `Ensure.That`, `Messages.Get`, `access.Staff`.

### HorseRegistrationService.ValidateReady(r)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Kiểm đủ thông tin intake bắt buộc và điều kiện để gửi/duyệt; không cho dữ liệu nháp thiếu vào hồ sơ ngựa chính thức.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `string.IsNullOrWhiteSpace`, `missing.Add`, `Ensure.That`, `Messages.Get`, `string.Join`, `Attachments.AnyAsync`.

### HorseRegistrationService.Draft(r)

**Kết quả:** `RegistrationDraftRequest`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển hồ sơ intake sang response nháp với các trường được phép xem/sửa.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

### HorseRegistrationService.CreateDraft(request)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới bản nháp trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

Lời gọi chính: `OperationResult.Created`.

### HorseRegistrationService.ListRegistrations(status, page, pageSize)

**Kết quả:** `Task<PageResponse<HorseRegistration>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `page` | Giá trị kiểu int? dùng trong ListRegistrations. |
| `pageSize` | Giá trị kiểu int? dùng trong ListRegistrations. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `current.Get`, `Ensure.Role`, `Registrations.AsQueryable`, `q.Where`, `pager.Page`, `q.OrderByDescending`.

### HorseRegistrationService.GetRegistration(id)

**Kết quả:** `Task<HorseRegistration>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Registration`.

### HorseRegistrationService.UpdateDraft(id, request)

**Kết quả:** `Task<HorseRegistration>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cập nhật bản nháp trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

### HorseRegistrationService.SubmitRegistration(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Gửi hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `OperationResult.NoContent`.

### HorseRegistrationService.ReviewRegistration(id, request)

**Kết quả:** `Task<RegistrationReviewResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Duyệt hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

### HorseRegistrationService.CancelRegistration(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Hủy hồ sơ đăng ký trong HorseRegistrationService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `access.Registration`, `Ensure.That`, `Messages.Get`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.
