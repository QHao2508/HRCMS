# Backend HorseClub

Backend ASP.NET Core .NET 10 đã triển khai các API cho tài khoản, hồ sơ/phân công ngựa, huấn luyện, y tế, chăm sóc/chuồng, tồn kho, thông báo, audit và báo cáo. Đây là mã nguồn backend; chưa có frontend, kết nối SMTP thật hay triển khai production.

## Chạy local

Cần .NET SDK 10. SQLite local không cần cài database server. Chạy từ thư mục repository:

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx --configuration Release
dotnet test HorseClub.slnx --configuration Release
dotnet run --project Horse_BackEnd --launch-profile http
```

API local theo launch profile: `http://localhost:5299`. Health check: `/health`. OpenAPI trong Development: `/openapi/v1.json`. Không có Swagger UI trong bản này; import OpenAPI vào Postman hoặc dùng file `Horse_BackEnd.http`.

Development tự áp dụng migration. Database, upload, key và email local nằm dưới `Horse_BackEnd/App_Data`, đã bị Git ignore. Không xóa thư mục này nếu cần giữ dữ liệu/key; mất key khiến token và dữ liệu CCCD đã bảo vệ không đọc được.

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

Local dùng `Email:Mode=DevelopmentFile`; worker ghi email thử nghiệm vào `App_Data/mail/*.eml`. Đây là đường thử local, không phải gửi email thật. Không có endpoint trả OTP. OTP sai bị đếm attempts; OTP đúng dùng một lần. Staff invitation/reset code có entropy cao, lưu hash trong database. Email outbox được lưu transaction với yêu cầu; body chứa code được xóa sau khi gửi thành công. Cần hạn chế quyền truy cập DB/mail/key và có chính sách dọn email local.

Login trả `tokenType`, `accessToken`, `expiresIn`, `refreshToken`. Gửi `Authorization: Bearer <accessToken>` cho API protected. Đây là opaque bearer token do ASP.NET Core Data Protection bảo vệ, không phải JWT. Dùng `/api/auth/refresh` để nhận token mới. Logout, reset password hoặc disable staff làm token của account đó mất hiệu lực. Refresh token có thể dùng đến hết hạn hoặc bị thu hồi cùng security stamp; chưa triển khai rotation cho từng thiết bị.

Production cấu hình `Email:Mode=Smtp`, `Email:Host`, `Email:Port`, `Email:From`, `Email:Username`, `Email:Password`. SMTP dùng TLS. Worker có retry/backoff. Email delivery là at-least-once; lỗi SMTP có thể làm retry gửi trùng cùng mã, nhưng mã vẫn chỉ dùng một lần.

## Enum và cấu hình

Các role, state, intensity, training type, injury type/severity, preventive care type, attachment type, notification/audit type đều là enum, có ID số ổn định trong database. JSON request/response dùng tên enum, ví dụ `HorseOwner`, `Veterinarian`, `PendingReview`, `InProgress`, `Fit`, `Heavy`, `Sprint`. Không gửi số như `3` thay cho tên. Enum bắt buộc phải có trong request; enum sai hoặc field JSON không tồn tại bị từ chối.

Frontend lấy tên enum từ `GET /api/metadata/enums`, tránh tự copy một danh sách thứ hai. Giống ngựa, pedigree, tên chuồng, phase/goal và nội dung hướng dẫn là dữ liệu nhập và lưu trong database; không cố định thành danh sách trong mã nguồn. Giới hạn định dạng/validation là hợp đồng dữ liệu; các giới hạn vận hành được đưa vào options/config.

| Nhóm cấu hình | Ý nghĩa |
| --- | --- |
| `Security` | Password, TTL OTP/reset/invite, attempts/resend/lockout, token lifetime, auth rate limit, RequireNationalId/NationalIdDigits |
| `Storage` | Path, MaxFileBytes, MaxAttachmentsPerRecord, RequestsPerMinute |
| `Business` | TimeZoneId, page/report limits, schedule grace, StartEarlyMinutes, overdue threshold |
| `Workers` | Enabled, polling, batch sizes và retry attempts |
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

Có context/migration riêng cho SQLite và SQL Server. Không dùng SQLite migration để triển khai SQL Server. Đặt connection string bằng secrets hoặc biến môi trường. Ví dụ tạo migration SQL để review trước deployment:

SQL tạo schema đã có sẵn tại [database.sql](database.sql); xem [hướng dẫn chạy bằng SSMS](DATABASE_SQL.md).

```powershell
dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --output horseclub-sqlserver.sql
```

Apply bằng công cụ triển khai phù hợp; application production mặc định `Database:AutoMigrate=false`. Khi cần `dotnet ef database update`, truyền connection thật qua `--connection` trong môi trường bảo mật; design-time factory chỉ có cấu hình local phục vụ scaffold, không đọc deployment secrets. Có thể dùng deployment script để tránh đưa credentials vào command history.

SQLite local dùng một instance và persistent disk. Write requests dùng gate trong process và serializable transaction; response success chỉ gửi sau commit. Background worker cũng phối hợp gate/transaction. SQL Server hỗ trợ transaction/concurrency token nhưng cần integration/load test trên server thật trước production hoặc chạy nhiều replicas. SQLite chưa phù hợp scale-out.

Production cần HTTPS, CORS origin thật, SMTP thật, persistence/backup cho database/uploads/key, giới hạn quyền filesystem và key encryption phù hợp nền tảng. Key mặc định lưu local, chưa tích hợp vault/KMS; national ID protection phụ thuộc vào việc bảo vệ key. Upload hiện kiểm signature/extension/size và force download; chưa có antivirus hoặc kiểm chứng nội dung đầy đủ. Email worker gửi trong transaction/gate; SMTP chậm có thể tăng latency write, cần tách worker và cơ chế lease nếu triển khai tải cao.

## Kiểm thử và phạm vi còn lại

Test suite dùng WebApplicationFactory và SQLite thật theo từng fixture, kiểm state/ownership/medical guards/OTP/lockout/refresh/CCCD/uploads/stock/stall/concurrent approval, OpenAPI và configurable limits. SQL Server chỉ được kiểm model/migration/sinh SQL; chưa xác minh với database server thật. CI chạy build và integration tests.

Chưa có export PDF/Excel báo cáo, 3D injury map, global search nâng cao, dữ liệu master chuẩn hóa qua UI quản trị, account multi-club, scheduling duration/overlap đầy đủ, cancellation/rejection policy ngoài draft/revision, device-level token rotation hoặc frontend. Flow 5 thi đấu không nằm trong scope. Các phần này cần issue riêng; không được mô tả là đã triển khai.
