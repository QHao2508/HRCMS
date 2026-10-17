# 15 tài khoản nhân viên mẫu

Dữ liệu giả lập trong `docs/demo/STAFF_SAMPLE_ACCOUNTS.json`, đã thêm vào Azure SQL `hrcms.database.windows.net / HRCMS` theo yêu cầu. Mỗi vai trò 3 người, có username, họ tên, email, điện thoại, địa chỉ, password hash, ID, CreatedAt và SecurityStamp. Tài khoản active/verified để test ngay; không phát sinh email OTP đến miền sample.

| Vai trò | Username |
|---|---|
| Bác sĩ thú y | demo.vet01, demo.vet02, demo.vet03 |
| Huấn luyện viên trưởng | demo.headtrainer01, demo.headtrainer02, demo.headtrainer03 |
| Huấn luyện viên | demo.trainer01, demo.trainer02, demo.trainer03 |
| Người cưỡi ngựa | demo.rider01, demo.rider02, demo.rider03 |
| Chăm sóc ngựa | demo.groom01, demo.groom02, demo.groom03 |

Email là `<username>@example.test`, thuộc dữ liệu kiểm thử. Các số điện thoại/địa chỉ/họ tên cũng là mẫu, không dùng để liên hệ người thật. Nhân viên thật vẫn được quản lý tạo và tự kích hoạt bằng OTP.

Credential demo được sinh ngẫu nhiên và lưu ngoài Git tại `%LOCALAPPDATA%/HRCMS/demo/azure-staff-credentials.json`. Danh sách đã kiểm tra lưu cùng thư mục trong `azure-staff-summary.json`. Không đưa các file này vào repository hoặc gửi mật khẩu trong log.

Chạy từ root backend:

```powershell
dotnet run --project tools/HorseClub.StaffSeed/HorseClub.StaffSeed.csproj -- --azure-hrcms
```

Công cụ kiểm chính xác server/database SQL, đọc SQL credential từ User Secrets và chỉ tạo 15 account sample đã định nghĩa. Chạy lại giữ account khớp và không tạo trùng/đổi mật khẩu. Không migrate, tạo database, xóa hay ghi đè tài khoản hiện có. Credential file cần được giữ để chạy lại cùng mật khẩu.
