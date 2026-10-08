> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Auth

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Auth/AuthenticationService.cs

Đăng ký/đăng nhập, OTP Verify/Reset/Invite, nhân viên, session stamp và dọn đăng ký chưa xác thực quá hạn.

### AuthenticationService.ValidateSession(id, stamp)

**Kết quả:** `Task<User?>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đối chiếu tài khoản, trạng thái kích hoạt và SecurityStamp để xác định token còn hợp lệ; trả null khi phiên bị thu hồi.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `stamp` | Giá trị kiểu string? dùng trong ValidateSession. |

Lời gọi chính: `Users.AsNoTracking`.

### AuthenticationService.Normalize(value)

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Trim khoảng trắng và chuyển email/tên đăng nhập về chữ thường để tra cứu và kiểm tra trùng thống nhất.

| Đầu vào | Ý nghĩa |
|---|---|
| `value` | Giá trị kiểu string dùng trong Normalize. |

Lời gọi chính: `value.Trim`.

### AuthenticationService.View(u)

**Kết quả:** `UserResponse`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển User thành UserResponse; chỉ trả các trường hồ sơ được phép, không đưa password hash hoặc SecurityStamp ra API.

| Đầu vào | Ý nghĩa |
|---|---|
| `u` | Giá trị kiểu User dùng trong View. |

### AuthenticationService.Register(r)

**Kết quả:** `Task<User>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra chính sách mật khẩu/căn cước, giải phóng đăng ký quá hạn, kiểm tra trùng rồi tạo chủ ngựa và xếp email OTP vào hàng đợi trong cùng transaction.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Ensure.That`, `Messages.Get`, `string.IsNullOrWhiteSpace`, `NationalId.All`, `cleanup.RemoveExpired`, `Users.AnyAsync`, `FirstName.Trim`, `LastName.Trim`, `Phone.Trim`, `Address.Trim`, `clock.GetUtcNow`, `hasher.HashPassword`, `protection.CreateProtector`, `Users.Add`, `db.SaveChangesAsync`.

### AuthenticationService.CreateStaff(r)

**Kết quả:** `Task<User>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo tài khoản nhân viên theo vai trò quản lý chọn, chưa đặt mật khẩu; sinh lời mời OTP để nhân viên tự kích hoạt.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Ensure.That`, `Messages.Get`, `Users.AnyAsync`, `FirstName.Trim`, `LastName.Trim`, `Users.Add`, `db.SaveChangesAsync`.

### AuthenticationService.Challenge(user, purpose)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Giới hạn tần suất gửi lại, vô hiệu mã cũ, sinh OTP 6 chữ số, lưu hash và tạo EmailMessage. OTP đăng ký không được vượt hạn tài khoản 24 giờ.

| Đầu vào | Ý nghĩa |
|---|---|
| `user` | Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép. |
| `purpose` | Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác. |

Lời gọi chính: `clock.GetUtcNow`, `Challenges.AnyAsync`, `now.AddSeconds`, `Challenges.Where`, `RandomNumberGenerator.GetInt32`, `now.AddMinutes`, `CreatedAt.AddHours`, `challengeHasher.HashPassword`, `Challenges.Add`, `EmailMessages.Add`, `Messages.Get`.

### AuthenticationService.Consume(user, purpose, code)

**Kết quả:** `Task<bool>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra mã OTP mới nhất theo đúng mục đích, hạn dùng và số lần thử; tăng số lần thử, so sánh hash, đánh dấu dùng một lần khi thành công hoặc vượt giới hạn.

| Đầu vào | Ý nghĩa |
|---|---|
| `user` | Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép. |
| `purpose` | Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác. |
| `code` | OTP 6 chữ số theo đúng mục đích; không log hoặc lưu vào URL. |

Lời gọi chính: `Challenges.Where`, `clock.GetUtcNow`, `code.All`, `challengeHasher.VerifyHashedPassword`.

### AuthenticationService.Login(r)

**Kết quả:** `Task<LoginAttempt>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra username/email và password hash, xử lý khóa đăng nhập; trả LoginAttempt phân biệt mật khẩu sai, cần xác thực, đăng ký quá hạn và tài khoản hợp lệ.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Users.Where`, `hasher.HashPassword`, `clock.GetUtcNow`, `string.IsNullOrEmpty`, `hasher.VerifyHashedPassword`, `db.SaveChangesAsync`.

### AuthenticationService.SetPassword(user, password)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Kiểm tra độ mạnh, hash mật khẩu mới, xác thực tài khoản và đổi SecurityStamp để thu hồi các phiên cũ; xóa bộ đếm đăng nhập sai.

| Đầu vào | Ý nghĩa |
|---|---|
| `user` | Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép. |
| `password` | Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response. |

Lời gọi chính: `hasher.HashPassword`, `Guid.NewGuid`.

### AuthenticationService.Principal(user)

**Kết quả:** `ClaimsPrincipal`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo danh tính claims từ tài khoản đã kiểm tra, gồm user ID, username, role và stamp để API cấp bearer token.

| Đầu vào | Ý nghĩa |
|---|---|
| `user` | Tài khoản đã tra cứu/kiểm; response chỉ được lấy trường cho phép. |

Lời gọi chính: `Id.ToString`, `Role.ToString`.

### AuthenticationService.ValidatePassword(password)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Áp dụng SecurityOptions hiện tại vào kiểm tra độ dài, chữ hoa, chữ thường và chữ số của mật khẩu.

| Đầu vào | Ý nghĩa |
|---|---|
| `password` | Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response. |

### AuthenticationService.CheckPassword(password, settings)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Kiểm tra mật khẩu theo một SecurityOptions được truyền vào; ném lỗi nghiệp vụ nếu không đáp ứng chính sách.

| Đầu vào | Ý nghĩa |
|---|---|
| `password` | Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response. |
| `settings` | Các giới hạn/chính sách cấu hình áp dụng tại thời điểm chạy. |

Lời gọi chính: `Ensure.That`, `password.Any`, `Messages.Get`.

### AuthenticationService.RegisterAccount(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Bao bọc kết quả đăng ký thành phản hồi Created chứa hồ sơ; đăng ký không tự cấp token đăng nhập.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `OperationResult.Created`, `AuthenticationService.View`.

### AuthenticationService.VerifyEmail(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chỉ xác thực chủ ngựa đang hoạt động và còn hạn đăng ký; tiêu thụ OTP Verify, cập nhật EmailVerified và trả cờ verified.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Users.SingleOrDefaultAsync`, `AuthenticationService.Normalize`, `clock.GetUtcNow`, `db.SaveChangesAsync`, `OperationResult.Ok`.

### AuthenticationService.ResendVerification(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo lại OTP Verify cho chủ ngựa chưa xác thực còn hạn; luôn trả thông báo chung để không tiết lộ email có tồn tại hay không.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Users.SingleOrDefaultAsync`, `AuthenticationService.Normalize`, `clock.GetUtcNow`, `db.SaveChangesAsync`, `OperationResult.Ok`, `Messages.Get`.

### AuthenticationService.ForgotPassword(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Xếp OTP Reset cho tài khoản đang hoạt động và đã xác thực; trả thông báo chung cả khi tài khoản không đủ điều kiện.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Users.SingleOrDefaultAsync`, `AuthenticationService.Normalize`, `db.SaveChangesAsync`, `OperationResult.Ok`, `Messages.Get`.

### AuthenticationService.GetProfile()

**Kết quả:** `Task<UserResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lấy tài khoản đã xác thực của request và chuyển sang DTO hồ sơ an toàn.

Lời gọi chính: `AuthenticationService.View`, `current.Get`.

### AuthenticationService.Logout()

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đổi SecurityStamp của tài khoản hiện tại để các access/refresh token cũ bị từ chối.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `current.Get`, `Guid.NewGuid`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### AuthenticationService.ListStaff(page, pageSize, role)

**Kết quả:** `Task<PageResponse<StaffResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Phân trang tài khoản nhân viên cho quản lý, hỗ trợ lọc role và trả trạng thái xác thực/kích hoạt.

| Đầu vào | Ý nghĩa |
|---|---|
| `page` | Giá trị kiểu int? dùng trong ListStaff. |
| `pageSize` | Giá trị kiểu int? dùng trong ListStaff. |
| `role` | Role enum chính xác của backend để kiểm quyền/lọc dữ liệu. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `Users.Where`, `q.Where`, `pager.Page`, `q.OrderBy`.

### AuthenticationService.ListStaffDirectory(role, page, pageSize)

**Kết quả:** `Task<PageResponse<StaffDirectoryResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Trả danh bạ nhân viên đang hoạt động phục vụ chọn người phân công; lọc role và phân trang, không trả thông tin xác thực nhạy cảm.

| Đầu vào | Ý nghĩa |
|---|---|
| `role` | Role enum chính xác của backend để kiểm quyền/lọc dữ liệu. |
| `page` | Giá trị kiểu int? dùng trong ListStaffDirectory. |
| `pageSize` | Giá trị kiểu int? dùng trong ListStaffDirectory. |

Lời gọi chính: `current.Get`, `Users.Where`, `q.Where`, `pager.Page`, `q.OrderBy`.

### AuthenticationService.InviteStaff(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Yêu cầu quyền quản lý, tạo nhân viên và lời mời OTP, lưu audit; trả hồ sơ tài khoản mới.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.Created`, `AuthenticationService.View`.

### AuthenticationService.SetStaffActive(id, r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cho quản lý bật/tắt tài khoản nhân viên, thu hồi phiên bằng stamp và ghi audit; không áp dụng cho quản lý hoặc chủ ngựa.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Users.FindAsync`, `Ensure.That`, `Messages.Get`, `Guid.NewGuid`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### AuthenticationService.ChangePassword(r, purpose)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra mật khẩu xác nhận và điều kiện tài khoản theo Reset/Invite; chỉ đặt mật khẩu khi OTP đúng mục đích được tiêu thụ thành công.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |
| `purpose` | Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `Ensure.That`, `Messages.Get`, `Users.SingleOrDefaultAsync`, `AuthenticationService.Normalize`, `db.SaveChangesAsync`, `OperationResult.Ok`.

## HorseClub.BLL/Auth/DemoAccountSeeder.cs

Đăng ký/đăng nhập, OTP Verify/Reset/Invite, nhân viên, session stamp và dọn đăng ký chưa xác thực quá hạn.

### DemoAccountSeeder.Seed(accounts, password, security, resetDemoPassword)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo bộ dữ liệu demo theo vai trò/phân công; chỉ chạy công cụ demo được gọi rõ ràng, không thuộc request thường.

| Đầu vào | Ý nghĩa |
|---|---|
| `accounts` | Giá trị kiểu IReadOnlyList<Account> dùng trong Seed. |
| `password` | Mật khẩu trong bộ nhớ cho kiểm/hash; không ghi ra log hoặc response. |
| `security` | Giá trị kiểu SecurityOptions dùng trong Seed. |
| `resetDemoPassword` | Giá trị kiểu bool dùng trong Seed. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.

Lời gọi chính: `ArgumentException.ThrowIfNullOrWhiteSpace`, `Database.BeginTransactionAsync`, `AuthenticationService.Normalize`, `Users.SingleOrDefaultAsync`, `Ensure.That`, `hasher.VerifyHashedPassword`, `Messages.Get`, `hasher.HashPassword`, `Guid.NewGuid`, `AuthenticationService.CheckPassword`, `Role.ToString`, `Users.Add`, `db.SaveChangesAsync`, `transaction.CommitAsync`.

## HorseClub.BLL/Auth/PendingRegistrationCleanup.cs

Đăng ký/đăng nhập, OTP Verify/Reset/Invite, nhân viên, session stamp và dọn đăng ký chưa xác thực quá hạn.

### PendingRegistrationCleanup.RemoveExpired(batchSize, token, email, userName)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Xóa chủ ngựa chưa xác thực quá hạn cùng challenge, email OTP và thông báo. Giữ tài khoản đã xác thực, nhân viên và dữ liệu có liên kết nghiệp vụ; caller quản lý transaction.

| Đầu vào | Ý nghĩa |
|---|---|
| `batchSize` | Giá trị kiểu int dùng trong RemoveExpired. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |
| `email` | Email đầu vào hoặc email chuẩn của tài khoản; không dùng để suy luận tài khoản tồn tại từ phản hồi chung. |
| `userName` | Giá trị kiểu string? dùng trong RemoveExpired. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

Lời gọi chính: `clock.GetUtcNow`, `Users.AsNoTracking`, `Registrations.Any`, `Horses.Any`, `Plans.Any`, `Sessions.Any`, `accounts.Select`, `Challenges.Where`, `ids.Contains`, `EmailMessages.Where`, `emails.Contains`, `challenges.Any`, `challenges.ExecuteDeleteAsync`, `Notifications.Where`, `Users.Where`.
