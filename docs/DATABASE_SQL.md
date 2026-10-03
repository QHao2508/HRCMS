# SQL Server database

[database.sql](database.sql) là script schema sinh từ migration `InitialSqlServer`: tạo bảng, khóa chính, khóa ngoại, indexes và lịch sử migration. Script dùng `GO`, chạy được bằng SQL Server Management Studio (SSMS) hoặc `sqlcmd`.

## Chạy bằng SSMS

1. Kết nối SQL Server và tạo một database trống với tên bạn chọn.
2. Mở `docs/database.sql`, chọn đúng database trong dropdown của cửa sổ query rồi Execute.
3. Cấu hình backend `Database:Provider=SqlServer` và `ConnectionStrings:SqlServer` qua User Secrets hoặc biến môi trường. Xem [BACKEND_GUIDE.md](BACKEND_GUIDE.md).

Script không tạo SQL login, mật khẩu, tài khoản ứng dụng hoặc dữ liệu mẫu. Manager đầu tiên được backend tạo từ `Bootstrap:ManagerEmail` và `Bootstrap:ManagerPassword` khi khởi động. Các enum nghiệp vụ lưu dạng `int` với giá trị cố định trong `HorseClub.DAL/Domain`; API sử dụng tên enum.

Script kiểm `__EFMigrationsHistory` để bỏ qua migration đã áp dụng. Chỉ dùng cho database trống hoặc database đã được quản lý bằng migrations này; không dùng để tự động chuyển đổi một schema khác. Backup trước khi cập nhật database có dữ liệu.

## Sinh lại khi schema thay đổi

```powershell
dotnet build HorseClub.slnx --configuration Release
dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --configuration Release --no-build --output docs/database.sql
```

File này dành cho SQL Server, không chạy trên SQLite/MySQL. Script được sinh thành công và model/migration đã có kiểm thử; chưa chạy trên SQL Server thực tế trong phiên này.
