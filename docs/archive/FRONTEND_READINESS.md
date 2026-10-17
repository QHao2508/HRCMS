> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../README.md).

# Kiểm tra backend trước tích hợp frontend — 07/10/2026

Backend đã chuyển sang SQL Server duy nhất và kiểm tra trên SQL Server Express local. Frontend nằm ở repository riêng; chưa sửa hoặc kiểm UI trong lần rà soát này.

**Cập nhật tích hợp frontend cùng ngày:** repository `E:/SWP391/HorseClub-frontend/HRCMS-Frontend` đã được rà soát và cấu hình Vite proxy tới API local 5299. Build/lint và 150 test frontend pass; kiểm service live qua SQL Server tạm pass, gồm intake/approval/assignment/training theo 7 role. Chromium headless đã kiểm 59 lượt mở màn hình; database fixture đã được dọn. Danh sách màn hình và hướng dẫn nằm trong `docs/API_INTEGRATION.md` của frontend. Các thay đổi chưa commit/push.

**Azure SQL cùng ngày:** backend hiện kết nối `hrcms.database.windows.net / HRCMS` bằng SQL Login trong User Secrets. Đã áp ba migrations và xác nhận 31 bảng nghiệp vụ. Health trực tiếp/proxy frontend, Swagger và smoke service login/me/dashboard/horses pass. Xem [AZURE_SQL_SETUP.md](../AZURE_SQL_SETUP.md). Các thông tin SQL Express dưới đây mô tả lần kiểm local trước đó.

## Ba layer

| Layer | Trách nhiệm và kết quả rà soát |
| --- | --- |
| Horse_BackEnd | Route/binding, HTTP validation/error/auth/CORS, OpenAPI, DI/startup và vòng đời transaction HTTP. Các endpoint chuyển request sang workflow/service BLL; không chứa truy vấn `db.*` hoặc SaveChanges. |
| HorseClub.BLL | Workflows, quyền/phạm vi, nghiệp vụ, DTO, services, workers, catalog message. ClubStartup sở hữu bootstrap Manager và kiểm kết nối cho health. |
| HorseClub.DAL | Entities/enums, EF DbContext, model/persistence, SQL Server migrations, nhận diện deadlock và database application locks. Không tham chiếu BLL/API/ASP.NET Core. |

Tham chiếu project: API → BLL → DAL. Không có tham chiếu ngược. BLL dùng EF qua DAL DbContext theo kiến trúc hiện tại; endpoint được inject DbContext để chuyển dependency cho workflow, không thực hiện truy vấn. TransactionMiddleware tại API chỉ điều phối request/response, transaction và file cleanup. Namespace Horse_BackEnd.* trong BLL/DAL được giữ để tương thích migration/model; assembly xác định layer.

Một số response vẫn dùng entity DAL, gồm Version/CreatedAt. Đây là hợp đồng đang có và đã được OpenAPI mô tả. Khi đổi entity cần kiểm ảnh hưởng frontend; việc chuẩn hóa toàn bộ response thành DTO riêng là một bước riêng, chưa đổi shape JSON trong lần này.

## Database và cấu hình

- Đã bỏ package/provider/context/design factory, migrations, recovery utility và test riêng cho database cũ. Runtime, tools và CI chỉ dùng SQL Server.
- Giữ nguyên ba SQL Server migrations, schema và UTC ticks trong các cột bigint. Test HasPendingModelChanges xác nhận model không lệch migrations.
- AutoMigrate=false ở cấu hình app/Development. Test bật migration trên database tạm riêng.
- Instance local `.\SQLEXPRESS` kết nối được; HRCMS đã được tạo và áp ba migrations hiện có. Bootstrap sử dụng cấu hình Manager sẵn có trong User Secrets; không thêm mật khẩu mặc định vào source.
- Đã sửa User Secrets local ConnectionStrings:SqlServer, Database:Provider và Database:AutoMigrate để runtime dùng HRCMS thay vì database cũ thiếu cột outbox. Không migrate/drop database cũ.
- Backup SQL Server dùng runbook [STORAGE_AND_RECOVERY.md](../STORAGE_AND_RECOVERY.md), kèm uploads và key ring cùng thời điểm.

## Kết quả kiểm tra

| Kiểm tra | Kết quả |
| --- | --- |
| Build toàn solution Release | 0 error, 0 warning |
| Toàn suite trên SQL Server | 85 pass, 0 fail, 0 skip |
| Layer dependencies và catalog message | Pass |
| Model/migration deployment SQL | Pass, không có pending model changes |
| Workflow, quyền/scope, FK/rollback, concurrency hai host | Pass trong suite hiện có |
| Workers, upload/storage, native SQL backup/restore | Pass trong suite hiện có |
| CORS preflight | Cho origin localhost:5173, Authorization; từ chối origin ngoài cấu hình |
| Runtime health/Swagger trên HRCMS | 200; health trả healthy |
| OpenAPI | 73 paths, 96 operations, 139 schemas, 87 operations có Bearer security, không có response schema rỗng |

TRX: `TestResults/sqlserver-only-final/` (Git ignored). Contract hiện tại: [openapi.current.json](../contracts/openapi.current.json), [api-inventory.current.csv](../contracts/api-inventory.current.csv), [openapi-audit.current.json](../contracts/openapi-audit.current.json). CI đã đổi sang SQL Server; chưa chạy lại GitHub CI cho các thay đổi local này.

## Bước tiếp theo: nối frontend

1. Chạy backend bằng `dotnet run --project Horse_BackEnd --configuration Release --launch-profile http`.
2. Dùng API base URL `http://localhost:5299`; frontend origin mặc định Development là `http://localhost:5173`. Nếu port/origin khác, cập nhật Cors:Origins.
3. Tích hợp login → me → metadata/enums, gửi opaque accessToken qua Authorization: Bearer. Làm refresh/logout theo [API_CONTRACT.md](../API_CONTRACT.md).
4. Dựa trên [CONTRACT_AND_SCREEN_MAP.md](CONTRACT_AND_SCREEN_MAP.md) để nối module theo role và entity scope.
5. Kiểm paging `{ items, page, pageSize, total }`, enum chuỗi, date/offset, null, 204 không body, lỗi có/không có JSON, conflict 409 và multipart upload.
6. Chạy UI E2E riêng với API thật. Bộ test backend hiện có chưa chứng minh toàn bộ UI, SMTP ngoài hệ thống, production/load hay khôi phục đầy đủ sang máy khác.

Hướng dẫn chạy test SQL Server: [SQLSERVER_TESTING.md](../SQLSERVER_TESTING.md).
