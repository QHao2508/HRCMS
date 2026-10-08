# Backend HorseClub

**Cấu hình Azure hiện tại — 07/10/2026:** appsettings chung trỏ tới `hrcms.database.windows.net / HRCMS`; mật khẩu được cấp qua User Secrets. Xem [AZURE_SQL_SETUP.md](AZURE_SQL_SETUP.md). Mô tả SQL Express bên dưới thuộc lần kiểm local trước đó; không dùng kết nối đó để triển khai schema Azure.

**BE-004 — 05/10/2026:** đã hoàn thiện Rider Plan scope, Plan session pagination, outcome history và precision speed theo cấu hình. Không có migration mới ở BE-004; xem [hướng dẫn contract và demo](BE-004_TRAINING.md). Các kết quả trước BE-004 bên dưới là lịch sử.

**BE-003 — 05/10/2026:** intake đã hỗ trợ partial Draft nullable; Submit/Approval kiểm đủ dữ liệu và ảnh/chứng nhận; Manager edit giới hạn thông tin quản lý có audit. Cần áp migration PartialRegistrationDraft đúng provider trước chạy binary mới trên DB cũ. Xem [hướng dẫn và lưu ý rollback BE-003](BE-003_INTAKE_AND_ASSIGNMENT.md); các con số sau bước 6 bên dưới là kết quả lịch sử.

Backend ASP.NET Core .NET 10 đã triển khai các API cho tài khoản, hồ sơ/phân công ngựa, huấn luyện, y tế, chăm sóc/chuồng, tồn kho, thông báo, audit và báo cáo. Đây là mã nguồn backend; chưa có frontend, kết nối SMTP thật hay triển khai production.

## Chạy local

Cần .NET SDK 10 và SQL Server. API mặc định dùng SqlServer. ConnectionStrings:SqlServer trong appsettings.json kết nối trực tiếp tới database HRCMS tại .\SQLEXPRESS bằng Windows Authentication (không chứa mật khẩu). Cả profile http/https chọn SqlServer, không ghi đè connection string. Nếu instance/database local khác, sửa appsettings.json. AutoMigrate=false ở cả cấu hình chung/Development: schema phải được chuẩn bị trước; startup không tự tạo/cập nhật schema. User Secrets/biến môi trường vẫn có thể ghi đè theo thứ tự cấu hình .NET; đã bỏ connection string và AutoMigrate đã thêm vào User Secrets trên máy Khoa. Khi triển khai, cấp kết nối phù hợp qua secrets/biến môi trường, không dùng database local test cho production. SQL Server là provider duy nhất; không có fallback sang database khác. Chạy từ thư mục repository:

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx --configuration Release
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx --configuration Release
dotnet run --project Horse_BackEnd --launch-profile http
```

API local theo launch profile: `http://localhost:5299`. Health check: `/health`. Trong Development, OpenAPI ở `/openapi/v1.json`, Swagger UI ở `/swagger`. UI dùng tài liệu OpenAPI hiện có, không sinh contract riêng.

Chạy `dotnet run --project Horse_BackEnd --launch-profile http`, rồi mở `http://localhost:5299/swagger`. Trong Authentication, mở POST `/api/auth/login` → Try it out → nhập email/password tài khoản demo → Execute. Copy `accessToken` trong response, bấm Authorize, dán token nguyên bản (không thêm `Bearer`), rồi Authorize → Close. Gọi GET `/api/auth/me` để kiểm tra role trước khi thử API BE02. Đổi role bằng Logout trong hộp Authorize rồi đăng nhập và dán token người tiếp theo. Swagger không lưu token qua lần tải lại trang; chỉ bật trong Development. Upload dùng Try it out, chọn type và file. Request thử vẫn ghi vào database đang kết nối, nên dùng môi trường demo riêng.

AutoMigrate=false: áp SQL Server migrations bằng dotnet ef hoặc script triển khai trước khi chạy. Upload, key và email local nằm dưới `Horse_BackEnd/App_Data`, đã bị Git ignore. Không xóa thư mục này nếu cần giữ dữ liệu/key; mất key khiến token và dữ liệu CCCD đã bảo vệ không đọc được.

## Tạo Club Manager đầu tiên

Không có tài khoản hay mật khẩu mặc định. Đặt thông tin riêng bằng user-secrets, thay placeholder trước khi chạy:

```powershell
dotnet user-secrets set "Bootstrap:ManagerEmail" "<email của bạn>" --project Horse_BackEnd
dotnet user-secrets set "Bootstrap:ManagerPassword" "<mật khẩu riêng đạt chính sách Security>" --project Horse_BackEnd
```

Có thể đặt `Bootstrap:ManagerUserName`, `Bootstrap:ManagerFirstName`, `Bootstrap:ManagerLastName`. Cấu hình email/password phải cùng có hoặc cùng không có. Bootstrap chỉ tạo Manager khi chưa có Manager, không reset mật khẩu tài khoản đã tồn tại. Sau khi tạo, xóa cấu hình bootstrap khỏi môi trường triển khai. Không commit user-secrets.

Manager tạo HeadTrainer/Trainer/WorkRider/Veterinarian/Groom bằng `POST /api/staff`; staff nhận invitation và tự chọn mật khẩu bằng `/api/auth/accept-invitation`. Endpoint staff không tạo Owner hoặc Manager khác.

## Đăng nhập và email

Owner tạo account qua `/api/auth/register`, nhận mã email và xác thực bằng `/api/auth/verify-email`. Password/ConfirmPassword phải khớp. Chính sách độ dài, OTP, resend, attempts và lockout nằm trong `Security`.

Development hiện chọn Gmail SMTP thật; địa chỉ gửi/App Password nằm trong User Secrets. `DevelopmentFile` chỉ dùng khi chủ động cấu hình cho test. Không có endpoint trả OTP. Đăng ký/reset/kích hoạt staff đều dùng OTP 6 chữ số, mặc định hết hạn sau 10 phút và chỉ dùng một lần; 5 lần sai vô hiệu hóa OTP. OTP lưu hash trong database. Email outbox được lưu transaction với yêu cầu; body chứa OTP được xóa sau khi gửi thành công. Xem [hướng dẫn test OTP](EMAIL_OTP_TEST_GUIDE.md).

Login trả `tokenType`, `accessToken`, `expiresIn`, `refreshToken`. Gửi `Authorization: Bearer <accessToken>` cho API protected. Đây là opaque bearer token do ASP.NET Core Data Protection bảo vệ, không phải JWT. Dùng `/api/auth/refresh` để nhận token mới. Logout, reset password hoặc disable staff làm token của account đó mất hiệu lực. Refresh token có thể dùng đến hết hạn hoặc bị thu hồi cùng security stamp; chưa triển khai rotation cho từng thiết bị.

Production cấu hình `Email:Provider=Smtp`, `Email:FromAddress`, `Email:FromName` và `Email:Smtp:Host/Port/Username/Password/EnableSsl`. Gmail yêu cầu TLS. Xem [lệnh dotnet user-secrets](GMAIL_USER_SECRETS.md). Worker có retry/backoff. Email delivery là at-least-once; lỗi SMTP có thể làm retry gửi trùng cùng mã, nhưng mã vẫn chỉ dùng một lần.

## Enum và cấu hình

Các role, state, intensity, training type, injury type/severity, preventive care type, attachment type, notification/audit type đều là enum, có ID số ổn định trong database. JSON request/response dùng tên enum, ví dụ `HorseOwner`, `Veterinarian`, `PendingReview`, `InProgress`, `Fit`, `Heavy`, `Sprint`. Không gửi số như `3` thay cho tên. Enum bắt buộc phải có trong request; enum sai hoặc field JSON không tồn tại bị từ chối.

Frontend lấy tên enum từ `GET /api/metadata/enums`, tránh tự copy một danh sách thứ hai. Giống ngựa, pedigree, tên chuồng, phase/goal và nội dung hướng dẫn là dữ liệu nhập và lưu trong database; không cố định thành danh sách trong mã nguồn. Giới hạn định dạng/validation là hợp đồng dữ liệu; các giới hạn vận hành được đưa vào options/config.

| Nhóm cấu hình | Ý nghĩa |
| --- | --- |
| `Security` | Password, TTL OTP/reset/invite, attempts/resend/lockout, token lifetime, auth rate limit, RequireNationalId/NationalIdDigits |
| `Storage` | Path, MaxFileBytes, MaxAttachmentsPerRecord, RequestsPerMinute |
| `Business` | TimeZoneId, page/report limits, schedule grace, StartEarlyMinutes, overdue threshold |
| `Workers` | Enabled, polling, batch sizes, retry attempts và EmailSendTimeoutSeconds |
| `Database` | Provider và AutoMigrate |
| `Email` | Delivery mode và SMTP |
| `Cors:Origins` | Danh sách origin frontend được phép; không AllowAnyOrigin |
| `DataProtection:Path` | Nơi giữ key ổn định, cần bảo vệ và backup |

Options có validation khi khởi động. Biến môi trường dùng `__`, ví dụ `Database__Provider=SqlServer`, `Email__Password=...`. `Security:RequireNationalId` mặc định false; khi bật, registration cần NationalId đúng định dạng cấu hình. NationalId được bảo vệ bằng Data Protection và không xuất hiện trong account/staff API.

Timestamps lưu UTC; các ngày nghiệp vụ, KPI hôm nay và bucket report dùng `Business:TimeZoneId`, mặc định `Asia/Ho_Chi_Minh`. Plan/boarding/measurement dùng `yyyy-MM-dd`; session/examination dùng timestamp ISO 8601 có offset. Distance: mét; time: giây; speed: m/s do server tính; height: cm; weight/portion: kg.

## API chính

Tất cả đường dẫn dưới `/api`, trừ `/health` và OpenAPI. List hỗ trợ `page`, `pageSize`; kết quả `{ items, page, pageSize, total }`. Chi tiết query/DTO xem OpenAPI.

| Module | Endpoint và thao tác |
| --- | --- |
| Auth | POST `/auth/register`, `/login`, `/verify-email`, `/resend-verification`, `/forgot-password`, `/reset-password`, `/accept-invitation`, `/refresh`, `/logout`; GET `/auth/me` |
| Staff | GET/POST `/staff`; GET `/staff/directory` (tên/role, không contact); PUT `/staff/{id}/active` |
| Intake | GET/POST `/registrations`; GET/PUT `/registrations/{id}`; POST `/{id}/submit`, `/{id}/review`, `/{id}/cancel` |
| Intake documents | GET/POST `/registrations/{id}/attachments`; GET `/registrations/{id}/attachments/{attachmentId}`; POST multipart `file`, `type`, certificateNumber/issueDate/expiryDate tùy chọn |
| Horse | GET `/horses`, `/horses/{id}`, `/horses/{id}/photo`; POST `/{id}/assignments`, `/{id}/measurements`, `/{id}/archive`; GET `/{id}/measurements` |
| Template | GET/POST `/training/templates`; PUT `/training/templates/{id}`; POST `/{id}/archive` |
| Plan | GET/POST `/training/plans`; GET/PUT `/training/plans/{id}`; GET `/{id}/history`; PUT `/{id}/status`; POST `/{id}/sessions` |
| Session | GET `/training/sessions`, `/training/sessions/{id}`; PUT `/{id}`; POST `/{id}/assign`, `/{id}/start`, `/{id}/results`, `/{id}/skip`, `/{id}/evaluation` |
| Medical | GET/POST `/horses/{id}/medical/records`, `/injuries`, `/restrictions`, `/treatments`, `/follow-ups`, `/preventive-care`; GET `/summary`; PUT `/records/{recordId}` tạo correction giữ bản cũ; POST `/preventive-care/{careId}/complete` |
| Care | GET/POST `/care/tasks`; POST `/care/tasks/{id}/record`; GET/POST `/care/incidents`; POST `/care/incidents/{id}/resolve`; GET/POST `/care/incidents/{id}/photos` và GET `/{photoId}` |
| Stable | GET/POST `/care/stables`, `/care/stalls`; POST `/care/stalls/{id}/occupancy`, `/vacate`, `/cleaned` |
| Inventory | GET/POST `/inventory`; GET/POST `/inventory/{id}/movements`; POST `/{id}/archive`; GET/POST `/inventory/replenishments`; POST `/replenishments/{id}/review` |
| Common | GET `/notifications`, `/audit`, `/reports`, `/dashboard`, `/metadata/enums`; POST `/notifications/{id}/read` |

## Quy tắc nghiệp vụ thực thi

- Account registration tách khỏi intake. Intake bắt đầu Draft; submit yêu cầu HorsePhoto và Certificate. PendingReview → RevisionRequired → resubmit hoặc Approved. Chỉ approval mới tạo Horse Profile; lặp/concurrent approval không tạo ngựa trùng.
- Owner chỉ đề xuất HeadTrainer/Groom/Veterinarian. Manager xác nhận official staff; assigned HeadTrainer chọn Trainer. Assignment history được giữ, thay assignment cũ bằng bản ghi kết thúc. Chưa hỗ trợ assignment có ngày bắt đầu trong tương lai.
- Trainer hiện đang được phân công mới được chỉnh training; WorkRider chỉ thao tác session được giao. Plan/session có state transition guards, snapshot history; không tạo Session khi Plan paused/completed/archived.
- Không có Rider: session là Planned. Có Rider: Assigned. Start → InProgress → Completed/IssueReported hoặc Skipped. Server chống result/evaluation trùng và xung đột Rider/horse đang tập.
- Guard kiểm tra restriction tại thời điểm dự kiến khi create/edit/assign và tại thời điểm thực tế khi start. Isolated chặn mọi training; Injured chặn Heavy; TrainingLock chặn Heavy, BlockAllTraining chặn mọi cường độ, MaxIntensity/MaxDistance/NoSprint chặn hoạt động không tương thích. Đây là policy ban đầu cần Club nghiệm thu; không coi TrainingLock là cấm mọi hoạt động.
- Restriction reason là hướng dẫn vận hành được training roles đọc; chẩn đoán/clinical notes chỉ Vet được phân công đọc. Manager/Owner nhận health summary thay vì clinical records. Owner khai báo sức khỏe không tự cấp Fit status; Horse mới bắt đầu Monitoring.
- Vet follow-up với clearance yêu cầu Fit; clearance đóng toàn bộ restrictions/injuries/treatments hiện còn mở của horse, giữ history và notify Trainer. Plan paused không tự resume. Khi cần clearance từng injury riêng, mở issue mở rộng policy trước khi dùng thực tế.
- Lock phát sinh khi session đang tập: notify Rider. Result vẫn được nhận để bảo toàn quan sát, nhưng vi phạm hiện tại tạo IssueReported/incident. Trainer không có thao tác override y tế.
- Groom chỉ ghi task được giao. Vet giao treatment/ice bath; Manager giao daily care. Feeding có approved và actual portion. Treatment instructions không xuất cho Owner/training roles.
- Stable không được nhận hai ngựa cùng lúc; di chuyển/archiving giữ occupancy history. Inventory stock chỉ đổi qua movement, không âm; approval replenishment chưa tăng stock, receipt thực tế mới tăng.
- Reports lọc ownership/assignment, date/group; WorkRider chỉ session mình, Groom chỉ care task mình. Clinical report chỉ Vet. Report lớn bị từ chối thay vì âm thầm cắt số liệu.
- Reminders cho session overdue, follow-up và preventive care; notification recipient scope, audit decisions, archive core records.

Lỗi nghiệp vụ có HTTP status và JSON `title`, `detail`, `referenceId`, `traceId`; thường 400 validation, 401 auth, 403 scope/role, 404 record, 409 state/concurrency/medical block, 413 report/upload limit, 429 throttling. Verify/reset sai mã trả kết quả `verified=false`/`changed=false` để vẫn commit bộ đếm attempts.

## SQL Server và triển khai

Chỉ có context/migrations SQL Server. Đặt connection string qua secrets hoặc biến môi trường. Ví dụ tạo migration SQL để review trước deployment:

SQL tạo schema đã có sẵn tại [database.sql](database.sql); xem [hướng dẫn chạy bằng SSMS](DATABASE_SQL.md).

```powershell
dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --output horseclub-sqlserver.sql
```

Apply bằng công cụ triển khai phù hợp; application production mặc định `Database:AutoMigrate=false`. Khi cần `dotnet ef database update`, truyền connection thật qua `--connection` trong môi trường bảo mật; design-time factory chỉ có cấu hình local phục vụ scaffold, không đọc deployment secrets. Có thể dùng deployment script để tránh đưa credentials vào command history.

Write requests dùng process gate và serializable transaction; response success chỉ gửi sau commit. SQL Server bảo vệ concurrency giữa replica bằng transaction/concurrency token và worker application locks. Kiểm load trên môi trường triển khai trước khi scale-out.

Production cần HTTPS, CORS origin thật, SMTP thật, persistence/backup cho database/uploads/key và giới hạn quyền filesystem. Ngoài Development mặc định yêu cầu key encryption Certificate hoặc WindowsDpapi trước startup; chưa tích hợp vault/KMS. NationalId protection cần giữ key ring và certificate/identity tương ứng. Upload kiểm signature/extension/size, force download/nosniff và cleanup file chưa commit; chưa có antivirus hoặc kiểm chứng nội dung đầy đủ. Xem [cấu hình keys và runbook khôi phục](STORAGE_AND_RECOVERY.md). Email worker gửi trong transaction/gate; SMTP chậm có thể tăng latency write, cần tách worker và cơ chế lease nếu triển khai tải cao.

## Kiểm thử và phạm vi còn lại

Test suite dùng WebApplicationFactory và SQL Server, database riêng cho từng fixture. Bắt buộc đặt HRCMS_TEST_SQLSERVER; fixture chỉ tạo/migrate/drop database HRCMS_Test_<GUID>. Xem [SQLSERVER_TESTING.md](SQLSERVER_TESTING.md) và [FRONTEND_READINESS.md](archive/FRONTEND_READINESS.md).

Chưa có export PDF/Excel báo cáo, 3D injury map, global search nâng cao, dữ liệu master chuẩn hóa qua UI quản trị, account multi-club, scheduling duration/overlap đầy đủ, cancellation/rejection policy ngoài draft/revision, device-level token rotation hoặc frontend. Flow 5 thi đấu không nằm trong scope. Các phần này cần issue riêng; không được mô tả là đã triển khai.
