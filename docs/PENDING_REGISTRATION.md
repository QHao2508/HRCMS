# Vòng đời đăng ký chưa xác thực

- Tài khoản chủ ngựa tự đăng ký có 24 giờ từ `User.CreatedAt` để xác thực email. `Security:PendingRegistrationHours` mặc định 24; gửi lại OTP không thay đổi mốc này.
- Đăng nhập đúng mật khẩu khi còn thời hạn trả HTTP 401 với `error=email_verification_required` và email chuẩn của tài khoản. Frontend chuyển đến `/verify-email`, giữ email và không lưu token. Sai mật khẩu vẫn trả `invalid_credentials`, không tiết lộ trạng thái tài khoản.
- Quá hạn trả HTTP 410 với `error=registration_expired`; frontend hướng dẫn đăng ký lại. Verify/resend không thể kích hoạt tài khoản quá hạn, kể cả trước lượt dọn dữ liệu tiếp theo.
- OTP vẫn có hiệu lực tối đa 10 phút, đồng thời không vượt quá hạn đăng ký 24 giờ.
- `AccountCleanupWorker` chạy khi backend hoạt động và `Workers:Enabled=true`, mặc định mỗi 60 giây, mỗi đợt 100 tài khoản. SQL Server transaction và khóa worker ngăn các instance dọn cùng dữ liệu. Nếu backend tắt, lần chạy tiếp theo sẽ dọn tài khoản quá hạn.
- Dọn dữ liệu gồm tài khoản hết hạn, challenge, email OTP và thông báo liên quan. Tài khoản đã xác thực và tài khoản nhân viên/quản lý không bị xóa. Dữ liệu lịch sử bất thường có liên kết nghiệp vụ được giữ để tránh xóa lan sang hồ sơ ngựa.
- Đăng ký lại cùng email/tên đăng nhập sau hạn được giải phóng ngay trong transaction đăng ký, không phải đợi lượt worker.

Không cần migration vì sử dụng các trường sẵn có. Kiểm thử `PendingRegistrationTests` dùng SQL Server tạm và đồng hồ điều khiển được để xác nhận ranh giới đúng 24 giờ, xóa dữ liệu liên quan, đăng ký lại và phân biệt mật khẩu sai.
