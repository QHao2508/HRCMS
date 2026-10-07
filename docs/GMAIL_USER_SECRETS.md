# Cấu hình Gmail bằng dotnet user-secrets

Dự án dùng cấu trúc `Email:Provider`, `Email:FromAddress`, `Email:FromName`, `Email:Smtp:*`. Không cần chạy script cấu hình nếu muốn dùng các lệnh dưới đây.

```powershell
cd E:\SWP391\HorseClub\Horse_BackEnd
dotnet user-secrets set "Email:Provider" "Smtp" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:FromAddress" "lequochao12a2@gmail.com" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:FromName" "HRCMS" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:Smtp:Host" "smtp.gmail.com" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:Smtp:Port" "587" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:Smtp:Username" "lequochao12a2@gmail.com" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:Smtp:Password" "<APP_PASSWORD_MOI>" --project Horse_BackEnd.csproj
dotnet user-secrets set "Email:Smtp:EnableSsl" "true" --project Horse_BackEnd.csproj
dotnet user-secrets set "Workers:Enabled" "true" --project Horse_BackEnd.csproj
```

Thay `<APP_PASSWORD_MOI>` bằng App Password được tạo cho đúng tài khoản gửi. Gmail App Password hiển thị thành các nhóm có dấu cách; backend bỏ dấu cách khi đăng nhập Gmail SMTP. Không dùng mật khẩu đăng nhập Google thông thường. `FromName` là tên hiển thị, không phải địa chỉ email.

Không cần `AuthVerification:SecretKey`: OTP được hash bằng ASP.NET PasswordHasher, có hạn dùng và giới hạn attempts; không sử dụng secret key của SmartCinema. Không thêm cấu hình không được code dùng.

Kiểm tra kết nối TLS và xác thực SMTP, không gửi email:

```powershell
cd E:\SWP391\HorseClub
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Test-GmailConnection.ps1
```

Nếu `PASS`, dừng/chạy lại backend (`dotnet run`) rồi bấm gửi lại OTP trên frontend. Nếu `535 5.7.8 BadCredentials`, Gmail đã từ chối credential; đổi environment hoặc provider không sửa được mật khẩu bị từ chối. Tạo App Password mới và cập nhật riêng `Email:Smtp:Password`.

Các khóa email cũ (`Email:Mode`, `Email:Host`, `Email:From`, `Email:Port`, `Email:Username`, `Email:Password`) đã được bỏ khỏi cấu hình máy này. SQL/Storage/Bootstrap secrets vẫn được giữ.

## Cấu hình không bí mật trong appsettings

```json
{
  "Email": {
    "Provider": "Smtp",
    "FromName": "HRCMS",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "EnableSsl": true
    }
  }
}
```

Email gửi đăng ký/reset/kích hoạt nhân viên đều chứa OTP 6 chữ số. Xem [hướng dẫn test](EMAIL_OTP_TEST_GUIDE.md).
