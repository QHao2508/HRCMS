> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Workers

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Workers/AccountCleanupWorker.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### AccountCleanupWorker.RunOnce(token)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Khóa lượt dọn trong SQL Server, xóa một lô tài khoản tự đăng ký chưa xác thực quá hạn và commit độc lập.

| Đầu vào | Ý nghĩa |
|---|---|
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

- Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.

Lời gọi chính: `scopes.CreateScope`, `Database.BeginTransactionAsync`, `WorkerDatabaseLock.TryAcquire`, `tx.CommitAsync`, `logger.LogInformation`.

### AccountCleanupWorker.ExecuteAsync(stoppingToken)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Vòng lặp nền của AccountCleanupWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.

| Đầu vào | Ý nghĩa |
|---|---|
| `stoppingToken` | Token dừng host; mọi vòng lặp/delay phải tôn trọng token này. |

Lời gọi chính: `logger.LogError`, `Task.Delay`, `TimeSpan.FromSeconds`.

## HorseClub.BLL/Workers/ClubMailSender.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### ClubMailSender.Send(recipient, subject, body, token)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Gửi email qua SMTP cấu hình, chuẩn hóa app password Gmail; chỉ provider DevelopmentFile trong Development mới ghi email kiểm thử.

| Đầu vào | Ý nghĩa |
|---|---|
| `recipient` | Giá trị kiểu string dùng trong Send. |
| `subject` | Giá trị kiểu string dùng trong Send. |
| `body` | Giá trị kiểu string dùng trong Send. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

Lời gọi chính: `env.IsDevelopment`, `Path.Combine`, `Directory.CreateDirectory`, `File.WriteAllTextAsync`, `Guid.NewGuid`, `Messages.Get`, `string.Equals`, `string.IsNullOrWhiteSpace`, `smtp.SendMailAsync`.

### ClubMailSender.CreateMessage(settings, recipient, subject, body)

**Kết quả:** `MailMessage`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo thư UTF-8 với người gửi/nhận, plain text và HTML alternate view; SMTP dùng multipart/alternative.

| Đầu vào | Ý nghĩa |
|---|---|
| `settings` | Các giới hạn/chính sách cấu hình áp dụng tại thời điểm chạy. |
| `recipient` | Giá trị kiểu string dùng trong CreateMessage. |
| `subject` | Giá trị kiểu string dùng trong CreateMessage. |
| `body` | Giá trị kiểu string dùng trong CreateMessage. |

Lời gọi chính: `Messages.Get`, `OtpEmailRenderer.Render`, `AlternateViews.Add`, `AlternateView.CreateAlternateViewFromString`, `To.Add`.

## HorseClub.BLL/Workers/EmailWorker.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### EmailWorker.RunOnce(token)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lấy lô email dưới khóa SQL, bỏ mã hết hạn/đã dùng, gửi với timeout và lưu trạng thái từng thư; lên lịch retry khi lỗi, không log OTP/credential.

| Đầu vào | Ý nghĩa |
|---|---|
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.

Lời gọi chính: `scopes.CreateScope`, `Database.BeginTransactionAsync`, `WorkerDatabaseLock.TryAcquire`, `clock.GetUtcNow`, `EmailMessages.Where`, `Challenges.Any`, `Challenges.AnyAsync`, `db.SaveChangesAsync`, `tx.CommitAsync`, `CancellationTokenSource.CreateLinkedTokenSource`, `sendTimeout.CancelAfter`, `TimeSpan.FromSeconds`, `Math.Min`, `Math.Pow`, `logger.LogWarning`, `error.GetType`.

### EmailWorker.ExecuteAsync(stoppingToken)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Vòng lặp nền của EmailWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.

| Đầu vào | Ý nghĩa |
|---|---|
| `stoppingToken` | Token dừng host; mọi vòng lặp/delay phải tôn trọng token này. |

Lời gọi chính: `logger.LogError`, `Task.Delay`, `TimeSpan.FromSeconds`.

## HorseClub.BLL/Workers/IClubMailSender.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### IClubMailSender.Send(recipient, subject, body, token)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Hợp đồng gửi thư cho worker: recipient/subject/body và cancellation token; implementation chịu trách nhiệm SMTP hoặc provider kiểm thử.

| Đầu vào | Ý nghĩa |
|---|---|
| `recipient` | Giá trị kiểu string dùng trong Send. |
| `subject` | Giá trị kiểu string dùng trong Send. |
| `body` | Giá trị kiểu string dùng trong Send. |
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

## HorseClub.BLL/Workers/OtpEmailRenderer.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### OtpEmailRenderer.Render(subject, plainText)

**Kết quả:** `string?`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Nhận email OTP đã biết, tách mã/nội dung và chèn vào template HTML đã encode; trả null với email không thuộc mẫu OTP.

| Đầu vào | Ý nghĩa |
|---|---|
| `subject` | Giá trị kiểu string dùng trong Render. |
| `plainText` | Giá trị kiểu string dùng trong Render. |

Lời gọi chính: `Regex.Match`, `plainText.Split`, `x.Trim`, `lines.Where`, `x.StartsWith`, `lines.Except`, `subject.StartsWith`, `string.Join`, `content.Select`, `notes.Select`, `disclaimers.Select`, `Regex.Replace`.

### OtpEmailRenderer.Encode(text)

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

HTML-encode nội dung động để tên người dùng hoặc dữ liệu email không chèn markup vào template.

| Đầu vào | Ý nghĩa |
|---|---|
| `text` | Giá trị kiểu string dùng trong Encode. |

Lời gọi chính: `WebUtility.HtmlEncode`.

### OtpEmailRenderer.LoadTemplate()

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc template OTP HTML embedded từ assembly để không phụ thuộc file runtime ngoài project.

Lời gọi chính: `Assembly.GetManifestResourceStream`, `reader.ReadToEnd`.

## HorseClub.BLL/Workers/ReminderWorker.cs

Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance.

### ReminderWorker.RunOnce(token)

**Kết quả:** `Task<int>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Quét công việc phòng bệnh, buổi tập quá hạn và follow-up y tế; gửi thông báo đúng nhân viên hiện được phân công, tránh tạo trùng.

| Đầu vào | Ý nghĩa |
|---|---|
| `token` | Cancellation token để hủy I/O/lượt công việc khi request hoặc host dừng. |

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.

Lời gọi chính: `scopes.CreateScope`, `Database.BeginTransactionAsync`, `WorkerDatabaseLock.TryAcquire`, `clock.GetUtcNow`, `Assignments.Where`, `Users.Any`, `Notifications.Add`, `Messages.Get`, `PreventiveCare.Where`, `Horses.Any`, `vets.Any`, `preventive.Select`, `vets.Where`, `preventiveHorseIds.Contains`, `Sessions.Where`, `now.AddMinutes`.

### ReminderWorker.Notify(recipient, type, message, reference)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Tạo thông báo từ MessageKey cho người nhận trong lô công việc, tăng bộ đếm và chờ transaction commit.

| Đầu vào | Ý nghĩa |
|---|---|
| `recipient` | Giá trị kiểu Guid dùng trong Notify. |
| `type` | Giá trị kiểu NotificationType dùng trong Notify. |
| `message` | Giá trị kiểu MessageKey dùng trong Notify. |
| `reference` | Giá trị kiểu Guid dùng trong Notify. |

Lời gọi chính: `Notifications.Add`, `Messages.Get`.

### ReminderWorker.ExecuteAsync(stoppingToken)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Vòng lặp nền của ReminderWorker: tôn trọng Workers:Enabled, chạy RunOnce theo lịch, xử lý lỗi và dừng theo cancellation token.

| Đầu vào | Ý nghĩa |
|---|---|
| `stoppingToken` | Token dừng host; mọi vòng lặp/delay phải tôn trọng token này. |

Lời gọi chính: `logger.LogError`, `Task.Delay`, `TimeSpan.FromSeconds`.
