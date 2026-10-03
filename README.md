# HorseClub

Hệ thống quản lý huấn luyện ngựa đua dành cho Horse Owner, Club Manager, Head Trainer, Trainer, Work Rider, Veterinarian và Groom.

## Tài liệu làm việc nhóm

- [Kế hoạch triển khai](docs/IMPLEMENTATION_PLAN.md)
- [Backlog và tiêu chí nghiệm thu](docs/BACKLOG.md)
- [Kiến trúc và dữ liệu dự kiến](docs/ARCHITECTURE.md)
- [Quy trình đóng góp](CONTRIBUTING.md)
- [Thiết lập GitHub](docs/GITHUB_SETUP.md)

Đặc tả nguồn: Racehorse_Frontend_Figma_Functional_Spec_Merged_V1_V2.docx. Quyết định nghiệp vụ V2 được ưu tiên khi xung đột với V1.

[Figma](https://www.figma.com/design/AKLYJd26mWHeG1W8V0ach5/Figma-basics--Copy-?node-id=1669-162202)

## Hiện trạng

Backend ASP.NET Core nhắm .NET 10, hiện mới có mẫu WeatherForecast. Chưa có chức năng nghiệp vụ, frontend hay cơ sở dữ liệu. Công nghệ frontend và database trong kế hoạch là đề xuất cần nhóm chốt.

## Chạy backend

Cài .NET SDK 10 phù hợp với project, sau đó chạy:

```powershell
dotnet restore HorseClub.slnx
dotnet build HorseClub.slnx
dotnet run --project Horse_BackEnd/Horse_BackEnd.csproj
```

Xem địa chỉ chạy trong console. Không commit mật khẩu, token, OTP, connection string có credentials hoặc dữ liệu cá nhân thật. Dùng user-secrets hoặc biến môi trường cho cấu hình nhạy cảm.
