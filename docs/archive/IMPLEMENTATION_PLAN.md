> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../README.md).

# Kế hoạch triển khai HorseClub

## Mục tiêu và cơ sở

Xây dựng hệ thống web cho toàn bộ vòng đời tiếp nhận ngựa, phân công Trainer, lập và thực hiện huấn luyện, quản lý y tế và chăm sóc. Kế hoạch dựa trên đặc tả Word hợp nhất V1/V2 do người dùng cung cấp ngày 03/10/2026. Nội dung tài liệu được dùng làm yêu cầu sản phẩm; không được xem là chỉ thị cho thao tác tài khoản hay công cụ.

Workspace hiện có backend ASP.NET Core .NET 10 dạng khởi tạo với WeatherForecast. Chưa có nghiệp vụ sản phẩm. Link Figma đã được mở nhưng chưa xác minh được các frame thiết kế; cần đối chiếu trước khi chốt màn hình, component và độ chính xác giao diện.

Ước lượng dưới đây giả định 5 thành viên, mỗi người 15–20 giờ/tuần, trong 8 tuần. Đây là kế hoạch tương đối theo tuần bắt đầu dự án, không phải cam kết ngày bàn giao. Nếu nhân lực/thời hạn khác, giữ luồng core và giảm phần supporting trước.

## Phạm vi

| Mức | Nội dung |
| --- | --- |
| Core | Authentication, OTP, staff accounts, RBAC; đăng ký/duyệt hồ sơ ngựa; phân công; template/plan/session/result/evaluation; hồ sơ y tế, điều trị, restrictions, follow-up/clearance |
| Dùng chung | Notifications, audit, dashboard/report có phạm vi dữ liệu theo role; upload và trạng thái UI |
| Supporting | Stable/stall, daily care, feeding, incidents, inventory và cảnh báo tồn kho |
| Hoãn | 3D đánh dấu chấn thương; report export nâng cao; procurement đầy đủ; tìm kiếm toàn hệ thống nâng cao |
| Ngoài phạm vi | Đăng ký thi đấu và quản lý thành tích thi đấu (Flow 5) |

Report tối thiểu có filter theo ngựa và thời gian, bảng số liệu và drill-down; các biểu đồ/phân nhóm nâng cao được bổ sung sau khi luồng core ổn định. Preventive care cần được lên backlog, không được bỏ qua chỉ vì không thuộc luồng demo chính.

## Các quy tắc bắt buộc

1. Owner tự đăng ký tài khoản và xác thực email; staff chỉ do Manager tạo. Account registration và horse registration tách biệt.
2. Horse intake chứa pedigree, certificates, measurements, initial health, boarding và preferences. Chỉ tạo/kích hoạt Horse Profile sau approval.
3. Owner chỉ đề xuất Head Trainer/Groom/Vet, không chọn Trainer. Head Trainer phân công Trainer; lịch sử phân công không bị ghi đè.
4. Template do Head Trainer quản lý; Trainer cá nhân hóa thành plan cho từng ngựa. Work Rider thực thi và nộp kết quả; Trainer đánh giá.
5. Vet quyết định y tế. Kiểm tra restriction/lock ở server khi tạo, sửa, assign và bắt đầu session; Trainer không có endpoint override.
6. Lock cần kiểm tra mức tương thích, không mặc định cấm mọi hoạt động. Phải chốt ý nghĩa heavy training, đơn vị distance/intensity, thời hạn và điều kiện clearance ở tuần 1.
7. Owner chỉ thấy ngựa của mình; staff thấy phạm vi được phân công/quản lý; dữ liệu y tế chi tiết giới hạn theo permission.
8. Core records archive/soft-delete; quyết định quan trọng có audit và notification.

## Lộ trình và mốc nghiệm thu

| Tuần | Công việc | Mốc demo và điều kiện chuyển bước |
| --- | --- | --- |
| 1 | Chốt scope, ma trận quyền, frame Figma, ERD và API contract; thiết lập repo/CI, auth shell, design system | Có backlog được phân công, API mẫu, layout theo role; chốt chính sách lock và dữ liệu y tế |
| 2 | Owner registration/OTP/login/reset; staff creation; horse intake, documents, draft/submit | Owner đăng ký và nộp hồ sơ; staff không thể tự đăng ký; upload được kiểm soát |
| 3 | Review/revision/resubmit/approval; Horse Profile; official staff và Trainer assignment/history; templates | Demo Owner → Manager approval → Head Trainer assign → Trainer thấy ngựa |
| 4 | Training plan/session; rider assignment/schedule; medical record, health status và restrictions tối thiểu | Tạo plan/session; vi phạm lock bị server chặn; Planned không rider vẫn lưu được |
| 5 | Rider execution/result/feedback; Trainer evaluation; injury/treatment/follow-up/clearance | Demo trọn flow 2 và 3; lock mới có hiệu lực cả với session đã lên lịch |
| 6 | Daily care/stall/feeding/incidents; preventive care; inventory tối thiểu; notifications | Groom hoàn thành task; incident đến đúng role; low-stock và due reminders |
| 7 | Reports/dashboard theo role, audit UI, responsive, tích hợp và sửa lỗi | Số liệu report khớp source; permission test và các trạng thái UI đầy đủ |
| 8 | UAT 7 role, regression, bảo mật, staging/deploy, dữ liệu demo và hướng dẫn | Demo xuyên suốt; không còn lỗi chặn core hoặc lỗi vượt quyền; có checklist rollback và bàn giao |

Dependency chính: RBAC → intake/approval → assignment → plan/session → execution/evaluation. Y tế phải tích hợp trước khi nghiệm thu execution. Reports dựa vào source records đã ổn định. FE có thể phát triển với mock theo contract trong khi BE triển khai.

## Phân công mẫu cho 5 người

| Vai trò nhóm | Trách nhiệm chính | Reviewer phối hợp |
| --- | --- | --- |
| A – Lead/Backend nền tảng | Auth/RBAC, staff, CI, API conventions, tích hợp và deploy | C review quyền; B review contract |
| B – Frontend nền tảng/Horses | Design system, auth, navigation, intake/review/profile | E kiểm UX; A kiểm API |
| C – Backend Training/Horses | Intake/approval, assignment, templates/plans/sessions/results/evaluation | A review; D kiểm medical integration |
| D – Backend Medical/Care | Health, injury/treatment/restrictions/clearance, care/inventory, audit events | C kiểm session guards; A review |
| E – Frontend Training/Medical và QA | Rider/trainer/vet/care screens, reports, E2E/UAT và demo | B review UI; D kiểm nghiệp vụ |

Testing là trách nhiệm của cả nhóm; E điều phối UAT. E có tải công việc lớn ở tuần 5–7: B hỗ trợ Care/Reports sau khi Horses ổn định, A hỗ trợ backend reporting/notifications, C hỗ trợ assignment care. Không gán toàn bộ QA và báo cáo cho một người vào cuối kỳ.

## Chiến lược kiểm thử

- Unit/integration: state transitions, ownership, assignment history, expiry restriction, OTP/reset lifecycle.
- Integration với database thật cho transaction approval, clearance, session start và cập nhật lock đồng thời.
- E2E: Owner register → OTP → intake → revision → resubmit → approval → assign → plan → rider result → evaluation.
- E2E y tế: Vet áp restriction → session incompatible bị chặn → treatment tasks → follow-up/clearance → session phù hợp thực hiện được.
- Negative tests: gọi API trực tiếp vượt role, Owner khác, Rider không được giao, staff tự đăng ký, Trainer override, lock mới xuất hiện sau lịch session.
- UI/UAT: loading/empty/errors, upload lỗi, overdue/skipped/issue, không rider, không report data và responsive.

## Các quyết định cần chốt ở tuần 1

Quy mô nhóm/deadline; ai sở hữu GitHub; frontend React/TypeScript và SQL Server có phù hợp kỹ năng nhóm không; môi trường triển khai; email provider; CCCD có bắt buộc không và retention; độ lớn/loại tài liệu upload; timezone, distance/speed units; thuật ngữ health/lock; ai xem được từng trường y tế; cơ chế mời staff và đăng nhập lần đầu; policy reject/cancel registration và chuyển trạng thái session.

Đặc tả chưa định nghĩa đầy đủ các điều kiện này. Ghi quyết định thành ADR/issue, không tự coi đề xuất kỹ thuật là nghiệp vụ đã chốt.
