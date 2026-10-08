> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Messages

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Messages/Messages.cs

MessageKey và JSON embedded: thông báo nghiệp vụ/email, định dạng tham số tập trung.

### Messages.Get(key, arguments)

**Kết quả:** `string`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tra template theo MessageKey và định dạng tham số với culture thống nhất; nội dung nghiệp vụ không hardcoded trong service.

| Đầu vào | Ý nghĩa |
|---|---|
| `key` | Khóa thông điệp enum trong catalog, không phải nội dung hiển thị. |
| `arguments` | Giá trị kiểu object?[] dùng trong Get. |

Lời gọi chính: `key.ToString`, `string.Format`.

### Messages.Load()

**Kết quả:** `IReadOnlyDictionary<string, string>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc catalog JSON embedded của BLL, kiểm đủ key và giữ dữ liệu dùng lại cho các request.

Lời gọi chính: `Assembly.GetManifestResourceStream`.
