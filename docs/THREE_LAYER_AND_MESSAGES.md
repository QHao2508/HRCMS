# Ba layer và message dùng chung

```text
Horse_BackEnd (API) → HorseClub.BLL (Business) → HorseClub.DAL (Data)
```

| Project | Trách nhiệm |
| --- | --- |
| `Horse_BackEnd` | Khai báo route, binding request, middleware/filter, cấu hình DI và startup. Endpoint gọi workflow trong BLL. |
| `HorseClub.BLL` | Workflows, kiểm quyền/phạm vi, nghiệp vụ, services, DTO, options, calendar, background workers và catalog message. |
| `HorseClub.DAL` | Entity, enum dữ liệu, EF DbContext, provider và migrations SQLite/SQL Server. |

DAL không tham chiếu BLL/API; BLL không tham chiếu API. API cấu hình DbContext và truyền dependency vào workflow. BLL dùng ASP.NET Core framework cho authentication, file/result và host integration. Các namespace `Horse_BackEnd.*` hiện có được giữ để tránh thay đổi migration/model contract; tên project/assembly thể hiện layer.

Không đặt truy vấn EF hoặc xử lý nghiệp vụ trong endpoint. Thêm operation vào workflow/service BLL; API chỉ định route, policy và chuyển request. Schema và persistence configuration đặt trong DAL. Không đổi schema hay tạo migration chỉ vì di chuyển file.

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

Bộ test kiểm các workflow hiện có, dependencies không ngược layer, đầy đủ khóa catalog và định dạng email/message động. Database đang chạy không cần reset sau refactor này.
