# Kiến trúc HorseClub hiện tại

Backend ASP.NET Core .NET 10 được chia thành ba project. Frontend React/Vite/JavaScript nằm tại repository riêng `E:\SWP391\HorseClub-frontend\HRCMS-Frontend` và đã nối API.

```text
Frontend pages → frontend services/Axios → HTTP
Horse_BackEnd/Endpoints → HorseClub.BLL/<Module>/*Service
                       → HorseClub.DAL/Data/ClubDbContext → Azure SQL
```

| Project/folder | Trách nhiệm |
|---|---|
| `Horse_BackEnd/Endpoints` | Route, binding, authorization metadata và HTTP response. Giữ Minimal API. |
| `Horse_BackEnd/Infrastructure` | Middleware, bearer-token protocol, adapter identity/upload, OpenAPI và Data Protection. |
| `HorseClub.BLL/Auth`, `Horses`, `Training`, `Medical`, `Care`, `Inventory`, `Attachments`, `Reporting`, `Metadata` | Scoped service cho từng module; dependencies được inject qua constructor. Không còn lớp Workflow đứng giữa endpoint và service. |
| `HorseClub.BLL/Contracts/<Module>` | Mỗi DTO trong một file, giữ namespace `HorseClub.BLL.Contracts` và JSON contract. |
| `HorseClub.BLL/Common` | Quyền/phạm vi, validation, events, calendar, pagination, options và storage. |
| `HorseClub.BLL/Workers` | Email, reminder và mail sender. |
| `HorseClub.DAL/Entities`, `Enums` | Mỗi entity/enum trong một file. Namespace tương ứng tên project/folder. |
| `HorseClub.DAL/Data/Configurations` | Mapping chung và index phục vụ truy vấn. |
| `HorseClub.DAL/Data/Migrations/SqlServer` | Bốn migration SQL Server và model snapshot. |

Hướng project reference: API → BLL → DAL. DAL không tham chiếu BLL/API; BLL không tham chiếu API. BLL dùng EF DbContext trực tiếp, chưa thêm repository bao quanh từng DbSet. Các thư viện Identity/Data Protection/hosting vẫn được BLL dùng; HTTP request/result được xử lý ở API qua adapter. `OperationResult` là kết quả ứng dụng, API chuyển sang HTTP bằng `OperationResultMapper`.

Một số response vẫn chứa entity để giữ tương thích frontend; chưa chuyển toàn bộ contract thành DTO độc lập. Không thêm tầng hoặc đổi payload chỉ để tổ chức lại file.

Database runtime: SQL Server/Azure SQL duy nhất. Azure target là `hrcms.database.windows.net / HRCMS`, mật khẩu trong User Secrets/secret store; frontend không giữ SQL credentials. AutoMigrate runtime vẫn false.

Request ghi có transaction Serializable và response buffering để rollback dữ liệu/file khi lỗi. Không còn semaphore dùng chung cho mọi request và worker. SQL transaction, concurrency token, unique constraint và application lock của worker bảo vệ tính nhất quán giữa các host. Xung đột trả 409; không tự replay POST. Refresh token là POST chỉ đọc, được đánh dấu để không mở transaction ghi.

Dashboard tổng hợp bảy chỉ số trong một SQL command, phân trang dùng AsNoTracking và thứ tự ổn định. Reports chỉ lấy các cột cần dùng, có giới hạn số bản ghi. Reminder lấy người nhận theo batch thay vì query trong từng vòng lặp. Migration `QueryPerformanceIndexes` bổ sung index composite và thay index đơn trùng tiền tố; không đổi bảng/cột/dữ liệu nghiệp vụ.

Xem [giải thích từng folder/file](PROJECT_STRUCTURE_EXPLAINED.md), [thay đổi và kiểm chứng hiệu năng](STRUCTURE_AND_PERFORMANCE.md), [quy tắc layer/message](THREE_LAYER_AND_MESSAGES.md) và [Azure SQL](AZURE_SQL_SETUP.md).
