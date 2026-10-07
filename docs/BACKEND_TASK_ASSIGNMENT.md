# Bảng phân công nhiệm vụ backend HorseClub

Ngày lập: 04/10/2026. Phạm vi: backend và chuẩn bị tích hợp frontend. Thiết kế frontend đã hoàn thành trên Figma theo thông tin của nhóm; mã frontend sẽ được push sau. Bảng này không giao việc dựng lại giao diện.

Tạm chia thành 5 vị trí **BE01–BE05**, chưa gán tên người thật. Nếu nhóm ít hơn 5 người, gộp BE01 + BE05 và BE02 + BE04; giữ BE03 phụ trách medical guards và phối hợp BE02. Mỗi task chỉ có một người chịu trách nhiệm chính; reviewer là người khác. Nhóm điền tên và deadline khi nhận việc.

## Hiện trạng để nhận việc đúng

- Đã có backend 3 layer: `Horse_BackEnd` API → `HorseClub.BLL` Business → `HorseClub.DAL` Data, enum nghiệp vụ và catalog 152 message chung.
- Đã có API các module dưới đây và 21 kiểm thử đạt trong lần kiểm tra gần nhất. Công việc chính là rà soát, hoàn thiện, kiểm chứng và tích hợp; không viết lại toàn bộ module.
- SQL Server local đã có database `HorseClub` với 31 bảng nghiệp vụ; backend kết nối Windows Authentication qua User Secrets. Health/OpenAPI đã hoạt động. Đây chưa phải kiểm chứng toàn bộ nghiệp vụ trên SQL Server hoặc production.
- Mã backend đang được review trong PR #1 trên nhánh `feat/backend-core`. Trước khi tạo nhánh việc mới, thống nhất base branch với lead để không bắt đầu từ `main` chưa có các thay đổi backend.

## Phân công theo người phụ trách

| Vị trí | Thành viên | Module chịu trách nhiệm | Nơi sửa chính | Reviewer |
| --- | --- | --- | --- | --- |
| BE01 – Nền tảng & tích hợp | Chưa gán | Auth/OTP/token, staff, RBAC chung, API contract, cấu hình/CI và điều phối tích hợp | BLL `AuthenticationService`, `AuthenticationService`, helpers; API startup/pipeline/auth routes | BE05; BE03 kiểm quyền dữ liệu y tế |
| BE02 – Ngựa & huấn luyện | Chưa gán | Intake/approval, assignment, templates/plans/sessions/results/evaluation | BLL `HorseRegistrationService`, `TrainingSessionService`, `HorseRegistrationService`, `TrainingPlanService/TrainingSessionService`; routes tương ứng | BE03 kiểm medical guard; BE01 kiểm auth/scope |
| BE03 – Thú y | Chưa gán | Hồ sơ khám, chấn thương, restriction, treatment, follow-up/clearance và preventive care | BLL `MedicalService`; phần medical guard trong `TrainingSessionService` phối hợp BE02 | BE02; BE05 kiểm bảo mật |
| BE04 – Chăm sóc & kho | Chưa gán | Care/feeding, incidents/photos, stable/stall, inventory và replenishment | BLL `CareService`, `InventoryService`, `IncidentPhotoService`, `AttachmentService` | BE03 kiểm treatment privacy; BE02 kiểm horse scope |
| BE05 – Dữ liệu, báo cáo & QA | Chưa gán | SQL Server/migrations, reports/dashboard, audit/notifications/reminders, test/UAT và bàn giao | DAL `Data`; BLL `ReportingService`, `BackgroundWorkers`; tests và tài liệu | BE01; chủ module xác nhận số liệu |

Mọi người tự viết kiểm thử cho phần mình sửa. BE05 điều phối và kiểm thử liên module; không nhận toàn bộ kiểm thử thay cả nhóm. BE05 quản lý thay đổi schema, nhưng chủ module phải đề xuất entity/field/quan hệ và tiêu chí dữ liệu trước khi tạo migration.

## Task có thể đưa lên GitHub Issues

| Mã | Ưu tiên | Chính | Reviewer | Việc cần thực hiện trên code hiện có | Đầu ra và tiêu chí nghiệm thu | Phụ thuộc |
| --- | --- | --- | --- | --- | --- | --- |
| BE-001 | P0 | BE01 | BE05 | Đối chiếu API/DTO với các màn hình và hành động trên Figma, lập danh sách thiếu/lệch; chốt kiểu enum, pagination, date/unit và lỗi | Bảng screen → endpoint → role → request/response; ghi rõ API đã có và API cần bổ sung; không tự sửa nghiệp vụ còn chưa chốt | Không |
| BE-002 | P0 | BE01 | BE03 | Rà auth, OTP, reset, invitation, refresh/logout và ma trận quyền 7 role | Test hết hạn/sai mã/lockout/revocation; public registration không tạo staff; Owner/Vet/Trainer khác scope bị chặn; hướng dẫn bootstrap Manager | BE-001 |
| BE-003 | P0 | BE02 | BE01 | Kiểm intake/photo/certificate, revision/resubmit/approval và official assignments | Demo Draft → Submit → Revision → Approval; chỉ approval tạo Horse; approval đồng thời không tạo trùng; Owner không chọn Trainer; giữ assignment history | BE-002 |
| BE-004 | P0 | BE02 | BE03 | Hoàn thiện và kiểm template/plan/session/rider/result/evaluation theo contract | Test trạng thái plan/session, Planned không rider, duplicate result/evaluation; speed đúng đơn vị; lưu history; Owner/Rider chỉ thấy scope phù hợp | BE-003, BE-005 |
| BE-005 | P0 | BE03 | BE02 | Kiểm medical records/correction, restriction, injuries/treatments/follow-up/clearance | Test lock được áp cả session đã lên lịch và khi start; Trainer không override; clinical notes không lộ cho role khác; correction giữ bản cũ; clearance không tự resume plan | BE-003 |
| BE-006 | P1 | BE03 | BE04 | Kiểm preventive care và phối hợp reminder cho due/follow-up | Schedule/complete đúng ngày; reminder đến người đúng scope, không phát lặp không kiểm soát; timezone khớp ngày nghiệp vụ | BE-005, BE-010 |
| BE-007 | P1 | BE04 | BE03 | Kiểm care/treatment/feeding và incident routing/photos | Groom chỉ ghi task được giao; feeding có approved/actual; không lộ treatment instructions; incident đến đúng role; ảnh có quyền/size/type validation | BE-003, BE-005 |
| BE-008 | P1 | BE04 | BE02 | Kiểm stables/stalls và occupancy/cleaning history | Không double-book stall; vacate/move/archive đóng occupancy; Groom chỉ thao tác trong scope; test thao tác đồng thời trên SQL Server | BE-003, BE-011 |
| BE-009 | P1 | BE04 | BE05 | Kiểm inventory/movements/replenishments | Không tồn âm kể cả cạnh tranh cập nhật; không ghi đè movement history; duyệt request chưa tăng stock; receipt tăng stock; cảnh báo tới Manager | BE-002, BE-011 |
| BE-010 | P1 | BE05 | BE01 | Kiểm report/dashboard, notification/audit, worker và catalog message | KPI khớp nguồn; filters/day/week/month đúng timezone; không vượt scope; notification read riêng từng user; đủ message key, không literal nội dung trong workflow | BE-004, BE-005, BE-007, BE-009 |
| BE-011 | P0 | BE05 | BE01 | Kiểm chứng các luồng trên SQL Server local; quản lý schema/seed test và tài liệu cài máy mới | Chạy migration/SQL trên database test riêng; test FK/unique/concurrency/rollback; script và model đồng bộ; dữ liệu demo giả; không reset database local hiện có | BE-002, BE-003 |
| BE-012 | P0 | BE01 | BE05 | Chuẩn bị contract/Postman hoặc HTTP requests, CORS và cấu hình môi trường cho frontend | Collection chạy được với account/horse test; ví dụ enum/date/upload; môi trường API base URL rõ; secrets không nằm trong repo; frontend biết xử lý 401/403/409/429 | BE-001, BE-002 |
| BE-013 | P0 | BE01 | BE02, BE03, BE04, BE05 | Sau khi frontend được push: phối hợp tích hợp API theo từng module và xử lý mismatch | Luồng Owner → Manager → HeadTrainer → Trainer → Rider → Vet/Groom chạy qua UI; không bỏ quyền/validation để sửa lỗi tích hợp; cập nhật contract theo quyết định nhóm | BE-012, code frontend được push |
| BE-014 | P0 | BE05 | BE01 | Regression/UAT và chuẩn bị môi trường bàn giao | CI xanh; UAT đủ 7 role; không còn lỗi vượt quyền hoặc chặn core; cấu hình SMTP/HTTPS/storage/backup được kiểm nếu triển khai; hướng dẫn run và rollback | BE-013 |

P0: ưu tiên để core và tích hợp hoạt động. P1: supporting cần kiểm chứng trước khi bàn giao đủ phạm vi; có thể làm song song sau dependency.

## Trình tự đề xuất

| Giai đoạn | Việc chạy song song | Mốc bàn giao |
| --- | --- | --- |
| 1 – Chốt contract & rà core | BE01: BE-001/002/012; BE02: BE-003; BE03: rà BE-005; BE05: thiết lập BE-011; BE04: rà supporting | Contract có owner, checklist thiếu theo Figma, môi trường SQL Server test sẵn sàng |
| 2 – Kiểm chứng nghiệp vụ | BE02: BE-004 cùng BE03: BE-005/006; BE04: BE-007/008/009; BE05: BE-010/011 | Demo core bằng API; không còn lỗi trạng thái/quyền/medical block; collection và tests được bàn giao |
| 3 – Tích hợp sau frontend push | BE01 điều phối BE-013, mỗi chủ module sửa API/contract phần mình; BE05 điều phối BE-014 | UAT qua UI đủ role, CI xanh, hướng dẫn chạy/deploy và bàn giao |

Chưa gán ngày cố định vì chưa có số người thực tế, thời gian làm và deadline. Nhóm chốt các thông tin này trước khi đặt due date cho issue. Việc chưa phụ thuộc mã frontend vẫn thực hiện được ngay; không chờ frontend push để kiểm backend.

## Quy tắc nhận việc và bàn giao

1. Một issue có mã task, một assignee, reviewer, dependency, checklist nghiệm thu và deadline được nhóm xác nhận.
2. Nhánh `feat/BE-003-intake-validation` hoặc `fix/BE-005-medical-scope`; thống nhất base có backend hiện tại. PR nhỏ, chủ module review cross-module khi có ảnh hưởng.
3. API chỉ khai báo route/binding/policy, BLL xử lý nghiệp vụ, DAL xử lý schema/persistence. Role/status/type dùng enum; thông báo dùng `MessageKey` và catalog chung; giới hạn lấy từ options.
4. Không commit connection secrets, password, OTP hoặc dữ liệu cá nhân thật. Dùng database test riêng cho dữ liệu test/seed; không chạy reset/drop trên database đang sử dụng.
5. PR bàn giao kèm endpoint/DTO thay đổi, migration nếu có, test đã chạy, HTTP requests mẫu và lỗi/policy còn cần chốt. CI xanh và có reviewer trước khi merge.
6. DoD cho task backend: API đúng contract, quyền phía server đúng, dữ liệu/history đúng, kiểm thử phù hợp đạt và tài liệu được cập nhật. DoD tích hợp: chạy thêm luồng UI và các trạng thái loading/empty/error/permission ở frontend.

## Cần chốt riêng, không tự mở rộng phạm vi

Policy clearance toàn ngựa hay từng injury; thời lượng session và overlap lịch; future assignment; cancel/reject ngoài draft/revision; report export; SMTP/deployment target. Những thay đổi này chỉ đưa vào task triển khai khi nhóm xác nhận cần cho sản phẩm. 3D injury nâng cao và Flow 5 thi đấu không mặc định thuộc bảng này.

Tài liệu liên quan: [API và cấu hình](BACKEND_GUIDE.md), [ba layer/message](THREE_LAYER_AND_MESSAGES.md), [SQL database](DATABASE_SQL.md), [quy trình đóng góp](../CONTRIBUTING.md).

## Nhật ký bàn giao BE01

- **BE-001 – mapping contract:** [CONTRACT_AND_SCREEN_MAP.md](CONTRACT_AND_SCREEN_MAP.md) ghi quyền, scope, routes và các điểm cần xác minh; [API_CONTRACT.md](API_CONTRACT.md) là contract hiện tại theo code/OpenAPI. Chưa xác nhận từng màn hình với chủ Figma nên mapping chưa được xem là contract UI cuối cùng.
- **BE-002 – auth/scope:** integration tests kiểm OTP hết hạn cho Verify/Reset/Invite, giới hạn attempt, lockout, role injection, token revocation, và ma trận bảy role cho staff directory cùng thao tác Manager-only. Cần chạy trên .NET 10 với SQL Server test trước khi đóng task.
- **BE-012 – frontend handoff:** [Horse_BackEnd.http](../Horse_BackEnd/Horse_BackEnd.http) có request mẫu an toàn cho auth, enum, upload, error, Owner và Manager. [BACKEND_GUIDE.md](BACKEND_GUIDE.md) và [API_CONTRACT.md](API_CONTRACT.md) ghi API base URL, CORS, cấu hình secret, status/error và cách dùng bearer token.
- **BE-013 – UI integration:** chưa thể hoàn tất cho đến khi frontend được xác nhận sẵn sàng để tích hợp và các câu hỏi contract còn mở được chủ trách nhiệm chốt.
