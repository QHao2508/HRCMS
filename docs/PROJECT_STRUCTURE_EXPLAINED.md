> Tài liệu đang dùng: [FOLDERS.md](FOLDERS.md) và [FUNCTION_REFERENCE.md](FUNCTION_REFERENCE.md). Các mục bên dưới mô tả mốc refactor; file CSS rỗng, icons.svg template và ảnh minh chứng đã được dọn.

# Giải thích cấu trúc dự án HRCMS

Cấu trúc sau refactor ngày 07/10/2026. Backend: `E:\SWP391\HorseClub`. Frontend: `E:\SWP391\HorseClub-frontend\HRCMS-Frontend`.

## 1. Bức tranh tổng thể

```text
React Page → frontend Service/Axios → API Endpoint
          → BLL Service theo module → DAL DbContext → Azure SQL
```

Ba layer thuộc backend: API nhận/trả HTTP, BLL xử lý nghiệp vụ, DAL lưu dữ liệu. Frontend là ứng dụng riêng. Giữ Minimal API; các Workflow đã được gom vào service theo module. Service nhận dependency qua constructor. Entity, enum và DTO được tách riêng để tìm file theo tên đối tượng.

BLL được phép sử dụng DbContext từ DAL. Chưa cần repository cho mỗi bảng. JSON/route/status hiện có được giữ để frontend hoạt động. Một số response còn chứa entity. Mật khẩu Azure nằm ngoài source trong User Secrets.

## 2. Backend

| Folder/file gốc | Vai trò |
|---|---|
| `HorseClub.slnx` | Solution chứa API, BLL, DAL, tests và DemoSeed. |
| `Horse_BackEnd/` | Project API. |
| `HorseClub.BLL/` | Project nghiệp vụ chia theo module. |
| `HorseClub.DAL/` | Project dữ liệu SQL Server. |
| `tests/`, `tools/`, `docs/` | Kiểm thử, công cụ và tài liệu. |
| `.github/` | CI và mẫu issue/PR. |
| `.config/dotnet-tools.json` | Local .NET tools, gồm dotnet-ef. |
| `.gitignore` | Loại build/cache/runtime/secrets khỏi Git. |
| `.editorconfig` | Thống nhất định dạng code và cảnh báo namespace import không dùng. |
| `README.md`, `CONTRIBUTING.md` | Cách chạy dự án và quy tắc đóng góp. |

### 2.1. API: Horse_BackEnd

| File | Vai trò |
|---|---|
| `Program.cs` | Composition root: cấu hình, authentication, SQL, middleware và routes. Gọi `AddClubBusiness()` để đăng ký nghiệp vụ. |
| `Horse_BackEnd.csproj` | Framework/package/reference/UserSecretsId của API. |
| `GlobalUsings.cs` | Namespace dùng chung cho API. |
| `appsettings.json` | Cấu hình mặc định; không có mật khẩu Azure. |
| `appsettings.Development.json` | Ghi đè CORS/email/log cho development. |
| `Properties/launchSettings.json` | Profile HTTP 5299/HTTPS 7284. |
| `Horse_BackEnd.http` | Request mẫu gửi từ IDE. |

**`Endpoints/`**

| File | Vai trò |
|---|---|
| `AttachmentEndpoints.cs` | API tài liệu hồ sơ/ảnh ngựa. |
| `AuthEndpoints.cs` | API tài khoản và nhân sự. |
| `CareEndpoints.cs` | API chăm sóc/chuồng/sự cố. |
| `HorseEndpoints.cs` | API đăng ký/ngựa/phân công. |
| `IncidentPhotoEndpoints.cs` | API ảnh sự cố. |
| `InventoryEndpoints.cs` | API kho. |
| `MedicalEndpoints.cs` | API y tế. |
| `MetadataEndpoints.cs` | API danh mục enum. |
| `ReportingEndpoints.cs` | API dashboard/báo cáo/thông báo/audit. |
| `TrainingEndpoints.cs` | API mẫu/kế hoạch/buổi tập/kết quả. |

**`Infrastructure/`**

| File | Vai trò |
|---|---|
| `AllowedRolesFilter.cs` | Kiểm vai trò truy cập endpoint. |
| `ApiContract.cs` | OpenAPI schemas/security/multipart/status. |
| `BearerSessionHandler.cs` | Đăng nhập/refresh theo giao thức bearer ASP.NET. |
| `ErrorMiddleware.cs` | Chuyển lỗi nghiệp vụ/database thành response an toàn. |
| `HttpCurrentIdentity.cs` | Cung cấp identity của request cho interface BLL. |
| `KeyProtection.cs` | Cấu hình Data Protection key ring. |
| `OperationResultMapper.cs` | Chuyển kết quả service sang HTTP/JSON/file. |
| `ReadOnlyOperation.cs` | Metadata đánh dấu POST chỉ đọc để bỏ transaction ghi. |
| `TransactionMiddleware.cs` | Transaction ghi và buffering/cleanup upload; không giữ khóa process chung. |
| `UploadRequestAdapter.cs` | Chuyển multipart HTTP sang input upload của BLL. |
| `ValidationFilter.cs` | Kiểm DTO trước khi chạy nghiệp vụ. |

### 2.2. BLL: service theo module

| File | Vai trò |
|---|---|
| `HorseClub.BLL.csproj` | BLL reference DAL; nhúng catalog message. |
| `DependencyInjection.cs` | `AddClubBusiness()` đăng ký scoped services, helpers và workers. |
| `GlobalUsings.cs` | Namespace dùng chung trong BLL. |
| `Auth/AuthenticationService.cs` | Đăng ký, đăng nhập, OTP/reset, invitation, staff và kiểm phiên. |
| `Auth/DemoAccountSeeder.cs` | Seed tài khoản demo có chủ đích. |
| `Horses/HorseAssignmentService.cs` | Phân công nhân sự và giữ lịch sử. |
| `Horses/HorseProfileService.cs` | Danh sách/chi tiết ngựa, số đo và archive. |
| `Horses/HorseRegistrationService.cs` | Nháp, cập nhật, gửi, hủy và duyệt hồ sơ ngựa. |
| `Training/TrainingPlanService.cs` | Tạo/sửa/trạng thái/lịch sử kế hoạch. |
| `Training/TrainingSessionService.cs` | Lập lịch, giao rider, bắt đầu, skip, kết quả, đánh giá và medical guard. |
| `Training/TrainingTemplateService.cs` | Mẫu huấn luyện. |
| `Medical/MedicalService.cs` | Khám, correction, injury, restriction, điều trị, follow-up và preventive care. |
| `Care/CareService.cs` | Care task, feeding, incident, stable/stall/occupancy. |
| `Care/IncidentPhotoService.cs` | Ảnh sự cố và quyền reporter. |
| `Inventory/InventoryService.cs` | Vật tư, movement và replenishment. |
| `Attachments/AttachmentService.cs` | Tài liệu hồ sơ, ảnh ngựa, upload/download theo quyền. |
| `Reporting/ReportingService.cs` | Dashboard, báo cáo, notification và audit. |
| `Metadata/MetadataService.cs` | Enum metadata theo quyền. |

**`Common/` — chức năng dùng chung**

| File | Vai trò |
|---|---|
| `Common/ApiException.cs` | Lỗi nghiệp vụ có mã/status/reference. |
| `Common/ClubAccess.cs` | Kiểm ownership, assignment và phạm vi từng đối tượng. |
| `Common/ClubCalendar.cs` | Ngày/giờ theo timezone nghiệp vụ. |
| `Common/ClubEvents.cs` | Ghi audit, notification và training history. |
| `Common/ClubStartup.cs` | Bootstrap Manager và health database. |
| `Common/CurrentUser.cs` | Đọc/cache người dùng trong request và kiểm security stamp. |
| `Common/Ensure.cs` | Validation và assertion nghiệp vụ. |
| `Common/FileDownload.cs` | Thông tin file tải về, không phải ASP.NET IResult. |
| `Common/ICurrentIdentity.cs` | Interface đọc identity, không phụ thuộc HttpContext. |
| `Common/OperationResult.cs` | Kết quả service để API map thành response. |
| `Common/Options/BusinessOptions.cs` | Giới hạn phân trang, report, lịch và số đo. |
| `Common/Options/EmailOptions.cs` | Cấu hình SMTP/email development. |
| `Common/Options/SmtpEmailOptions.cs` | Các khóa `Email:Smtp:*`: Host, Port, Username, Password, EnableSsl. |
| `Workers/OtpEmailRenderer.cs`, `Workers/Templates/OtpEmail.html` | Định dạng email OTP thành HTML, điền mã động và encode dữ liệu người dùng. SMTP giữ phần text dự phòng. |
| `Common/Options/SecurityOptions.cs` | Chính sách mật khẩu, OTP, token và lockout. |
| `Common/Options/StorageOptions.cs` | Giới hạn file/storage. |
| `Common/Options/WorkerOptions.cs` | Batch, poll, timeout và retry của worker. |
| `Common/PageReader.cs` | Kiểm giới hạn, phân trang, count và AsNoTracking. |
| `Common/Storage/UploadFile.cs` | Tên/độ dài/hàm mở stream của file. |
| `Common/Storage/UploadFiles.cs` | Tìm file theo tên field. |
| `Common/Storage/UploadForm.cs` | Fields và files của upload. |
| `Common/Storage/UploadRequest.cs` | Input upload và cancellation. |
| `Common/Storage/UploadStorage.cs` | Lưu file an toàn và cleanup theo kết quả transaction. |

**`Workers/`**: `EmailWorker.cs` xử lý outbox/retry; `ReminderWorker.cs` tạo nhắc việc theo batch; `IClubMailSender.cs` là hợp đồng gửi email; `ClubMailSender.cs` triển khai SMTP/email development. Worker dùng SQL application lock để phối hợp nhiều host.

**`Messages/`**: `MessageKey.cs` chứa mã; `messages.en.json` chứa template; `Messages.cs` đọc/format catalog. Giữ nội dung thông báo tập trung.

**`Contracts/<Module>/`**: mỗi file là một DTO cùng tên. `*Request` mô tả input/validation; `*Response` mô tả output. Tên type và JSON được giữ khi tách file. Các file hiện có:

| Module | Các file DTO |
|---|---|
| Attachments | AttachmentCreatedResponse.cs, AttachmentResponse.cs |
| Auth | ActiveRequest.cs, EmailRequest.cs, LoginErrorResponse.cs, LoginRequest.cs, PasswordChangedResponse.cs, RefreshRequest.cs, RegisterRequest.cs, ResetRequest.cs, StaffDirectoryResponse.cs, StaffRequest.cs, StaffResponse.cs, UserResponse.cs, VerificationResponse.cs, VerifyRequest.cs |
| Care | CareCompletionRequest.cs, CareRequest.cs, CareTaskSummaryResponse.cs, IncidentPhotoCreatedResponse.cs, IncidentPhotoResponse.cs, IncidentRequest.cs, NameRequest.cs, OccupancyRequest.cs, StallRequest.cs, StallSummaryResponse.cs |
| Common | ApiErrorResponse.cs, HealthResponse.cs, MessageResponse.cs, PageResponse.cs, ReasonRequest.cs |
| Horses | AssignmentRequest.cs, HorseDetailResponse.cs, HorsePreferencesResponse.cs, MeasurementRequest.cs, RegistrationDraftRequest.cs, RegistrationRequest.cs, RegistrationReviewResponse.cs, ReviewRequest.cs |
| Inventory | InventoryRequest.cs, MovementRequest.cs, ReplenishmentRequestDto.cs, ReplenishmentReviewRequest.cs, StockMovementResponse.cs |
| Medical | FollowUpRequest.cs, InjuryRequest.cs, MedicalRequest.cs, MedicalSummaryResponse.cs, PreventiveCareSummaryResponse.cs, PreventiveCompletionRequest.cs, PreventiveRequest.cs, RestrictionRequest.cs, RestrictionSummaryResponse.cs, TreatmentRequest.cs |
| Reporting | DashboardResponse.cs, ReportCareResponse.cs, ReportKpiResponse.cs, ReportResponse.cs, ReportSeriesResponse.cs, ReportTrainingResponse.cs |
| Training | EvaluationRequest.cs, PlanDetailResponse.cs, PlanRequest.cs, PlanStatusRequest.cs, ResultRequest.cs, RiderRequest.cs, SessionDetailResponse.cs, SessionRequest.cs, TemplateRequest.cs |

### 2.3. DAL: entity, enum, mapping và migrations

| File/folder | Vai trò |
|---|---|
| `HorseClub.DAL.csproj` | EF Core SQL Server/Design và framework. |
| `GlobalUsings.cs` | Namespace entity/enum dùng trong DAL. |
| `Data/ClubDbContext.cs` | DbSets, gọi mapping và cập nhật concurrency version khi SaveChanges. |
| `Data/SqlServerClubDbContext.cs` | Context SQL Server kế thừa context chung. |
| `Data/SqlServerDesignFactory.cs` | Factory cho EF tools; cấu hình design-time không thay kết nối runtime Azure. |
| `Data/DatabaseFailures.cs` | Nhận diện lỗi SQL như deadlock. |
| `Data/WorkerDatabaseLock.cs` | Application lock theo transaction cho worker. |
| `Data/Configurations/ClubModelConfiguration.cs` | PK/FK, unique indexes, chiều dài, decimal, UTC ticks và concurrency. |
| `Data/Configurations/ReadPerformanceConfiguration.cs` | Các composite index theo truy vấn/phạm vi. |

**`Entities/` — mỗi entity một file**

| File | Đối tượng dữ liệu |
|---|---|
| `Attachment.cs` | Metadata tài liệu/ảnh hồ sơ. |
| `AuditEvent.cs` | Lịch sử thao tác. |
| `CareTask.cs` | Công việc chăm sóc/feeding. |
| `EmailChallenge.cs` | OTP/reset/invitation challenge. |
| `EmailMessage.cs` | Email outbox và trạng thái giao nhận. |
| `Entity.cs` | Lớp cơ sở ID/CreatedAt/Version. |
| `Horse.cs` | Ngựa đã được tiếp nhận. |
| `HorseRegistration.cs` | Hồ sơ đăng ký ngựa. |
| `Incident.cs` | Sự cố. |
| `IncidentPhoto.cs` | Metadata ảnh sự cố. |
| `Injury.cs` | Chấn thương. |
| `InventoryItem.cs` | Vật tư và tồn kho. |
| `Measurement.cs` | Số đo ngựa theo ngày. |
| `MedicalFollowUp.cs` | Tái khám/clearance. |
| `MedicalRecord.cs` | Hồ sơ khám và correction. |
| `MedicalRestriction.cs` | Hạn chế y tế có hiệu lực. |
| `Notification.cs` | Thông báo người dùng. |
| `PreventiveCare.cs` | Chăm sóc y tế định kỳ. |
| `ReplenishmentRequest.cs` | Yêu cầu bổ sung. |
| `SessionResult.cs` | Kết quả buổi tập. |
| `Stable.cs` | Khu chuồng. |
| `StaffAssignment.cs` | Lịch sử phân công nhân sự. |
| `Stall.cs` | Ô chuồng. |
| `StallOccupancy.cs` | Lịch sử ngựa ở ô chuồng. |
| `StockMovement.cs` | Lịch sử nhập/xuất. |
| `TrainerEvaluation.cs` | Đánh giá của trainer. |
| `TrainingPlan.cs` | Kế hoạch huấn luyện. |
| `TrainingRevision.cs` | Snapshot lịch sử training. |
| `TrainingSession.cs` | Buổi tập. |
| `TrainingTemplate.cs` | Mẫu huấn luyện. |
| `TreatmentPlan.cs` | Kế hoạch điều trị. |
| `User.cs` | Tài khoản và trạng thái bảo mật. |

**`Enums/`**: mỗi file chứa enum cùng tên, thể hiện role/status/type/policy. Các giá trị lưu trong database không được tùy ý đổi số.

`AttachmentType.cs`, `AuditAction.cs`, `CareStatus.cs`, `CareType.cs`, `ChallengePurpose.cs`, `CleaningStatus.cs`, `DatabaseProvider.cs`, `EmailDeliveryMode.cs`, `HealthStatus.cs`, `HorseGender.cs`, `IncidentSeverity.cs`, `IncidentType.cs`, `InjurySeverity.cs`, `InjuryStatus.cs`, `InjuryType.cs`, `Intensity.cs`, `NotificationType.cs`, `PlanStatus.cs`, `PreventiveCareType.cs`, `RegistrationStatus.cs`, `ReplenishmentStatus.cs`, `ReportGrouping.cs`, `Role.cs`, `SessionStatus.cs`, `TrainingType.cs`.

**`Data/Migrations/SqlServer/`**:

| Nhóm file | Vai trò |
|---|---|
| `20261003125915_InitialSqlServer.cs` + `.Designer.cs` | Schema ban đầu và model tương ứng. |
| `20261004130807_WorkerDeliveryReliability.cs` + `.Designer.cs` | Schema phục vụ reliability của worker. |
| `20261005060507_PartialRegistrationDraft.cs` + `.Designer.cs` | Cho phép lưu hồ sơ nháp từng phần. |
| `20261007115208_QueryPerformanceIndexes.cs` + `.Designer.cs` | Composite indexes cho scope/dashboard/history; không đổi bảng/cột. |
| `SqlServerClubDbContextModelSnapshot.cs` | Model mới nhất để EF so sánh migration tiếp theo. |

File `.cs` có Up/Down; Designer/Snapshot do EF sinh. Không sửa SQL riêng rồi bỏ quên model/migration. Cấu trúc bảng hiện có được giữ qua refactor namespace/file.

### 2.4. `tests/Horse_BackEnd.Tests/`

| File | Mục đích |
|---|---|
| `Horse_BackEnd.Tests.csproj` | Package test và reference tới backend. |
| `ClubFactory.cs` | Khởi tạo ứng dụng/database test và hỗ trợ fixture. Database test tách khỏi HRCMS thật. |
| `SqlServerFactAttribute.cs` | Điều kiện chạy test cần SQL Server. |
| `WorkflowTests.cs` | Các luồng API/nghiệp vụ phối hợp nhiều thao tác. |
| `PerformanceTests.cs` | Query count, reads không tracking và SMTP không chặn write độc lập. |
| `AdditionalTests.cs` | Các trường hợp bổ sung ngoài nhóm test chính. |
| `ApiContractTests.cs` | Contract HTTP/OpenAPI và hình dạng phản hồi. |
| `DatabaseWorkflowTests.cs` | Luồng nghiệp vụ gắn với database. |
| `FrontendReadinessTests.cs` | CORS và khả năng kết nối SQL Server phục vụ tích hợp frontend. |
| `IntakeTests.cs` | Hồ sơ tiếp nhận/đăng ký ngựa. |
| `TrainingTests.cs` | Luồng huấn luyện. |
| `LayerAndMessageTests.cs` | Ràng buộc layer/dependency và catalog thông báo. |
| `SqlServerConcurrencyTests.cs` | Xung đột và thao tác đồng thời trên SQL Server. |
| `StorageRecoveryTests.cs` | Trường hợp phục hồi/nhất quán storage. |
| `StorageSqlServerTests.cs` | Quan hệ giữa file storage và giao dịch SQL Server. |
| `WorkerTests.cs` | Hành vi công việc nền. |
| `WorkerSqlServerTests.cs` | Worker với locking/giao nhận trên SQL Server. |

Tên test cho biết nhóm kiểm tra; đọc từng method để biết chính xác scenario được bảo vệ. Test không phải module production và không thay thế kiểm thử đầy đủ trên môi trường triển khai.

### 2.5. `tools/` và `.github/`

| File | Giải thích |
|---|---|
| `tools/Set-AzureSqlConnection.ps1` | Hỏi mật khẩu qua input ẩn và lưu connection string vào User Secrets. Không đưa mật khẩu vào Git. |
| `tools/Update-AzureSqlDatabase.ps1` | Kiểm tra đúng Azure server/database, áp dụng schema và xác nhận số bảng/migration. |
| `tools/HorseClub.DemoSeed/HorseClub.DemoSeed.csproj` | Project console cho công cụ seed. |
| `tools/HorseClub.DemoSeed/Program.cs` | Điểm chạy seed tài khoản demo theo cấu hình. |
| `.github/workflows/backend-ci.yml` | Pipeline build và kiểm thử SQL Server trên GitHub Actions. |
| `.github/ISSUE_TEMPLATE/bug.md` | Mẫu báo lỗi. |
| `.github/ISSUE_TEMPLATE/feature.md` | Mẫu đề xuất chức năng. |
| `.github/pull_request_template.md` | Mẫu mô tả thay đổi và kiểm chứng khi tạo PR. |

### 2.6. `docs/`: đọc tài liệu nào để làm gì?

| File | Nội dung chính |
|---|---|
| `ARCHITECTURE.md` | Tổng quan kiến trúc; đã cập nhật theo cấu trúc service/module hiện tại. |
| `THREE_LAYER_AND_MESSAGES.md` | Quy tắc ba layer và cách dùng thông báo chung. |
| `BACKEND_GUIDE.md` | Hướng dẫn backend, cấu hình và giới hạn thực tế. |
| `API_CONTRACT.md` | Quy ước API, dữ liệu, lỗi và hành vi client cần biết. |
| `CONTRACT_AND_SCREEN_MAP.md` | Ánh xạ chức năng/màn hình với contract. |
| `FRONTEND_READINESS.md` | Trạng thái và kiểm chứng khả năng tích hợp frontend. |
| `AZURE_SQL_SETUP.md` | Cách cấu hình và tạo schema Azure SQL. |
| `DATABASE_SQL.md` | Giải thích schema/script SQL. |
| `SQLSERVER_TESTING.md` | Thiết lập và chạy tests SQL Server. |
| `STORAGE_AND_RECOVERY.md` | Lưu file và phục hồi tính nhất quán. |
| `WORKER_RELIABILITY.md` | Cơ chế worker, locking và giao nhận. |
| `POLICY_DECISIONS.md` | Các quyết định chính sách nghiệp vụ. |
| `T02_POLICY_BASELINE.md` | Baseline chính sách cho công việc/phạm vi tương ứng. |
| `BE-003_INTAKE_AND_ASSIGNMENT.md` | Thiết kế/hướng dẫn intake và assignment. |
| `BE-004_TRAINING.md` | Thiết kế/hướng dẫn training. |
| `BE02_DEMO_SETUP.md` | Thiết lập demo phần xác thực/tài khoản. |
| `BACKEND_TASK_ASSIGNMENT.md` | Phân công backend. |
| `BACKLOG.md` | Các công việc còn lại/đề xuất. |
| `IMPLEMENTATION_PLAN.md` | Kế hoạch triển khai. |
| `PROJECT_ANALYSIS_AND_TASKS.md` | Phân tích dự án và công việc. |
| `ENVIRONMENT_SETUP_PROGRESS.md` | Tiến độ thiết lập môi trường. |
| `GITHUB_SETUP.md` | Hướng dẫn GitHub/quy trình repository. |
| `PROJECT_STRUCTURE_EXPLAINED.md` | Tài liệu giải thích folder/file mà bạn đang đọc. |
| `database.sql` | Script SQL được lưu trong tài liệu; kiểm nội dung/phạm vi trước khi dùng. |
| `azure-sql-schema.sql` | Script idempotent sinh từ bốn migration SQL Server, dùng cho triển khai Azure đã chuẩn bị. |
| `Export-ContractInventory.ps1` | Công cụ xuất inventory contract. |
| `demo/BE02-accounts.json` | Dữ liệu cấu hình tài khoản demo. |
| `http/BE-003-intake.http` | Request mẫu cho intake/assignment. |
| `http/BE-004-training.http` | Request mẫu cho training. |

Trong **`docs/contracts/`**:

| File | Giải thích |
|---|---|
| `openapi.baseline.json` | Bản OpenAPI baseline để so sánh. |
| `openapi.current.json` | Bản OpenAPI hiện tại đã xuất tại thời điểm cập nhật. |
| `api-inventory.csv` | Inventory endpoint đã lưu. |
| `api-inventory.current.csv` | Inventory endpoint phiên bản hiện tại được lưu. |
| `openapi-audit.json` | Kết quả audit contract đã lưu. |
| `openapi-audit.current.json` | Kết quả audit phiên bản hiện tại được lưu. |
| `anonymous-probes.json` | Kết quả probe endpoint bằng truy cập anonymous đã lưu. |

Các file `current` là snapshot, không tự thay đổi mỗi lần sửa code. OpenAPI của API đang chạy phản ánh runtime; tài liệu kế hoạch không tự chứng minh tính năng đã triển khai.

## 3. Frontend: repository React riêng

### 3.1. File gốc và điểm khởi động

| Folder/file | Giải thích |
|---|---|
| `package.json` | Dependencies và scripts chạy dev/build/lint/tests/live tests. |
| `package-lock.json` | Khóa phiên bản dependency để cài đặt nhất quán. |
| `vite.config.js` | Cấu hình Vite, cổng 5173 và proxy development đến API 5299. |
| `eslint.config.js` | Quy tắc kiểm tra JavaScript/React. |
| `.env.example` | Mẫu cấu hình URL API/proxy; không chứa SQL credentials. |
| `.gitignore` | Loại dependency, build và cấu hình local khỏi Git. |
| `index.html` | HTML gốc, phần tử `root` để React mount. |
| `README.md` | Cách chạy frontend. |
| `FIGMA_BE02_UI.md` | Tài liệu UI/Figma cho phần tài khoản. |
| `docs/API_INTEGRATION.md` | Hướng dẫn kết nối API và chạy kiểm thử tích hợp. |
| `public/favicon.svg` | Icon tab trình duyệt. |
| `src/main.jsx` | Mount React, bật StrictMode và import Bootstrap/CSS dùng chung. |
| `src/App.jsx` | Ghép BrowserRouter, AuthProvider và AppRoutes. |
| `src/style/global.css` | Styles toàn cục được import tại entry point. |

### 3.2. `src/routes/` và `src/layouts/`

| File | Giải thích |
|---|---|
| `routes/AppRoutes.jsx` | Bản đồ URL → page; phân chia public và khu vực nghiệp vụ. Đọc file này để biết màn hình nào thật sự có route. |
| `routes/ProtectedRoute.jsx` | Bảo vệ route cần đăng nhập. |
| `routes/RoleRoute.jsx` | Kiểm role khi truy cập màn hình. Quyền thật vẫn được backend kiểm lại. |
| `routes/navigation.js` | Menu điều hướng theo vai trò. |
| `routes/redirects.js` | Quy tắc chuyển hướng phù hợp trạng thái/đường dẫn. |
| `layouts/AppLayout.jsx` | Khung ứng dụng sau đăng nhập: menu, thông tin người dùng và logout. |
| `layouts/PublicLayout.jsx` | Khung trang công khai/xác thực. |

### 3.3. `src/pages/`: các màn hình hiện có

| File | Màn hình/nội dung |
|---|---|
| `Dashboard.jsx` | Tổng quan dùng dữ liệu API theo phạm vi người dùng. |
| `NotFound.jsx` | URL không tồn tại. |
| `PermissionDenied.jsx` | Người dùng không có quyền xem màn hình. |
| `auth/Login.jsx` | Đăng nhập. |
| `auth/Register.jsx` | Đăng ký tài khoản. |
| `auth/VerifyEmail.jsx` | Xác minh email. |
| `auth/ForgotPassword.jsx` | Yêu cầu khôi phục mật khẩu. |
| `auth/ResetPassword.jsx` | Đặt lại mật khẩu. |
| `auth/AcceptInvitation.jsx` | Nhận lời mời tài khoản nhân sự/thiết lập mật khẩu. |
| `registrations/RegistrationList.jsx` | Danh sách hồ sơ đăng ký ngựa. |
| `registrations/RegistrationCreate.jsx` | Tạo hồ sơ qua wizard. |
| `registrations/RegistrationDetail.jsx` | Xem/chỉnh sửa và thao tác hồ sơ theo trạng thái/quyền. |
| `reviews/ReviewPages.jsx` | Các trang xét duyệt hồ sơ dành cho quyền quản lý. |
| `horses/HorsePages.jsx` | Gom các trang danh sách/chi tiết ngựa, ảnh và phân công. Một file có nhiều component màn hình. |
| `training/TemplatePages.jsx` | Các trang quản lý mẫu huấn luyện. |
| `training/PlanPages.jsx` | Các trang kế hoạch huấn luyện và thông tin liên quan. |
| `training/SessionPages.jsx` | Các trang buổi tập, giao rider, bắt đầu, kết quả và đánh giá theo quyền. |

Backend có thêm y tế, chăm sóc và kho, nhưng frontend hiện chưa có các nhóm page chuyên biệt tương ứng trong cây `src/pages/`. Có API không đồng nghĩa đã có màn hình hoàn chỉnh.

### 3.4. `src/components/`: UI dùng lại

| File | Giải thích |
|---|---|
| `WorkflowUI.jsx` | Heading, trạng thái loading/error, pagination, field, badge và thông báo thao tác dùng chung. |
| `auth/AuthButton.jsx` | Nút dùng trong giao diện xác thực. |
| `auth/AuthForm.jsx` | Khung form xác thực. |
| `auth/AuthInput.jsx` | Input xác thực và lỗi field. |
| `auth/LogoutButton.jsx` | Nút đăng xuất kết nối logic auth. |
| `auth/PasswordSetupForm.jsx` | Form thiết lập mật khẩu dùng lại. |
| `registrations/RegistrationAttachments.jsx` | UI tài liệu/ảnh của hồ sơ. |
| `registrations/RegistrationError.jsx` | Hiển thị lỗi thao tác/nghiệp vụ. |
| `registrations/RegistrationForm.jsx` | Các field nhập hồ sơ. |
| `registrations/RegistrationStatus.jsx` | Hiển thị trạng thái hồ sơ. |
| `registrations/RegistrationWizard.jsx` | Điều phối các bước nhập hồ sơ, lưu nháp và thao tác liên quan. |

Page là màn hình gắn với URL; component là phần UI được ghép vào màn hình. Khi cần sửa một input dùng ở nhiều trang, sửa component giúp tránh lặp code.

### 3.5. `src/context/`: trạng thái và hooks

| File | Giải thích |
|---|---|
| `AuthContext.jsx` | Cung cấp trạng thái xác thực cho cây React và khôi phục session. |
| `useAuth.js` | Hook để component đọc/sử dụng auth context. |
| `useAuthForm.js` | Quản lý values, validation, pending, lỗi và chống submit đồng thời cho form auth. |
| `useAuthCooldown.js` | Đếm thời gian chờ gửi lại yêu cầu như OTP; lưu deadline theo mục đích trong sessionStorage. |
| `useMutation.js` | Quản lý thao tác ghi: pending, lỗi, thông báo và trạng thái kết quả chưa chắc chắn khi mạng lỗi. |
| `useRegistrationResource.js` | Quản lý việc tải resource, loading/error và reload. |

Folder này có cả provider và custom hook; không phải mọi file ở đây đều tạo một React Context riêng.

### 3.6. `src/services/`: nơi nối frontend với API

| File | Giải thích |
|---|---|
| `api.js` | Axios client chung: base URL, Bearer token, refresh khi phù hợp và xử lý response. Đây là chỗ cấu hình HTTP chung. |
| `apiError.js` | Chuẩn hóa lỗi API/network để UI sử dụng nhất quán. |
| `authService.js` | Gọi API đăng nhập/đăng ký/OTP/reset/invitation/me/refresh/logout. |
| `authValidation.js` | Validation và policy baseline cho form auth phía client. Backend vẫn là nơi quyết định cuối cùng. |
| `sessionStore.js` | Lưu/đọc phiên và phát thay đổi trạng thái; khôi phục user qua API, không tin profile cache làm bằng chứng quyền. |
| `registrationService.js` | Gọi API hồ sơ: danh sách, tạo/sửa nháp, gửi, hủy và dữ liệu liên quan. |
| `registrationValidation.js` | Kiểm field và chuẩn hóa dữ liệu hồ sơ phía client. |
| `registrationAttachments.js` | Kiểm file, tạo multipart và upload/download attachment được bảo vệ. |
| `clubService.js` | API ngựa, ảnh, danh mục nhân sự, phân công và xét duyệt. |
| `trainingService.js` | API mẫu, kế hoạch, buổi tập, kết quả, đánh giá và lịch sử. |
| `trainingPayload.js` | Chuẩn bị payload training: field được phép gửi, kiểu số và thời gian. |
| `historyView.js` | Chuyển snapshot JSON lịch sử thành nội dung dễ hiển thị. |
| `workflowHelpers.js` | Helper trạng thái, quyền thao tác và dữ liệu dùng chung cho các flow. |

`services/` frontend là lớp gọi HTTP và chuẩn bị dữ liệu UI; `Services/` backend là xử lý nghiệp vụ server. Hai folder trùng ý nghĩa tên nhưng trách nhiệm khác nhau.

### 3.7. Constants, utilities và tests

| File | Giải thích |
|---|---|
| `src/constants/roles.js` | Định nghĩa role phía client và các ánh xạ dùng trong UI. |
| `src/constants/registration.js` | Hằng số/trạng thái/danh mục cho hồ sơ đăng ký. |
| `src/constants/registrationWizard.js` | Cấu hình các bước wizard hồ sơ. |
| `src/constants/training.js` | Hằng số/trạng thái/danh mục training. |
| `src/utils/userDisplay.js` | Helper hiển thị tên/thông tin người dùng. |
| `tests/auth.test.js` | Kiểm logic xác thực. |
| `tests/accountLifecycle.test.js` | Kiểm vòng đời tài khoản. |
| `tests/be02.test.js` | Kiểm phần UI/flow tài khoản BE02. |
| `tests/registrations.test.js` | Kiểm hồ sơ đăng ký. |
| `tests/routing.test.js` | Kiểm route/điều hướng/phân quyền frontend. |
| `tests/live/api.test.mjs` | Gọi service thật tới API chạy thực tế; cấu hình riêng cho dữ liệu/fixture test. |

Tests thường dùng mock để kiểm frontend độc lập; live tests dùng API thật. Đây là hai mức kiểm tra khác nhau.

## 4. Thư mục sinh tự động hoặc chỉ phục vụ máy local

| Folder | Giải thích |
|---|---|
| `.git/` | Lịch sử Git, branch và metadata repository. Không sửa thủ công. |
| `.vs/` | Cache/cấu hình local của Visual Studio. |
| `bin/` | Kết quả compile .NET. Có thể tạo lại bằng build. |
| `obj/` | File trung gian/restore/build .NET. |
| `node_modules/` | Thư viện frontend được cài từ package lock. Không phải mã nguồn do nhóm viết. |
| `dist/` | Kết quả Vite build frontend dùng triển khai. |
| `Horse_BackEnd/App_Data/` | Dữ liệu runtime như file upload, key ring hoặc email development tùy cấu hình. Không xem đây là file build có thể tùy tiện xóa. |
| `TestResults/` | Kết quả/log/script kiểm chứng local. |
| `merge-backup-20261007/` | Bản giữ lại công việc local khi xử lý merge trước đó; không thuộc kiến trúc runtime. |

User Secrets nằm ngoài repository theo cơ chế .NET. Không đưa mật khẩu, token hoặc key ring vào frontend/source control. Việc còn file SQLite trong binary/cache cũ không có nghĩa runtime hiện còn dùng SQLite.

## 5. Theo dõi một thao tác

Owner mở `RegistrationCreate.jsx` → `RegistrationWizard.jsx` → `registrationService.js` → Axios → `HorseEndpoints.cs` → `HorseRegistrationService.cs` → `ClubDbContext.cs` → Azure SQL. Response quay lại service/hook/page. Khi duyệt, backend cập nhật hồ sơ, tạo Horse/số đo/audit/notification trong cùng transaction.

## 6. Thứ tự đọc và nơi sửa

Đọc `Program.cs` → `DependencyInjection.cs` → một endpoint → service của module → DTO/entity → `ClubDbContext.cs`/Configurations. Phía frontend đọc `main.jsx` → `App.jsx` → `AppRoutes.jsx` → page → service.

Sửa UI ở frontend pages/components/CSS. Sửa nghiệp vụ ở BLL module. Sửa dữ liệu ở DAL entity/mapping/migration. Sửa HTTP ở API. Thay SQL connection ở backend secrets; frontend chỉ biết URL API.

Kiểm chứng và giới hạn hiệu năng được ghi tại [STRUCTURE_AND_PERFORMANCE.md](STRUCTURE_AND_PERFORMANCE.md). Các thay đổi đang ở working tree local cho đến khi commit/push.
