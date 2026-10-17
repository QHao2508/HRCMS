# Ba layer và message dùng chung

API → BLL → DAL. API giữ Minimal API, HTTP binding/response/middleware; BLL chứa scoped services theo module; DAL giữ entity/enum/EF mapping/migrations. Endpoint không truy vấn database trực tiếp. DependencyInjection trong BLL đăng ký service; Program API ghép runtime.

Không còn Workflow ở giữa endpoint và service. Endpoint inject I…Service; BLL inject repository/query interface và IUnitOfWork, không dùng EF Core/DbContext/IQueryable. Repository chia theo nghiệp vụ thay vì tạo generic repository cho từng bảng. BLL vẫn dùng Identity/Data Protection/hosting; các kiểu HttpRequest/HttpContext/IResult không nằm trong signature service. Identity và upload được truyền qua interface/input riêng; API chuyển OperationResult thành HTTP.

Entity/enum/DTO tách thành file riêng. Namespace DAL là HorseClub.DAL.Entities/Enums/Data; DTO giữ HorseClub.BLL.Contracts. Đổi vị trí/namespace không đổi JSON hoặc bảng. SQL Server là database duy nhất.

Transaction ghi nằm ở API, dùng Serializable và response buffering. Không giữ semaphore chung giữa request và SMTP. Worker dùng SQL application lock; conflict trả 409 và caller reload. Refresh là POST chỉ đọc. Xem ARCHITECTURE.md và STRUCTURE_AND_PERFORMANCE.md.

## Message dùng chung

- `HorseClub.BLL/Messages/MessageKey.cs`: enum khóa thông báo ổn định.
- `HorseClub.BLL/Messages/messages.en.json`: nội dung, mỗi thông báo dùng một template.
- `HorseClub.BLL/Messages/Messages.cs`: đọc catalog nhúng trong assembly và format bằng invariant culture.

```csharp
Ensure.That(condition, Messages.Get(MessageKey.PermissionDenied), 403, "forbidden");
events.Notify(user.Id, NotificationType.SessionAssigned,
    MessageKey.ATrainingSessionWasAssignedToYou, session.Id);
var detail = Messages.Get(MessageKey.StaffMustBeAnActive, Role.Trainer);
```

`ClubEvents.Notify/Managers/HorseStaff` nhận `MessageKey`, tránh truyền literal trực tiếp. Nội dung lỗi, notification, reminder, thông báo phản hồi và email subject/body dùng catalog. Placeholder dùng `{0}`, `{1}` hoặc format `{2:O}`; không nối câu trong business code. Validation từ DataAnnotations được đưa về message dùng chung thay vì trả câu mặc định từ framework.

Thêm message: thêm khóa enum với giá trị mới, thêm đúng tên khóa vào JSON và gọi `Messages.Get`/`ClubEvents`. Không tái sử dụng số enum đã có cho nghĩa khác. Catalog được nhúng lúc build; thay nội dung cần rebuild/deploy. Notification/email đã lưu trong database giữ nội dung tại lúc tạo. Mã lỗi API, route, tên cấu hình, MIME và log kỹ thuật là định danh kỹ thuật, không phải nội dung thông báo người dùng.

## Migration và kiểm tra

```powershell
dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --output horseclub-sqlserver.sql
dotnet test HorseClub.slnx --configuration Release
```

Bộ test kiểm workflow, dependencies, service interfaces, transaction rollback, outbox claim đồng thời và SignalR với token hiện có. Không reset database; cần áp dụng migration bổ sung cho outbox trước khi chạy phiên bản mới, kể cả khi dispatcher đang tắt. Xem REPOSITORY_SIGNALR_IMPLEMENTATION.md.
