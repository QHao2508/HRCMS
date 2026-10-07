# Bước 5 — Email worker và reminder

Ngày: 04/10/2026. Đã sửa và kiểm thử backend; chưa kết nối tài khoản SMTP bên ngoài hoặc triển khai production.

## Những thay đổi đã hoàn thành

- Worker đọc `WorkerOptions.Enabled` lúc khởi chạy, tránh việc cấu hình test/host được áp sau thời điểm đăng ký service. Khi false, hai vòng nền kết thúc mà không xử lý queue; `RunOnce` là điểm chạy một lượt cho kiểm thử, không phải API public.
- SQL Server dùng `sp_getapplock` riêng cho email/reminder, owner Transaction và timeout 0. Replica chưa lấy được khóa bỏ qua lượt, thử lại ở poll sau; khóa tự giải phóng khi commit/rollback.
- Email commit từng message. Hủy ở message tiếp theo không rollback trạng thái của email đã gửi/lưu trước đó.
- Thêm `Workers:EmailSendTimeoutSeconds`, mặc định 30, hợp lệ 1–300. Timeout được ghi như một delivery failure; shutdown cancellation giữ message đang xử lý chưa gửi thành công để lần chạy sau xử lý lại.
- Retry giữ backoff 2, 4, 8, 16, 32, 60 phút, sau đó tối đa 60 phút; dừng khi đạt MaxEmailAttempts. Log chỉ có messageId, attempt và loại exception, không ghi recipient/body/mật khẩu SMTP. Đạt giới hạn retry có log Error.
- Challenge tạo email kèm ChallengeId và ExpiresAt. Queue hết hạn hoặc challenge đã consumed/bị thay thế được đánh dấu DiscardedAt và xóa Body, không gọi sender. Email gửi thành công cũng xóa Body và NextAttemptAt.
- Reminder lọc người nhận active, đúng role và assignment active trước giới hạn batch. Hồ sơ chưa có Vet không bị đánh dấu ReminderSent; khi gán Vet hợp lệ, lượt sau gửi được. Các query có thứ tự ổn định theo ngày/id; hồ sơ không có người nhận không chặn batch phía sau.
- Nhắc session chỉ cho Assigned thuộc plan Active, ngựa chưa archive, quá OverdueAfterMinutes; gửi Rider active và Trainer hiện được gán. Nhắc preventive/follow-up chỉ cho Vet phù hợp. Notification và marker được commit cùng transaction.
- Khi workers enabled, startup từ chối SMTP thiếu Host/From hoặc DevelopmentFile ngoài Development. Khi disabled, có thể chạy API với SMTP chưa cấu hình.

## Cấu hình vận hành

Development mặc định ghi email ở `Horse_BackEnd/App_Data/mail/*.eml`; các file này chứa mã xác thực, chỉ dùng môi trường phát triển và không đưa vào Git. Database xóa Body sau delivery nhưng file .eml vẫn tồn tại cho việc đọc thủ công.

Production/Staging dùng `Email:Provider=Smtp`, `Email:FromAddress`, `Email:FromName` và `Email:Smtp:Host/Port/Username/Password/EnableSsl` qua cấu hình môi trường hoặc secret store. Gmail yêu cầu TLS, port mặc định 587. Không đưa mật khẩu thật vào appsettings/README. Xem [cấu hình dotnet user-secrets](GMAIL_USER_SECRETS.md).

| Tùy chọn Workers | Mặc định | Tác dụng |
| --- | --- | --- |
| Enabled | true | Bật cả email/reminder; restart host sau khi đổi |
| EmailPollSeconds | 5 | Chu kỳ lấy email |
| EmailBatchSize | 10 | Tối đa số email gửi/discard mỗi lượt |
| MaxEmailAttempts | 8 | Giới hạn delivery failures |
| EmailSendTimeoutSeconds | 30 | Thời gian gửi tối đa cho sender hỗ trợ cancellation |
| ReminderPollSeconds | 60 | Chu kỳ reminder |
| ReminderBatchSize | 100 | Tối đa record mỗi nhóm preventive/session/treatment; không phải tổng notification |

Ngày preventive/follow-up dùng `Business:TimeZoneId`, mặc định Asia/Ho_Chi_Minh. Session overdue dùng thời điểm UTC và `Business:OverdueAfterMinutes`, mặc định 60; đúng biên 60 phút chưa được tính quá hạn.

## Migration

Có migration `WorkerDeliveryReliability` cho SQL Server. Thêm ba cột nullable ChallengeId/ExpiresAt/DiscardedAt, FK từ email tới challenge và indexes cho queue/reminder. Không có bảng nghiệp vụ mới. [database.sql](database.sql) đã được sinh lại bằng script SQL Server idempotent.

Trước khi chạy binary mới trên database cũ, áp migration đúng provider bằng quy trình [DATABASE_SQL.md](DATABASE_SQL.md). Các test tạo DB riêng, chạy cả migration mới và kiểm downgrade/upgrade trên DB thử nghiệm có dữ liệu; chưa áp migration vào database cá nhân bước 1 hoặc database production.

Email tồn tại từ trước migration có ChallengeId/ExpiresAt null, vẫn giữ khả năng gửi lại theo behavior cũ. Worker không đoán hạn mã từ nội dung Body. Trước khi bật delivery trên hệ thống có queue cũ, người vận hành cần xem xét/discard queue mã cũ theo chính sách thực tế. Không tự xóa dữ liệu cũ trong migration.

## Kiểm chứng

Thêm 14 trường hợp: 11 dùng được trên cả hai provider và 3 SQL-only.

Kết quả hồi quy cuối: **SQL Server Express 51 pass/0 fail/0 skip**. Build Release 0 warning/0 error. TRX nằm trong thư mục Git-ignored `TestResults/step5/sqlserver`.

- Retry đúng hạn, restart dùng state đã lưu, không gửi lại email SentAt khác null; bỏ qua future/exhausted và xử lý batch bounded.
- Timeout, shutdown cancellation và message đã commit trước đó; mã expired/superseded không tới sender.
- DevelopmentFile tạo đúng một .eml; SMTP thiếu cấu hình bị chặn lúc startup; disabled hosted workers không lấy queue.
- Reminder theo ngày Việt Nam, chờ Vet active, không starvation, loại hồ sơ completed/archived/paused/future và không lặp sau khi tạo worker mới.
- Hai host SQL Server có gate riêng: một email đang gửi không bị replica gửi lại; reminder có đúng một notification cho mỗi recipient/event.
- SQL trigger fault làm toàn bộ notification/ReminderSent rollback; lượt sau phục hồi. Migration giữ email/user cũ khi nâng cấp.

Chạy worker tests hoặc cả suite từ repository root:

```powershell
dotnet test HorseClub.slnx -c Release --filter 'FullyQualifiedName~WorkerTests|FullyQualifiedName~WorkerSqlServerTests'
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx -c Release --logger trx --results-directory TestResults/step5/sqlserver
Remove-Item Env:HRCMS_TEST_SQLSERVER
```

Factory tạo/xóa database HRCMS_Test_<guid> riêng; không dùng database nêu trong InitialCatalog của connection string. CI hiện tại tự chạy các test mới; chưa xác nhận run GitHub sau thay đổi.

## Giới hạn còn lại

SMTP delivery vẫn có thể lặp: server mail nhận message nhưng process dừng hoặc DB commit thất bại trước khi SentAt được lưu. Khóa SQL ngăn hai worker cùng xử lý bình thường, không cung cấp exactly-once cho SMTP. Mã xác thực vẫn chỉ dùng một lần; provider có idempotency key hoặc hệ thống delivery riêng là công việc tiếp theo nếu cần bảo đảm cao hơn.

Email vẫn gửi trong transaction/process gate. Timeout giới hạn mỗi lần gửi, nhưng SMTP chậm vẫn chặn write trong process; chưa đo load hoặc tách claim/lease và delivery khỏi API. Lượt worker đang chạy không tự replay ngay khi DB lỗi; vòng nền ghi log và poll lại sau 10 giây. Cần theo dõi queue age, exhausted attempts và log worker trước khi tăng tải.

Preventive dùng ReminderSent; session/follow-up dedupe theo notification đã tồn tại của event/type, giữ semantics hiện có: không gửi lại cho nhân sự được gán mới sau khi event đã được thông báo. Xóa notification trực tiếp có thể làm session/follow-up được nhắc lại. Chưa thêm recurrence, delivery receipts hoặc audit bảng reminder riêng.

Chưa kiểm SMTP nhà cung cấp thật, TLS/credential/network bên ngoài, khả năng tới inbox, kill process thật hay nhiều replica dưới tải. Các lỗi send/timeout/cancellation dùng sender có kiểm soát; DevelopmentFile dùng sender thật trên filesystem tạm.

Tham khảo cơ chế khóa: [Microsoft sp_getapplock](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17). Giới hạn gửi lại sau crash tương tự rủi ro được giải thích trong [duplicate delivery của Microsoft](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection); dự án hiện dùng SMTP, không dùng Azure Service Bus.
