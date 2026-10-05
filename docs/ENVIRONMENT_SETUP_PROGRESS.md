# Bước 1 — Chuẩn bị và kiểm chứng môi trường HRCMS

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
