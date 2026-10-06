# HorseClub – HRCMS

Hệ thống quản lý câu lạc bộ và huấn luyện ngựa đua, từ tiếp nhận hồ sơ, phân công nhân sự đến huấn luyện, thú y, chăm sóc và quản lý kho. Hệ thống phục vụ 7 vai trò: Horse Owner, Club Manager, Head Trainer, Trainer, Work Rider, Veterinarian và Groom.

## Chức năng chính

- **Tài khoản và phân quyền:** Owner đăng ký/xác thực email; Manager tạo staff qua invitation; đăng nhập, refresh, reset mật khẩu và kiểm quyền/phạm vi dữ liệu phía server.
- **Hồ sơ ngựa:** draft, submit tài liệu, revision, approval và Horse Profile; phân công Head Trainer/Trainer/Vet/Groom, giữ lịch sử.
- **Huấn luyện:** template, plan, session, giao Work Rider, kết quả và đánh giá. Kiểm medical restrictions khi lập lịch, assign và start.
- **Thú y:** khám, chấn thương, điều trị, restrictions, follow-up/clearance và preventive care; giới hạn clinical records theo quyền.
- **Vận hành:** care/feeding, incidents/photos, stable/stall/occupancy, stock movements và replenishment.
- **Dùng chung:** notifications, reminders, audit, dashboard và reports theo phạm vi từng role.

Backend đã có API và kiểm thử. Thiết kế frontend đã hoàn thành trên [Figma](https://www.figma.com/design/AKLYJd26mWHeG1W8V0ach5/Figma-basics--Copy-?node-id=1669-162202) theo thông tin nhóm; mã frontend sẽ được push và tích hợp sau tại repository riêng [HRCMS-Frontend](https://github.com/QHao2508/HRCMS-Frontend). Repository HRCMS này dành cho backend, database và tài liệu liên quan. Đặc tả nguồn: `Racehorse_Frontend_Figma_Functional_Spec_Merged_V1_V2.docx`; quyết định V2 được ưu tiên khi xung đột V1.

## Công nghệ và cấu trúc

ASP.NET Core **.NET 10**, EF Core, SQL Server; hỗ trợ SQLite cho môi trường thử nghiệm. Role/status/type dùng enum, thông báo dùng catalog chung qua `MessageKey`, giới hạn vận hành dùng options và secrets tách khỏi mã nguồn.

| Project/thư mục | Trách nhiệm |
| --- | --- |
| `Horse_BackEnd` | API: routes, request binding, HTTP pipeline, DI và startup |
| `HorseClub.BLL` | Business: workflows, services, DTO, quyền và catalog message |
| `HorseClub.DAL` | Data: entities/enums, DbContext, provider và migrations |
| `tests/Horse_BackEnd.Tests` | Kiểm thử nghiệp vụ, phân quyền, layer và messages |
| `docs` | SQL schema, hướng dẫn và bảng phân công backend |

Luồng phụ thuộc: **API → BLL → DAL**. Xem [quy tắc ba layer và message](docs/THREE_LAYER_AND_MESSAGES.md).

## Chuẩn bị môi trường

Cài .NET SDK 10 và SQL Server; quản lý database bằng SSMS hoặc `sqlcmd`. Clone repository rồi chạy terminal tại thư mục chứa `HorseClub.slnx`:

```powershell
dotnet restore HorseClub.slnx
dotnet tool restore
dotnet build HorseClub.slnx --configuration Release
```

## Kết nối backend với SQL Server

### 1. Tạo database và schema

Trên máy đã thiết lập, instance `localhost` có database **HorseClub**, gồm 31 bảng nghiệp vụ. Máy của thành viên khác cần tạo database và cấu hình riêng; User Secrets không được đồng bộ qua Git.

Trong SSMS: kết nối bằng Windows Authentication, tạo database `HorseClub`, mở [docs/database.sql](docs/database.sql), chọn đúng database trong dropdown và Execute.

Hoặc dùng `sqlcmd` nếu đã cài công cụ:

```powershell
sqlcmd -S localhost -E -C -b -Q "IF DB_ID(N'HorseClub') IS NULL CREATE DATABASE [HorseClub];"
sqlcmd -S localhost -d HorseClub -E -C -b -i docs/database.sql
```

Script tạo bảng, khóa, indexes và kiểm lịch sử để bỏ qua migration đã áp dụng. Dùng cho database trống hoặc database quản lý bằng migrations của dự án; không reset dữ liệu hiện có. Xem [hướng dẫn SQL](docs/DATABASE_SQL.md).

### 2. Lưu kết nối trong User Secrets

Chạy từ thư mục gốc repository:

```powershell
dotnet user-secrets set "Database:Provider" "SqlServer" --project Horse_BackEnd
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost;Database=HorseClub;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" --project Horse_BackEnd
dotnet user-secrets set "Database:AutoMigrate" "false" --project Horse_BackEnd
```

Windows Authentication dùng tài khoản Windows chạy backend; tài khoản này cần quyền đọc/ghi database. Nếu dùng named instance, thay server bằng tên thật, ví dụ `.\SQLEXPRESS`, trong cả lệnh SQL và connection string. `TrustServerCertificate=True` phục vụ SQL Server local có certificate tự ký.

`AutoMigrate=false` vì đã áp schema SQL ở bước 1. User Secrets được đọc khi chạy môi trường **Development** qua launch profile. Môi trường triển khai dùng biến môi trường `Database__Provider`, `ConnectionStrings__SqlServer` hoặc secret store phù hợp. Không commit connection string có mật khẩu.

### 3. Chạy và kiểm tra

```powershell
dotnet run --project Horse_BackEnd --configuration Release --no-build --launch-profile http
```

| URL | Kết quả cần kiểm tra |
| --- | --- |
| `http://localhost:5299/health` | HTTP 200, `{"status":"healthy"}` khi database kết nối được |
| `http://localhost:5299/openapi/v1.json` | API contract trong Development; import vào Postman |

Chưa có Swagger UI. Có requests mẫu tại [Horse_BackEnd.http](Horse_BackEnd/Horse_BackEnd.http). Nếu backend đang chạy ở cổng 5299, dùng instance đó hoặc dừng trước khi chạy thêm.

### 4. Tạo Manager đầu tiên

Schema không có tài khoản hay mật khẩu mặc định. Thay placeholder bằng thông tin riêng:

```powershell
dotnet user-secrets set "Bootstrap:ManagerEmail" "<email của bạn>" --project Horse_BackEnd
dotnet user-secrets set "Bootstrap:ManagerPassword" "<mật khẩu riêng đạt chính sách Security>" --project Horse_BackEnd
```

Khởi động lại backend để tạo Manager nếu chưa có Manager. Sau khi tạo thành công, có thể xóa cấu hình bootstrap:

```powershell
dotnet user-secrets remove "Bootstrap:ManagerEmail" --project Horse_BackEnd
dotnet user-secrets remove "Bootstrap:ManagerPassword" --project Horse_BackEnd
```

Manager tạo staff qua invitation; Owner tự đăng ký qua API. Development ghi email thử vào `Horse_BackEnd/App_Data/mail`; cấu hình SMTP thật khi cần gửi email. Không commit password, OTP, uploads hoặc Data Protection keys. Chi tiết tại [BACKEND_GUIDE.md](docs/BACKEND_GUIDE.md).

## Kiểm thử và tích hợp frontend

```powershell
dotnet test HorseClub.slnx --configuration Release
```

Bộ kiểm thử hiện có 85 trường hợp; CI build/test trên PR và khi push `main`. Kiểm chứng local sau BE-004 ngày 05/10/2026: SQL Server Express chạy 84 pass/1 SQLite-only skip; SQLite chạy 72 pass/13 SQL-only skip. Đã kiểm workflow, FK/rollback, race giữa hai backend độc lập, hợp đồng OpenAPI cho 96 operations, worker retry/expiry/reminder và storage/backup/restore. CI có thêm job SQL Server; chưa xác nhận job mới đã chạy trên GitHub. Xem [cách chạy SQL Server](docs/SQLSERVER_TESTING.md), [contract API hiện tại](docs/API_CONTRACT.md), [worker/migration bước 5](docs/WORKER_RELIABILITY.md) và [storage/khôi phục bước 6](docs/STORAGE_AND_RECOVERY.md). BE-003 đã bổ sung partial Draft, kiểm submit/approval và Manager edit có audit; xem [bàn giao BE-003](docs/BE-003_INTAKE_AND_ASSIGNMENT.md). BE-004 bổ sung training scope, pagination và history; xem [bàn giao BE-004](docs/BE-004_TRAINING.md). Ngoài Development cần cấu hình key encryption trước startup. SMTP bên ngoài, UI, kill-process recovery và load testing vẫn chưa được nghiệm thu.

Frontend gọi **API**, backend truy cập **database**. Khi tích hợp, đặt API base URL theo môi trường, cấu hình `Cors:Origins` cho origin frontend thực tế và sử dụng DTO/enum trong OpenAPI. Chủ module backend phối hợp xử lý mismatch sau khi nhận mã frontend.

## Phân công backend và tài liệu nhóm

**File phân chia backend: [docs/BACKEND_TASK_ASSIGNMENT.md](docs/BACKEND_TASK_ASSIGNMENT.md)**. Bảng có 5 vị trí BE01–BE05, 14 task, reviewer, dependency, ưu tiên và tiêu chí nghiệm thu. Nhóm điền tên thành viên/deadline trước khi nhận việc.

- [Hướng dẫn backend/API](docs/BACKEND_GUIDE.md)
- [SQL schema](docs/database.sql) và [hướng dẫn database](docs/DATABASE_SQL.md)
- [Kiến trúc](docs/ARCHITECTURE.md) và [ba layer/message](docs/THREE_LAYER_AND_MESSAGES.md)
- [Kế hoạch ban đầu](docs/IMPLEMENTATION_PLAN.md) và [backlog](docs/BACKLOG.md)
- [Quy trình đóng góp](CONTRIBUTING.md) và [thiết lập GitHub](docs/GITHUB_SETUP.md)

`main` là nhánh dùng chung sau khi merge backend. Mỗi nhiệm vụ tạo nhánh riêng và gửi PR kèm kiểm thử/reviewer. Phạm vi chưa có gồm report export, 3D injury nâng cao và scheduling duration/overlap đầy đủ; xem BACKEND_GUIDE. Flow 5 thi đấu nằm ngoài phạm vi hiện tại.
