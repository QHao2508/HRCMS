# Chức năng từng folder

Backend ba layer: API → BLL → DAL → Azure SQL. Blob/SMTP là dịch vụ ngoài, được gọi từ BLL.

| Folder | Chức năng |
|---|---|
| `.config` | Manifest local .NET tool để dotnet tool restore cài đúng phiên bản dotnet-ef. |
| `.github` | Workflow CI và template issue/PR; dùng bởi GitHub dù không được import trong ứng dụng. |
| `.github/ISSUE_TEMPLATE` | Mẫu báo lỗi/tính năng giúp ghi rõ trigger, kỳ vọng và bằng chứng. |
| `.github/workflows` | CI restore/build/test với SQL Server và các kiểm tra của repository. |
| `HorseClub.BLL` | Layer nghiệp vụ: phối hợp quy tắc, phân quyền theo dữ liệu, DAL và các dịch vụ ngoài; không trả IResult/HttpRequest. |
| `HorseClub.BLL/Attachments` | Kiểm tệp intake, metadata và quyền upload/download ảnh/chứng nhận; đọc ảnh ngựa mới nhất. |
| `HorseClub.BLL/Auth` | Đăng ký/đăng nhập, OTP Verify/Reset/Invite, nhân viên, session stamp và dọn đăng ký chưa xác thực quá hạn. |
| `HorseClub.BLL/Care` | Chuồng, công việc chăm sóc và sự cố; phân công, ghi nhận và duyệt đúng vai trò. |
| `HorseClub.BLL/Common` | Các quy tắc dùng chung: quyền theo ngựa, user hiện tại, lịch múi giờ, audit, phân trang và kết quả nghiệp vụ. |
| `HorseClub.BLL/Common/Options` | Kiểu cấu hình với DataAnnotations và giá trị mặc định. User Secrets/environment ghi đè cấu hình khi khởi động. |
| `HorseClub.BLL/Common/Storage` | Abstraction upload, Azure private blob, transaction rollback cleanup và logo branding public có tên cố định. |
| `HorseClub.BLL/Contracts` | DTO request/response và enum kết quả API theo module; không đưa password hash/credential vào response. |
| `HorseClub.BLL/Contracts/Attachments` | Request/response DTO của module Attachments; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Auth` | Request/response DTO của module Auth; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Care` | Request/response DTO của module Care; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Common` | Request/response DTO của module Common; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Horses` | Request/response DTO của module Horses; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Inventory` | Request/response DTO của module Inventory; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Medical` | Request/response DTO của module Medical; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Reporting` | Request/response DTO của module Reporting; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Contracts/Training` | Request/response DTO của module Training; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend. |
| `HorseClub.BLL/Horses` | Intake bản nháp/gửi/duyệt, hồ sơ ngựa chính thức, số đo và lịch sử phân công nhân sự. |
| `HorseClub.BLL/Inventory` | Vật tư, nhập/xuất/điều chỉnh kho và yêu cầu bổ sung có duyệt. |
| `HorseClub.BLL/Medical` | Khám, chấn thương, hạn chế vận động, điều trị, tái khám và xác nhận đủ điều kiện. |
| `HorseClub.BLL/Messages` | MessageKey và JSON embedded: thông báo nghiệp vụ/email, định dạng tham số tập trung. |
| `HorseClub.BLL/Metadata` | Cung cấp danh sách enum hợp lệ cho hợp đồng frontend/backend. |
| `HorseClub.BLL/Reporting` | Aggregate dashboard, thông báo, audit và báo cáo có phạm vi/giới hạn dữ liệu. |
| `HorseClub.BLL/Training` | Giáo án → kế hoạch cá nhân hóa → buổi tập → kết quả/đánh giá; kiểm trạng thái và hạn chế sức khỏe. |
| `HorseClub.BLL/Workers` | Hosted worker gửi email, nhắc lịch và xóa đăng ký hết hạn; dùng SQL lock và transaction để không chạy trùng giữa instance. |
| `HorseClub.BLL/Workers/Templates` | HTML email OTP embedded. Renderer encode dữ liệu động; không chứa OTP cố định của người dùng. |
| `HorseClub.DAL` | Layer dữ liệu: entity, enum domain, EF Core, schema và truy vấn SQL Server; không tham chiếu ngược API/BLL. |
| `HorseClub.DAL/Data` | DbContext SQL Server, design-time factory, nhận diện lỗi DB và application lock cho worker. |
| `HorseClub.DAL/Data/Configurations` | Quan hệ restrictive, chuyển UTC ticks, concurrency token và index tối ưu truy vấn. |
| `HorseClub.DAL/Data/Migrations` | Lịch sử schema EF Core; bắt buộc giữ migration đã áp dụng để nâng cấp database đúng thứ tự. |
| `HorseClub.DAL/Data/Migrations/SqlServer` | Migration SQL Server và ModelSnapshot sinh tự động. Không sửa/xóa chỉ vì không có nơi gọi trực tiếp trong C#. |
| `HorseClub.DAL/Entities` | Một entity mỗi file, ánh xạ bảng SQL. Entity chung giữ Id, CreatedAt, Version; các quan hệ được cấu hình ở Data/Configurations. |
| `HorseClub.DAL/Enums` | Giá trị domain chính xác gửi qua API/lưu DB; frontend dịch label riêng, không thay tên enum. |
| `Horse_BackEnd` | Layer API: composition root, HTTP, authentication, middleware, OpenAPI; chuyển request/response và gọi BLL. |
| `Horse_BackEnd/App_Data` | Tập hợp file phục vụ Horse_BackEnd/App_Data. |
| `Horse_BackEnd/App_Data/keys` | Tập hợp file phục vụ Horse_BackEnd/App_Data/keys. |
| `Horse_BackEnd/App_Data/mail` | Tập hợp file phục vụ Horse_BackEnd/App_Data/mail. |
| `Horse_BackEnd/Endpoints` | Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL. |
| `Horse_BackEnd/Infrastructure` | Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI. |
| `Horse_BackEnd/Properties` | Profile chạy local HTTP/HTTPS, cổng và môi trường Development; không chứa mật khẩu. |
| `docs` | Tài liệu thiết kế/chính sách, hướng dẫn chạy/Azure/SMTP, hợp đồng và giải thích code; README là điểm vào tìm tài liệu. |
| `docs/contracts` | OpenAPI, bảng endpoint và kết quả audit hợp đồng; baseline dùng so sánh, current phản ánh API hiện tại. |
| `docs/demo` | Mô tả tài khoản/dữ liệu demo dùng cho công cụ và hướng dẫn; không phải dữ liệu production. |
| `docs/http` | Request HTTP mẫu kiểm thử intake/huấn luyện từ IDE; không chứa token/mật khẩu thật. |
| `docs/reference` | Tra cứu từng function theo module: mục đích, đầu vào, lời gọi chính và lưu ý quyền/transaction/I/O. |
| `tests` | Các project kiểm thử; không thuộc runtime ứng dụng. |
| `tests/Horse_BackEnd.Tests` | WebApplicationFactory và SQL Server tạm; kiểm hợp đồng, quyền, state workflow, workers, storage và hiệu năng. Test Azure thật chỉ chạy khi bật cờ rõ ràng. |
| `tools` | Script cấu hình/test kết nối/migration và công cụ seed demo. Script đọc credential kín từ User Secrets, không ghi secret vào Git. |
| `tools/HorseClub.DemoSeed` | CLI tạo dữ liệu demo theo vai trò; không chạy tự động trong backend bình thường. |

## Dữ liệu và file không được dọn như cache

User Secrets, file cấu hình local, Data Protection keys trong App_Data/keys, credential và dữ liệu Azure không phải build artifact. Migration/snapshot, package lock, CI, test và tài liệu baseline vẫn cần cho bảo trì. node_modules cung cấp dependency cho frontend chạy; chỉ cache Vite được dọn. bin/obj/dist/TestResults có thể tạo lại.

Các folder hệ thống/third-party không thuộc source: .git giữ lịch sử repository; .vs giữ thiết lập/cache IDE cá nhân; node_modules giữ dependency để chạy npm. Các cache/build bin, obj, dist, .vite và TestResults được tạo lại bởi tool, không lưu code nghiệp vụ.
