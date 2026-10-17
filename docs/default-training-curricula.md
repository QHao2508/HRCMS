# Giáo án dùng chung và giao theo chuyên môn

## Hành vi

- Mọi HeadTrainer, kể cả tài khoản mới, đều thấy 5 giáo án mặc định: thể lực nền tảng, nhịp chạy ổn định, tốc độ theo từng đoạn, cổng xuất phát, vận động nhẹ giữa các buổi nặng.
- Bản mặc định không cho sửa hoặc lưu trữ. HeadTrainer có thể tạo bản sao để tùy chỉnh hoặc tạo giáo án riêng.
- Quãng đường, thời lượng, cường độ là lựa chọn minh họa cho câu lạc bộ; cần điều chỉnh theo từng ngựa và chỉ dẫn thú y. Liên kết tham khảo không xác nhận các con số minh họa này.
- HeadTrainer giao giáo án cho Trainer đang hoạt động, đã xác minh email, đủ tất cả chuyên môn yêu cầu và đạt cấp độ tối thiểu (1–3). Trainer chỉ thấy các giáo án mới đã được giao và phù hợp năng lực hiện tại.
- Backend kiểm tra lại việc giao và năng lực khi tạo/sửa kế hoạch, buổi tập, bắt đầu buổi tập và thay Trainer phụ trách. FE không phải lớp kiểm soát duy nhất.
- Tạo kế hoạch chọn mục tiêu/giai đoạn có sẵn; tạo buổi tập chọn bài tập với thông số có sẵn. Chọn **Khác** để nhập thông số riêng; loại bài tập vẫn thuộc chuyên môn giáo án.
- Giáo án cũ chưa có cấu hình lựa chọn được giữ tương thích với kế hoạch và buổi tập cũ.

## Kiểm thử trên giao diện

1. Đăng nhập HeadTrainer, vào **Giáo án mẫu**: kiểm tra đủ 5 bản mặc định, thử **Tạo bản sao** và sửa lựa chọn của bản sao.
2. Mở **Giao giáo án**. Trainer thiếu chuyên môn/cấp độ bị đánh dấu không phù hợp; giao cho Trainer đủ điều kiện. Nếu không có Trainer phù hợp, cấu hình chuyên môn ở quản lý nhân sự trước.
3. Đăng nhập Trainer được giao. Tạo kế hoạch với giáo án đó: lựa chọn có sẵn tự điền mục tiêu/giai đoạn; **Khác** mở nhập riêng.
4. Tạo buổi tập trong khoảng ngày kế hoạch, trong ca làm việc của Trainer/Rider và ngoài lịch bận/hạn chế y tế. Bài tập có sẵn tự điền và khóa các thông số; **Khác** mở nhập riêng.
5. Thử Trainer chưa được giao hoặc thiếu chuyên môn: backend từ chối. Thu hồi giáo án hoặc giảm chuyên môn rồi thử bắt đầu buổi tập để kiểm tra lại quyền.

## API và dữ liệu

- `GET /api/training/templates/defaults`
- `GET /api/training/templates/{id}/trainer-candidates`
- `POST /api/training/templates/{id}/assign` với `trainerId`, `assigned`.
- Migration `20261013143607_DefaultTrainingCurricula` thêm bảng giao giáo án, cột cấu hình lựa chọn và 5 giáo án dùng chung; giữ dữ liệu hiện có.
- Kế hoạch dùng `templatePlanOptionKey`, `useCustomPlan`; buổi tập dùng `templateExerciseKey`, `useCustomExercise`. Chọn Khác phải gửi khóa `null` và cờ tương ứng `true`.

## Kết quả xác nhận

- 26 kiểm thử BE liên quan tới training/contract/giáo án và 68 kiểm thử FE liên quan đã đạt; FE lint và build đạt.
- Kiểm thử UI qua API thật trên database SQL Express riêng: tài khoản HeadTrainer mới thấy mặc định, giao đúng năng lực, tạo bản sao, Trainer tạo kế hoạch và buổi tập tương lai với tùy chọn Khác thành công.
- Bộ kiểm thử toàn dự án còn các kỳ vọng cũ không khớp bản merge về medical/role/menu; không coi toàn bộ suite đã đạt.
- Azure HRCMS đã áp migration: 5 giáo án mặc định, 6 giáo án cũ, 1 kế hoạch, 2 buổi tập sau migration.
