# Phân công nhân sự cho ngựa

## Ai phân công ai

| Người thao tác | Được giao | Ghi chú |
|---|---|---|
| Quản lý CLB | Huấn luyện viên trưởng, Nhân viên chăm sóc, Bác sĩ thú y | Không giao Huấn luyện viên trực tiếp |
| Huấn luyện viên trưởng **đang phụ trách ngựa** | Huấn luyện viên | Chọn theo chuyên môn |

- Mỗi vai trò chỉ có **một người đang phụ trách** một ngựa. Giao người mới thì người cũ tự kết thúc (`Active = false`, `EndDate = StartDate` của người mới); lịch sử giữ nguyên.
- Đổi Huấn luyện viên trưởng: bắt buộc `changeReason`; lý do lưu vào ghi chú phân công và audit. Thông báo: chủ ngựa `HeadTrainerChangedForOwner`, người cũ `YouWereReplacedAsHeadTrainer`, người mới `YouAreNowHeadTrainer`.
- Đổi Nhân viên chăm sóc: công việc chưa bắt đầu chuyển sang người mới; việc đang làm chỉ chuyển khi tick "tiếp quản" và ghi lý do.

## Khi nào một Huấn luyện viên "Phù hợp"

`TrainingSchedulingService.Evaluate` là **nơi duy nhất** chứa quy tắc. Danh sách ứng viên (`GET /api/training/trainer-candidates`) và thao tác lưu (`POST /api/horses/{id}/assignments`) đều gọi nó, nên "Phù hợp" trên màn hình luôn lưu được.

Kiểm tra theo thứ tự, mỗi lỗi là một `issue` (MessageKey + tham số, Frontend tự dịch):

1. Có chuyên môn đang chọn → `TrainerSpecialtyMismatch`.
2. Với mỗi giáo án của kế hoạch đang chạy trên ngựa: đã được Huấn luyện viên trưởng giao giáo án đó **và** đủ chuyên môn ở mức tối thiểu → `TrainerNotQualifiedForTemplate` (kèm tên giáo án).
3. Với mỗi buổi tập chưa xong của ngựa:
   - buổi đang diễn ra thì chỉ người đang phụ trách được giữ → `TrainerSessionInProgress`;
   - đúng loại huấn luyện của buổi (nếu đã khai hồ sơ chuyên môn) → `TrainerSpecialtyMismatch`;
   - nằm trong ca làm (nếu đã khai ca) → `StaffOutsideWorkingHours` / `SchedulingDurationRequired`;
   - không trùng nghỉ phép → `StaffTimeOffConflict`;
   - không trùng buổi tập của ngựa khác → `TrainerScheduleOverlap`.

Danh sách ứng viên trả `matches = issues rỗng`, `matchLevel` = mức ở chuyên môn đang chọn; sắp xếp: phù hợp → mức cao → ít ngựa → tên.

## Dữ liệu được nạp thế nào

Mỗi request nạp một lần: buổi tập chưa xong của ngựa (join kế hoạch để biết giáo án), giáo án và danh sách Trainer được giao, rồi chuyên môn / ca làm / nghỉ phép / lịch ngựa khác của **tất cả** ứng viên theo lô. Trước đây mỗi ứng viên, mỗi buổi tập là vài truy vấn riêng.

## Frontend

`src/pages/horses/AssignmentForm.jsx` (form) và `src/components/TrainerCandidates.jsx` (bảng ứng viên + lý do). Các hàm thuần `assignableRoles`, `currentHeadTrainer`, `staffOptions`, `assignmentRequest` có test ở `tests/assignment-form.test.js`.
