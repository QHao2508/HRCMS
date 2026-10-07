# Giải thích function: HorseClub.BLL-Common

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Common/ClubAccess.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### ClubAccess.Horse(id, allowArchived)

**Kết quả:** `Task<Horse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lấy hồ sơ ngựa trong phạm vi sở hữu hoặc phân công của người đang đăng nhập; chặn truy cập ngoài phạm vi.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `allowArchived` | Giá trị kiểu bool dùng trong Horse. |

Lời gọi chính: `current.Get`, `Ensure.Found`, `Horses.FindAsync`, `Messages.Get`, `Assignments.AnyAsync`, `Sessions.AnyAsync`.

### ClubAccess.Horses()

**Kết quả:** `Task<IQueryable<Horse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Xây dựng IQueryable đã giới hạn phạm vi ngựa theo vai trò/danh tính để mọi truy vấn danh sách dùng cùng chính sách.

Lời gọi chính: `current.Get`, `Horses.Where`, `q.Where`, `Sessions.Any`, `Assignments.Any`.

### ClubAccess.Trainer(horseId)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Xác nhận người dùng là huấn luyện viên hiện đang được phân công cho ngựa trước khi thay đổi huấn luyện.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.That`, `Assignments.AnyAsync`, `Messages.Get`.

### ClubAccess.Vet(horseId)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Xác nhận quyền bác sĩ thú y và phạm vi ngựa trước thao tác y tế.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`.

### ClubAccess.Staff(id, role)

**Kết quả:** `Task<User>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra nhân sự tồn tại, hoạt động và có đúng vai trò yêu cầu trước khi phân công.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `role` | Role enum chính xác của backend để kiểm quyền/lọc dữ liệu. |

Lời gọi chính: `Ensure.Found`, `Users.FindAsync`, `Ensure.That`, `Messages.Get`.

### ClubAccess.Registration(id)

**Kết quả:** `Task<HorseRegistration>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra quyền chủ sở hữu/quản lý đối với hồ sơ đăng ký để tránh lộ tài liệu intake ngoài phạm vi.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `current.Get`, `Ensure.Found`, `Registrations.FindAsync`, `Ensure.That`, `Messages.Get`.

### ClubAccess.MedicalDetails(horseId)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Kiểm tra quyền xem dữ liệu y tế chi tiết của ngựa trước khi trả hồ sơ khám và hạn chế vận động.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |

## HorseClub.BLL/Common/ClubCalendar.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### ClubCalendar.Local(instant)

**Kết quả:** `DateTime`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển thời điểm UTC sang múi giờ nghiệp vụ đã cấu hình.

| Đầu vào | Ý nghĩa |
|---|---|
| `instant` | Giá trị kiểu DateTimeOffset dùng trong Local. |

Lời gọi chính: `TimeZoneInfo.ConvertTime`.

### ClubCalendar.DateAt(instant)

**Kết quả:** `DateOnly`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lấy ngày nghiệp vụ tại thời điểm truyền vào theo múi giờ câu lạc bộ, không dựa vào múi giờ máy chủ.

| Đầu vào | Ý nghĩa |
|---|---|
| `instant` | Giá trị kiểu DateTimeOffset dùng trong DateAt. |

Lời gọi chính: `DateOnly.FromDateTime`.

### ClubCalendar.Today(clock)

**Kết quả:** `DateOnly`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tính ngày hiện tại từ TimeProvider và múi giờ nghiệp vụ để dùng thống nhất khi kiểm tra lịch.

| Đầu vào | Ý nghĩa |
|---|---|
| `clock` | Giá trị kiểu TimeProvider dùng trong Today. |

Lời gọi chính: `clock.GetUtcNow`.

### ClubCalendar.DayRange(instant)

**Kết quả:** `(DateTimeOffset Start, DateTimeOffset End)`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo cặp mốc UTC đầu/cuối của một ngày nghiệp vụ để lọc thời gian trong database.

| Đầu vào | Ý nghĩa |
|---|---|
| `instant` | Giá trị kiểu DateTimeOffset dùng trong DayRange. |

Lời gọi chính: `zone.GetUtcOffset`, `day.AddDays`.

### ClubCalendar.IsValidZone(id)

**Kết quả:** `bool`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra hệ thống nhận diện được timezone đã cấu hình trước khi ứng dụng khởi động.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `TimeZoneInfo.FindSystemTimeZoneById`.

## HorseClub.BLL/Common/ClubEvents.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### ClubEvents.TrainingHistory(plan, session, result, evaluation)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Lưu snapshot kế hoạch/buổi tập/kết quả/đánh giá để có lịch sử thay đổi và xem lại diễn biến.

| Đầu vào | Ý nghĩa |
|---|---|
| `plan` | Giá trị kiểu TrainingPlan dùng trong TrainingHistory. |
| `session` | Giá trị kiểu TrainingSession? dùng trong TrainingHistory. |
| `result` | Giá trị kiểu SessionResult? dùng trong TrainingHistory. |
| `evaluation` | Giá trị kiểu TrainerEvaluation? dùng trong TrainingHistory. |

Lời gọi chính: `JsonSerializer.Serialize`, `TrainingRevisions.Add`, `current.Get`.

### ClubEvents.Audit(action, referenceId, detail)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Xếp sự kiện audit vào DbContext với người thực hiện và đối tượng liên quan; caller lưu cùng thay đổi nghiệp vụ.

| Đầu vào | Ý nghĩa |
|---|---|
| `action` | Giá trị kiểu AuditAction dùng trong Audit. |
| `referenceId` | Giá trị kiểu Guid dùng trong Audit. |
| `detail` | Giá trị kiểu string dùng trong Audit. |

Lời gọi chính: `current.Get`, `Audit.Add`.

### ClubEvents.Notify(recipient, type, message, reference)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Xếp thông báo theo enum và MessageKey cho người nhận, giữ cùng transaction nghiệp vụ.

| Đầu vào | Ý nghĩa |
|---|---|
| `recipient` | Giá trị kiểu Guid dùng trong Notify. |
| `type` | Giá trị kiểu NotificationType dùng trong Notify. |
| `message` | Giá trị kiểu MessageKey dùng trong Notify. |
| `reference` | Giá trị kiểu Guid? dùng trong Notify. |

Lời gọi chính: `Notifications.Add`, `Messages.Get`.

### ClubEvents.Managers(type, message, reference)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Lấy người quản lý hoạt động để định tuyến các yêu cầu cần duyệt.

| Đầu vào | Ý nghĩa |
|---|---|
| `type` | Giá trị kiểu NotificationType dùng trong Managers. |
| `message` | Giá trị kiểu MessageKey dùng trong Managers. |
| `reference` | Giá trị kiểu Guid dùng trong Managers. |

Lời gọi chính: `Users.Where`.

### ClubEvents.HorseStaff(horseId, type, message, roles)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Lấy nhân viên đang được phân công cho ngựa để định tuyến thông báo đúng người.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `type` | Giá trị kiểu NotificationType dùng trong HorseStaff. |
| `message` | Giá trị kiểu MessageKey dùng trong HorseStaff. |
| `roles` | Giá trị kiểu Role[] dùng trong HorseStaff. |

Lời gọi chính: `Assignments.Where`, `roles.Contains`.

## HorseClub.BLL/Common/ClubStartup.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### ClubStartup.Initialize(securityLimits, token)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Kiểm tra/khởi tạo tài khoản quản lý theo bootstrap cấu hình; không tạo mật khẩu mặc định công khai.

| Đầu vào | Ý nghĩa |
|---|---|
| `securityLimits` | Giá trị kiểu SecurityOptions dùng trong Initialize. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `string.IsNullOrWhiteSpace`, `Messages.Get`, `Users.AnyAsync`, `AuthenticationService.CheckPassword`, `Ensure.That`, `DataAnnotations.EmailAddressAttribute`, `AuthenticationService.Normalize`, `Users.Add`, `db.SaveChangesAsync`.

### ClubStartup.CanConnect(token)

**Kết quả:** `Task<bool>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra kết nối database cho health endpoint bằng cancellation token của request.

| Đầu vào | Ý nghĩa |
|---|---|
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

Lời gọi chính: `Database.CanConnectAsync`.

## HorseClub.BLL/Common/CurrentUser.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### CurrentUser.Get()

**Kết quả:** `Task<User>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh tính request, tra user và kiểm trạng thái/stamp; dùng lại kết quả trong scope để không tra user lặp lại.

Lời gọi chính: `Ensure.That`, `Guid.TryParse`, `Messages.Get`, `Users.FindAsync`.

## HorseClub.BLL/Common/Ensure.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### Ensure.That(condition, message, status, code)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Chuyển điều kiện nghiệp vụ sai thành ApiException với HTTP status, mã lỗi và reference ID do caller chỉ định.

| Đầu vào | Ý nghĩa |
|---|---|
| `condition` | Giá trị kiểu bool dùng trong That. |
| `message` | Giá trị kiểu string dùng trong That. |
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `code` | OTP 6 chữ số theo đúng mục đích; không log hoặc lưu vào URL. |

### Ensure.Found(value)

**Kết quả:** `T`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Trả entity khi tồn tại hoặc ném lỗi not_found; giúp service không tiếp tục xử lý dữ liệu null.

| Đầu vào | Ý nghĩa |
|---|---|
| `value` | Giá trị kiểu T? dùng trong Found. |

Lời gọi chính: `Messages.Get`.

### Ensure.Role(user, roles)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Yêu cầu user thuộc một trong các vai trò cho phép; chặn thao tác nghiệp vụ trái quyền.

| Đầu vào | Ý nghĩa |
|---|---|
| `user` | Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép. |
| `roles` | Giá trị kiểu Role[] dùng trong Role. |

Lời gọi chính: `roles.Contains`, `Messages.Get`.

### Ensure.Validate(value)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Thực hiện kiểm tra DataAnnotations cho đối tượng và tập hợp lỗi hợp đồng dữ liệu.

| Đầu vào | Ý nghĩa |
|---|---|
| `value` | Giá trị kiểu object dùng trong Validate. |

Lời gọi chính: `Validator.TryValidateObject`, `string.Join`, `errors.Select`, `Messages.Get`, `value.GetType`, `property.GetValue`, `Enum.IsDefined`, `e.GetType`.

## HorseClub.BLL/Common/OperationResult.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### OperationResult.Ok(value)

**Kết quả:** `OperationResult`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo OperationResult Ok thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.

| Đầu vào | Ý nghĩa |
|---|---|
| `value` | Giá trị kiểu object? dùng trong Ok. |

### OperationResult.Created(location, value)

**Kết quả:** `OperationResult`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo OperationResult Created thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.

| Đầu vào | Ý nghĩa |
|---|---|
| `location` | Giá trị kiểu string dùng trong Created. |
| `value` | Giá trị kiểu object? dùng trong Created. |

### OperationResult.NoContent()

**Kết quả:** `OperationResult`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo OperationResult NoContent thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.

### OperationResult.File(content, contentType, fileName)

**Kết quả:** `OperationResult`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo OperationResult File thuần dữ liệu để layer API quyết định JSON/status hoặc stream HTTP.

| Đầu vào | Ý nghĩa |
|---|---|
| `content` | Giá trị kiểu Stream dùng trong File. |
| `contentType` | MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng. |
| `fileName` | Giá trị kiểu string dùng trong File. |

## HorseClub.BLL/Common/PageReader.cs

Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ.

### PageReader.Page(q, page, pageSize)

**Kết quả:** `Task<PageResponse<T>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra page/pageSize, đếm tổng và đọc một trang dữ liệu không tracking; tránh tải cả bảng để phân trang.

| Đầu vào | Ý nghĩa |
|---|---|
| `q` | Giá trị kiểu IQueryable<T> dùng trong Page. |
| `page` | Giá trị kiểu int? dùng trong Page. |
| `pageSize` | Giá trị kiểu int? dùng trong Page. |

Lời gọi chính: `Ensure.That`, `Messages.Get`, `q.AsNoTracking`, `q.CountAsync`.

## HorseClub.BLL/Common/Options/EmailOptions.cs

Kiểu cấu hình với DataAnnotations và giá trị mặc định. User Secrets/environment ghi đè cấu hình khi khởi động.

### EmailOptions.CanDeliver(isDevelopment)

**Kết quả:** `bool`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm cấu hình đủ khả năng gửi email theo provider/môi trường trước khi worker chạy.

| Đầu vào | Ý nghĩa |
|---|---|
| `isDevelopment` | Giá trị kiểu bool dùng trong CanDeliver. |

Lời gọi chính: `string.IsNullOrWhiteSpace`, `string.Equals`.

## HorseClub.BLL/Common/Options/StorageOptions.cs

Kiểu cấu hình với DataAnnotations và giá trị mặc định. User Secrets/environment ghi đè cấu hình khi khởi động.

### StorageOptions.IsValid()

**Kết quả:** `bool`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm provider, HTTPS, container và credential mode; từ chối cấu hình Azure thiếu hoặc không an toàn.

Lời gọi chính: `string.IsNullOrWhiteSpace`, `Regex.IsMatch`, `Uri.TryCreate`, `Host.EndsWith`, `ConnectionString.Contains`.

## HorseClub.BLL/Common/Storage/AzureBlobStore.cs

Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định.

### AzureBlobStore.AzureBlobStore(container)

**Kết quả:** Khởi tạo instance với dependency/configuration đã truyền.

Tạo adapter BlobContainerClient từ credential cấu hình hoặc client được inject trong kiểm thử; không xuất khóa ra frontend.

| Đầu vào | Ý nghĩa |
|---|---|
| `container` | Giá trị kiểu BlobContainerClient dùng trong AzureBlobStore. |

### AzureBlobStore.AzureBlobStore(options)

**Kết quả:** Khởi tạo instance với dependency/configuration đã truyền.

Tạo adapter BlobContainerClient từ credential cấu hình hoặc client được inject trong kiểm thử; không xuất khóa ra frontend.

| Đầu vào | Ý nghĩa |
|---|---|
| `options` | Cấu hình/hợp đồng tùy hàm; các giá trị được truyền rõ ràng từ caller. |

Lời gọi chính: `string.IsNullOrWhiteSpace`, `TimeSpan.FromSeconds`.

### AzureBlobStore.Write(name, bytes, token, contentType)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Upload blob với điều kiện If-None-Match để không ghi đè, giữ MIME của nội dung.

| Đầu vào | Ý nghĩa |
|---|---|
| `name` | Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm. |
| `bytes` | Nội dung file trong bộ nhớ, đã được caller kiểm loại và giới hạn dung lượng. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |
| `contentType` | MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng. |

### AzureBlobStore.OpenRead(name, token)

**Kết quả:** `Task<Stream>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Mở stream download từ container private; chỉ chuyển BlobNotFound thành 404, giữ lỗi hạ tầng để xử lý đúng.

| Đầu vào | Ý nghĩa |
|---|---|
| `name` | Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

### AzureBlobStore.Delete(name, token)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Xóa blob cùng snapshot nếu tồn tại; dùng trong dọn upload chưa commit hoặc kiểm thử.

| Đầu vào | Ý nghĩa |
|---|---|
| `name` | Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

### AzureBlobStore.Required()

**Kết quả:** `BlobContainerClient`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Yêu cầu container Azure đã được cấu hình trước khi thao tác; không tự fallback sang local.

## HorseClub.BLL/Common/Storage/BrandingService.cs

Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định.

### BrandingService.OpenLogo(token)

**Kết quả:** `Task<Stream>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chỉ đọc blob logo cố định từ BrandingOptions; người gọi không thể chọn một blob ảnh ngựa khác.

| Đầu vào | Ý nghĩa |
|---|---|
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

Lời gọi chính: `blobs.OpenRead`.

## HorseClub.BLL/Common/Storage/UploadFiles.cs

Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định.

### UploadFiles.GetFile(name)

**Kết quả:** `UploadFile?`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tìm tệp có tên form tương ứng trong abstraction danh sách upload; trả null khi không có.

| Đầu vào | Ý nghĩa |
|---|---|
| `name` | Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm. |

Lời gọi chính: `Items.FirstOrDefault`.

## HorseClub.BLL/Common/Storage/UploadRequest.cs

Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định.

### UploadRequest.ReadFormAsync()

**Kết quả:** `Task<UploadForm>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc multipart qua adapter đã được API cung cấp với cancellation token, không đưa HttpRequest vào BLL.

## HorseClub.BLL/Common/Storage/UploadStorage.cs

Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định.

### UploadStorage.ValidateName(name)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Chỉ cho phép storage key UUID dạng 32 ký tự hex để ngăn path traversal ở luồng upload hồ sơ.

| Đầu vào | Ý nghĩa |
|---|---|
| `name` | Tên/key đầu vào theo mục đích hàm; xem kiểu và điều kiện kiểm trong thân hàm. |

Lời gọi chính: `Ensure.That`, `Regex.IsMatch`, `Messages.Get`.

### UploadStorage.Resolve(storageName)

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghép đường dẫn local an toàn cho provider kiểm thử, kiểm tra symlink/reparse point; Azure không có thư mục local.

| Đầu vào | Ý nghĩa |
|---|---|
| `storageName` | Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn. |

Lời gọi chính: `Directory.CreateDirectory`, `string.IsNullOrEmpty`, `Path.GetDirectoryName`, `Ensure.That`, `File.GetAttributes`, `Messages.Get`, `Path.Combine`, `File.Exists`.

### UploadStorage.Write(storageName, bytes, token, contentType)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Ghi file mới theo provider, theo dõi blob/file vừa tạo để có thể dọn khi transaction rollback; không ghi đè key có sẵn.

| Đầu vào | Ý nghĩa |
|---|---|
| `storageName` | Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn. |
| `bytes` | Nội dung file trong bộ nhớ, đã được caller kiểm loại và giới hạn dung lượng. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |
| `contentType` | MIME đã xác định từ nội dung file; dùng khi lưu/stream để browser đọc đúng. |

- Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn.

Lời gọi chính: `blobs.Write`, `created.Add`, `stream.WriteAsync`, `stream.FlushAsync`.

### UploadStorage.Commit()

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đánh dấu upload đã được commit với database để cơ chế dọn cuối request không xóa tài nguyên hợp lệ.

### UploadStorage.OpenRead(storageName, token)

**Kết quả:** `Task<Stream>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc blob Azure hoặc file kiểm thử sau khi kiểm storage key; trả stream để không tải cả file vào response buffer.

| Đầu vào | Ý nghĩa |
|---|---|
| `storageName` | Blob/file key đã kiểm tra, không phải URL public hoặc đường dẫn do client tùy ý chọn. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

Lời gọi chính: `blobs.OpenRead`, `Ensure.That`, `File.Exists`, `Messages.Get`.

### UploadStorage.CleanupUncommitted()

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Dọn tài nguyên mới không được commit, nhưng giữ tài nguyên DB đang tham chiếu hoặc chưa xác nhận được trạng thái DB; log ID để đối soát.

Lời gọi chính: `Attachments.AnyAsync`, `IncidentPhotos.AnyAsync`, `TimeSpan.FromSeconds`, `blobs.Delete`, `File.Delete`, `logger.LogWarning`, `error.GetType`.
