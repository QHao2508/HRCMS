# Quản trị câu lạc bộ và thông tin training — 09/10/2026

Đã triển khai lần lượt FE, kiểm tra FE, sau đó BE, kiểm tra BE và cuối cùng nối các API mới ở FE. Không áp dụng migration lên database dự án và không gửi email tới nhân viên thật trong quá trình kiểm thử.

## Chức năng

- Chỉ HorseOwner có nhãn **Ngựa của tôi**. Các vai trò còn lại có nhãn **Ngựa**; quyền xem vẫn theo scope sở hữu/phân công của API.
- ClubManager có màn hình Nhân sự, Nhật ký hoạt động và Giao diện website. Route FE và service BLL đều kiểm quyền; ẩn menu không thay thế authorization.
- Mời đủ sáu role nội bộ, bao gồm ClubManager, không mời HorseOwner. Nhân viên xác nhận OTP rồi tự tạo mật khẩu. OTP và proof không được lưu trong URL/localStorage hoặc audit.
- Kế hoạch hiển thị tên ngựa và Trainer; buổi tập hiển thị tên ngựa, Rider và Trainer. Tìm kiếm theo tên áp dụng trong SQL trước Count/Skip/Take, giữ nguyên scope và thứ tự ổn định.
- Website có tiêu đề/mô tả hero, tiêu đề/mô tả phần tính năng và ba nhóm tính năng; logo, ảnh hero và ảnh nền thay được. Giá trị ban đầu lấy từ cấu hình Website:Defaults. Nội dung được render như text, không thực thi HTML.
- Lưu chữ và xuất bản ảnh là hai thao tác riêng, được ghi rõ trên FE. Request ghi yêu cầu Version hiện tại; nếu có cập nhật đồng thời thì trả 409 và cần tải lại. Ảnh chỉ nhận PNG/JPEG có extension khớp chữ ký và giới hạn Storage:MaxFileBytes.
- Ảnh website dùng cùng storage abstraction và cơ chế rollback/reconciliation với upload hiện có. Runtime Azure giữ container private, endpoint public chỉ đọc ảnh đã được cấu hình cho website, không nhận storage key tùy ý.

## API

| API | Hợp đồng |
|---|---|
| GET /api/training/plans | Thêm search; items có horseName, trainerName |
| GET /api/training/sessions | Thêm search; items có horseName, trainerName, riderName |
| GET /api/training/plans/{id} | Thêm horseName, trainerName, sessionNames cho trang buổi tập hiện tại |
| GET /api/training/sessions/{id} | Thêm horseName, trainerName, riderName |
| POST /api/staff | Email, UserName, FirstName, LastName, Phone, Address, Role; ClubManager được mời mọi role nội bộ |
| POST /api/staff/{id}/resend-invitation | ClubManager; nhân viên còn active và chưa kích hoạt; áp dụng cooldown |
| POST /api/auth/invitation/verify | Email, Code; trả verified và setupToken khi thành công |
| POST /api/auth/invitation/password | Email, SetupToken, Password, ConfirmPassword; trả changed |
| GET /api/audit | ClubManager; referenceId, search theo tên actor, from/to theo ngày câu lạc bộ, page/pageSize; trả actorName và enum action |
| GET /api/website | Public; trả nội dung, version và các phiên bản ảnh; không trả credential |
| PUT /api/website | ClubManager; Version và nội dung; trả bản đã lưu |
| POST /api/website/assets/{kind} | ClubManager; multipart file + version, kind: Logo/Hero/Background |
| GET /api/website/assets/{kind} | Public; chỉ stream ảnh của kind đang được website tham chiếu |

Proof tạo mật khẩu dùng Data Protection có hạn, gắn với tài khoản và SecurityStamp. Đặt mật khẩu hoặc gửi lại lời mời làm proof cũ mất hiệu lực. Endpoint accept-invitation cũ được giữ để tương thích client cũ; FE mới sử dụng hai endpoint mới. Reset mật khẩu và đăng ký chủ ngựa không đổi.

DTO list giữ các field cũ của plan/session, bổ sung tên; detail giữ các field cũ. Không tải toàn bộ nhân viên/ngựa lên FE để tìm kiếm, không gọi riêng từng bản ghi để lấy tên.

## Áp dụng database và cấu hình

Migration mới: `20261009025334_WebsiteManagement` chỉ thêm bảng WebsiteSettings. Script [WEBSITE_MANAGEMENT_MIGRATION.sql](WEBSITE_MANAGEMENT_MIGRATION.sql) đi từ migration RealtimeResourceSignals; kiểm tra các migration trước đã áp dụng, chọn đúng database HRCMS trong SSMS rồi thực thi script. Không chạy script trên database khác.

Nếu dùng CLI, truyền đúng connection string qua `dotnet ef database update --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --connection "<connection string database HRCMS>"`. Design-time factory cũ có database mặc định khác; không dùng lệnh thiếu --connection để cập nhật database thực.

Sau migration, chạy lại BE và FE. Runtime cần cấu hình SQL Server, SMTP/worker và Azure Blob có credential hợp lệ. Không bật AutoMigrate trên database thật chỉ để thử giao diện. Các credential hiện có không bị thay đổi bởi công việc này.

## Kiểm thử thủ công

1. Đăng nhập ClubManager: thấy Nhân sự, Nhật ký hoạt động, Giao diện website và Ngựa.
2. Cấp tài khoản Trainer hoặc một role nội bộ khác; kiểm tra email OTP qua SMTP đang cấu hình. Không có field mật khẩu trong form cấp tài khoản.
3. Nhân viên mở Kích hoạt tài khoản nhân viên, nhập email/OTP. OTP sai cho phép sửa lại; OTP đúng chuyển sang bước đặt mật khẩu. Hoàn tất rồi đăng nhập tài khoản mới.
4. Thử OTP/proof đã dùng lại, proof của email khác và proof hết hạn: không được kích hoạt.
5. Chỉnh tiêu đề homepage, xem trước, hủy hoặc lưu. Sau lưu mở homepage để đối chiếu; Nhật ký hoạt động có tên người thực hiện và hành động cập nhật.
6. Tải logo, ảnh giới thiệu và ảnh nền PNG/JPEG. Kiểm tra ảnh đổi đúng; WebP/PDF hoặc extension giả phải bị từ chối.
7. Mở hai phiên quản lý cùng cấu hình, lưu một phiên rồi thử lưu phiên còn lại: nhận xung đột, tải lại trước khi sửa tiếp.
8. Với dữ liệu training có ngựa/Trainer/Rider được phân công: kiểm tra các cột tên và tìm theo tên từng người/ngựa, đổi trang, xóa tìm kiếm, kết hợp trạng thái buổi tập. Chủ ngựa khác không thấy dữ liệu ngoài scope.
9. Đăng nhập HorseOwner: chỉ nhãn ngựa của người này là Ngựa của tôi; không truy cập được ba màn hình quản lý hoặc API ghi website/audit.

Kiểm thử tự động dùng SQL Server với database HRCMS_Test_<UUID> do fixture tự tạo và xóa, worker tắt. Kiểm tra Azure thực tế được tách riêng và cần cấu hình Azure live; regression không gửi email thật.
