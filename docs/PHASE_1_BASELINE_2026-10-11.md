# Giai đoạn 1 — baseline và backlog thực tế

Chốt ngày 11/10/2026 (giờ Việt Nam). Đây là kiểm kê code/contract, không phải nghiệm thu. Theo yêu cầu người dùng, không chạy thêm test trong giai đoạn này; các ca cần chạy nằm trong PHASE_1_DEFERRED_TESTS_2026-10-11.md. Giữ nguyên các tài liệu và thay đổi cá nhân đang có.

## Phiên bản chung

| Repo | Nhánh bàn giao | Commit baseline |
|---|---|---|
| Backend QHao2508/HRCMS | main | d5c71bf7219dd1106f657670d9c3d2959f8004f2 |
| Frontend QHao2508/HRCMS-Frontend | main | a3f389addd70ffbbfc31379db115125138038b75 |

Frontend sử dụng: E:/SWP391/HorseClub-frontend/HRCMS-Frontend. Thư mục TestResults/FrontendMerge là checkout tích hợp, không dùng thay cho thư mục của team. Backend: E:/SWP391/HorseClub.

Cấu hình đã chạy: BE Development localhost:5300, Workers__Enabled=false; FE 127.0.0.1:5174, API_PROXY_TARGET=http://localhost:5300; Azure SQL HRCMS và Azure Blob theo cấu hình/user secrets hiện có. Không ghi secrets vào repo. Tài khoản Azure mẫu của lần trước đã bị vô hiệu hóa; hồ sơ có dấu [AZURE TEST 20261011] còn để đối chiếu. Cấu hình này phục vụ tích hợp, chưa phải release/staging đã nghiệm thu. Khi đổi cổng phải cập nhật proxy/CORS phù hợp; không hardcode cổng trong service FE.

## Rà soát nhánh

Đã fetch refs hai repo ngày chốt. Frontend không còn remote branch ngoài ancestry main. Các nhánh auth-clean/giabao từng được reconcile giữ implementation hiện tại, không đồng nghĩa mọi UI thay thế trên các nhánh đó được transplant. Đã rà tên file trong src của toàn bộ remote refs: không thấy module care/feeding, inventory/replenishment, incident/stable, preventive hay reports riêng. #26 chưa có bằng chứng code FE trong các refs hiện có; không tính Implemented chỉ theo ô tracking.

Backend còn:

| Nhánh | Delta so với merge-base main | Quyết định giai đoạn 1 |
|---|---|---|
| feat/BE01 | Test OTP/workflow và tài liệu, 3 file | Hào review, Đức chọn port những regression hữu ích; chưa merge và chưa chạy |
| feat/horse-scope-archive | Commit 5241226; 12 file, workflow/service theo cấu trúc cũ | Đức + Khoa review so với archive/history main đã có. Port từng gap nếu có; không merge toàn bộ code cũ |
| khoa/manager-training-updates | Commit merge 5a7d08f; diff ba chấm không có file thay đổi | Không có feature delta để áp dụng; Đức có thể đóng nhánh sau review |

Không tự merge các nhánh BE trong giai đoạn chốt. Việc hợp nhất phải có diff review và checklist test ghi kèm.

## Trạng thái 35 task — thay cho snapshot cũ

Implemented = tìm thấy code. Partial = chưa đủ toàn bộ mô tả task. Needs Review = có code nhưng còn gap/quy tắc phải chốt. Không có hàng nào được tự nâng Done trong đợt này.

| # | Trạng thái kiểm kê | Việc còn lại |
|---|---|---|
| 1 | Implemented | UAT OTP/email/session |
| 2 | Implemented | Đối chiếu #22, staff active và UAT quyền |
| 3 | Needs Review | KPI, scope, report limits tại #24 |
| 4 | Implemented | Realtime hai phiên tại #31 |
| 5 | Implemented | Intake/file/revision/approval UAT |
| 6 | Implemented | Review archive/history theo #22–23 |
| 7 | Implemented | Plan/template state và paging UAT |
| 8 | Implemented | Reassignment, medical lock giữa buổi UAT |
| 9 | Implemented | Clinical scope, correction/clearance UAT |
| 10 | Implemented | Reminder/timezone UAT và FE #25 |
| 11 | Needs Review | Privacy care #24, occupancy projection và ảnh sự cố |
| 12 | Implemented | Inventory state/concurrency UAT |
| 13 | Implemented | Account recovery/network UAT |
| 14 | Implemented | OTP expired/wrong/reused UAT |
| 15 | Implemented | Double submit, upload và giữ form |
| 16 | Implemented | Allowlist Manager, revision và audit |
| 17 | Implemented | Browser archive/history/reassignment |
| 18 | Implemented | Plan/template scope và responsive |
| 19 | Implemented | Session/result/evaluation và lỗi state |
| 20 | Implemented | Header chung; thêm menu module mới và responsive |
| 21 | Implemented | Khoa review/test bằng chứng đã có, UAT sau cùng |
| 22 | Implemented | Đức review nhánh archive cũ, current/history scope |
| 23 | Implemented | Đức review rollback, reason và lifecycle đủ mô tả |
| 24 | Needs Review | Code hiện cho Manager xem instructions/notes Treatment; IceBath không được che cho Owner như Treatment. Hào phải sửa/chốt privacy, không chỉ che ở FE |
| 25 | Partial | Medical có; thiếu preventive UI, owner/non-Vet summary cần đối chiếu |
| 26 | Not present on main | Thiếu FE care/feeding trong refs đã rà |
| 27 | Not present on main | Thiếu FE inventory/replenishment |
| 28 | Not present on main | Thiếu FE incident/photos/stable/occupancy |
| 29 | Partial | Có list, lọc role, invite/resend, activation/active display; thiếu thao tác active/inactive, lọc/tìm kiếm nếu cần theo mô tả |
| 30 | Partial | Audit có; thiếu Reports UI; rà raw audit detail/privacy |
| 31 | Pending acceptance | Chưa có E2E hai phiên Azure với dispatcher hoạt động |
| 32 | Partial | Có test/API evidence; chưa đủ UAT tất cả màn hình/7 role |
| 33 | Pending review | Chưa xác nhận CI/staging/RC và restore drill |
| 34 | Partial | Có tài liệu; chưa rehearsal/bàn giao theo bản cuối |
| 35 | Deferred | Chưa retention/replay admin; làm cuối nếu giữ đủ 35 task |

Sửa nhận định trước: #29 chưa hoàn thành toàn bộ dù đã có page; #26 không có page trong main. Không dùng 42,9% cũ làm tỷ lệ nghiệm thu. Bảng Google Sheets chưa được sửa; file này là đề xuất cập nhật có bằng chứng để team áp dụng.

## Phân công và thứ tự giao việc

Đây là phân công đề xuất theo team trong bảng, chưa gửi thông báo tới thành viên.

| Gói | Owner | Reviewer | Đầu ra cụ thể | Phụ thuộc |
|---|---|---|---|---|
| P1-01 privacy/KPI #24 | Hào BE | Khoa | DTO care đúng field/role, IceBath/Treatment policy; thống nhất overdue/report | Làm trước nghiệm thu #26/#30 |
| P1-02 medical/preventive #25 | Bảo FE, Khoa BE | Lạc | Trang preventive và action schedule/complete; summary nonclinical | Contract medical hiện có |
| P1-03 care/feeding #26 | Bảo FE, Hào BE | Lạc | List/filter/paging, create và transitions; portion và Owner summary | P1-01 |
| P1-04 inventory #27 | Lạc FE, Hào BE | Bảo | Items/movements, replenishment/request/review; không tự tăng tồn | Contract inventory hiện có |
| P1-05 incident/stable #28 | Lạc FE, Hào BE | Bảo, Đức | Photos/resolve; stables/stalls, occupancy/clean/vacate | Privacy, occupancy projection phải chốt |
| P1-06 staff #29 | Bảo FE, Hào BE | Lạc | Active toggle đúng đối tượng, invite/resend/filters hoàn chỉnh | SetStaffActive và security stamp |
| P1-07 reports/audit #30 | Lạc FE, Hào BE | Khoa | Report filters/KPI/series/no-data/limit; audit detail đúng quyền | P1-01 |
| P1-08 integration #22–23/#31–34 | Đức | Hào/Khoa BE, Bảo/Lạc FE | Review pending refs, release checklist, test evidence, rehearsal | Toàn bộ feature trước UAT |

FE tiếp tục dùng SystemHeader, WorkflowUI, useRegistrationResource/useMutation, session/apiError và catalog hiện có. Không tạo AuthContext/api client/header riêng. Enum API giữ nguyên, label tiếng Việt. Các module mới đề xuất route /medical/preventive-care, /care/tasks, /care/incidents, /care/stables, /inventory, /inventory/replenishments, /reports; đây là route FE dự kiến, chưa triển khai. Guard UI phải khớp BE; không cấp quyền chỉ dựa menu.

## Điều kiện đóng giai đoạn 1

Đã chốt commit, kiểm refs, ghi thiếu từng task, phân công và contract; đã tạo checklist test hoãn. Điểm còn mở trước triển khai: Hào/Khoa review privacy; Hào/Đức quyết định projection occupancy; Đức review pending BE branch. Đây là các công việc triển khai/review giai đoạn 2, không ngầm coi đã giải quyết. Giai đoạn 1 hoàn thành ở mức kiểm kê/kế hoạch, chưa nghiệm thu phần mềm.
