# Bước 1 — Chuẩn bị và kiểm chứng môi trường HRCMS

**Đổi tên database dự án — 05/10/2026:** theo yêu cầu Khoa, database đang dùng HorseClub_BE02_Test đã đổi tên thành **HRCMS**; giữ 5 account demo và 3 migration. HorseClub_IntegrationTest không có dữ liệu nghiệp vụ (chỉ history migration), đã xóa. Cả hai được native SQL backup COPY_ONLY/CHECKSUM và VERIFYONLY trước thao tác. appsettings.json, công cụ seed, credentials local và hướng dẫn demo đã cập nhật HRCMS. Tên database trong các bản ghi cũ bên dưới là lịch sử; không cần tạo lại hai database cũ.

**Kết nối trực tiếp từ appsettings.json — cập nhật 05/10/2026:** theo yêu cầu Khoa, ConnectionStrings:SqlServer đã chuyển vào appsettings.json, Windows Authentication tới .\SQLEXPRESS/HorseClub_BE02_Test (database đã có). Bỏ connection string trong launch profiles và hai giá trị ConnectionStrings:SqlServer/Database:AutoMigrate đã thêm vào User Secrets. AutoMigrate=false cả chung/Development để không tự cập nhật schema hiện có. Debug build 0 warning/error; host tạm 5313 đọc cấu hình này, Health/Swagger 200, không chạy migration; đã dừng. Log: TestResults/SqlServerDefault/appsettings-connection.log. Các mô tả cấu hình trước đó dưới đây là lịch sử.

**Xác nhận SQL Server mặc định — 05/10/2026:** appsettings chung/Development, fallback startup và cả profile http/https đã chọn SqlServer. Connection vẫn ở User Secrets, database local test `HorseClub_BE02_Test`. Đã chạy đúng profile http tại 5299: Health/Swagger 200, EmailWorker sinh `SELECT TOP(1) ... FROM [EmailMessages]`, không có LIMIT/PRAGMA trong log kiểm chứng. Host tạm đã dừng. SQLite chỉ còn là lựa chọn tường minh cho fixture/utility, không là mặc định API. Fixture test đã thay DI cho cả hai provider để không kế thừa database local từ User Secrets; 27 ca Intake/Training/layer-message pass sau sửa; toàn suite SQL Server 84 pass/1 SQLite-only skip, 0 fail. Log kiểm chứng: TestResults/SqlServerDefault/api-sqlserver.log; TRX: TestResults/SqlServerDefault/sqlserver (Git ignored).

**Cấu hình test Swagger SQL Server — 05/10/2026:** User Secrets trên máy Khoa đã chọn `Database:Provider=SqlServer`, `Database:AutoMigrate=true`, kết nối Windows Authentication đến `.\SQLEXPRESS`, database mới `HorseClub_BE02_Test`. Database có 31 bảng nghiệp vụ và đủ InitialSqlServer, WorkerDeliveryReliability, PartialRegistrationDraft. Host kiểm chứng tạm tại cổng 5310 trả `/health` 200 healthy và `/swagger/index.html` 200; đã dừng sau khi kiểm tra. Không tạo tài khoản ứng dụng trong lần kiểm chứng, không thay đổi database `HorseClub` hoặc `HorseClub_IntegrationTest`. Khởi động lại profile http để dùng cấu hình này tại cổng 5299; kết nối nằm trong User Secrets, không được commit vào repo. Đây là smoke test kết nối, không thay cho suite nghiệp vụ SQL Server.

**BE-004 — 05/10/2026:** hoàn thiện training scope/pagination/history/speed precision, thêm 12 ca test. Build Release 0 warning/error; SQL Server 84 pass/1 skip, SQLite 72 pass/13 skip. Không đổi schema hoặc medical clearance. Xem [bàn giao BE-004](BE-004_TRAINING.md); chờ reviewer BE03 và tích hợp frontend.

**BE-003 — 05/10/2026:** đã triển khai partial Draft nullable, validate submit/approval, giới hạn Manager edit với before/after audit và kiểm quyền official assignment. Có migration PartialRegistrationDraft hai provider, SQL/OpenAPI đã xuất lại. Release build 0 warning/error; suite 73 ca: SQL Server 72 pass/1 skip, SQLite 60 pass/13 skip. Xem [bàn giao BE-003](BE-003_INTAKE_AND_ASSIGNMENT.md). Chờ reviewer BE01; không migrate DB cá nhân hoặc tự làm BE-004/BE-005.

**T02 — 05/10/2026:** đã đọc đầy đủ nguồn Word hợp nhất V1/V2 và lập [policy, traceability và gap backend](T02_POLICY_BASELINE.md). P01–P10 có quyết định/quy ước core; Khoa xác nhận clearance theo vấn đề được chọn, partial Draft và archive đóng vận hành giữ history. T02 hoàn thành phần chốt policy, chờ reviewer nhóm; chưa thay đổi API/schema hoặc tự thực hiện task module khác. Các ghi chú “chưa có nguồn Word” và “toàn bộ policy chờ xác nhận” ở phần lịch sử dưới đây phản ánh ngày 04/10.

Ngày thực hiện: 04/10/2026.

## SDK và Visual Studio

.NET SDK là bộ công cụ restore/build/test ứng dụng .NET. Visual Studio là IDE; bản Insiders là một kênh phát hành IDE, không phải tên của .NET SDK. Có thể làm dự án bằng dotnet CLI mà không cần Visual Studio Insiders.

Máy hiện có SDK **10.0.401**, phù hợp TargetFramework **net10.0** của dự án. Không cần cài thêm SDK để thực hiện bước này.

## Kết quả đã kiểm chứng

| Kiểm tra | Kết quả |
| --- | --- |
| `dotnet --list-sdks` | 10.0.401 tại C:\Program Files\dotnet\sdk |
| Restore solution | Thành công cả 4 project |
| Restore local tool | dotnet-ef 10.0.12 thành công |
| Release build | Thành công, 0 warning, 0 error |
| Bộ test hiện tại | 21 pass, 0 fail, 0 skip |
| SQL Server | SQL Server Express đang chạy tại `.\SQLEXPRESS` |
| Kết nối Windows Authentication | Thành công |
| Database test riêng | `HorseClub_IntegrationTest` |
| Schema | 31 bảng nghiệp vụ, 33 FK, migration `20261003125915_InitialSqlServer` |
| Backend kết nối database test | `/health` HTTP 200, `{"status":"healthy"}` |
| OpenAPI | HTTP 200, 73 path, 53 schema, có route login |

Các tests hiện tại chạy workflow trên SQLite; test SQL Server hiện có kiểm model/migration generation. Không coi 21 test pass là bằng chứng toàn bộ workflow chạy đúng trên SQL Server.

Lần chạy tests trong sandbox có 15 fail do quyền Windows Event Log và tài nguyên môi trường. Chạy lại đúng cùng bộ test ngoài sandbox đạt 21/21; không thay đổi code để làm tests pass.

Kết quả test thành công lưu ở `TestResults/Admin_DESKTOP-FMHCKTV_2026-10-04_12_44_03_net10.0.trx`, được Git ignore.

## Các lệnh đã dùng

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx --configuration Release --no-restore
dotnet test HorseClub.slnx --configuration Release --no-build --no-restore --logger trx --results-directory TestResults

sqlcmd -S .\SQLEXPRESS -E -C -b -Q "IF DB_ID(N'HorseClub_IntegrationTest') IS NULL CREATE DATABASE [HorseClub_IntegrationTest];"
sqlcmd -S .\SQLEXPRESS -d HorseClub_IntegrationTest -E -C -b -i docs/database.sql
```

Khi chạy lại script schema, phải chọn đúng database test và chỉ dùng database thuộc migration của dự án. Không áp schema tùy ý lên database khác.

Smoke test chạy backend tạm thời tại `http://localhost:5301`, dùng biến môi trường riêng trong process để chọn SqlServer, connection string database test, AutoMigrate=false và Workers=false. Không sửa appsettings/User Secrets. Bootstrap Manager bị tắt bằng command-line override; database test không có tài khoản ứng dụng được tạo bởi bước này. Keys test ở `Horse_BackEnd/App_Data/environment-check/keys` được Git ignore. Backend tạm đã được dừng sau khi kiểm tra.

## Bước tiếp theo

**Cập nhật bước 2:** đã lập [bản đồ contract và API](CONTRACT_AND_SCREEN_MAP.md), [các quyết định nghiệp vụ](POLICY_DECISIONS.md) và inventory OpenAPI đầy đủ 96 operations. Đối chiếu frontend được hoãn theo người dùng vì bộ phận frontend chưa hoàn thành. Kiểm thử/sửa metadata backend tiếp tục theo behavior hiện có; không coi các policy đề xuất là đã được xác nhận.

**Cập nhật bước 3:** đã chạy bộ test mở rộng: SQL Server 34 pass; SQLite 27 pass/7 SQL-only skip. Đã sửa SQL Server deadlock 1205 bị EF bọc làm API trả 500; nay trả 409 transaction_conflict. Xem [SQLSERVER_TESTING.md](SQLSERVER_TESTING.md) cho phạm vi, hướng dẫn và giới hạn. Database test mỗi fixture được tạo/dọn riêng, không reset database bước 1.

1. Lập bảng contract theo màn hình và action từ nguồn yêu cầu thật (T01/T05); thông tin từ code là baseline.
2. Chốt policy medical lock/clearance, draft, quyền clinical, archive và lịch duration/overlap (T02).
3. Xây bộ kiểm thử SQL Server cho workflow/concurrency trên database test riêng (T39), sau đó xử lý các vấn đề ưu tiên đã nêu trong bản phân tích.

**Cập nhật bước 4:** hoàn thiện OpenAPI/DTO và bổ sung 3 test contract; xem [API_CONTRACT.md](API_CONTRACT.md). Bước tiếp theo tập trung worker email/reminder và độ tin cậy vận hành backend; đối chiếu frontend tiếp tục hoãn.

**Cập nhật bước 5:** đã sửa/kiểm worker email và reminder, có 14 test mới và migration WorkerDeliveryReliability cho cả hai provider. Xem [WORKER_RELIABILITY.md](WORKER_RELIABILITY.md). Chưa áp migration vào database cá nhân hoặc kết nối SMTP bên ngoài; SQL test dùng database riêng.

**Cập nhật bước 6:** hoàn thiện upload cleanup theo transaction, giới hạn đường dẫn storage, cấu hình mã hóa key ring và CLI backup/verify/restore SQLite. Native SQL backup được restore vào DB test mới và đọc qua API. Toàn suite 61 ca: SQL Server 60 pass/1 skip, SQLite 48 pass/13 skip; build Release 0 warning/error. Kết quả TRX ở TestResults/step6/sqlserver và TestResults/step6/sqlite (Git ignored). Xem [STORAGE_AND_RECOVERY.md](STORAGE_AND_RECOVERY.md) cho hướng dẫn và giới hạn; chưa cấu hình certificate production, retention hoặc offsite backup scheduler.

Chưa cấu hình tài khoản Manager cá nhân hoặc bật SMTP thực. Các thay đổi backend tới nay gồm xử lý deadlock, typed response/metadata và validation examination lồng nhau; không áp policy mới.
