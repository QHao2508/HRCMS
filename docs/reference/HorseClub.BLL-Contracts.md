# Giải thích function: HorseClub.BLL-Contracts

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Contracts/Auth/LoginErrorResponse.cs

Request/response DTO của module Auth; kiểm đầu vào bằng annotations và định dạng JSON ổn định cho frontend.

### LoginErrorResponse.From(status, email)

**Kết quả:** `LoginErrorResponse`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển enum LoginStatus thành mã lỗi API cố định, chỉ kèm email khi mật khẩu đúng và cần xác thực.

| Đầu vào | Ý nghĩa |
|---|---|
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `email` | Email đầu vào hoặc email chuẩn của tài khoản; không dùng để suy luận tài khoản tồn tại từ phản hồi chung. |
