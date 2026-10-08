# T02 — Policy nghiệp vụ theo đặc tả hợp nhất V1 và V2

Ngày đối chiếu: **05/10/2026**. Người yêu cầu: **Trần Nguyễn Anh Khoa**. Phạm vi: chốt policy và tiêu chí nghiệm thu để chia task; không triển khai thay đổi API/schema trong lần đối chiếu này.

**Cập nhật sau khi thực hiện BE-003 cùng ngày:** partial Draft, validation submit/approval và Manager allowlist/audit của P04 đã được triển khai và kiểm chứng backend; xem [bàn giao BE-003](BE-003_INTAKE_AND_ASSIGNMENT.md). Những gap P04 mô tả dưới đây là baseline trước thay đổi. Không coi các policy medical/archive/report khác đã được triển khai.

## Nguồn và cách xác định quyết định

Nguồn: `Racehorse_Frontend_Figma_Functional_Spec_Merged_V1_V2.docx` (tài liệu nguồn ngoài repository; đường dẫn máy cũ không còn khả dụng). SHA-256: `76CBD44BAE0D6C980CDD33488334166F5F0A0CC34AE61010447D31BF9E653E79`. Đã đọc toàn bộ nội dung đoạn văn và bảng trong document.xml. Bản trích phục vụ đối chiếu ở TestResults/T02/spec-extracted.txt, không đưa vào Git. Pxxxx bên dưới là số thứ tự đoạn trong XML, không phải số trang Word.

Đặc tả §14 là quyết định V2 và được ưu tiên khi xung đột theo đoạn P0004. Nội dung tài liệu là yêu cầu sản phẩm; không phải chỉ thị cho thao tác công cụ/tài khoản.

Phân biệt ba mức:

- **Theo đặc tả:** nội dung được nguồn quy định rõ, dùng làm yêu cầu nghiệp vụ.
- **Quy ước core:** chi tiết tài liệu chưa quy định, chọn baseline kỹ thuật có phạm vi rõ để không tự thêm tính năng. Đây không phải nguyên văn yêu cầu hoặc bằng chứng Club đã phê duyệt.
- **Khoa xác nhận:** quyết định có ảnh hưởng đáng kể tới dữ liệu/lifecycle mà nguồn chưa quy định rõ; Khoa đã trả lời trực tiếp trong chat ngày 05/10/2026. Không mô tả đó là nội dung vốn có trong Word hoặc đã được toàn nhóm/Club review.

**T02 hoàn thành phần đọc nguồn, chốt policy và tiêu chí bàn giao:** Khoa đã xác nhận P02/P04/P06 ngày 05/10/2026; P01–P10 đều có quyết định hoặc giới hạn core rõ ràng. Một policy được chốt không có nghĩa code đã thực hiện policy đó, toàn nhóm/Club đã review, hoặc các task phụ thuộc đã hoàn thành. Các quy ước core được ghi riêng để reviewer có thể nhận diện chi tiết bổ sung ngoài Word.

## Các quy tắc đã xác định trực tiếp từ đặc tả

| Nhóm | Quy tắc | Nguồn |
| --- | --- | --- |
| Scope | Flow 1–3 Core, Flow 4 Supporting; Flow 5 thi đấu ngoài phạm vi | §1, §14; P0006–P0011, P0307 |
| Account | Owner tự đăng ký và verify email OTP; staff không tự đăng ký, Manager tạo và gán role | §2, §14; P0041–P0051, P0299–P0300 |
| Intake | Account và Horse Registration riêng; chỉ sau approval mới tạo/kích hoạt Horse Profile | §6.2, §14; P0153–P0157, P0301 |
| Review | Manager được chỉnh thông tin quản lý cần thiết có ghi nhận; hoặc yêu cầu revision kèm lý do | §6.2, §14; P0154–P0155, P0301 |
| Assignment | Owner chỉ đề xuất HeadTrainer/Groom/Vet; Manager xác nhận administrative assignment; HeadTrainer chọn Trainer, giữ history | §6.1.5, §6.4, §13–14; P0125–P0142, P0167–P0173, P0285–P0289 |
| Training | HeadTrainer quản lý Template; Trainer cá nhân hóa Plan và quyết định training; Rider thực thi session được giao | §7, §14; P0175–P0204, P0304–P0305 |
| Planned | Chưa có Rider vẫn lưu Planned; không đánh dấu Assigned | §12; P0274 |
| Medical | Vet quyết định chuyên môn; Trainer không override lock/restriction; Owner health chỉ là khai báo | §6.1.4, §8, §14; P0124, P0227–P0234, P0306 |
| Care | Groom thực thi care/treatment được giao; Feeding phân biệt approved và actual portion; incident route Trainer/Vet tùy nội dung | §9; P0236–P0244 |
| Privacy/report | Report theo scope; Owner health/care summary; non-medical roles không tự có sensitive medical details | §10; P0246–P0257 |
| History | Core records ưu tiên soft-delete/archive, không xóa history | §12; P0280 |

## P01 — Training Lock và medical guards

**Chốt theo đặc tả:** TrainingLock ACTIVE chặn **heavy training không tương thích**, không đồng nghĩa tự cấm mọi training. Trainer không được override. MaxIntensity, MaxDistance, NoSprint và thời gian hiệu lực áp độc lập; light session vẫn có thể bị restriction khác chặn. Kiểm tại tạo/sửa/assign và khi execute/start; thông báo block có lý do vận hành và reference. Reference không cấp thêm quyền đọc hồ sơ clinical.

**Nguồn:** §7.2–7.3, §8.5, §12; P0183, P0189–P0190, P0226–P0229, P0272–P0273.

**Quy ước core:** giữ BlockAllTraining để Vet cấm mọi buổi; Isolated chặn mọi training, Injured chặn Heavy, Monitoring không tự chặn tất cả. Đây là baseline backend cho ý nghĩa từng HealthStatus, đặc tả chỉ liệt kê status mà không định nghĩa đầy đủ điều kiện của chúng. Restriction hiệu lực `ValidFrom <= thời điểm <= ValidUntil` khi có ValidUntil; không tự đặt khoảng nghỉ hay intensity mới.

**Nghiệm thu:** TrainingLock + Heavy bị chặn; Light chỉ được phép khi không vi phạm guard khác; Light Sprint bị NoSprint chặn; Trainer không có override; restriction tương lai/hết hạn được đánh giá theo thời điểm thao tác. Task triển khai/kiểm: **T19**.

## P02 — Medical clearance

**Chốt theo đặc tả:** Vet thực hiện follow-up, có thể tiếp tục treatment, Monitoring có restriction hoặc recovered/clearance; clearance cập nhật restriction/lock và thông báo Trainer. Trainer tự quyết định điều chỉnh/khởi động lại Plan; hệ thống không tự resume.

**Nguồn:** §8.4–8.6; P0222, P0231–P0234.

**Khoa xác nhận:** clearance **theo vấn đề y tế được chọn**, giữ nguyên injury/treatment/restriction không liên quan. Đây là lựa chọn bổ sung nguồn, không phải yêu cầu nguyên văn Word.

**Quy tắc triển khai:** Vet chỉ được chọn các record/injury/treatment/restriction thuộc cùng horse và scope được giao, có danh sách tác động rõ. Không tự đóng toàn bộ vấn đề chỉ vì Clearance=true; restriction tương lai không được xóa ngầm. Lịch sử/quan hệ với follow-up giữ lại. Clearance một vấn đề không buộc whole-horse Fit; Vet đánh giá HealthStatus theo toàn bộ vấn đề còn hiệu lực và guard tiếp tục áp. Không đổi treatment chưa hoàn tất thành Completed trừ khi Vet xác nhận kết thúc treatment đó.

**Gap backend:** hiện Clearance=true yêu cầu Fit, đóng toàn bộ restriction chưa cleared kể cả tương lai, injury Active và treatment chưa completed; bỏ sót Recovering. Đây là implementation hiện tại, khác policy vừa chốt. Task: **T23**, phối hợp **T25**; cần DTO chọn scope, validation quan hệ, audit và tests; migration do chủ module thiết kế nếu cần.

**Nghiệm thu:** horse có injury chân A và vấn đề B; clearance A không đóng B/treatment/restriction tương ứng. Selected Id của horse khác bị từ chối. Có thể tiếp tục Monitoring với restriction còn lại; không tự resume plan; audit chỉ rõ các mục đã đóng.

## P03 — HealthStatus và hồ sơ nhập lùi ngày

**Theo đặc tả:** hồ sơ khám có Examination Date, HealthStatus và timeline; Vet quyết định trạng thái chuyên môn, khai báo Owner không tự cấp Fit (§6.1.4, §8.2; P0124, P0215–P0217).

**Chốt quy ước core:** hồ sơ nhập lùi ngày/correction không được vô ý ghi đè assessment mới hơn. Current HealthStatus theo assessment có hiệu lực mới nhất; bản correction thay thế bản tiền nhiệm trong chuỗi, giữ lịch sử và actor/time. Khi cùng ExaminationAt, dùng thứ tự ghi nhận server CreatedAt rồi Id làm tie-break xác định; GUID chỉ để ổn định khi thời gian ghi nhận bằng nhau, không suy ra thứ tự y tế từ GUID. Correction giữ nguyên ExaminationAt của assessment được sửa; đổi ngày phải là assessment riêng, tránh chuyển một correction cũ thành đánh giá mới bằng cách sửa timestamp.

Injury/restriction đang hiệu lực vẫn được kiểm độc lập; một record Fit không tự clearance mọi vấn đề. Injury nhập lùi ngày không tự ghi đè assessment mới hơn; điều kiện giảm guard phải do Vet xử lý rõ. Timeline bất nhất phải báo để Vet rà lại, không tự suy luận chẩn đoán.

**Gap/backend và nghiệm thu:** PostRecords/PostFollowUps hiện luôn gán HealthStatus; correction và injury chưa có timeline thống nhất. Test Isolated hôm nay + Fit tuần trước vẫn Isolated; correction cũ không đổi assessment mới; cùng timestamp và injury có kết quả xác định. Task: **T22**, phối hợp **T19/T23/T25**. Quy tắc thời gian/tie-break này là quyết định kỹ thuật core, không có chi tiết tương ứng trong Word.

## P04 — Draft, review và cancel

**Chốt theo đặc tả:** Save Draft/Submit/Cancel; Revision Required có lý do rồi Owner resubmit; chỉ approval tạo Horse Profile. Manager **được chỉnh thông tin quản lý cần thiết** có audit. Đề xuất cũ “Manager chỉ được yêu cầu revision” không được dùng để thay thế quyết định V2.

**Nguồn:** §6.1.6–6.2, §14; P0144–P0157, P0301.

**Quy ước core:** submit cần đủ intake, HorsePhoto và Certificate; Owner không sửa PendingReview; cancel ở Draft/RevisionRequired. Submitted/Resubmit là action chuyển đến PendingReview, không bắt buộc tạo status lưu riêng. Không thêm hard rejection hoặc cancel sau approval nếu chưa có yêu cầu. Manager không đổi Owner, người chọn Trainer hoặc health chuyên môn qua edit intake. Allowlist quản lý core: tên nhận diện, registration number và boarding dates; sửa có actor/time và before/after. Chỉnh pedigree/physical/document/khai báo sức khỏe Owner thì Request Revision thay vì tự sửa toàn form. Allowlist này là chi tiết core bổ sung cho câu “thông tin quản lý cần thiết” của Word.

**Khoa xác nhận:** **cho lưu Draft thiếu thông tin; Submit mới yêu cầu đủ intake, ảnh và chứng nhận**. Draft thuộc Owner xác thực, chưa tạo Horse Profile; field đã nhập vẫn phải đúng kiểu/giá trị/quan hệ. Trường còn trống không giả thành giá trị mặc định hợp lệ. Có action lưu Draft, không tự tuyên bố có autosave nền. Submit/resubmit validate toàn hồ sơ trước đổi trạng thái.

**Gap:** RegistrationRequest hiện bắt buộc đủ dữ liệu khi Create/Edit; Manager PendingReview dùng Apply toàn request. Task: **T13**, phối hợp **T12/T05/T16**; cần DTO và nullability phù hợp, không chỉ bỏ validation.

**Nghiệm thu:** Owner lưu Draft chỉ có Horse Name được; Submit thiếu field/file bị từ chối có lỗi rõ và giữ Draft. Owner không sửa PendingReview; Manager chỉ sửa allowlist có audit; RevisionRequired có lý do và resubmit lại; approval tạo một Horse.

## P05 — Thời lượng và overlap lịch

**Chốt phạm vi core:** session có Date/Time và các target; chưa đưa planned duration, ScheduledEndAt hoặc nghỉ bắt buộc vào core. Nguồn §7.3/P0188 không có duration/end time hoặc định nghĩa interval overlap. Chưa được công bố rằng calendar chống chồng lấn khoảng thời gian.

**Quy ước core:** giữ kiểm Rider trùng ScheduledAt và chặn horse/Rider cùng InProgress, luôn kiểm medical guard. Assigned chiếm quyền thực thi của Rider được giao; Planned chưa có Rider không thể start. Chọn sửa interval overlap sau khi có yêu cầu duration rõ, ghi ở **T18** là deferred, không đánh dấu Done bằng kiểm cùng timestamp.

**Nghiệm thu baseline:** hai session Assigned cùng Rider/cùng instant bị chặn; không hai buổi InProgress cho một horse/Rider; không tự giả định 30/60 phút hoặc rest interval. T18 vẫn là phần mở rộng chưa nghiệm thu, dù quyết định phạm vi của P05 đã rõ.

## P06 — Archive và công việc còn mở

**Chốt theo đặc tả:** ưu tiên archive/soft-delete core, giữ history (§12/P0280). Archive không phải hành động medical clearance và không được tự đổi injury thành Recovered hoặc treatment thành Completed.

**Quy ước core:** Manager thao tác archive; không archive khi session đang InProgress; chặn tạo/sửa/execute mới và không reminder horse archived. Owner/Manager được đọc lịch sử trong scope; quyền clinical không mở rộng vì archive.

**Khoa xác nhận:** **kết thúc assignment, đóng plan/công việc chưa hoàn tất có lý do archive; giữ lịch sử chỉ đọc**.

**Mapping core:** assignment Active=false, ghi EndDate tại ngày archive theo timezone Club, giữ creator/history; plan Active/Paused → Archived; session Planned/Assigned → Skipped với reason HorseArchived; care Pending → Skipped với reason HorseArchived. Không đổi Completed/Skipped/IssueReported hoặc result/evaluation cũ. Task thực sự InProgress phải được người có quyền ghi kết quả hoặc kết thúc có lý do trước khi archive; không tự ghi Completed giả cho care/treatment. Đóng occupancy ở instant archive; toàn bộ thay đổi vận hành/notification/audit trong cùng transaction. Gửi thông báo cho người bị ảnh hưởng trước khi kết thúc assignment hoặc lấy recipient snapshot trong transaction. Medical records/injuries/treatments/restrictions vẫn giữ lịch sử và trạng thái đúng, không clearance do archive.

Historical read-only phải có quyền riêng: Owner và Manager xem phần vận hành theo scope, Vet/nhân sự cũ chỉ phần lịch sử được phép theo nhiệm vụ đã có, không được cấp rộng clinical hoặc quyền sửa. Không có route đọc ngựa archived hiện tại không được diễn giải thành yêu cầu xóa khả năng đọc history.

**Gap:** backend hiện chỉ archive flag + đóng occupancy; read horse archived trả 409, chưa có historical read-only rõ và chưa xử lý toàn bộ work/assignments. Task: **T15**, phối hợp **T14/T27/T29/T38**.

**Nghiệm thu:** archive lúc session/care InProgress bị chặn; archive thành công đóng assignment/plan/pending session/care/occupancy đúng mapping và reason; API cũ không sửa/execute, worker không nhắc; lịch sử đọc đúng scope; fault transaction không để trạng thái dở dang; clinical không tự đổi thành recovered/completed.

## P07 — Clinical privacy và instructions để thực thi

**Chốt theo đặc tả:** diagnosis/medication/clinical notes không tự hiển thị cho non-medical roles; Owner nhận health/care summary. Groom được cấp instructions cần thiết để làm treatment/care được giao, không có quyền xem toàn hồ sơ khám. Manager có club-wide operating/medical summary, không mặc định có diagnosis/medication chi tiết.

**Nguồn:** §1, §8.4, §9–10, §13; P0014, P0223, P0238, P0253–P0257, P0295–P0297.

**Chốt quy ước quyền tối thiểu:** Vet assigned xem/sửa clinical trong scope. Groom chỉ instructions task của mình, gồm Treatment và IceBath; medication liều dùng thực sự cần để thực thi là ngoại lệ theo task, không mở clinical record. Owner/HeadTrainer/Trainer/Rider/Manager nhận restriction vận hành và summary phù hợp, không treatment/ice-bath instructions có nội dung clinical. Restriction reason phải là hướng dẫn vận hành, không dùng làm chỗ chứa diagnosis. Không mở rộng bằng report, notification, incident hoặc download.

**Gap:** CareService che Treatment nhưng chưa che IceBath cùng rule, và clinical flag hiện cho phép Manager xem instructions. Các đường list/detail/report/attachment cần kiểm field-level. Task: **T24**, phối hợp **T08/T16/T27/T28/T33**. Trường có reference tới medical record vẫn phải kiểm quyền trước đọc nội dung.

## P08 — Reassignment và lịch sử Rider

**Chốt theo đặc tả:** HeadTrainer chọn Trainer; assignment không ghi đè history; Rider chỉ thực thi session được giao (§6.4, §7.4, §13; P0171–P0173, P0193–P0197, P0289–P0293).

**Quy ước core:** staff cũ mất quyền sửa ngay sau assignment kết thúc; Trainer mới nhận trách nhiệm trên horse, creator của Plan giữ nguyên. Rider chỉ start/submit session còn được giao, được đọc lịch sử session đã thực hiện dưới dạng read-only; lịch sử đó không cấp quyền đọc toàn current Horse Profile/clinical hay session của Rider khác. Manager/Owner/current staff đọc history trong scope tương ứng; không tự thêm unassign/future-assignment API.

**Gap:** ClubAccess.Horses hiện cho Rider horse scope khi có bất kỳ session được giao, chưa tách historical vs current field-level. Task: **T14**, phối hợp **T08/T20/T21/T24**.

## P09 — Overdue, reminders và giới hạn report

**Theo đặc tả:** có overdue/missed session, follow-up/preventive due, filters Day/Week/Month/Custom, pagination và No data (§5, §10–12; P0090–P0091, P0247–P0248, P0263–P0266, P0278). Không quy định 60 phút hoặc một báo cáo chỉ 100 dòng.

**Chốt quy ước core:** overdue session là Assigned, Plan Active, horse chưa archived, thời điểm hiện tại **lớn hơn** ScheduledAt + Business:OverdueAfterMinutes (mặc định core 60, cấu hình được). KPI mang tên overdue và reminder dùng cùng điều kiện; “đã qua giờ” nếu cần là chỉ số khác, không dùng cùng tên. Due theo ngày Club Business:TimeZoneId (core Asia/Ho_Chi_Minh), UTC cho lưu instant; không lấy timezone máy chạy.

Reminder chỉ đánh dấu đã tạo khi có recipient active đúng role/assignment và transaction notification/marker commit; không tự nhận là đã gửi tới inbox. Một event không phát lặp sau restart thông thường; đã gửi trước reassignment không tự phát lại cho recipient mới trong core. SMTP vẫn at-least-once, không cam kết exactly-once. Report/list trả đủ qua paging hoặc báo limit rõ, không âm thầm Take(100).

**Gap:** recipient-aware worker đã được sửa bước 5; Dashboard vẫn tính quá ScheduledAt ngay, khác threshold worker; plan detail và clinical report vẫn Take(100). Task: **T33/T34/T35/T36/T38**, phối hợp **T26**. Test 59/60/61 phút, paused/archived/inactive, giao Vet sau due, ngày Việt Nam và trên 100 records.

## P10 — Result, evaluation và điều chỉnh session tương lai

**Chốt theo đặc tả:** Rider gửi kết quả/abnormal observation; Trainer evaluate, quyết định Continue hoặc Adjust future sessions; evaluation được lưu history. Abnormal observation tạo Issue/Incident và notify (§7.4–7.5, §12; P0195–P0204, P0275).

**Quy ước core:** AdjustFutureSessions là quyết định/yêu cầu chỉnh, không là thuật toán tự đổi distance/intensity/date. Trainer phải sửa rõ từng future session, kiểm lại quyền/medical/time và giữ revision; không sửa kết quả/final sessions. UI không thông báo lịch đã đổi chỉ vì cờ true. Lock phát sinh giữa buổi không làm mất observation/result; lưu IssueReported và báo Vet/Trainer, không xem đó là quyền tiếp tục training bị cấm.

**Backend hiện tại:** evaluation lưu flag intent; EditSession đã kiểm current Trainer, session Planned/Assigned, Plan Active, medical guard, Rider conflict và ghi TrainingHistory/audit. Không thiếu API chỉnh thủ công chỉ vì flag không tự đổi lịch. Cần kiểm luồng evaluation → edit session có history và UI diễn đạt đúng; T21 chưa tự coi Done qua lần đọc code này. Task: **T20/T21**, phối hợp **T19**. Không tự thêm sinh lịch theo frequency hay điều chỉnh tự động nếu chưa có rule.

## Bàn giao và trạng thái task

| Policy | Trạng thái hiện tại | Task triển khai chính |
| --- | --- | --- |
| P01 | Chốt yêu cầu lock + baseline HealthStatus | T19 |
| P02 | Khoa xác nhận clearance theo vấn đề được chọn; có gap code | T23/T25 |
| P03 | Chốt quy ước temporal/correction core, có gap code | T22 |
| P04 | Khoa xác nhận partial Draft, submit đầy đủ; review theo V2 | T13/T12 |
| P05 | Chốt core không duration; interval overlap deferred | T18 |
| P06 | Khoa xác nhận đóng vận hành khi archive, giữ history read-only | T15 |
| P07 | Chốt privacy và quyền thực thi tối thiểu, có gap code | T24/T08 |
| P08 | Chốt reassignment/history scope core, có gap code | T14/T08 |
| P09 | Chốt threshold/timezone/paging core, có gap code | T33/T34/T35/T38 |
| P10 | Chốt điều chỉnh thủ công có history | T20/T21 |

Không triển khai các task trong cột cuối trong lần xử lý T02 này. Không đổi file Word gốc. Các task cần thay code/schema vẫn được giao riêng cho chủ module/reviewer; việc tài liệu policy xong không tự đánh dấu chúng Done. T01 đã có nguồn Word cho đối chiếu nhưng chưa tự hoàn thành Figma/frontend. T06/T07 không được coi hoàn thành qua thao tác này.
