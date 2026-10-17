> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Attachments

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Attachments/AttachmentService.cs

Kiểm tệp intake, metadata và quyền upload/download ảnh/chứng nhận; đọc ảnh ngựa mới nhất.

### AttachmentService.GetHorsePhoto(horseId)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm quyền xem ngựa, chọn ảnh HorsePhoto mới nhất từ hồ sơ đăng ký rồi stream nội dung storage; không công khai blob riêng.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `Ensure.Found`, `Attachments.Where`, `OperationResult.File`, `storage.OpenRead`.

### AttachmentService.ListAttachments(registrationId)

**Kết quả:** `Task<List<AttachmentResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Trả metadata tệp của hồ sơ theo thời gian, không trả khóa Azure hoặc nội dung file.

| Đầu vào | Ý nghĩa |
|---|---|
| `registrationId` | ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Registration`, `Attachments.Where`.

### AttachmentService.UploadAttachment(registrationId, request)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm quyền/trạng thái hồ sơ, giới hạn tệp, chữ ký PNG/JPEG/PDF và metadata; upload storage rồi lưu Attachment/audit. Transaction middleware dọn blob nếu ghi DB thất bại.

| Đầu vào | Ý nghĩa |
|---|---|
| `registrationId` | ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký. |
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Có ghi blob/file; tài nguyên chưa commit được cơ chế upload đối soát/dọn.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Registration`, `current.Get`, `Ensure.That`, `Messages.Get`, `request.ReadFormAsync`, `Files.GetFile`, `Attachments.CountAsync`, `Enum.IsDefined`, `int.TryParse`, `file.OpenReadStream`, `stream.ReadExactlyAsync`, `contentType.StartsWith`, `Path.GetExtension`, `Path.GetFileName`, `Guid.NewGuid`, `storage.Write`.

### AttachmentService.DownloadAttachment(registrationId, id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm quyền hồ sơ và attachment thuộc hồ sơ đó rồi stream file với tên/content type đã lưu.

| Đầu vào | Ý nghĩa |
|---|---|
| `registrationId` | ID hồ sơ intake để liên kết và kiểm quyền attachment/thông tin đăng ký. |
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Registration`, `Ensure.Found`, `Attachments.SingleOrDefaultAsync`, `OperationResult.File`, `storage.OpenRead`.

### AttachmentService.ParseDate(value)

**Kết quả:** `DateOnly?`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc ngày metadata theo yyyy-MM-dd, cho phép rỗng và từ chối ngày sai định dạng.

| Đầu vào | Ý nghĩa |
|---|---|
| `value` | Giá trị kiểu string dùng trong ParseDate. |

Lời gọi chính: `string.IsNullOrWhiteSpace`, `Ensure.That`, `DateOnly.TryParseExact`, `Messages.Get`.

### AttachmentService.Detect(b)

**Kết quả:** `string?`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Nhận diện PNG/JPEG/PDF bằng chữ ký bytes; không tin tên file hoặc MIME client gửi.

| Đầu vào | Ý nghĩa |
|---|---|
| `b` | Giá trị kiểu byte[] dùng trong Detect. |

Lời gọi chính: `b.AsSpan`, `ASCII.GetString`.
