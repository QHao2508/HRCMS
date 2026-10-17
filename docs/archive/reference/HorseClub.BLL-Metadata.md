> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Metadata

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Metadata/MetadataService.cs

Cung cấp danh sách enum hợp lệ cho hợp đồng frontend/backend.

### MetadataService.GetEnums()

**Kết quả:** `Task<Dictionary<string, string[]>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Trả các tên enum hợp lệ cho client; giá trị API tách khỏi nhãn hiển thị tiếng Việt.

Lời gọi chính: `current.Get`, `Assembly.GetTypes`, `Enum.GetNames`.
