# HorseClub

Hệ thống quản lý huấn luyện ngựa đua dành cho Horse Owner, Club Manager, Head Trainer, Trainer, Work Rider, Veterinarian và Groom.

## Tài liệu làm việc nhóm

- [Kế hoạch triển khai](docs/IMPLEMENTATION_PLAN.md)
- [Backlog và tiêu chí nghiệm thu](docs/BACKLOG.md)
- [Kiến trúc và dữ liệu dự kiến](docs/ARCHITECTURE.md)
- [Quy trình đóng góp](CONTRIBUTING.md)
- [Thiết lập GitHub](docs/GITHUB_SETUP.md)
- [Chạy backend và tài liệu API](docs/BACKEND_GUIDE.md)

Đặc tả nguồn: Racehorse_Frontend_Figma_Functional_Spec_Merged_V1_V2.docx. Quyết định nghiệp vụ V2 được ưu tiên khi xung đột với V1.

[Figma](https://www.figma.com/design/AKLYJd26mWHeG1W8V0ach5/Figma-basics--Copy-?node-id=1669-162202)

## Hiện trạng

Backend ASP.NET Core .NET 10 với EF Core, migration SQLite/SQL Server, auth/OTP và 7 role, hồ sơ/phân công, training, medical guards, care/stable/inventory, reports, notifications và audit. Ba project API → BLL → DAL; các role/status/type dùng enum và thông báo dùng catalog chung qua `MessageKey`. Cấu hình và secrets tách khỏi business code. Xem [quy tắc ba layer và message](docs/THREE_LAYER_AND_MESSAGES.md). Frontend chưa được triển khai.

## Chạy backend

Cài .NET SDK 10 phù hợp với project, sau đó chạy:

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx
dotnet test HorseClub.slnx
dotnet run --project Horse_BackEnd --launch-profile http
```

API: `http://localhost:5299`, OpenAPI development: `/openapi/v1.json`, health: `/health`. Cấu hình Manager đầu tiên và SMTP theo [hướng dẫn backend](docs/BACKEND_GUIDE.md). Không có mật khẩu mặc định; không commit credentials hoặc dữ liệu cá nhân thật.
