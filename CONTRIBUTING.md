# Quy trình đóng góp

## Nhận việc

Mỗi issue có một người chịu trách nhiệm, người review, milestone và tiêu chí nghiệm thu. Chia issue lớn thành PR có thể review trong một buổi. Trước khi code, thống nhất API, dữ liệu và trạng thái UI với người phụ trách phần liên quan.

## Nhánh và pull request

- `main`: bản ổn định, demo được; mọi thay đổi đi qua PR.
- `feat/HC-001-short-name`, `fix/HC-001-short-name`, `docs/short-name`: nhánh ngắn hạn từ main.
- Đồng bộ main trước khi tạo PR; không force-push main.
- Commit ghi mục đích: `feat(auth): verify owner email OTP`.
- PR liên kết issue, mô tả hành vi, cách kiểm thử và ảnh UI nếu có.
- Ít nhất một người khác review; CI phải xanh trước khi merge. Squash merge và xóa nhánh đã merge.

Branch protection chỉ có hiệu lực khi đã cấu hình trên GitHub; tài liệu này không tự bật bảo vệ.

## Definition of Done

- Đạt toàn bộ tiêu chí nghiệm thu issue và đúng phân quyền phía server.
- UI có loading, empty, validation/error và permission state phù hợp.
- Kiểm thử nghiệp vụ thay đổi; cập nhật API/schema/migration nếu liên quan.
- Không lộ thông tin y tế, dữ liệu ngựa của Owner khác hay secrets.
- Luồng liên quan chạy được với dữ liệu demo; reviewer đã kiểm tra.
- PR merge vào main, issue đóng và tài liệu vận hành được cập nhật.

## Nhịp làm việc

Đầu tuần: chọn việc theo năng lực và dependency. Mỗi ngày: cập nhật đã làm, việc tiếp theo và blocker ngay trong issue. Cuối tuần: demo luồng hoàn chỉnh và rà lại backlog. Lỗi chặn demo được ưu tiên trước chức năng mới.
