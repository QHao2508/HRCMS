# Giải thích function: HorseClub.BLL-DependencyInjection.cs

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/DependencyInjection.cs

Layer nghiệp vụ: phối hợp quy tắc, phân quyền theo dữ liệu, DAL và các dịch vụ ngoài; không trả IResult/HttpRequest.

### DependencyInjection.AddClubBusiness(services)

**Kết quả:** `IServiceCollection`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đăng ký service nghiệp vụ, clock, storage và background worker với vòng đời DI phù hợp để API sử dụng.

| Đầu vào | Ý nghĩa |
|---|---|
| `services` | Giá trị kiểu IServiceCollection dùng trong AddClubBusiness. |

Lời gọi chính: `services.AddSingleton`.
