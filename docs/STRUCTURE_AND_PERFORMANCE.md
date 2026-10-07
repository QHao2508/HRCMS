# Cấu trúc và hiệu năng — 07/10/2026

## Kết quả thay đổi

Giữ ASP.NET Core Minimal API và ba project API → BLL → DAL. Endpoint gọi scoped service theo module, dependency được inject qua constructor. Không còn folder Workflows/Services/Infrastructure chung trong BLL: nghiệp vụ nằm ở Auth, Horses, Training, Medical, Care, Inventory, Attachments, Reporting, Metadata; hỗ trợ nằm ở Common và Workers.

Entity, enum và DTO được tách theo file; namespace DAL phản ánh project thay vì Horse_BackEnd. DTO giữ namespace HorseClub.BLL.Contracts để validation và JSON contract tương thích. Mapping chuyển sang Data/Configurations. DependencyInjection.cs đăng ký BLL; Program.cs ghép runtime API.

HTTP request/result được xử lý ở API: BearerSessionHandler, HttpCurrentIdentity, UploadRequestAdapter và OperationResultMapper. Service không nhận HttpRequest/HttpContext hoặc trả IResult. BLL vẫn dùng thư viện Identity/Data Protection/hosting. Một số response vẫn chứa entity DAL để giữ contract; chưa đổi toàn bộ payload sang DTO độc lập.

## Các tối ưu có kiểm chứng

| Trước | Sau | Kiểm chứng |
|---|---|---|
| Dashboard gửi bảy truy vấn đếm nối tiếp | Bảy chỉ số trong một SQL command | Test interceptor xác nhận tổng hai SQL readers cho request: một identity lookup và một aggregate. Kiểm dữ liệu owner và trường hợp rỗng. |
| Semaphore chung cho mọi request ghi và cả worker SMTP | Request độc lập không chờ semaphore; SQL Server điều phối transaction/xung đột | Test giữ SMTP chờ trong cùng host, thao tác tạo stable vẫn hoàn thành trước khi email được thả. |
| Query danh sách entity có tracking | PageReader dùng AsNoTracking | Test xác nhận returned Horse không vào ChangeTracker. |
| Sort phân trang có thể hòa giá trị | Thêm Id làm thứ tự phụ cho các danh sách | Giữ pagination ổn định khi cùng tên/ngày/thời điểm. |
| Report lấy toàn bộ entity session/care | Projection chỉ lấy cột dùng trong response/aggregate | Tests report/privacy vẫn pass; giới hạn bản ghi được giữ. |
| Reminder query người nhận cho từng record | Query người nhận theo batch và lookup trong memory | Các tests reminder, recipient eligibility, retry/rollback và multi-host vẫn pass. |
| POST refresh mở transaction Serializable dù không ghi dữ liệu | Metadata ReadOnlyOperation bỏ transaction ghi cho refresh | Tests auth/refresh/revocation vẫn pass. |
| SQL commands của worker được log ở Info trong development | Log command ở Warning, có thể bật lại khi điều tra | Giảm log lặp; không bật sensitive data logging. |

Không tự replay POST khi deadlock/concurrency conflict. Transaction ghi vẫn Serializable và response buffering, upload cleanup, auth-attempt commit được giữ. Worker vẫn có SQL application lock. Email vẫn giữ transaction riêng khi gửi để giữ quy tắc giao nhận hiện có; SMTP có thể gây chờ trên các record liên quan, dù không còn khóa cả process.

## SQL indexes và Azure

Migration `20261007115208_QueryPerformanceIndexes` tạo 14 composite index phục vụ ownership/assignment, dashboard, lịch và lịch sử; thay 10 index đơn trùng tiền tố. Không đổi bảng/cột hoặc reset dữ liệu nghiệp vụ. Mapping/snapshot không có pending model changes.

Migration đã áp thành công lên `hrcms.database.windows.net / HRCMS`: 31 bảng nghiệp vụ + __EFMigrationsHistory, tổng bốn migrations. Lần kết nối đầu báo database tạm thời không sẵn sàng; lần thử lại preflight/schema/verification thành công. Runtime AutoMigrate vẫn false. Script SQL idempotent và script deploy đã cập nhật.

Index phù hợp các query hiện tại; lợi ích thực tế trên dữ liệu lớn cần theo dõi execution plan, logical reads và tần suất ghi. Chưa đo production load/P95/P99 hoặc cam kết một tỷ lệ tăng tốc trên mọi API.

## Kiểm chứng bản bàn giao

- Baseline trước refactor: 85/85 backend tests pass.
- Bản cuối: Release build không warning/error; 89/89 backend tests pass, không skip. Database test là SQL Express tạm HRCMS_Test_<GUID>, không dùng Azure HRCMS để chạy fixture.
- Bốn test bổ sung: service không lộ kiểu HTTP, dashboard query count/scoping, SMTP không chặn write độc lập và pagination không tracking.
- Tests giữ approval một ngựa, không double-book stall, stock không âm, assignment history, restriction-vs-start, rollback, worker delivery/reminder nhiều host.
- Frontend: 150/150 tests pass; live service smoke qua Vite proxy pass login/me/metadata/dashboard/horses/logout với API mới và Azure SQL.
- OpenAPI giữ 96 operations; tests xác nhận success schema/status, bearer security và multipart upload.

TRX nằm trong TestResults/structure-performance-baseline và TestResults/structure-performance-verified. Các kết quả local trong TestResults không commit. Đây là integration/regression tests và đo số command, chưa phải benchmark tải production.

## Đọc cấu trúc mới

Đọc [ARCHITECTURE.md](ARCHITECTURE.md), [PROJECT_STRUCTURE_EXPLAINED.md](PROJECT_STRUCTURE_EXPLAINED.md), rồi theo một endpoint xuống service → DTO/entity → DbContext/Configurations.

Các thay đổi đang ở working tree local; chưa commit/push. API development đang chạy trên localhost:5299 và frontend dùng proxy localhost:5173. Bước phát triển tiếp theo có thể dựa trên route/JSON contract hiện có.
