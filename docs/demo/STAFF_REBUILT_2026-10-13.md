# Tài khoản staff đã tạo lại — 13/10/2026

Đã tạo và đọc lại xác nhận 23 tài khoản trên Azure SQL HRCMS. ClubManager hiện tại được giữ nguyên toàn bộ thông tin, bao gồm mật khẩu và security stamp. Tổng Users sau tạo: 24.

Username theo `role.ten`, viết thường và không dấu để phù hợp quy tắc username của hệ thống. Họ tên hiển thị vẫn có dấu. Mỗi hồ sơ có họ tên, email, điện thoại, địa chỉ và role, Active=true, EmailVerified=true. Đây là tài khoản/hồ sơ mẫu theo yêu cầu; không đại diện cho người thật. Email `@example.test` không dùng để gửi thư thực tế. Không có Worker/SMTP được khởi động bởi công cụ tạo tài khoản.

| Role | Username | Họ tên |
|---|---|---|
| HeadTrainer | headtrainer.minh | Nguyễn Văn Minh |
| HeadTrainer | headtrainer.phong | Trần Quốc Phong |
| HeadTrainer | headtrainer.khanh | Lê Minh Khánh |
| Trainer | trainer.an | Nguyễn Hoàng An |
| Trainer | trainer.bao | Trần Gia Bảo |
| Trainer | trainer.duc | Lê Anh Đức |
| Trainer | trainer.nam | Phạm Hải Nam |
| Trainer | trainer.son | Võ Thanh Sơn |
| WorkRider | workrider.huy | Nguyễn Quang Huy |
| WorkRider | workrider.kiet | Trần Tuấn Kiệt |
| WorkRider | workrider.long | Lê Thành Long |
| WorkRider | workrider.quan | Phạm Minh Quân |
| WorkRider | workrider.tuan | Võ Anh Tuấn |
| Groom | groom.mai | Nguyễn Thị Mai |
| Groom | groom.ha | Trần Ngọc Hà |
| Groom | groom.linh | Lê Thùy Linh |
| Groom | groom.trang | Phạm Thu Trang |
| Groom | groom.yen | Võ Hải Yến |
| Veterinarian | vet.lan | Nguyễn Ngọc Lan |
| Veterinarian | vet.thao | Trần Phương Thảo |
| Veterinarian | vet.ngoc | Lê Bảo Ngọc |
| Veterinarian | vet.tung | Phạm Thanh Tùng |
| Veterinarian | vet.vinh | Võ Quang Vinh |

Chi tiết email, điện thoại và địa chỉ: [STAFF_REBUILT_2026-10-13.json](STAFF_REBUILT_2026-10-13.json). Mật khẩu là giá trị người dùng chỉ định trong chat, không ghi vào Git/JSON danh sách. Trong SQL chỉ lưu PasswordHash theo PasswordHasher hiện có.

Công cụ đã kiểm tra PasswordHasher cho từng tài khoản bằng mật khẩu được chỉ định; xác nhận đủ số lượng từng role, đầy đủ trường hồ sơ và Manager không đổi qua connection mới. Sau đó kiểm tra AuthenticationService.Login bằng username cho cả 23 tài khoản trên Azure: 23/23 Authenticated; transaction kiểm tra đã rollback. Đây là kiểm tra BLL, không phải đăng nhập HTTP/trình duyệt. 13 PublicReferenceRecords vẫn được giữ. Không tự gán chuyên môn hoặc ca làm việc chưa được cung cấp/xác nhận, và không tạo lại dữ liệu ngựa đã xóa.

Kết quả thao tác local (Git ignore): `TestResults/AccountRebuild/result.json`. Công cụ đã dùng override cho mật khẩu demo trong một lần chạy được người dùng yêu cầu; không thay đổi password policy đăng ký/reset của ứng dụng.
