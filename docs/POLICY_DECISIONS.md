# Bước 2 — Quy tắc nghiệp vụ cần xác nhận

Ngày: 04/10/2026. Tất cả mục P01–P10 dưới đây đang **chờ xác nhận** từ yêu cầu/nhóm/người dùng. Việc đọc hoặc tạo tài liệu không được tính là đã đồng ý đổi behavior. Không thay đổi entity, schema hay workflow trong bước này.

Người dùng cho phép hoãn đối chiếu frontend vì chưa hoàn thành. Backend tiếp tục với baseline hiện tại; không yêu cầu frontend hoặc quyết định toàn bộ P01–P10 trước khi làm kiểm thử SQL Server và sửa metadata contract. Lựa chọn này không đồng nghĩa chấp nhận các thay đổi policy đề xuất.

Phân biệt: "Hiện tại" là behavior quan sát từ code; "Đề xuất" là phương án cụ thể để review; "Kiểm nghiệm thu" là ví dụ cần test sau khi thống nhất. Chủ nghiệp vụ phải xác nhận những lựa chọn ảnh hưởng vận hành/clinical; không coi chính sách hiện tại là hướng dẫn chuyên môn y tế.

## P01 — Ý nghĩa medical lock

**Hiện tại:** Isolated/BlockAllTraining chặn mọi training. Injured và TrainingLock chặn Heavy. MaxIntensity/MaxDistance/NoSprint kiểm từng buổi. Monitoring không tự chặn. Guard kiểm create/edit/assign tại thời điểm dự kiến, start tại thời điểm thực tế.

**Đề xuất:** giữ phân biệt TrainingLock với BlockAllTraining đang có cho core, hiển thị nhãn UI nêu rõ giới hạn, ví dụ "chặn tập nặng" và "cấm mọi buổi tập". Vet dùng limits để mô tả hoạt động phù hợp. Nếu đặc tả định nghĩa TrainingLock=cấm mọi training, phải đổi guard và labels cùng nhau.

**Kiểm nghiệm thu:** lock chặn Heavy nhưng không tự chặn Light phù hợp các limits khác; NoSprint chặn Sprint dù Light; nhiều restriction áp đồng thời; expired restriction không chặn; Trainer không override được. Guard nên đánh giá toàn khoảng buổi nếu P05 thêm duration.

## P02 — Clearance toàn ngựa hay theo từng vấn đề

**Hiện tại:** follow-up clearance yêu cầu Fit, đóng mọi restriction chưa cleared (cả tương lai), injury Active và treatment chưa completed trên horse; không tự resume plan. Injury Recovering chưa được điều kiện clearance đóng.

**Phương án A — giữ core hiện tại:** clearance là quyết định toàn ngựa, UI có danh sách tất cả thứ sẽ đóng và xác nhận rõ; xử lý nhất quán Recovering. Không giả vờ hỗ trợ clearance riêng injury.

**Phương án B — đề xuất khi cần sử dụng nhiều vấn đề y tế độc lập:** chọn rõ injury/treatment/restriction được clearance; không xóa giới hạn unrelated/future. Clearance một injury không tự cấp Fit cho toàn horse nếu vấn đề khác còn ảnh hưởng; Vet quyết định health aggregate. Cần DTO/quan hệ/validation và migration khi cần, không chỉ sửa một vòng foreach.

**Kiểm nghiệm thu:** horse có injury chân và vấn đề khác; clearance chân không kết thúc treatment còn lại theo B. Theo A, người dùng thấy toàn bộ danh sách tác động trước thao tác. Cả hai phương án đều giữ history và không tự resume plan.

## P03 — Trạng thái sức khỏe theo thời gian khám

**Hiện tại:** thêm record/follow-up luôn gán HealthStatus dù ExaminationAt cũ. Correction có kiểm nếu không có examination mới hơn previous, nhưng chưa có quy tắc timeline thống nhất.

**Đề xuất:** HealthStatus chính thức theo assessment có hiệu lực mới nhất; backdated entry chỉ thêm history. Correction của lần khám đang có hiệu lực có thể đổi trạng thái; correction một lần cũ không đổi trạng thái mới. Phải chốt tie-break khi cùng ExaminationAt và cách injury event liên hệ với health assessment, không chỉ OrderBy thời gian rồi bỏ qua injury.

**Kiểm nghiệm thu:** khám mới Isolated, nhập thêm hồ sơ Fit tuần trước -> vẫn Isolated. Sửa hồ sơ cũ không thay state mới. Hai assessment cùng timestamp có thứ tự hiệu lực xác định và audit rõ.

## P04 — Draft, review và reject

**Hiện tại:** tạo Draft cần đủ RegistrationRequest hợp lệ, nên không lưu được form đang thiếu fields. Manager được sửa PendingReview; approve=false thành RevisionRequired; cancel chỉ Draft/RevisionRequired.

**Đề xuất:** Owner lưu partial Draft; submit mới yêu cầu đủ fields, HorsePhoto và Certificate. Manager yêu cầu revision thay vì âm thầm sửa nội dung Owner gửi, ngoại trừ trường quản trị nếu được đặc tả cho phép. Giữ RevisionRequired/Cancelled trong core, chỉ thêm Rejected nếu có yêu cầu từ Figma/Word.

**Kiểm nghiệm thu:** lưu chỉ name ở Draft được, submit bị trả lỗi thiếu cụ thể; Owner chỉnh PendingReview bị chặn; revision giữ lý do và audit; approval tạo một Horse. Partial Draft cần thiết kế nullability/schema/DTO trước khi triển khai.

**Phương án giữ phạm vi nhỏ:** giữ Draft đầy đủ như hiện tại nhưng UI phải nói rõ "lưu hồ sơ đã điền đủ"; không quảng bá autosave partial.

## P05 — Thời lượng và xung đột lịch

**Hiện tại:** chỉ ScheduledAt, kiểm Rider đúng cùng instant; start chặn horse/rider có session InProgress. Không có Planned duration/end time.

**Đề xuất:** thêm planned duration hoặc ScheduledEndAt; kiểm khoảng `[start,end)` cho cả horse và Rider. Planned chưa Rider vẫn chiếm slot horse; Assigned chiếm cả horse/Rider. Skipped không chiếm slot; Completed giữ lịch sử. Hai buổi 09:00–09:30 và 09:30–10:00 được phép; 09:15–09:45 xung đột. Chốt nhu cầu thời gian nghỉ giữa buổi; không tự đặt số phút không có yêu cầu.

**Kiểm nghiệm thu:** create/edit/assign kiểm overlap, cross-date/timezone đúng; 2 request đồng thời không double-book; restriction bắt đầu giữa buổi được đánh giá theo P01. Cần schema và test SQL Server. Nếu chỉ core hiện tại, ghi rõ chưa bảo đảm calendar overlap.

## P06 — Archive ngựa và lịch sử

**Hiện tại:** Manager archive nếu không có buổi InProgress; đóng occupancy; Horse đọc bình thường bị 409 và list loại bỏ. Assignment/plan/pending care chưa tự đóng toàn bộ.

**Đề xuất:** archive là kết thúc vận hành: kết thúc assignments, xử lý các plan/session/care chưa final bằng trạng thái thích hợp đã chốt, đóng occupancy, thông báo người liên quan. Read history có route/filter read-only đúng scope; không tạo/update công việc mới. Không xóa records/result/clinical history.

**Kiểm nghiệm thu:** archive với InProgress bị chặn; pending Rider/Groom thấy bị kết thúc, không thao tác được; Manager và Owner còn đọc lịch sử của horse trong quyền; worker không nhắc công việc archived. Cần quyết định cụ thể status/reason trước khi sửa hàng loạt records.

## P07 — Ai đọc hướng dẫn chăm sóc y tế?

**Hiện tại:** chỉ V assigned đọc clinical records. Care Treatment instructions/notes được V/G thực thi/M đọc; Owner bị che. IceBath không bị che cùng rule. Restriction reason có thể đọc trong HS.

**Đề xuất:** diagnosis/medication/clinical notes chỉ V. Treatment/IceBath instructions cần thực thi cho V và G được giao; Manager đọc trạng thái vận hành, hạn chế clinical detail trừ khi policy Club cho phép. Owner/T/R/H chỉ nhận summary cần thiết, không instructions/clinical notes. Restriction reason là operational text có kiểm nội dung; không dùng để chứa diagnosis riêng.

**Kiểm nghiệm thu:** cùng chuỗi medication ghi trong Treatment và IceBath đều không xuất cho Owner/Trainer/Rider qua list/report/notifications/incidents/photos. Nếu Manager được phép đọc instructions, phải ghi rõ đó là ngoại lệ vận hành; không suy ra Manager được toàn clinical.

## P08 — Quyền sau reassignment và Rider session cũ

**Hiện tại:** staff chỉ có assignment Active cho Horse scope; Rider có bất kỳ session giao mình là đọc Horse được. Trainer hiện tại có thể xử lý plan cũ trên horse; plan.TrainerId giữ người tạo.

**Đề xuất:** staff cũ ngừng quyền sửa ngay; Rider chỉ sửa session hiện tại được giao. Lịch sử session Rider đã thực hiện vẫn read-only nhưng Horse profile/summary hiện tại cần chốt hạn chế nếu không còn nhiệm vụ. Phân biệt plan creator với Trainer đang chịu trách nhiệm; không ghi đè creator khi reassign.

**Kiểm nghiệm thu:** Trainer cũ không sửa được, Trainer mới thấy plan trong scope; Rider bị unassign không start/nộp result; lịch sử được hiển thị đúng mức dữ liệu đã chốt.

## P09 — Overdue, reminders và reports

**Hiện tại:** dashboard overdue từ ScheduledAt < now; reminder từ ScheduledAt < now-OverdueAfterMinutes (mặc định 60) và plan Active. Preventive reminder đánh dấu sent dù chưa có Vet recipient. Một số detail/clinical list cắt 100.

**Đề xuất:** dùng threshold cấu hình thống nhất cho nhãn Overdue; nếu cần KPI "đã qua giờ" riêng, đặt tên riêng. Chỉ đánh dấu reminder delivered/created khi có recipient; xử lý recipient mới/idempotency. List có pagination/total, report không âm thầm thiếu dữ liệu.

**Kiểm nghiệm thu:** 59/60/61 phút theo threshold, paused/archived, ngày local; due khi chưa có Vet rồi assign phải có reminder; trên 100 records có thể đọc đủ hoặc nhận thông báo limit rõ.

## P10 — Result, evaluation và điều chỉnh tương lai

**Hiện tại:** result được lưu kể cả lock giữa buổi, IssueReported/incident khi bất thường; duplicate result/evaluation bị chặn. AdjustFutureSessions=true chỉ lưu intent, không tự sửa sessions.

**Đề xuất:** giữ cách bảo toàn result; evaluation có action dẫn Trainer tới sửa các buổi tương lai, giữ revision từng thay đổi. Chỉ triển khai tự điều chỉnh khi có quy tắc cụ thể về session nào, distance/intensity/date mới và quyền/guard.

**Kiểm nghiệm thu:** cờ true không khiến UI nói đã cập nhật lịch; sửa session future bị kiểm lại medical/overlap; past/final sessions không bị viết lại.

## Quyết định cần trả lời trước khi triển khai các thay đổi domain

| ID | Lựa chọn cần chốt | Đề xuất hiện tại | Chủ review |
| --- | --- | --- | --- |
| P01 | TrainingLock chặn Heavy hay mọi training? | Giữ chặn Heavy, BlockAllTraining chặn mọi buổi | BE03 + Club |
| P02 | Clearance toàn ngựa hay chọn từng vấn đề? | Theo từng vấn đề nếu cần vận hành nhiều injury; core có thể giữ toàn ngựa với UI rõ | BE03 + Club |
| P03 | Quy tắc latest assessment, correction, injury và tie-break? | Backdated không ghi đè trạng thái mới | BE03 + BE02 |
| P04 | Draft partial hay phải đầy đủ? Manager sửa nội dung? | Partial draft; Manager yêu cầu revision | BE02 + Owner/Manager |
| P05 | Duration, overlap, nghỉ giữa buổi? | Duration + interval overlap; nghỉ theo yêu cầu thật | BE02 + HeadTrainer |
| P06 | Pending work/history sau archive? | Đóng vận hành, read-only history | BE02 + BE04 + Manager |
| P07 | Manager đọc instructions y tế không? | Operational summary; clinical hạn chế cho V/G thực thi | BE03 + BE04 + Club |
| P08 | Historical Rider scope và quyền Trainer mới? | Read-only historical session, current Horse scope có giới hạn | BE01 + BE02 |
| P09 | Threshold và reminder idempotency? | Cùng threshold cấu hình, recipient-aware | BE05 + chủ module |
| P10 | Evaluation tự sửa hay chỉ đề nghị chỉnh? | Chỉnh thủ công có history | BE02 + Trainer |

Không cần đợi tất cả các quyết định trên để làm C01–C05 sửa metadata, hoặc dựng workflow tests SQL Server theo behavior hiện tại. Test phải ghi rõ baseline hay expected policy mới; không dùng test để âm thầm xác nhận một policy chưa chốt.
