# Kiểm thử SQL Server

SQL Server là database duy nhất của runtime và tests. Cần .NET 10 SDK và SQL Server có quyền tạo/drop database test. Không có test database mặc định thay thế nếu thiếu kết nối.

```powershell
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx --configuration Release --logger trx --results-directory TestResults/sqlserver-only
Remove-Item Env:HRCMS_TEST_SQLSERVER
```

ClubFactory thay InitialCatalog bằng HRCMS_Test_<GUID>, tự migrate và chỉ drop database fixture sở hữu. Replica dùng cùng database nhưng không sở hữu cleanup. Tests không migrate/drop HRCMS hoặc database trong connection string đầu vào. Nếu tiến trình bị kill, kiểm tên database trước khi dọn thủ công.

CI build Release và chạy toàn suite trên SQL Server 2022 container. Windows local dùng SQL Server Express. Kết quả kiểm hiện tại nằm trong [FRONTEND_READINESS.md](archive/FRONTEND_READINESS.md); phần dưới mô tả phạm vi các test đã có và lịch sử sửa lỗi concurrency.

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
