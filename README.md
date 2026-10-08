# HorseClub — HRCMS

Backend quản lý câu lạc bộ/ngựa đua: tài khoản, intake/approval, assignment, training, y tế, chăm sóc, kho, thông báo và báo cáo. ASP.NET Core .NET 10, EF Core và SQL Server duy nhất.

## Cấu trúc

```text
Endpoint → I…Service → BLL service → I…Repository + IUnitOfWork → DAL/EF Core
```

Giữ Minimal API và ba project API → BLL → DAL. Đọc [mục lục tài liệu](docs/README.md), [kiến trúc](docs/ARCHITECTURE.md) và [repository/Unit of Work/SignalR](docs/REPOSITORY_SIGNALR_IMPLEMENTATION.md).

## Azure SQL và chạy API

Server `hrcms.database.windows.net`, database `HRCMS`. Mật khẩu SQL Login lưu trong User Secrets, không commit source. Chi tiết: [AZURE_SQL_SETUP.md](docs/AZURE_SQL_SETUP.md).

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx --configuration Release
./tools/Set-AzureSqlConnection.ps1
./tools/Update-AzureSqlDatabase.ps1
dotnet run --project Horse_BackEnd --configuration Release --no-build --launch-profile http
```

Script kết nối chỉ cần chạy khi cấu hình máy/kết nối mới; script schema chạy khi có migration mới. AutoMigrate runtime false. Bootstrap Manager dùng cấu hình riêng trong secrets; không có mật khẩu mặc định.

- API/health: `http://localhost:5299/health`
- Swagger development: `http://localhost:5299/swagger`
- OpenAPI: `http://localhost:5299/openapi/v1.json`

## Frontend

React/Vite/JavaScript tại `E:\SWP391\HorseClub-frontend\HRCMS-Frontend`, repository [HRCMS-Frontend](https://github.com/QHao2508/HRCMS-Frontend). Đã có auth, dashboard, registration/review, horse và training. Vite cổng 5173 proxy API tới 5299. Frontend không kết nối SQL trực tiếp. Xem [FRONTEND_READINESS.md](docs/archive/FRONTEND_READINESS.md) và `docs/API_INTEGRATION.md` trong frontend.

## Kiểm thử

```powershell
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx --configuration Release
```

Tests tạo/drop database tạm `HRCMS_Test_<GUID>` trên SQL local; không dùng Azure HRCMS làm database test. Xem [SQLSERVER_TESTING.md](docs/SQLSERVER_TESTING.md).

## Tài liệu

[Backend guide](docs/BACKEND_GUIDE.md), [API contract](docs/API_CONTRACT.md), [quy tắc layer/message](docs/THREE_LAYER_AND_MESSAGES.md), [phân công](docs/archive/BACKEND_TASK_ASSIGNMENT.md), [backlog](docs/archive/BACKLOG.md), [contributing](CONTRIBUTING.md).

## Đọc và bảo trì code

- [Chức năng từng folder](docs/FOLDERS.md)
- [Bảng báo cáo và vấn đáp](docs/BAO_CAO_CAU_TRUC_VA_LUONG_NGHIEP_VU.md)
- [Tài liệu lịch sử](docs/archive/README.md)

Chú thích XML/JSDoc giải thích mục đích, đầu vào và điểm cần lưu ý ngay trước function. Giữ migration, package lock và Data Protection keys khi dọn project.
