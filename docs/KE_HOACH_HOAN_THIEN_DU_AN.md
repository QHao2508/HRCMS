# Kế hoạch hoàn thiện HRCMS

Ngày lập: 08/10/2026. Mốc bắt đầu: backend `e2ebf8a`, frontend `1755b66` trên main. Đây là kế hoạch công việc tiếp theo, không đánh dấu các tính năng dự kiến là đã triển khai. Team đã xác nhận: **BE gồm Hào, Khoa, Đức; FE gồm Lạc, Bảo**. Deadline Chủ nhật 11/10/2026. Kế hoạch nước rút bên dưới áp dụng cho deadline này; các giai đoạn phía sau là backlog hoàn thiện đầy đủ, không cam kết hoàn thành tất cả trong ba ngày.

## Lịch nước rút đến Chủ nhật 11/10/2026

Mục tiêu thực tế: bản bàn giao có luồng demo nhất quán, không lỗi chặn dữ liệu/quyền và biết rõ phần chưa hoàn thành. Khối lượng đầy đủ 34–57 ngày công vượt quá năng lực còn lại của 5 người trong khoảng ba ngày; không nhận là hoàn chỉnh nếu chưa đạt checklist. Supporting care/kho/report chỉ giảm phạm vi khi nhóm/thầy đồng ý.

### Phân công theo team đã xác nhận

| Vị trí | Trách nhiệm chính | Đầu ra trước khi đóng tính năng |
|---|---|---|
| Bảo — FE | Medical/preventive UI, care tasks/feeding; staff UI sau ưu tiên chính | Vet/Groom thao tác đúng quyền; clearance nối sau contract sửa; validation/empty/error tiếng Việt |
| Lạc — FE | Kho/replenishment, incident/chuồng và report/audit theo thứ tự ưu tiên; navigation, FE build/deploy | Không tăng tồn khi duyệt; UI phản ánh quyền và conflict; hỗ trợ browser UAT |
| Khoa — BE | Medical clearance/timeline/privacy, regression intake/training; chốt contract với Bảo | Test vấn đề A/B, record nhập lùi ngày, không rò clinical; DTO/migration có review |
| Hào — BE | Care privacy, report/KPI, staff/kho API; hỗ trợ Bảo/Lạc và review BE | Groom chỉ instructions task mình, non-medical không clinical; KPI đúng rule; API hỗ trợ UI |
| Đức — BE | Reassignment/archive, tích hợp backend, SignalR, CI/staging/migration và release candidate | Scope/history/archive đúng; phiên bản hai repo tương thích; test report/release checklist |

Đức là BE, không phải QA chuyên trách. Mỗi người tự test module và review chéo; Đức điều phối tích hợp/UAT, Hào↔Khoa review rule, Bảo↔Lạc review UI. Cả nhóm cập nhật tài liệu và diễn tập demo. Dùng nhánh ngắn theo task, tránh hai người sửa cùng module/file; không push trực tiếp main trong đợt tích hợp nhóm.

### Lịch theo ngày (giờ Việt Nam)

| Thời điểm | Công việc | Điều kiện qua mốc |
|---|---|---|
| Thứ Năm 08/10, phần thời gian còn lại | Chốt checklist demo, danh sách thiếu, owner; thống nhất contract medical; xác minh CI hai commit hiện tại; dựng môi trường tích hợp | Có một command chạy FE/BE, account demo bảy role và danh sách P0; không tự thay policy đã chốt |
| Thứ Sáu 09/10, trước 12:00 | BE đưa contract và test rule ưu tiên; FE làm khung/màn hình theo contract/mock đã thống nhất | FE/BE dùng cùng DTO/enum; gap dữ liệu/migration được ghi rõ, không tích hợp dựa payload phỏng đoán |
| Thứ Sáu 09/10, trước 20:00 | Tích hợp lượt 1: medical, care/kho tối thiểu; giữ luồng intake/training cũ chạy được | Build/tests đạt; chạy được happy path bằng API thật; cập nhật bug list |
| Thứ Bảy 10/10, trước 12:00 | Đóng tính năng; chọn phạm vi bàn giao theo kết quả thực tế; không thêm module lớn | Chức năng chưa đạt được ghi hạn chế hoặc xin thống nhất giảm scope; không che lỗi bằng UI |
| Thứ Bảy 10/10, chiều/tối | UAT bảy role, API cross-scope, lỗi clinical, rollback/concurrency, realtime hai phiên, upload/email và staging smoke | Không còn lỗi phân quyền/mất dữ liệu hoặc luồng chính không thể hoàn thành; sửa lỗi và chạy lại phần bị ảnh hưởng |
| Chủ nhật 11/10, buổi sáng | Chốt release candidate, regression, script migration/backup, tài liệu và demo rehearsal | Gắn đúng commit BE/FE; không đổi feature sau mốc này; chỉ sửa lỗi chặn có kiểm tra lại |
| Chủ nhật 11/10, trước giờ nộp/báo cáo | Kiểm health/login, dữ liệu demo, dự phòng video/screenshot, chốt known limitations và bàn giao | Nhóm thực hiện lại demo trên đúng bản bàn giao; không dùng database chung làm fixture destructive |

Giờ nộp chính xác chưa được cung cấp. Nếu nộp sáng Chủ nhật, release candidate phải chốt tối Thứ Bảy. Nếu FE1/FE2 quá tải, ưu tiên medical và một luồng care/kho được nghiệm thu, không cố dựng nhiều page chỉ hiển thị mà không hoàn thành nghiệp vụ.

### Thứ tự ưu tiên và việc chưa đưa vào đợt này

1. Bắt buộc: auth/intake/assignment/training cũ không regression; medical correctness/privacy; role/scope; test và demo ổn định.
2. Tiếp theo: medical UI, care task/feeding, kho movement/replenishment và thông báo chạy thật trên môi trường bàn giao.
3. Làm khi còn thời gian: staff management UI, incident/stall UI, report filters/audit UI; backend có sẵn không đồng nghĩa giao diện đã hoàn thành.
4. Hoãn phần nâng cấp không chặn release: outbox replay admin, dashboard biểu đồ mới, export mới, tối ưu UI lớn, multi-instance/backplane. Có thể tách chunk nhỏ nếu không ảnh hưởng lịch tích hợp.

Archive, clearance và lịch sử không được triển khai nửa chừng rồi bật cho người dùng: thiếu test hoặc migration/data mapping thì ghi là chưa đạt và giải quyết với nhóm. Thi đấu, payments, mobile, auto-schedule và overlap theo duration tiếp tục ngoài phạm vi.

### Checklist nghiệm thu tối Chủ nhật

- Owner tạo/verify account → draft/upload/submit → Manager revision/approve → phân công → Trainer plan/session → Rider kết quả → Trainer evaluation.
- Vet tạo restriction; session không phù hợp bị chặn; follow-up/clearance không đóng vấn đề khác; clinical chỉ đúng người thấy.
- Nếu supporting thuộc bản nộp: Groom hoàn thành task/feeding; movement kho không âm; review replenishment không tăng tồn.
- Sai Owner/staff/Rider không đọc/sửa được tài nguyên ngoài scope bằng request trực tiếp; logout/token cũ và file private đúng.
- SignalR hai phiên nhận sau commit và tải lại REST; nếu chưa đạt thì ghi trạng thái rõ, không tuyên bố realtime đã nghiệm thu.
- CI/build/regression đạt, migration đã thử riêng; schema dùng chung chỉ cập nhật bằng script đã review; có backup/runbook và bản demo dự phòng.

Trạng thái hiện tại: đây là phân công/ước lượng đề xuất, chưa giao task cho thành viên qua công cụ hoặc đánh dấu hạng mục Done.

## 1. Thế nào là hoàn thành?

Dự án hoàn thành khi người dùng của bảy vai trò thực hiện được các luồng trong phạm vi trên giao diện, backend kiểm đúng quyền và quy tắc, dữ liệu nhất quán, test/CI đạt, có môi trường demo triển khai được và bộ tài liệu bàn giao tương ứng phiên bản.

| Điều kiện nghiệm thu | Bằng chứng bắt buộc |
|---|---|
| Luồng tài khoản → intake → assignment → training → medical → care → inventory → report dùng được | Kịch bản UAT có dữ liệu và tài khoản cho bảy vai trò; reviewer ghi kết quả. |
| Đúng policy đã xác nhận | Ma trận P01–P10 đối chiếu code/test; không dùng snapshot cũ làm bằng chứng hoàn thành. |
| Đúng quyền đối tượng và trường dữ liệu | Test gọi API trực tiếp với Owner khác, nhân sự hết phân công, Rider cũ và vai trò không được xem clinical. |
| Giao diện hoàn chỉnh trong phạm vi | Loading, empty, lỗi, sai quyền, validation, phân trang, responsive và tiếng Việt ở mỗi màn hình. |
| Realtime hoạt động trên môi trường demo | Hai phiên người dùng nhận thay đổi sau commit; reconnect/refresh token/logout không rò dữ liệu hoặc tạo kết nối trùng. |
| Triển khai và phục hồi được | Hướng dẫn cấu hình, migration, health/smoke, backup/restore và phương án quay lại phiên bản trước đã diễn tập. |
| Không còn lỗi chặn bàn giao | Không còn lỗi dữ liệu, phân quyền, clinical privacy hoặc luồng chính không thể hoàn thành. |

Giữ ba project API → BLL → DAL, service interface, repository theo nghiệp vụ và UoW. Giữ cơ chế bearer/refresh hiện có. Ưu tiên API contract hiện có; thay đổi cần thiết cho policy phải thiết kế DTO/route bổ sung hoặc lộ trình tương thích rõ, cập nhật frontend và test cùng đợt. Migration mới kiểm trên database riêng trước khi áp dụng dùng chung.

Phạm vi hoàn thiện gồm medical/care/inventory đang có backend. Thi đấu, thanh toán, ứng dụng mobile, sinh lịch tự động, tự điều chỉnh training và interval overlap khi chưa có duration nằm ngoài kế hoạch này. Không cần đổi kiến trúc hoặc thêm Azure SignalR chỉ để hoàn thành đồ án một API instance.

## 2. Hiện trạng đã đối chiếu

| Nhóm | Đã có | Còn phải làm |
|---|---|---|
| Kiến trúc | 15 service interfaces; repository/query/UoW; EF ở DAL | Duy trì boundary tests khi thêm tính năng. |
| Auth và intake | Backend và màn hình auth, OTP, draft/review/approval | UAT lifecycle, lỗi mạng, tài khoản chưa xác thực/hết hạn và chỉnh sửa hành chính. |
| Nhân sự/phân công | API mời, kích hoạt, active; màn hình phân công trong hồ sơ ngựa | Màn hình quản lý nhân sự; tách current scope khỏi historical read. |
| Training | Template/plan/session/result/evaluation và màn hình cơ bản | UAT đổi người phụ trách, restriction giữa buổi, chỉnh tương lai thủ công và lịch sử. |
| Medical | API khám/correction/injury/restriction/treatment/follow-up/preventive | Sửa clearance/timeline/privacy trước; chưa có route page medical riêng trong AppRoutes. |
| Care/chuồng/sự cố | Backend tasks, incidents/photos, stables/stalls/occupancy | Sửa privacy; làm màn hình cho Manager/Vet/Groom và summary cho Owner. |
| Kho | Backend item/movement/replenishment | Chưa có màn hình kho riêng trong AppRoutes; cần quy trình nhập/xuất/duyệt thực tế. |
| Dashboard/report/audit | Dashboard UI; API reports/audit | Màn hình báo cáo và audit, bộ lọc/phân trang, kiểm scope và clinical fields. |
| Notification/SignalR | Hub, outbox, provider, chuông tiếng Việt, các list tải lại | Chưa chứng minh phát sự kiện toàn luồng trên Azure; chưa bật dispatcher khi smoke; retention/replay chưa có. |
| Dữ liệu/kiểm thử | Azure HRCMS đủ 7 migration, 32 bảng ứng dụng; local backend 113 đạt/1 bỏ qua; FE 164 đạt | Chạy lại theo từng đợt; E2E browser, UAT bảy vai trò, staging/release và restore drill. |

Số test là kết quả gần nhất, không phải tỷ lệ hoàn thành dự án. Không tính phần trăm hoàn thành khi chưa có danh sách chức năng nghiệm thu đầy đủ.

## 3. Giai đoạn A — Chốt checklist và sửa quy tắc nền (ưu tiên P0)

P0 là lỗi/quy tắc có thể làm sai dữ liệu, lộ thông tin hoặc chặn luồng chính. Hoàn thành A trước khi nghiệm thu giao diện medical/archive.

| ID | Việc cụ thể | Nghiệm thu | Phụ thuộc / người phụ trách đề xuất |
|---|---|---|---|
| A01 | Lập ma trận chức năng → role → endpoint → màn hình → test; đối chiếu policy hiện có và Figma mới nhất nếu có | Mỗi yêu cầu có trạng thái và bằng chứng; tách ngoài scope; gap chưa xác minh không ghi Done | Người phụ trách tích hợp + reviewer nghiệp vụ |
| A02 | Clearance theo vấn đề được chọn (P02): chọn injury/treatment/restriction cùng horse; thể hiện danh sách tác động trước xác nhận | Clearance A không đóng B, không tự hoàn tất treatment khác hoặc xóa restriction tương lai; cho Monitoring khi còn vấn đề; audit đầy đủ; không tự resume plan | A01; BE medical, FE phối hợp DTO |
| A03 | Timeline sức khỏe (P03): thống nhất assessment mới nhất, correction chain và tie-break; không ghi đè bởi record nhập lùi ngày | Isolated hôm nay + Fit tuần trước vẫn Isolated; correction cũ không đổi assessment mới; correction giữ ngày khám của bản được sửa | A01; BE medical |
| A04 | Clinical privacy (P07): kiểm list/detail/report/task/attachment/notification; áp cả Treatment và IceBath | Manager/Owner không thấy clinical instructions; Groom chỉ task được giao và thông tin cần thực thi; Vet đúng scope; response không chỉ che bằng UI | A01; BE quyền + reviewer |
| A05 | Reassignment/Rider history (P08): tách quyền hiện tại và lịch sử | Trainer cũ mất quyền sửa ngay; Trainer mới tiếp quản; Rider đọc lịch sử buổi mình thực hiện nhưng không được mở rộng current horse/clinical scope | A01; BE scope + FE |
| A06 | Archive trọn vòng đời (P06): đóng assignment, plan, pending session/care, occupancy bằng transaction; ghi reason và recipient snapshot | Chặn công việc đang thực thi; giữ record hoàn tất; history chỉ đọc đúng scope; không medical clearance ngầm; worker bỏ qua archived; fault rollback không dở dang | A04/A05; BE lifecycle |
| A07 | Rà KPI/reminder/report theo P09; chỉ sửa gap còn tồn tại sau đối chiếu code mới | Ngưỡng overdue dùng cùng rule; test 59/60/61 phút, paused/archived, ngày UTC+7; không âm thầm cắt dữ liệu >100; report có limit rõ | A01; BE reporting |

Căn cứ hiện tại: `MedicalService.RecordFollowUp` vẫn clearance horse-wide; `CreateExamination` gán HealthStatus trực tiếp; `CorrectExamination` có kiểm record mới hơn nhưng chưa đủ toàn policy temporal; `CareService.ListTasks` cho Manager clinical và chỉ che Treatment; `HorseProfileService.ArchiveHorse` mới đặt Archived/đóng occupancy. Đây là gap đã đọc code, không suy ra chỉ từ backlog cũ.

A02/A03/A05/A06 có thể cần thay DTO/schema. Trước khi sửa: xác định migration bổ sung, dữ liệu cũ có đủ liên kết để backfill không, rule cho dữ liệu thiếu, ảnh hưởng client cũ và rollback. Không tự gán quan hệ y tế từ dữ liệu thiếu hoặc sửa migration đã áp dụng.

## 4. Giai đoạn B — Hoàn thiện giao diện nghiệp vụ

Giữ component/form/API client và message catalog đang có; UI gọi API thật. Không bỏ qua permission vì đã ẩn nút.

| ID | Màn hình/luồng | Nội dung | Tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|---|
| B01 | Nhân sự | Danh sách/lọc role, mời nhân viên, active/inactive, trạng thái kích hoạt | Manager thao tác; nhân viên nhận OTP theo luồng hiện có; không lộ password/token; role khác bị chặn trực tiếp API | A01 |
| B02 | Medical | Summary, timeline khám, correction, injury, restriction, treatment, follow-up/clearance, preventive care | Vet làm đủ vòng đời; UI hiển thị tác động clearance; non-medical chỉ nhận summary/operational restrictions | A02/A03/A04 |
| B03 | Care tasks | Giao task, danh sách cá nhân, complete/skip/issue, feeding approved/actual, treatment instructions | Groom chỉ thực thi task mình; task y tế đúng quyền; trạng thái/lỗi/portion được validate | A04 |
| B04 | Sự cố | Báo incident, upload/download ảnh có quyền, danh sách và xử lý theo Trainer/Vet | Route đúng vai trò xử lý; ảnh private; sai người báo không upload; resolved không sửa sai lifecycle | A04; B03 tích hợp |
| B05 | Chuồng | Khu/ô chuồng, occupancy hiện tại/lịch sử, xếp/chuyển/trả, đánh dấu sạch | Không hai ngựa cùng occupancy active một ô; Groom vệ sinh đúng phạm vi; UI phản ánh conflict | A06 |
| B06 | Kho | Danh sách item, tồn tối thiểu, movement nhập/xuất, replenishment và review | Không âm tồn; Groom không nhập tăng tồn; duyệt request không tự tăng tồn; nhận hàng ghi movement riêng | A01 |
| B07 | Báo cáo/audit | Horse/date/group filters, paging/limit/no-data, drill-down và lịch sử thao tác | Owner đúng ngựa, Manager đúng trường được phép; không lộ clinical qua report; tải hết trang hoặc báo limit | A04/A05/A07 |
| B08 | Hoàn thiện màn hình cũ | Auth/intake/assignment/training, archive/history, validation, tiếng Việt, mobile layout | Không mất form khi lỗi/realtime; không double-submit; evaluation chỉ ghi ý định chỉnh, sửa session tương lai có history | A05/A06; chạy xuyên module |

Mỗi màn hình có bảng quyền hành động, loading/empty/error/403/409, phân trang và tiếng Việt. Biểu đồ chỉ bổ sung khi có KPI và dữ liệu hỗ trợ; không thêm export/report format mới nếu chưa nằm trong nghiệm thu.

## 5. Giai đoạn C — Realtime và vận hành

| ID | Công việc | Tiêu chí nghiệm thu | Phụ thuộc |
|---|---|---|---|
| C01 | Mở rộng subscription cho các list/module mới | Hai phiên thấy thay đổi sau commit; rollback không phát; form đang nhập không bị reset | B02–B07 theo từng module |
| C02 | E2E realtime trên staging/Azure | Bật cờ BE/FE, proxy WebSocket/CORS đúng; notification/read/DataChanged; reconnect, refresh token, revoked stamp, logout, nhiều tab | A04/A05; có môi trường staging |
| C03 | Retention và xử lý outbox lỗi | Policy thời gian lưu được cấu hình; dọn bounded batch hàng đã gửi; quan sát hàng attempts exhausted; replay có quyền/audit nếu đưa vào phạm vi; không tự replay lệnh nghiệp vụ | A01; BE vận hành |
| C04 | Readiness và log | Lỗi worker/upload/migration có log hữu ích; health phù hợp; không log query access_token/password/clinical payload | C02; BE hạ tầng |
| C05 | Giảm bundle frontend | Tách chunk theo route/SignalR, tránh tải module không dùng; đo build trước/sau, auth/reconnect không đổi | B ổn định; FE |

Outbox là cơ chế phát có retry, không phải đảm bảo exactly-once đến người dùng. Một API instance đủ cho đợt bàn giao này; multi-instance/backplane là việc riêng nếu có yêu cầu triển khai tương ứng.

## 6. Giai đoạn D — Kiểm thử tích hợp và UAT

| ID | Bộ kiểm tra | Bằng chứng đạt |
|---|---|---|
| D01 | Backend regression và migration upgrade | Policy mới có test tình huống chính/lỗi/rollback/concurrency; upgrade từ schema cũ có dữ liệu; layer tests, SQL tests đạt; không dùng shared HRCMS làm fixture |
| D02 | Frontend lint/build/unit và contract | Payload/enum/status đúng API; không tự retry POST; callback/realtime không ghi đè response mới; catalog đầy đủ |
| D03 | Browser E2E bảy vai trò | Owner intake/revision; Manager approve/staff; HeadTrainer assign; Trainer plan/evaluate; Rider execute; Vet medical; Groom care; kho và report đúng quyền |
| D04 | API security theo đối tượng/trường | Cross-owner/assignment, stale token, expired OTP, private file, clinical fields và historical read được kiểm bằng request trực tiếp |
| D05 | Concurrency, dữ liệu lớn và thời gian | Hai reviewer/stock movements/session starts; >100 records/paging; UTC+7 và ngưỡng overdue; giới hạn upload/report |
| D06 | UAT có reviewer | Checklist chức năng ký nhận; log bug có repro, expected/actual, severity và bản sửa; không còn lỗi chặn release |

Một module chỉ Done khi cả BE + FE + quyền + tests + tài liệu đạt. Số lượng test tăng không thay thế kịch bản nghiệm thu. Azure upload live và SMTP thực kiểm trong môi trường được kiểm soát, bằng dữ liệu test có chủ đích.

## 7. Giai đoạn E — Triển khai và bàn giao

| ID | Công việc | Đầu ra/điều kiện hoàn tất |
|---|---|---|
| E01 | CI cho cả hai repo | Backend build/SQL tests; frontend lint/build/test; browser smoke khi có cấu hình; kết quả gắn đúng commit, không dùng secret production trong PR |
| E02 | Staging giống môi trường demo | HTTPS, API/FE URLs, secret configuration, persisted Data Protection keys, private Blob, SMTP, CORS/WebSocket, worker flags; phiên bản hai repo tương thích |
| E03 | Migration và recovery drill | SQL script review; thử trên bản sao database; backup/restore dữ liệu và file/key dependencies; diễn tập quay lại binary; nếu schema không tương thích thì dùng kế hoạch restore/roll-forward |
| E04 | Release rehearsal | Seed demo có chủ đích, smoke/UAT trên staging, kiểm worker/realtime thực, không gửi email tới người thật ngoài mục đích; quản lý tài khoản demo |
| E05 | Bàn giao đồ án | ERD hiện hành, architecture/sequence/state diagrams, API contract, ma trận quyền, test report, hướng dẫn cài/chạy, runbook, demo script, known limitations; tag/version cho hai repo |

Chọn hosting và ngân sách trước E02. Không tự mua dịch vụ hoặc thay cấu hình database dùng chung trong bước lập kế hoạch. Mỗi đợt schema áp dụng dùng chung cần có script cụ thể và kết quả kiểm thử riêng.

## 8. Thứ tự và khối lượng dự kiến

Đơn vị là ngày công tập trung, ước lượng sơ bộ gồm code và test của hạng mục; không phải lịch calendar. Cần điều chỉnh sau A01/Figma và năng lực nhóm.

| Đợt | Phạm vi | Ước lượng |
|---|---|---|
| 1 | A01 và thiết kế contract/migration cho gap policy | 1–2 ngày công |
| 2 | A02–A07: correctness, privacy, scope, archive, KPI | 8–13 ngày công |
| 3 | B01–B08: UI nghiệp vụ và tích hợp | 14–22 ngày công |
| 4 | C01–C05: realtime/vận hành | 3–6 ngày công |
| 5 | D01–D06: cross-module regression/E2E/UAT và sửa lỗi | 5–9 ngày công |
| 6 | E01–E05: staging, recovery, tài liệu và bàn giao | 3–5 ngày công |

Tổng sơ bộ 34–57 ngày công, cộng dự phòng 20% cho gap dữ liệu, tích hợp và UAT. Không chia thẳng cho số thành viên vì medical policy, scope và acceptance có phụ thuộc. Nhóm có thể làm UI khung, nhân sự/kho và CI trong khi BE sửa medical; nghiệm thu medical/care/archive phải đợi quy tắc nền ổn định. Không gán tên người hoặc ngày hoàn thành khi chưa có thông tin.

Nếu deadline ngắn, ưu tiên A và core auth/intake/assignment/training/medical + security/UAT trước; scope supporting chỉ giảm khi người dùng/nhóm đồng ý, không tự bỏ care/kho/report khỏi yêu cầu “hoàn chỉnh”. Không đánh đổi correctness/privacy để đủ số màn hình.

## 9. Gói công việc nên bắt đầu tiếp theo

1. Hoàn thành A01 bằng bảng coverage hiện hành, lấy policy đã xác nhận làm chuẩn; ghi rõ gap chắc chắn và mục cần đối chiếu lại.
2. Thiết kế A02/A03/A04: DTO chọn scope clearance, quan hệ dữ liệu y tế, quy tắc timeline và ma trận clinical fields; có ví dụ dữ liệu trước/sau.
3. Thêm regression tests chứng minh gap, rồi sửa BE medical/privacy theo thiết kế và migration bổ sung nếu cần; chạy database riêng.
4. Làm B02 medical UI dựa trên contract đã ổn định; sau đó care/incident/chuồng, kho và reports.

Ba thông tin giúp chốt lịch: deadline báo cáo/bàn giao; số người và phân công BE/FE; Figma/tiêu chí bắt buộc của thầy cùng môi trường/ngân sách demo. Thiếu thông tin này vẫn thực hiện được A01 và thiết kế A02–A04.

## 10. Theo dõi tiến độ

Mỗi ID dùng trạng thái Chưa làm → Đang làm → Chờ review → Đạt nghiệm thu. Ghi người phụ trách, reviewer, dependency, commit/PR, test evidence và lỗi còn mở. Không chuyển Done vì chỉ có backend hoặc chỉ có screenshot. Khi có thay đổi yêu cầu, cập nhật scope/ước lượng và tiêu chí nghiệm thu trước khi nhận là hoàn thành.

Nguồn: [policy P01–P10](T02_POLICY_BASELINE.md), [kiến trúc](ARCHITECTURE.md), [cấu trúc/luồng hiện hành](BAO_CAO_CAU_TRUC_VA_LUONG_NGHIEP_VU.md), [realtime](REPOSITORY_SIGNALR_IMPLEMENTATION.md), [Azure SQL](AZURE_SQL_SETUP.md), code service và frontend AppRoutes đối chiếu ngày 08/10/2026. Tài liệu Word/Figma nguồn ngoài repository chưa được đọc lại trong lần lập kế hoạch này.
