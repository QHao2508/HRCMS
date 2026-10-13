# Kế hoạch dọn tài khoản và tạo lại đủ role

Database đã kiểm tra: Azure `hrcms.database.windows.net / HRCMS`, 13/10/2026. Sau xác nhận của người dùng, đã commit dọn tài khoản và dữ liệu liên quan lúc 13:48 (UTC+7). Theo yêu cầu tiếp theo, đã tạo lại 23 staff (3 HeadTrainer; 5 Trainer, WorkRider, Groom và Veterinarian mỗi role), giữ ClubManager: tổng 24. Xem [danh sách hiện hành](demo/STAFF_REBUILT_2026-10-13.md). Bảng đề xuất 12 tài khoản bên dưới là kế hoạch trước yêu cầu mới, không phải số tài khoản hiện tại.

## Hiện trạng trước khi dọn

| Role | Hiện có | Giữ lại |
|---|---:|---:|
| ClubManager | 1 | 1 |
| HorseOwner | 4 | 0 |
| HeadTrainer | 3 | 0 |
| Trainer | 3 | 0 |
| WorkRider | 3 | 0 |
| Veterinarian | 4 | 0 |
| Groom | 3 | 0 |

Tài khoản quản lý được giữ: username `clubmanager`. Không thay đổi mật khẩu, trạng thái hay hồ sơ của tài khoản này.

20 tài khoản còn lại có dữ liệu phụ thuộc. Có FK restrictive từ đăng ký/ngựa đến Owner, kế hoạch đến Trainer, buổi tập đến WorkRider; vì vậy không thể chỉ DELETE Users mà giữ các bản ghi đó nguyên trạng.

Phạm vi đã được người dùng xác nhận và kiểm lại trước khi xóa: 9 registrations, 7 horses cùng dữ liệu thuộc chúng: 20 assignments, 7 measurements, 5 plans, 7 sessions, 3 results, 2 evaluations, 27 training revisions, 4 medical records, 1 injury, 1 restriction, 2 treatments, 5 care tasks, 3 incidents, 2 incident-photo metadata, 4 preventive-care records, 2 occupancies và 19 registration-attachment metadata.

Giữ riêng: 13 public reference records vừa nhập, 6 inventory items, 1 stable, 3 stalls, 5 training templates. Sau xác nhận “Xóa tất cả các dữ liệu liên quan”, phạm vi được mở rộng đến stock movements, replenishments, audit, notification và outbox có tham chiếu tài khoản/bản ghi bị xóa. Các giao dịch tồn kho bị xóa được đảo khỏi Stock trong cùng transaction; không giả mạo actor thành ClubManager. Chứng từ/lịch sử của Manager không liên quan được giữ lại.

Với file ảnh/chứng từ: tách việc xóa metadata SQL khỏi xóa blob/file vật lý. Việc dọn tài khoản không tự cấp quyền xóa vĩnh viễn file ngoài SQL. Nếu đóng occupancy vì dọn ngựa, Manager kiểm tra thực tế trạng thái vệ sinh chuồng; không suy đoán tình trạng sạch.

## Quy trình thực hiện sau khi xác nhận phạm vi

1. Kiểm tra lại đúng server/database, đúng tài khoản ClubManager, snapshot 20 tài khoản; dừng nếu có tài khoản mới hoặc role thay đổi.
2. Xuất bản sao phục hồi được bảo vệ, gồm các bảng sẽ xóa; không đưa email, password hash, security stamp hay OTP vào Git/log.
3. Khi không có tác vụ ghi đang chạy, dùng transaction để xóa bản ghi phụ thuộc theo thứ tự FK; giữ tài khoản quản lý và dữ liệu được giữ ở trên. Thu hồi phiên các tài khoản bị xóa theo cơ chế identity lookup của API.
4. Xóa challenges/mail pending gắn tài khoản bị xóa, notifications/outbox của các recipient đó; tránh gửi invitation/OTP cũ sau reset.
5. Kiểm tra chỉ còn ClubManager, các khóa ngoại hợp lệ, dữ liệu tham chiếu vẫn 13 và kho/chuồng không bị mất. Nếu kiểm tra thất bại, rollback.
6. Đọc lại qua kết nối mới, ghi audit reset với số lượng và actor quản lý; không ghi thông tin xác thực. Sau đó mới bắt đầu quy trình tạo tài khoản mới được duyệt.

## Đề xuất bộ tài khoản mới

Đây là kế hoạch, chưa tạo tài khoản hoặc gửi email. Chỉ dùng email người thực sự sử dụng hệ thống; các username dưới đây là tên dự kiến, chưa xác nhận danh tính.

| Role | Số lượng mới | Username dự kiến | Mục đích |
|---|---:|---|---|
| ClubManager | 0 | clubmanager hiện có | Quản lý và phân công |
| HorseOwner | 2 | owner01, owner02 | Sở hữu ngựa; kiểm tra cách ly dữ liệu giữa hai Owner |
| HeadTrainer | 1 | headtrainer01 | Phân công Trainer, quản lý mẫu huấn luyện |
| Trainer | 3 | trainer01, trainer02, trainer03 | Có lựa chọn theo chuyên môn và kiểm tra đổi người phụ trách |
| WorkRider | 2 | rider01, rider02 | Lịch cưỡi, thực hiện/kết quả buổi tập, kiểm tra chồng lịch |
| Veterinarian | 1 | veterinarian01 | Khám, điều trị, hạn chế và tái khám |
| Groom | 2 | groom01, groom02 | Chăm sóc, báo sự cố, vệ sinh, xuất vật tư, kiểm tra bàn giao |

Tổng sau tạo: 12 tài khoản (11 mới và 1 ClubManager giữ lại), đủ 7 role. Đây là cấu hình gọn cho chạy toàn bộ flow; mở rộng thêm Vet/HeadTrainer khi cần kiểm thử ngoài phạm vi phân công.

### Trình tự tạo và chuẩn bị dữ liệu

1. Thu thập tên, email, điện thoại, địa chỉ của người được cấp tài khoản; phân biệt người thật với tài khoản demo. Không lấy tên bác sĩ/trainer công khai rồi tự tạo tài khoản mạo danh.
2. Staff: ClubManager tạo lời mời theo đúng role; người nhận xác nhận và tự đặt mật khẩu. Owner dùng đăng ký và xác minh email. Không dùng chung mật khẩu hoặc tự đánh dấu đã xác minh email người khác.
3. HeadTrainer, Vet, Groom sẵn sàng trước khi đăng ký/duyệt ngựa. Mỗi Owner có ít nhất một ngựa được xác nhận thuộc CLB; không tự nhận ngựa HKJC là tài sản CLB.
4. Phân công staff cho từng ngựa. Nhập hồ sơ Trainer theo chuyên môn đã xác nhận: ví dụ trainer01 thiên về Trot/Canter, trainer02 Sprint/Gallop, trainer03 Walk/Recovery nếu các giá trị này phù hợp enum thật của hệ thống. Mức 1–3 do quản lý/chuyên môn xác nhận, không suy từ tên hoặc thống kê đua.
5. Khai báo ca làm việc và nghỉ của Trainer/Rider theo lịch thật; kiểm tra giờ trống trước khi xếp buổi tập.
6. Vet tạo dữ liệu khám/điều trị thật; Manager/Vet giao task phù hợp cho Groom; kiểm kê vật tư và chuồng trước khi ghi occupancy hoặc stock.
7. Kiểm tra đăng nhập cả 7 role, quyền Owner khác, đúng Groom, chặn clinical data ngoài Vet, chồng lịch Rider/Trainer, bàn giao và báo cáo đúng phạm vi.

Thông tin còn cần trước khi tạo tài khoản: danh sách người dùng và email nhận invitation/OTP; dùng hệ thống thật hay tài khoản demo; chuyên môn, ca làm việc và phân công được xác nhận. Không tự tạo mới chỉ vì người dùng yêu cầu lập kế hoạch.

## Kết quả dọn đã áp dụng

- Còn 1 User: `clubmanager`. Tất cả cột hồ sơ/trạng thái/mật khẩu/security stamp của Manager được so sánh trước/sau trong transaction và giữ nguyên.
- Xóa 20 Users, 9 Registrations, 7 Horses, 20 Assignments, 7 Measurements; 5 Plans, 7 Sessions, 3 Results, 2 Evaluations, 27 TrainingRevisions.
- Xóa 4 MedicalRecords, 1 Injury, 1 Restriction, 2 Treatments, 4 PreventiveCare; 5 CareTasks, 3 Incidents, 2 IncidentPhotos, 19 Attachments, 2 Occupancies.
- Xóa 11 Challenges, 11 EmailMessages, 96 Notifications, 707 RealtimeOutboxMessages và 144 Audit liên quan; không còn tham chiếu GUID đến các bản ghi đã xóa trong những bảng ứng dụng còn lại.
- Xóa 2 Replenishments và 2 StockMovements liên quan. Stock của 2 Inventory items được điều chỉnh bằng cách trừ tổng Quantity của những movement bị xóa; dữ liệu còn lại không có tồn âm.
- Giữ 13 PublicReferenceRecords, 6 Inventory items, 1 Stable, 3 Stalls, 5 Templates; giữ 6 StockMovements, 19 Audit và 280 RealtimeOutboxMessages của Manager không liên quan đến dữ liệu đã xóa.
- Metadata ảnh/chứng từ liên quan đã xóa khỏi SQL; blob/file vật lý bên ngoài database chưa bị xóa.

Backup toàn bộ 37 bảng ứng dụng trước xóa được mã hóa Windows DPAPI CurrentUser, đã giải mã trong bộ nhớ và kiểm tra SHA-256 thành công. Bản backup và receipt local nằm ở `TestResults/AccountCleanup/run-20261013-064827/` (Git ignore). Chỉ tài khoản Windows tạo backup có thể giải mã bằng DPAPI; không đưa nội dung tài khoản/OTP/password hash vào tài liệu hoặc Git. Có một lượt chạy thử thành công với rollback trước khi áp dụng.
