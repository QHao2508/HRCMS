# Giải thích function: Horse_BackEnd-Endpoints

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## Horse_BackEnd/Endpoints/AttachmentEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### AttachmentEndpoints.MapAttachments(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Attachments với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapAttachments. |

Lời gọi chính: `api.MapGet`, `moduleService.GetHorsePhoto`, `api.MapGroup`, `a.MapGet`, `moduleService.ListAttachments`, `a.MapPost`, `moduleService.UploadAttachment`, `UploadRequestAdapter.Create`, `moduleService.DownloadAttachment`.

## Horse_BackEnd/Endpoints/AuthEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### AuthEndpoints.MapAuth(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Auth với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapAuth. |

Lời gọi chính: `api.MapGroup`, `auth.MapPost`, `moduleService.RegisterAccount`, `moduleService.VerifyEmail`, `moduleService.ResendVerification`, `moduleService.ForgotPassword`, `auth.MapGet`, `moduleService.GetProfile`, `moduleService.Logout`, `staff.MapGet`, `moduleService.ListStaff`, `moduleService.ListStaffDirectory`, `staff.MapPost`, `moduleService.InviteStaff`, `staff.MapPut`, `moduleService.SetStaffActive`.

### AuthEndpoints.MapPassword(auth, route, purpose)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Password với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `auth` | Giá trị kiểu RouteGroupBuilder dùng trong MapPassword. |
| `route` | Giá trị kiểu string dùng trong MapPassword. |
| `purpose` | Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác. |

Lời gọi chính: `auth.MapPost`, `moduleService.ChangePassword`.

## Horse_BackEnd/Endpoints/BrandingEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### BrandingEndpoints.MapBranding(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Branding với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapBranding. |

Lời gọi chính: `api.MapGet`, `branding.OpenLogo`, `Results.Stream`.

## Horse_BackEnd/Endpoints/CareEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### CareEndpoints.MapCare(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Care với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapCare. |

Lời gọi chính: `api.MapGroup`, `c.MapGet`, `moduleService.ListTasks`, `c.MapPost`, `moduleService.CreateTask`, `moduleService.RecordTask`, `moduleService.ListIncidents`, `moduleService.ReportIncident`, `moduleService.ResolveIncident`, `moduleService.ListStables`, `moduleService.CreateStable`, `moduleService.ListStalls`, `moduleService.CreateStall`, `moduleService.OccupyStall`, `moduleService.VacateStall`, `moduleService.MarkStallClean`.

## Horse_BackEnd/Endpoints/HorseEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### HorseEndpoints.MapHorses(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Horses với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapHorses. |

Lời gọi chính: `api.MapGroup`, `r.MapPost`, `moduleService.CreateDraft`, `r.MapGet`, `moduleService.ListRegistrations`, `moduleService.GetRegistration`, `r.MapPut`, `moduleService.UpdateDraft`, `moduleService.SubmitRegistration`, `moduleService.ReviewRegistration`, `moduleService.CancelRegistration`, `h.MapGet`, `moduleService.ListHorses`, `moduleService.GetHorse`, `h.MapPost`, `moduleService.AssignStaff`.

## Horse_BackEnd/Endpoints/IncidentPhotoEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### IncidentPhotoEndpoints.MapIncidentPhotos(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module IncidentPhotos với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapIncidentPhotos. |

Lời gọi chính: `api.MapGroup`, `group.MapGet`, `moduleService.ListPhotos`, `group.MapPost`, `moduleService.UploadPhoto`, `UploadRequestAdapter.Create`, `moduleService.DownloadPhoto`.

## Horse_BackEnd/Endpoints/InventoryEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### InventoryEndpoints.MapInventory(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Inventory với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapInventory. |

Lời gọi chính: `api.MapGroup`, `inventory.MapGet`, `moduleService.ListItems`, `inventory.MapPost`, `moduleService.CreateItem`, `moduleService.ListMovements`, `moduleService.RecordMovement`, `moduleService.ArchiveItem`, `moduleService.ListReplenishments`, `moduleService.RequestReplenishment`, `moduleService.ReviewReplenishment`.

## Horse_BackEnd/Endpoints/MedicalEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### MedicalEndpoints.MapMedical(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Medical với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapMedical. |

Lời gọi chính: `api.MapGroup`, `m.MapGet`, `moduleService.GetSummary`, `moduleService.ListExaminations`, `m.MapPost`, `moduleService.CreateExamination`, `moduleService.ListInjuries`, `m.MapPut`, `moduleService.CorrectExamination`, `moduleService.RecordInjury`, `moduleService.ListRestrictions`, `moduleService.CreateRestriction`, `moduleService.ListTreatments`, `moduleService.CreateTreatment`, `moduleService.ListFollowUps`, `moduleService.RecordFollowUp`.

## Horse_BackEnd/Endpoints/MetadataEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### MetadataEndpoints.MapMetadata(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Metadata với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapMetadata. |

Lời gọi chính: `api.MapGet`, `moduleService.GetEnums`.

## Horse_BackEnd/Endpoints/ReportingEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### ReportingEndpoints.MapReporting(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Reporting với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapReporting. |

Lời gọi chính: `api.MapGroup`, `n.MapGet`, `moduleService.ListNotifications`, `n.MapPost`, `moduleService.MarkNotificationRead`, `api.MapGet`, `moduleService.ListAudit`, `moduleService.BuildReport`, `moduleService.GetDashboard`.

## Horse_BackEnd/Endpoints/TrainingEndpoints.cs

Khai báo URL, phương thức HTTP, authorization/rate limit và schema response theo module; nghiệp vụ nằm ở BLL.

### TrainingEndpoints.MapTraining(api)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký endpoint HTTP của module Training với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.

| Đầu vào | Ý nghĩa |
|---|---|
| `api` | Giá trị kiểu RouteGroupBuilder dùng trong MapTraining. |

Lời gọi chính: `api.MapGroup`, `t.AddEndpointFilter`, `t.MapGet`, `moduleService.ListTemplates`, `t.MapPost`, `moduleService.CreateTemplate`, `t.MapPut`, `moduleService.UpdateTemplate`, `moduleService.ArchiveTemplate`, `moduleService.Create`, `moduleService.ListPlans`, `moduleService.GetPlan`, `moduleService.UpdatePlan`, `moduleService.SetPlanStatus`, `moduleService.ListHistory`, `moduleService.ListSessions`.
