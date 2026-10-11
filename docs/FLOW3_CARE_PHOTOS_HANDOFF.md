# Flow 3 — bàn giao chăm sóc và ảnh sự cố

Ngày bàn giao: 12/10/2026. Nhánh bàn giao ở BE và FE: `khoa/fix-flow3`.

## Phạm vi

Các thay đổi lần này sửa giao diện FE: không dùng UUID làm số đăng ký ngựa, hướng dẫn khi chưa có phác đồ điều trị, mở phần tải ảnh sau khi báo sự cố và giữ header/menu khi cuộn. Không thay đổi API, schema hoặc quyền backend. Commit backend này bổ sung tài liệu bàn giao.

## Hợp đồng đã đối chiếu

- `GET /api/horses/{horseId}/medical/treatments`: danh sách phác đồ theo quyền y tế, có phân trang và trường `completed`.
- `POST /api/care/tasks`: việc loại `Treatment` cần `treatmentPlanId`; phác đồ phải chưa hoàn tất và thuộc đúng ngựa. Không đưa phác đồ đã hoàn tất vào lựa chọn FE.
- `POST /api/care/incidents`: trả sự cố vừa tạo, gồm `id`, `reporterId` và `resolved`; FE dùng ID này để mở chi tiết.
- `GET /api/care/incidents/{id}/photos`: xem danh sách ảnh trong phạm vi được cấp quyền.
- `POST /api/care/incidents/{id}/photos`: multipart với trường `file`. Chỉ người báo sự cố được tải lên, trước khi sự cố được xử lý. Backend kiểm tra chữ ký PNG/JPEG, phần mở rộng và giới hạn lưu trữ; ảnh được tải qua API có kiểm quyền.
- Sự cố đã xử lý vẫn xem ảnh đã có, nhưng không được bổ sung ảnh theo hợp đồng hiện tại.

## Kiểm thử bàn giao

1. Đăng nhập Veterinarian, chọn ngựa thiếu số đăng ký ở Chấn thương: tên không kèm UUID, context báo chưa có số đăng ký.
2. Ở Công việc chăm sóc, chọn ngựa chưa có phác đồ: thấy hướng dẫn và liên kết tạo phác đồ; không lưu được việc điều trị thiếu phác đồ.
3. Tạo phác đồ cho đúng ngựa ở trang Điều trị, quay lại và tải lại danh sách: chọn được phác đồ đang hoạt động. Phác đồ hoàn tất không xuất hiện.
4. Báo sự cố bằng tài khoản có quyền: chi tiết tự mở. Người báo chọn PNG/JPEG và tải ảnh trước khi xử lý; sau đó xem/tải ảnh từ chi tiết.
5. Với người tiếp nhận hoặc sự cố đã xử lý: hiển thị lý do không thể thêm ảnh, không hiện thao tác upload trái quyền.
6. Cuộn các màn nghiệp vụ trên desktop và mobile: header/menu giữ vị trí, phần nội dung vẫn cuộn; menu mobile cuộn ngang.

FE đã đạt lint, build và 214 tests. Kiểm chứng UI dùng fixture trong bộ nhớ, không ghi bệnh án hoặc sự cố vào Azure. Truy vấn chỉ đọc Azure xác nhận ngựa trong phản hồi người dùng chưa có phác đồ điều trị tại thời điểm kiểm tra. Kiểm thử upload ảnh thực tế cần thực hiện bằng tài khoản/ngựa thử nghiệm được cấp quyền.

Các file cấu hình Azure, thông tin đăng nhập, công cụ đặt lại tài khoản và bằng chứng thử nghiệm trong `TestResults` được giữ ngoài Git.
