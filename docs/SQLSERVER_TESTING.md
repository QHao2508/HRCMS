# Bước 3 — Workflow và concurrency trên SQL Server

Ngày kiểm chứng: 04/10/2026. Frontend được hoãn theo người dùng. Các tests dùng behavior hiện tại, không triển khai các policy đề xuất P01–P10.

## Kết quả

| Kiểm tra local | Kết quả |
| --- | --- |
| Release build | 0 lỗi, 0 cảnh báo |
| SQLite regression | 27 pass, 7 SQL-only skip, 0 fail |
| SQL Server Express `.\SQLEXPRESS` | 34 pass, 0 skip, 0 fail |
| Tests mới | 13: 6 workflow/persistence dùng được cả hai provider, 7 SQL-only |

21 trường hợp cũ được chạy lại trên SQL Server thật cùng 13 trường hợp mới. TRX cuối cùng:

- `TestResults/step3/sqlite/Admin_DESKTOP-FMHCKTV_2026-10-04_13_09_46_net10.0.trx`.
- `TestResults/step3/sqlserver/Admin_DESKTOP-FMHCKTV_2026-10-04_13_09_50_net10.0.trx`.

TestResults bị Git ignore. CI đã thêm job SQL Server container và upload TRX của hai job; chưa chạy job mới trên GitHub trong phiên này, chưa commit/push.

## Cách chạy lại

Cần .NET 10 SDK và SQL Server riêng dùng cho test. Tài khoản test cần quyền tạo/drop database test.

```powershell
dotnet build HorseClub.slnx --configuration Release

# Mặc định: SQLite; các SQL-only tests báo skip.
dotnet test HorseClub.slnx --configuration Release --no-build --no-restore --logger trx --results-directory TestResults/step3/sqlite

# SQL Server: cả suite đổi provider, các SQL-only tests được bật.
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
try {
    dotnet test HorseClub.slnx --configuration Release --no-build --no-restore --logger trx --results-directory TestResults/step3/sqlserver
} finally {
    Remove-Item Env:HRCMS_TEST_SQLSERVER
}
```

Không đặt connection string SQL authentication có mật khẩu thật vào file tracked. CI dùng credential chỉ cho container test dùng một lần, không phải secret triển khai.

## Cách cô lập dữ liệu

ClubFactory tạo tên database mới `HRCMS_Test_<GUID>` cho mỗi fixture. InitialCatalog trong connection string đầu vào bị thay bằng tên mới: tests không migrate hay delete database người dùng chỉ định. DisposeAsync kiểm tên database chính xác và pattern trước khi EnsureDeleted. Replica chia sẻ database/key của owner nhưng không sở hữu quyền cleanup; hai host có WriteGate độc lập. Nếu test process bị kill/crash trước cleanup, database test có thể còn lại; phải kiểm tên cụ thể trước khi dọn thủ công.

Fixture SQL Server thay DI DbContext rõ ràng để không vô tình chạy SQLite khi Minimal API chọn provider trước callback test config. Replica tests kiểm cả hai context IsSqlServer, cùng tên database và gate khác nhau.

Migrations được áp bằng startup của backend lên database mới. Không reset `HorseClub_IntegrationTest` của bước 1 hoặc database sử dụng thực tế. Keys/uploads/mail test không dùng dữ liệu cá nhân thật; workers bị tắt trong fixture.

## Đã kiểm những gì?

| Nhóm | Bằng chứng test mới |
| --- | --- |
| Kho | Nhiều request tranh xuất: stock không âm, stock khớp movement, không dư audit/low-stock notification |
| Replenishment | Approval không tăng stock, không review hai lần, receipt mới tăng stock |
| Assignment | Giữ history, một Trainer Active, Trainer cũ mất quyền sửa, creator plan vẫn còn |
| Medical lock giữa buổi | Không bỏ result, IssueReported + incident một lần, notification tới Rider |
| Clearance | Đóng restriction/injury Active/treatment theo policy hiện tại; plan Paused không tự resume |
| Version | DbContext có version cũ không ghi đè stock đã commit |
| Approval giữa hai host | Một Horse/Measurement/audit/notification dù requests cạnh tranh |
| Occupancy giữa hai host | Một occupancy mở cho stall, audit chỉ cho transaction thành công |
| Kho giữa hai host | Một withdrawal thắng, transaction thua rollback, movement/stock/notification nhất quán |
| Assignment giữa hai host | Một active Trainer và history khớp số thao tác đã commit |
| Restriction-vs-start | Start trước lock có thể thành công nhưng result bị ghi issue; lock trước start thì start bị chặn; conflict có thể được caller gửi lại |
| Rollback do DB failure | Trigger trên DB test làm INSERT measurement thất bại: approval không để Horse/Measurement/audit/notification hay ReviewedBy dở dang |
| Foreign key | SQL Server từ chối Session có Horse/Plan không tồn tại, không lưu orphan |

Các race tests gửi request đồng thời qua hai TestServer host độc lập và assert kết quả dữ liệu sau commit. Chúng không phải load test với nhiều replica trên server production.

## Lỗi thực tế đã sửa

Trước sửa, SQL Server suite có 5 lỗi ở các ca race: stock, occupancy, approval, restriction-vs-start và assignment. TRX ghi SQL error **1205** nằm trong exception EF bọc, API trả 500 `internal_error`.

SQL Server rollback transaction được chọn làm deadlock victim. Xem [Microsoft: deadlocks guide](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-deadlocks-guide?view=sql-server-ver17).

Đã thêm DAL `DatabaseFailures.IsDeadlock` để nhận diện 1205 trong inner exception chain, và API ErrorMiddleware trả:

```json
{
  "type": "urn:horseclub:error:transaction_conflict",
  "title": "transaction_conflict",
  "status": 409,
  "detail": "Record changed. Reload and retry.",
  "referenceId": null,
  "traceId": "<request trace>"
}
```

Nội dung detail thực tế lấy từ catalog `RecordChangedReloadAndRetry` (không hardcode nội dung mới). Giữ transaction Serializable và response buffering. Không tự replay POST có side effects trong middleware. Caller nhận 409 cần reload trạng thái trước khi gửi thao tác mới. Deadlock có thể xảy ra ở request cạnh tranh dù dữ liệu vẫn được transaction bảo vệ; thay đổi này xử lý response, không loại bỏ mọi deadlock.

Không bật EnableRetryOnFailure đơn lẻ với transaction thủ công: nếu cần retry phía server, phải thiết kế replay toàn unit và side effects/idempotency theo [EF Core connection resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency).

## CI và giới hạn còn lại

CI dùng SQL Server 2022 Developer container theo [hướng dẫn container chính thức](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/quickstart-install-docker?view=sql-server-ver17), health check qua mssql-tools18, sau đó chạy toàn suite với HRCMS_TEST_SQLSERVER. Môi trường SQL Server local đã kiểm; job Linux container chỉ mới cấu hình, cần quan sát lần CI chạy tiếp theo.

Chưa bao phủ: worker SMTP/reminder với nhiều replica, stress/load/deadlock rate, mọi API/role/field, file retention/rollback, full duration overlap, filtered unique constraints cho occupancy/assignment, và các policy mới. Chưa thay schema/migration hoặc đổi clearance/draft/privacy policy trong bước này.

Bước tiếp theo của backend: hoàn thiện response DTO/OpenAPI và sửa các lỗi dữ liệu/quyền đã xác định theo phạm vi được thống nhất; không cần chờ frontend.
