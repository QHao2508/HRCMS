# Mã thông điệp giữa Backend và Frontend

Backend không gửi câu chữ để hiển thị. Lỗi API và thông báo chỉ mang **mã** (`MessageKey`) và **tham số**. Frontend dịch mã đó sang tiếng Việt trong `src/messages/serverMessages.js`. Không bên nào so khớp câu tiếng Anh.

## Lỗi API

```json
{ "type": "urn:horseclub:error:validation_error", "title": "validation_error", "status": 400,
  "detail": "page must be positive ...", "messageKey": "PageMustBePositiveAndPageSizeBetween1And", "args": ["100"],
  "referenceId": null, "traceId": "..." }
```

- Service dùng `Ensure.That(điều kiện, Messages.Text(MessageKey.X, thamSo...), status, code)` hoặc `new ApiException(status, code, Messages.Text(...))`. Hai hàm này chỉ nhận `MessageText` (mã + tham số), nên không truyền được chuỗi tự viết.
- `detail` là câu tiếng Anh, chỉ dùng để log và debug. Frontend hiển thị theo `messageKey` + `args` (tham số theo vị trí `{0}`, `{1}`).

## Thông báo

`GET /api/notifications` trả `NotificationResponse`:

| Trường | Ý nghĩa |
|---|---|
| `messageCode` | `MessageKey` của nội dung, ví dụ `PreventiveCareIsDue` |
| `type` | `NotificationType` dạng chuỗi, Frontend dùng để chọn màn hình cần mở |
| `referenceId` | ID đối tượng liên quan |
| `horseId`, `horseName` | Ngựa liên quan, suy ra khi đọc từ `referenceId` (ngựa, công việc chăm sóc, buổi tập, phác đồ, chăm sóc phòng ngừa, sự cố) |
| `subject` | Tên hồ sơ đăng ký hoặc vật tư khi thông báo không gắn với ngựa |
| `read`, `createdAt` | Trạng thái đã xem, thời điểm tạo |

Bảng `Notifications` lưu cột `MessageCode`. Migration `20261026020000_NotificationMessageCodes` điền mã cho thông báo cũ một lần, từ nội dung đã lưu; thông báo nào không khớp thì lấy mã mặc định theo loại. Cột `Message` (tiếng Anh) chỉ còn để tra cứu.

## Hợp đồng và kiểm tra

- `docs/contracts/server-message-keys.json` liệt kê mọi `MessageKey` client có thể nhận, kèm số tham số. Khóa chỉ dùng phía server (email, cấu hình, ghi chú) nằm trong `Messages.ServerOnly` và không có trong file này.
- Test `MessageContractTests` báo lỗi khi file không khớp với enum. Sau khi thêm hoặc sửa `MessageKey`, chạy `HRCMS_UPDATE_CONTRACTS=1 dotnet test --filter MessageContractTests` để ghi lại file.
- Chép file sang Frontend (`src/messages/server-message-keys.json`) rồi thêm bản dịch vào `serverMessages.js`. Test `server-messages.test.js` của Frontend báo lỗi khi thiếu hoặc thừa bản dịch, hoặc dùng sai tham số. Khi hai repo nằm cạnh nhau, test này còn so sánh với bản của Backend.
