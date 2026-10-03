# Kiến trúc dự kiến

Đề xuất monorepo để nhóm phối hợp FE/BE và API contract trong cùng PR. Giữ backend hiện tại; không di chuyển project cho đến khi nhóm thống nhất.

```text
HorseClub/
  Horse_BackEnd/          ASP.NET Core API hiện có
  Horse_FrontEnd/         React + TypeScript, dự kiến tạo sau
  tests/                 integration và E2E, dự kiến
  docs/                  kế hoạch, contract, ERD, quyết định
  .github/               CI và mẫu issue/PR
```

## Công nghệ đề xuất

React + TypeScript + Vite cho frontend; ASP.NET Core với EF Core và SQL Server cho backend/database. Dùng framework identity phù hợp, password hashing chuẩn, email OTP/reset, access control ở server. Storage cho attachments tách khỏi public webroot; truy cập qua endpoint có quyền hoặc URL ngắn hạn. Không triển khai microservices cho quy mô này.

Đây là đề xuất. Database, frontend, authentication transport và hosting chưa được triển khai. Nếu dùng cookie cần CSRF protection; nếu dùng token cần chốt lưu trữ/refresh/revocation. Không dùng token trong localStorage như quyết định mặc định chưa đánh giá.

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

ERD vật lý và tên entity cuối cùng cần chốt tuần 1. Reports đọc dữ liệu nguồn theo quyền, không tạo bản sao y tế công khai. Danh sách trên không phải schema đã có.

## API contract và tính nhất quán

Nhóm thống nhất REST/OpenAPI trước khi FE tích hợp: pagination, filters, sort whitelist, timestamps có timezone, unit đo, lỗi validation và mã lỗi nghiệp vụ. Session blocked trả reason/reference phù hợp quyền; không lộ diagnosis cho role không được xem.

Server kiểm tra role và phạm vi entity trên mọi endpoint. Approval tạo Horse và đánh dấu registration trong cùng transaction, retry không tạo ngựa trùng. Session start/assignment recheck medical state tại thời điểm thao tác; dùng transaction/concurrency control để tránh race với Vet cập nhật lock. Result submission chống nộp trùng; audit ghi actor, action, entity và timestamp, tránh secrets/CCCD trong logs.

Upload giới hạn loại/size, xác minh nội dung, tên lưu ngẫu nhiên, kiểm quyền tải và chính sách retention. OTP có TTL, giới hạn attempts/resend và không lưu plaintext. Dữ liệu demo phải giả lập.
