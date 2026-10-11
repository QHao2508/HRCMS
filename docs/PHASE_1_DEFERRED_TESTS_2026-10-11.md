# Checklist kiểm thử sau khi hoàn thành chức năng

Ngày 11/10/2026. Theo yêu cầu người dùng: ghi lại để chạy sau, không chạy test mới trong giai đoạn 1. Mọi ô dưới đây chưa được tick. Bằng chứng cũ (200 FE test, 145 BE pass/1 skip SQL Express, Azure live workflow) chỉ là mốc tham chiếu; không thay cho test bản cuối.

Mỗi ca ghi commit BE/FE, môi trường, role, bước thực hiện, expected/actual, ảnh/log và người review. Dùng dữ liệu mẫu có đánh dấu; database thật không chạy bộ test tạo/xóa database. Worker/email test chỉ dùng người nhận thử đã đồng ý, không gửi thông báo cho tài khoản thật ngoài phạm vi.

## Trước UAT — Đức điều phối

- [ ] Build, lint, FE regression và full BE regression trên database riêng; phân biệt skip/fail.
- [ ] CI đúng commit hai repo; migration chạy thử riêng, backup/restore; cấu hình secrets/key protection/storage/proxy/CORS.
- [ ] Chuẩn bị 7 role, hai Owner, staff đang/hết phân công, ngựa hoạt động/archived; tài khoản hết hạn/chưa verify/inactive.
- [ ] Đối chiếu contract/schema/source hiện hành; không chạy bằng binary hoặc checkout cũ.

## Tài khoản và nhân sự — #1,2,13,14,29

- [ ] Đăng ký/verify đúng; OTP sai/hết hạn/tái sử dụng/resend rate limit; registration pending hết hạn.
- [ ] Login username/email, sai password/lockout, refresh, reset, logout và token cũ.
- [ ] Invite → verify/password → login; resend và trạng thái kích hoạt đúng.
- [ ] Active/inactive staff đúng đối tượng; không toggle Manager/Owner; token/security stamp/SignalR bị thu hồi.
- [ ] Staff list/filter/paging/empty/error; ảnh hưởng inactive tới assignment rõ ràng.

## Intake/hồ sơ — #5,6,15,16,17,22,23

- [ ] Draft thiếu dữ liệu; upload PNG/JPEG/PDF hợp lệ/sai/quá cỡ; file private và Owner khác không đọc được.
- [ ] Submit thiếu file/field; revision/resubmit; Manager chỉ sửa field được phép, audit before/after.
- [ ] Hai lần approval/double submit/concurrent mutation không tạo trùng; lỗi giữa transaction rollback.
- [ ] Đổi Trainer/Rider/staff: người cũ mất write, lịch sử đúng scope, không mở current profile/clinical.
- [ ] Archive chặn InProgress session/care; đóng assignment, plan, pending work, occupancy; giữ medical/history/reason.
- [ ] Browser history-only/archived readonly; 401/403/404/409/500 và network không bị che thành dữ liệu giả.

## Huấn luyện — #7,8,18,19

- [ ] Template create/edit/archive và plan cũ; plan states/date/session paging >100, tìm current Trainer/creator.
- [ ] Plan/session create/edit/assign/start/result/evaluation/skip; speed/result và duplicate state guard.
- [ ] Restriction kiểm create/edit/assign/start; active/recovering injury chặn Heavy dù assessment Fit.
- [ ] Restriction xuất hiện giữa buổi; observation giữ lại; future adjustment không tự đổi lịch/resume.
- [ ] Plan quá hạn không tự Completed; complete khi còn pending bị chặn.

## Thú y/phòng ngừa — #9,10,21,25

- [ ] Vet đúng assignment; role khác không đọc/write clinical bằng API trực tiếp; summary không lộ notes.
- [ ] Khám mới, backdated, future reject; correction giữ exact timestamp và bản gốc; correction đã có dùng bản mới nhất.
- [ ] Injury/restriction/treatment cùng horse/record, ngày và review; enum tiếng Việt không làm đổi payload.
- [ ] Clearance A không đóng B; Monitoring hợp lệ; empty/duplicate/foreign/future/already-closed ID bị chặn.
- [ ] Không chọn clearance gửi arrays rỗng; không tự resume plan; audit đủ ID ảnh hưởng.
- [ ] Preventive schedule/complete, future/duplicate reject, paging >100, ngày Asia/Ho_Chi_Minh.
- [ ] Reminder active/current Vet, không lặp, không nhắc archived/completed, chưa recipient không đánh dấu sent.

## Care/incident/stable — #11,24,26,28

- [ ] Owner/Manager không đọc clinical instructions/notes Treatment và IceBath ngoài policy; kiểm raw API và report/audit.
- [ ] Groom chỉ task của mình, không khám/phác đồ đầy đủ; Vet chỉ create Treatment/IceBath, Treatment cần active plan.
- [ ] Feeding approved/actual portion; Pending/InProgress → Completed/Skipped/IssueReported, final không ghi lại.
- [ ] IssueReported tạo incident đúng; reporter/scope/routed role/photo private, resolve/photo sau resolved.
- [ ] Stables/stalls list/paging; occupancy hiển thị đúng ngựa, occupy/vacate/clean, chuồng đã occupied/conflict.
- [ ] Groom clean chỉ khi thuộc scope ngựa đang trong ô; UI không cho gọi action vô nghĩa sau vacate.

## Kho/báo cáo — #3,12,24,27,30

- [ ] Manager create/movement/archive; Groom chỉ quyền được phép; tồn không âm, quantity 0/out of bounds.
- [ ] Replenishment request/review đúng vai trò; review không tự cộng tồn; reviewed không duyệt lại.
- [ ] Archive còn tồn hoặc pending request bị chặn; concurrency không mất stock movement.
- [ ] Reports horse/date/groupBy/no-data/KPI/series đúng; ngày Việt Nam, quá range/records trả lỗi rõ.
- [ ] Owner, Rider, Groom chỉ dữ liệu trong scope; clinical chỉ Vet; audit raw detail không rò medical.

## Chung/realtime/release — #4,20,31–35

- [ ] Header/logo/font/color nhất quán Home/auth/dashboard/training/medical/module mới; mobile/keyboard/focus/no overflow.
- [ ] Menu và route đúng 7 role; tiếng Việt cả success/validation/backend errors/notification.
- [ ] Hai phiên: notification/read/DataChanged sau commit, rollback không phát; reconnect/refresh/logout không duplicate/leak.
- [ ] Email/reminder/outbox dispatcher chạy thật trên môi trường thử; blob upload/download/logo ảnh hoạt động.
- [ ] #35 nếu triển khai: retention bounded batch, exhausted visibility, replay authorization/audit/idempotency.
- [ ] Rehearsal đủ luồng 7 role trên release cuối; known limitations, runbook/version và backup demo.

Chuyển task Done chỉ sau khi ca liên quan đạt và reviewer ghi bằng chứng. Failed/Blocked phải giữ nguyên và tạo bug có bước tái hiện; không tick theo số test tổng.
