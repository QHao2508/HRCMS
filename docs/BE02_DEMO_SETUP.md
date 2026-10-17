# Tài khoản demo để test BE02 bằng Swagger

Database: `HRCMS`, SQL Server Express `.\SQLEXPRESS`, theo appsettings.json. Công cụ chỉ tạo tài khoản demo, không tạo ngựa/assignment/plan/session để Khoa tự thử BE-003 → BE-004. Không cần OTP hoặc staff invitation. API vẫn kiểm đăng nhập, token và quyền như bình thường; không có endpoint bypass Authentication.

## Chạy seed

Từ repository, chạy:

```powershell
dotnet run --project tools/HorseClub.DemoSeed -- . HRCMS
```

Schema phải có trước. CLI chỉ chấp nhận SQL Express local, Windows Authentication, database local `HRCMS` hoặc có hậu tố `_Test` và tên khớp đối số xác nhận; không migrate/create/drop database. Nếu đã có account đúng email, username, role, active, verified và mật khẩu demo, giữ nguyên; nếu xung đột, rollback và báo lỗi, không reset account cũ. Password policy dùng SecurityOptions và AuthenticationService.CheckPassword; password lưu bằng PasswordHasher như auth hiện có. CLI gọi BLL DemoAccountSeeder → DAL DbContext; không thêm route hoặc thay luồng auth BE01.

Danh sách seed ở docs/demo/BE02-accounts.json. Mật khẩu sinh ngẫu nhiên lần đầu và lưu trong **TestResults/BE02/demo-credentials.json** (Git ignored); lần sau dùng lại file này. Không xóa file nếu muốn seed lại và tiếp tục dùng password cũ. Đây là credentials local cho demo, không phải tài khoản dùng triển khai.

## Đăng nhập

Theo yêu cầu Khoa, mật khẩu chung của 5 tài khoản demo local đã đặt thành **123**. Chỉ công cụ seed demo có chế độ reset tường minh; không giảm SecurityOptions hoặc thay validation register/reset/invitation của API. Các token cũ bị thu hồi sau đổi mật khẩu; login lại và Authorize token mới.

Để đặt lại mật khẩu demo:

```powershell
$env:HRCMS_DEMO_PASSWORD='123'
dotnet run --project tools/HorseClub.DemoSeed -- . HRCMS --reset-demo-password
Remove-Item Env:HRCMS_DEMO_PASSWORD
```

| Role | Email |
| --- | --- |
| HorseOwner | be02.owner@example.test |
| ClubManager | be02.manager@example.test |
| HeadTrainer | be02.headtrainer@example.test |
| Trainer | be02.trainer@example.test |
| WorkRider | be02.rider@example.test |

1. Mở file credentials local, copy giá trị `password` không lấy dấu ngoặc kép. Các tài khoản dùng chung mật khẩu demo này.
2. Chạy `dotnet run --project Horse_BackEnd --launch-profile http` và mở http://localhost:5299/swagger.
3. POST /api/auth/login với email trong bảng và password vừa copy. Mong đợi 200 và accessToken.
4. Authorize bằng accessToken, không thêm `Bearer`. GET /api/auth/me kiểm đúng role.
5. Bắt đầu Owner POST /api/registrations; lần lượt Manager review/assignment, HeadTrainer chọn Trainer/tạo Template, Trainer tạo Plan/session, Rider start/result, Trainer evaluation.

Sau seed, các role staff **chưa được phân công ngựa**. Phải làm official assignment qua API trong BE-003 trước khi Trainer tạo Plan. Owner preference không thay cho assignment. Không cần bật Workers để login/demo BE02 bằng các account đã seed; khi kiểm notifications/reminders hoặc luồng OTP riêng, bật worker và chọn email delivery phù hợp.

Request mẫu: [BE-003 intake](http/BE-003-intake.http), [BE-004 training](http/BE-004-training.http). Chưa có dữ liệu Horse/Plan tạo sẵn, không có ảnh/chứng nhận giả tự upload.
