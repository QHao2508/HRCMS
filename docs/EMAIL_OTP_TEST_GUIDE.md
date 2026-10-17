# Kiểm tra email OTP

Quy tắc hiện tại: cả đăng ký Owner, quên mật khẩu và kích hoạt nhân viên đều gửi OTP **6 chữ số**, không gửi link và không cần frontend public. Mặc định cả ba OTP hết hạn sau **10 phút**, dùng một lần, tối đa **5 lần nhập sai**. Mã được hash trong bảng Challenges; nội dung outbox xóa sau gửi thành công. Gửi lại đăng ký/reset cách tối thiểu 60 giây, mã cũ mất hiệu lực.

Email thật có giao diện HTML: header xanh HRCMS, khung OTP lớn viền đứt, lưu ý nền vàng và footer tự động. Mã/thời hạn lấy từ nội dung OTP thực tế; không dùng mã mẫu cố định. SMTP gửi kèm phần văn bản thuần để ứng dụng không hiển thị HTML vẫn đọc được. Template chung nằm ở `HorseClub.BLL/Workers/Templates/OtpEmail.html`; nội dung từng luồng ở `Messages/messages.en.json`. Tên đăng nhập nhân viên được HTML encode trước khi hiển thị.

Tên đăng nhập hoặc email đều đăng nhập được. API giữ field JSON `email` để tương thích frontend cũ; giá trị field có thể là username. Email reset/kích hoạt vẫn phải nhập địa chỉ email nhận mã để xác định tài khoản, không nhập username vào field này.

## Chuẩn bị

Khởi động lại backend để dùng code/cấu hình mới. Frontend chạy `npm run dev`. Gmail SMTP phải đã cấu hình bằng User Secrets. Không tái sử dụng token dài nhận trước thay đổi này; yêu cầu OTP mới.

```powershell
cd E:\SWP391\HorseClub\Horse_BackEnd
dotnet run
```

Trong terminal khác:

```powershell
cd E:\SWP391\HorseClub-frontend\HRCMS-Frontend
npm.cmd run dev
```

## 1. Owner đăng ký

Mở `/register`, đăng ký bằng email bạn kiểm soát. Email tiêu đề `HRCMS - Xác thực đăng ký tài khoản` chứa OTP 6 số. Nhập tại `/verify-email`. Xác thực xong mới đăng nhập được. Gửi lại tạo OTP mới; OTP đã dùng/sai/hết hạn không xác thực được.

## 2. Quên mật khẩu

Tại `/forgot-password`, nhập email của tài khoản đã kích hoạt. Nhận mail `HRCMS - Đặt lại mật khẩu`. Mở `/reset-password`, nhập email, OTP và mật khẩu mới/nhập lại. Xong đăng nhập bằng mật khẩu mới; token đăng nhập cũ bị thu hồi. Email không tồn tại hoặc tài khoản chưa kích hoạt trả phản hồi chung, không gửi OTP reset.

## 3. Nhân viên

Manager đăng nhập rồi tạo staff qua Swagger `POST /api/staff`, cung cấp email thật của nhân viên và `userName` được cấp, cùng vai trò/thông tin yêu cầu. Frontend chưa có màn hình quản lý tạo staff riêng.

Nhân viên nhận mail `HRCMS - Kích hoạt tài khoản nhân viên`, gồm **OTP 6 số và tên đăng nhập Manager cấp**. Mở `/accept-invitation` (liên kết Nhận lời mời nhân viên trên Login), nhập email nhận mail, OTP và tự đặt mật khẩu. Đây là đặt mật khẩu lần đầu, Manager không cấp mật khẩu chung. Sau đó đăng nhập bằng username được cấp hoặc email. Khi quên mật khẩu về sau, dùng luồng 2.

Nếu OTP mời nhân viên hết hạn, cần Manager cấp lời mời mới; hiện chưa có endpoint resend invitation riêng. Không tạo lại cùng email vì tài khoản đã tồn tại.

## Kiểm chứng

Test tự động dùng SQL Server tạm và không gửi mail thật. Bao gồm OTP 6 số, tài khoản chưa xác thực không đăng nhập được, token hết hạn/đã dùng/5 lần sai bị từ chối, kích hoạt staff rồi đăng nhập bằng username. Việc thư vào Inbox/Spam và chính sách Gmail cần test trên email thực tế của bạn.

Kết quả kiểm tra ngày 07/10/2026: backend **94/94**, không bỏ qua test; frontend **150/150**, build Vite thành công. Test không thay đổi dữ liệu Azure SQL của dự án.
