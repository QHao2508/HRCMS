# Điều chỉnh business rule — 11/10/2026

Phạm vi: hoàn thiện các đề xuất được người dùng đồng ý trong chat. Tài liệu này cập nhật rule hiện hành; các Gap lịch sử trong T02 không phải tình trạng code hiện tại.

| Rule | Hành vi đã triển khai |
|---|---|
| Tài khoản | Username 3–80 ký tự ASCII chữ/số/`.`/`_`/`-`, trim và lowercase ở BE; không nhận khoảng trắng nội bộ hoặc `@`. Email/username chống trùng bằng service và unique index; public register dùng lỗi chung, Manager invite trả lỗi theo field. Không đổi danh tính tài khoản cũ. |
| Khóa staff | Manager xem tổng assignment/session/care đang mở và tối đa 20 dòng mỗi loại, không có clinical text. Khóa bình thường bị chặn nếu còn công việc; khóa khẩn cấp bắt buộc lý do, thu hồi security stamp ngay, audit và thông báo Manager để bàn giao. Giữ công việc, không tự tạo kết quả giả. |
| Bàn giao | Trainer mới tiếp quản quyền trên plan đang mở; creator giữ nguyên, người cũ mất write. Thay Groom chuyển Pending task sang người mới. InProgress bị chặn trừ khi Manager chọn bàn giao rõ và nhập lý do: task giữ InProgress, audit From/To/reason, không tự Completed. Rider InProgress không được đổi trực tiếp: Trainer phải skip có lý do trước khi lập/giao buổi thay thế. |
| Sức khỏe | HealthStatus, injury và restriction kiểm độc lập; Vet quyết định chuyên môn; Trainer không override. Fit không tự clearance. Clearance chọn vấn đề; không đóng vấn đề khác hoặc tự resume. |
| Lịch | FE yêu cầu nhập 1–480 phút, BE tính ScheduledEndAt; khoảng [start,end), hai buổi tiếp giáp được phép. Chặn chồng ngựa/Rider giữa buổi Planned/Assigned của plan Active hoặc InProgress. Kiểm restriction xuất hiện giữa khoảng dự kiến. Dữ liệu/API cũ không có duration giữ unknown, chỉ kiểm được instant; cần bổ sung khi sửa bằng FE mới, không suy đoán 30/60 phút. |
| Nhận hàng | Approval không tăng tồn. Manager nhận từng phần, tổng nhận không vượt Quantity; Approved → PartiallyReceived → Received. Tăng stock và tạo movement liên kết request cùng transaction/audit. Groom không nhận hàng; nhận trùng/over-receipt bị chặn. Ghi chú đề nghị và review tách riêng; tên item archived vẫn có trong lịch sử. Archive item chặn Pending/Approved/PartiallyReceived. |
| Sự cố | Resolve bắt buộc operational Resolution (<=2000), lưu ResolvedBy/ResolvedAt, không resolve lại; role/scope vẫn kiểm ở BE. Không đưa diagnosis/medication vào tóm tắt xử lý, lưu vào hồ sơ clinical riêng. Sự cố cũ đã resolved giữ metadata unknown. |
| Archive | HTTP mới bắt buộc Category + Reason: LeftClub/Transferred/Sold/Deceased/Other; lưu actor/time và audit. Giữ các guard InProgress và transaction đóng vận hành; không đổi chuyên môn y tế. Hồ sơ cũ không tự gán lý do. |

## Contract và triển khai

- GET /api/staff/{id}/workload (Manager).
- PUT /api/staff/{id}/active bổ sung emergency/reason; normal disable có thể trả 409 staff_open_work.
- POST /api/horses/{id}/assignments bổ sung takeOverInProgress/takeOverReason cho Groom.
- SessionRequest bổ sung durationMinutes; response session/list bổ sung durationMinutes/scheduledEndAt.
- POST /api/care/incidents/{id}/resolve bắt buộc body { resolution }.
- POST /api/horses/{id}/archive bắt buộc body { category, reason }.
- POST /api/inventory/replenishments/{id}/receive { quantity, reason }.
- Replenishment list thêm itemName/reviewNotes/receivedQuantity/receivedAt. Enum mới PartiallyReceived/Received; giữ số enum cũ.

Migration: 20261011120226_BusinessRuleLifecycle, chỉ thêm cột và FK movement → replenishment. SQL delta tại BUSINESS_RULES_MIGRATION_2026-10-11.sql; database.sql là script idempotent toàn bộ. Không chạy Down trên dữ liệu mới để rollback; dùng backup/roll-forward được review. Deploy BE/FE cùng phiên bản, áp schema trước khi khởi động BE mới. Đã áp vào Azure HRCMS theo xác nhận người dùng ngày 2026-10-11. BE mới chạy cổng 5299, FE thực tế đã đồng bộ và chạy cổng 5173; health trực tiếp và qua proxy đều 200 healthy; OpenAPI có endpoint workload và nhận hàng mới.

## Bằng chứng

- Full BE regression: 157 pass / 1 Azure upload skip / 0 fail, 158 total trên SQL Express riêng.
- Bổ sung kiểm tra upgrade dữ liệu cũ: 1 pass; không tự tạo lý do/archive/resolution/receipt history cho legacy rows.
- FE: 210 pass / 0 fail, lint/build đạt; còn cảnh báo bundle khoảng 762 kB.
- Test mới: username, duplicate concurrent, manager field conflict, staff workload/privacy/emergency/token, Groom handover InProgress, incident resolution, partial/concurrent receiving, archive metadata, horse/Rider interval adjacency và mid-session restriction.
- Browser thử localhost:5180 bị từ chối quyền truy cập; chưa xác nhận hình ảnh hoặc thao tác trực quan của các form mới. Không dùng API tests làm bằng chứng browser UAT.
- Môi trường browser riêng: SQL Express HRCMS_UIRules_20261011, dữ liệu [RULE TEST], Workers=false. Không gửi email hoặc ghi Azure. Server thử được dừng sau công việc.

## Checklist còn cần xác nhận

- Đã hoàn thành: áp SQL delta Azure HRCMS, khởi động BE mới, đồng bộ và khởi động FE người dùng.
- Browser: lỗi trùng ở từng field staff; workload/khẩn cấp; Groom handover; session duration/overlap; receive một phần/đủ; resolve persisted text; archive reason/readonly ở desktop/mobile.
- Legacy session cần bổ sung duration để kiểm đầy đủ khoảng thời gian; không quảng cáo interval guard hoàn chỉnh cho dữ liệu unknown.
- Realtime đa phiên/SMTP/staging/recovery tiếp tục theo checklist dự án, không được đánh dấu đã đạt bởi đợt business rule này.
