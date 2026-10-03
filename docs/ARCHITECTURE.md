# Kiến trúc HorseClub

Monorepo để nhóm phối hợp FE/BE và API contract trong cùng PR. Backend đã triển khai theo cấu trúc bên dưới; frontend vẫn là phần dự kiến. Xem BACKEND_GUIDE.md để biết cấu hình và giới hạn thực tế.

```text
HorseClub/
  Horse_BackEnd/          ASP.NET Core API hiện có
  Horse_FrontEnd/         React + TypeScript, dự kiến tạo sau
  tests/                 integration tests backend; E2E UI chưa có
  docs/                  kế hoạch, contract, ERD, quyết định
  .github/               CI và mẫu issue/PR
```

## Công nghệ

React + TypeScript + Vite vẫn là đề xuất frontend. Backend dùng ASP.NET Core, EF Core, SQLite local và hỗ trợ SQL Server qua provider/context/migration riêng. ASP.NET Core PasswordHasher và opaque bearer tokens bảo vệ đăng nhập; email OTP/reset và staff invitation có expiry/attempt limit. Upload được đọc qua endpoint có quyền, tách khỏi webroot. Không triển khai microservices cho quy mô này.

Backend có refresh token và account-wide revocation qua security stamp. Frontend cần chốt chiến lược lưu token trước khi tích hợp. Hosting và SMTP thật chưa triển khai; SQL Server chưa được test trên server thật.

## Module và dữ liệu

| Module | Entity dự kiến và quan hệ |
| --- | --- |
| Identity | User, Role, Permission, UserRole, verification/reset tokens; owner và staff cùng identity nhưng khác lifecycle |
| Horse Intake | HorseRegistration → documents, measurements, declared health, boarding, preferred staff; owner là người gửi |
| Horse | Horse → Owner; registration approved liên kết duy nhất Horse; measurements và boarding có history |
| Assignment | HorseStaffAssignment, TrainerAssignment: staff, horse, start/end, status; không ghi đè history |
| Training | StandardTemplate → HorseTrainingPlan → TrainingSession → SessionResult → TrainerEvaluation; session có assigned rider |
| Medical | MedicalRecord → Injury/TreatmentPlan; MedicalRestriction/Lock và FollowUp/Clearance có hiệu lực theo thời gian |
| Care | Stable/Stall và occupancy history; CareTask có thể liên kết TreatmentPlan; FeedingRecord; Incident |
| Inventory | Item, StockMovement, ReplenishmentRequest; tồn kho dựa trên movement, không sửa số mà mất history |
| Common | Notification, AuditEvent, attachment metadata, report projections và preventive care schedules |

Schema vật lý hiện nằm trong Data/ClubDbContext.cs và migrations theo provider. Reports đọc dữ liệu nguồn theo quyền, không tạo bản sao y tế công khai. Bảng trên là bản đồ module; tên table thực tế theo DbSet trong context.

## API contract và tính nhất quán

Nhóm thống nhất REST/OpenAPI trước khi FE tích hợp: pagination, filters, sort whitelist, timestamps có timezone, unit đo, lỗi validation và mã lỗi nghiệp vụ. Session blocked trả reason/reference phù hợp quyền; không lộ diagnosis cho role không được xem.

Server kiểm tra role và phạm vi entity trên mọi endpoint. Approval tạo Horse và đánh dấu registration trong cùng transaction, retry không tạo ngựa trùng. Session start/assignment recheck medical state tại thời điểm thao tác; dùng transaction/concurrency control để tránh race với Vet cập nhật lock. Result submission chống nộp trùng; audit ghi actor, action, entity và timestamp, tránh secrets/CCCD trong logs.

Upload giới hạn loại/size, xác minh nội dung, tên lưu ngẫu nhiên, kiểm quyền tải và chính sách retention. OTP có TTL, giới hạn attempts/resend và không lưu plaintext. Dữ liệu demo phải giả lập.
