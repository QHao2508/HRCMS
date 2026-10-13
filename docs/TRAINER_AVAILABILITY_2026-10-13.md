# Chuyên môn trainer, lịch trống và dữ liệu thực tế — 13/10/2026

## Phân tích và phạm vi

Hệ thống đã có lịch ngựa/WorkRider nhưng chưa có hồ sơ chuyên môn, ca làm việc hoặc kiểm tra trainer phụ trách nhiều ngựa cùng giờ. Nguồn công khai cung cấp tên, mã ngựa, phả hệ và thành tích trainer; không cung cấp lịch làm việc nội bộ, quyền sở hữu tại HRCMS hoặc kết luận sức khỏe hiện tại.

Thực hiện ba phần liên kết:

1. Thu thập 6 hồ sơ ngựa và 5 trainer thật từ The Hong Kong Jockey Club, mỗi hồ sơ có URL nguồn và ngày thu thập. Lưu tại [JSON dữ liệu](data/public-racing-reference-2026-10-13.json). FE có thư viện nguồn tại danh sách ngựa và trang nhân sự. Không tự tạo tài khoản thật, gán ngựa quốc tế cho chủ sở hữu HRCMS hoặc gán sức khỏe Fit. Ngựa nghỉ đua giữ trạng thái nguồn Retired.
2. Hiển thị khoảng trống theo lịch huấn luyện trên hồ sơ ngựa và form tạo/sửa buổi tập: ngựa, trainer hiện tại và WorkRider được chọn; tìm giao các khoảng đủ thời lượng, chọn giờ bắt đầu, tải lại lịch và cảnh báo giờ chưa được xác nhận. Giờ hiển thị theo múi giờ CLB.
3. Quản lý xác nhận chuyên môn theo sáu loại bài tập hiện hành: Walk/Trot/Canter/Gallop/Sprint/Recovery. Mỗi loại có mức 1 đủ năng lực, 2 thành thạo, 3 chuyên sâu. Quản lý khai báo nhiều ca theo tuần và khoảng nghỉ. HeadTrainer được phân công cho ngựa chọn chuyên môn cần thiết và trainer phù hợp; danh sách ưu tiên mức thành thạo rồi số ngựa đang phụ trách.

## Quy tắc

- Khoảng thời gian là `[start,end)`: hai buổi tiếp giáp được phép. Buổi tập cần nằm trọn trong một ca đã khai báo; ca qua nửa đêm khai báo thành hai ca ở hai ngày.
- Backend kiểm tra trainer hiện tại từ phân công đang hoạt động, không dùng creator của plan đã bàn giao làm người phụ trách hiện tại.
- Chặn trùng trainer giữa nhiều ngựa khi tạo/sửa/giao rider/bắt đầu, khi đổi trainer và khi mở lại plan Paused. Transaction Serializable hiện có bảo vệ hai server đặt cùng trainer đồng thời.
- Profile đã có chuyên môn là nguồn có thẩm quyền: bài tập không khớp bị chặn. Phân công có `requiredTrainingType` bắt buộc có chuyên môn tương ứng; bỏ field khi bàn giao không xóa yêu cầu của phân công hiện hành.
- Sửa profile không được làm mất chuyên môn cần bởi phân công hoặc buổi tập đang mở, không được đặt ca/nghỉ mâu thuẫn các booking tương lai. Profile thay thế nguyên bộ trong transaction và ghi audit.
- Nhân sự thiếu ca làm việc hoặc thiếu chuyên môn không có khoảng trống chung được xác nhận. Ngựa/buổi tập thiếu thời lượng không được đoán thành 30/60 phút; giao diện giữ ngày ở trạng thái Unknown. Buổi đang InProgress giữ nguồn lực bận cho tới khi hoàn tất hoặc bỏ qua.
- Dữ liệu API cũ không có chuyên môn/ca/thời lượng vẫn giữ tương thích. Với thời lượng chưa biết, API legacy chỉ kiểm mốc bắt đầu theo hợp đồng cũ; không tuyên bố đã kiểm đầy đủ khoảng. FE mới yêu cầu thời lượng khi tạo/sửa.
- Lịch hiển thị chỉ phản ánh training, ca/nghỉ đã khai báo và điều kiện y tế cho bài tập. Chưa suy ra lịch chăm sóc, vận chuyển, thời gian hồi phục hay sự kiện ngoài hệ thống. Điều kiện y tế do backend kiểm cho loại bài tập/cường độ/quãng đường; không hiển thị nội dung lâm sàng trong các dòng bận.
- Lịch của ngựa chỉ cho Manager hoặc HeadTrainer/Trainer trong phạm vi hiện hành. Owner/rider không tra toàn bộ lịch của nhân sự. Lịch trainer/rider trên ngựa khác chỉ trả khoảng bận tổng quát, không trả tên/ID ngựa khác hoặc nội dung buổi tập.

## API

| Endpoint | Quyền / dữ liệu |
|---|---|
| `GET /api/staff/{id}/training-profile` | Manager; specialties, workingHours, timeOff |
| `PUT /api/staff/{id}/training-profile` | Manager; thay thế nguyên bộ, giữ tương thích booking |
| `GET /api/training/trainer-candidates?horseId=…&trainingType=Trot` | Manager hoặc HeadTrainer được phân công; không trả email/contact/password |
| `GET /api/training/availability` | Manager/HeadTrainer/Trainer trong phạm vi ngựa |
| `GET /api/training/reference-data` | Manager/HeadTrainer/Trainer; snapshot công khai đã thu thập |
| `POST /api/horses/{id}/assignments` | Thêm `requiredTrainingType` tùy chọn theo hợp đồng tương thích; FE trainer luôn gửi |

Availability query bắt buộc `horseId`, `date` dạng yyyy-MM-dd, `durationMinutes` 1–480, `trainingType`, `intensity`, `distanceMetres`; tùy chọn `riderId`, `exceptSessionId` của buổi Planned/Assigned thuộc đúng ngựa khi chỉnh sửa. Response gồm trạng thái, `free`, `busy` của mỗi nguồn lực và `commonFree`.

WorkingHours dùng `dayOfWeek` tên enum Sunday…Saturday, `startMinute/endMinute` tính từ 00:00 theo timezone CLB. Có thể khai báo hai ca để biểu diễn nghỉ trưa; ca không chồng nhau. TimeOff gửi timestamp có offset; database lưu UTC ticks. Không lưu lý do nghỉ cá nhân.

## Schema và triển khai môi trường vận hành

Migration `20261013044927_TrainerExpertiseAndAvailability` thêm bảng `TrainerExpertise`, `StaffWorkingHours`, `StaffTimeOff` và cột nullable `Assignments.RequiredTrainingType`. Không gán chuyên môn hoặc ca làm việc giả cho nhân sự cũ.

SQL delta idempotent: [TRAINER_AVAILABILITY_MIGRATION_2026-10-13.sql](TRAINER_AVAILABILITY_MIGRATION_2026-10-13.sql). Script toàn bộ schema được tạo lại tại `database.sql` và `azure-sql-schema.sql`. OpenAPI mới tại `contracts/openapi.current.json` có 112 operations.

Để đưa vào môi trường vận hành: backup database, áp migration, cập nhật BE và FE cùng phiên bản, rồi Manager xác nhận profile thực tế của Trainer/WorkRider. Chưa áp migration vào Azure HRCMS và chưa khởi động lại BE của người dùng trong đợt sửa này. Kiểm thử dùng database SQL Express tạm, không ghi data thật/booking hoặc gửi email vào Azure.

## Kiểm chứng

- BE: build Release đạt, regression 166 pass / 1 Azure upload skip / 0 fail (167 total) trên SQL Express tạm. Test concurrency và mở lại plan nằm trong regression.
- FE: lint/build đạt và 224 tests pass; build còn cảnh báo bundle lớn khoảng 802 kB. Browser kiểm tra component với fixture, không đại diện UAT live.
- Test nghiệp vụ: hồ sơ/ca chồng nhau, chuyên môn và phạm vi HeadTrainer, giữ yêu cầu chuyên môn khi đổi người, khoảng trống chung, thời gian nghỉ, không lộ ngựa khác, loại buổi đang sửa, thiếu ca/thời lượng, adjacency, chuyên môn không phù hợp, profile ảnh hưởng booking, plan Paused mở lại và hai host đặt cùng trainer.
- FE: hợp đồng query/profile không replay write, timezone +07, toàn bộ duration phải nằm trong khoảng trống, trạng thái thiếu dữ liệu, bảng mức thành thạo và số ngựa đang phụ trách.
- Browser preview dùng component thật và axios adapter trong bộ nhớ: đổi 60→120 phút làm gợi ý thay đổi, chọn 15:00 cập nhật ô bắt đầu, lưu profile giữ thông báo thành công, thư viện có URL nguồn và trạng thái nghỉ đua. Desktop 1366px và mobile 390px; mobile không tràn ngang. Preview không phải bằng chứng UAT trên database vận hành.

## Dữ liệu cần CLB xác nhận

Hồ sơ HKJC là dữ liệu thực tế quốc tế, không chứng minh một cá nhân/ngựa đang thuộc CLB của người dùng. Để có dữ liệu vận hành thật cần CLB cung cấp/xác nhận tài khoản nhân sự, chủ sở hữu, ca/nghỉ, chứng nhận chuyên môn, bệnh án và lịch tập. Không suy ra các field này từ thành tích đua hoặc tự tạo chúng để lấp khoảng trống.
