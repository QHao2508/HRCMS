# Giải thích function: Horse_BackEnd-Infrastructure

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## Horse_BackEnd/Infrastructure/AllowedRolesFilter.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### AllowedRolesFilter.InvokeAsync(context, next)

**Kết quả:** `ValueTask<object?>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra vai trò cho endpoint có giới hạn role, chặn request không đủ quyền trước khi gọi handler.

| Đầu vào | Ý nghĩa |
|---|---|
| `context` | Giá trị kiểu EndpointFilterInvocationContext dùng trong InvokeAsync. |
| `next` | Giá trị kiểu EndpointFilterDelegate dùng trong InvokeAsync. |

Lời gọi chính: `Ensure.Role`.

## Horse_BackEnd/Infrastructure/ApiContract.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### ApiContractTransformer.TransformAsync(operation, context, cancellationToken)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Bổ sung schema lỗi, bearer security, mã HTTP và hợp đồng multipart/download vào OpenAPI theo metadata endpoint.

| Đầu vào | Ý nghĩa |
|---|---|
| `operation` | Giá trị kiểu OpenApiOperation dùng trong TransformAsync. |
| `context` | Giá trị kiểu OpenApiOperationTransformerContext dùng trong TransformAsync. |
| `cancellationToken` | Giá trị kiểu CancellationToken dùng trong TransformAsync. |

Lời gọi chính: `Regex.Replace`, `path.Trim`, `path.StartsWith`, `context.GetOrCreateSchemaAsync`, `Responses.TryAdd`, `status.ToString`, `SecuritySchemes.TryAdd`, `HttpMethods.IsPost`, `path.Contains`, `string.Join`, `required.Add`, `HttpMethods.IsGet`, `path.EndsWith`, `types.ToDictionary`.

### ApiContractTransformer.AddError(status, description)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Bổ sung response lỗi OpenAPI khi mã HTTP chưa có để không ghi đè schema endpoint đã khai báo.

| Đầu vào | Ý nghĩa |
|---|---|
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `description` | Giá trị kiểu string dùng trong AddError. |

Lời gọi chính: `Responses.TryAdd`, `status.ToString`.

## Horse_BackEnd/Infrastructure/BearerSessionHandler.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### BearerSessionHandler.Login(request, service, context)

**Kết quả:** `Task<IResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển LoginAttempt thành lỗi HTTP có mã để frontend điều hướng, hoặc cấp bearer token chỉ cho tài khoản đã xác thực. Cho phép transaction lưu bộ đếm đăng nhập dù trả 401.

| Đầu vào | Ý nghĩa |
|---|---|
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |
| `service` | Giá trị kiểu AuthenticationService dùng trong Login. |
| `context` | Giá trị kiểu HttpContext dùng trong Login. |

Lời gọi chính: `service.Login`, `Results.SignIn`, `AuthenticationService.Principal`, `Results.Json`, `LoginErrorResponse.From`.

### BearerSessionHandler.Refresh(request, options, service, clock)

**Kết quả:** `Task<IResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Giải mã và kiểm hạn refresh token, xác thực user/stamp rồi cấp phiên mới; trả Unauthorized khi token hoặc tài khoản không hợp lệ.

| Đầu vào | Ý nghĩa |
|---|---|
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |
| `options` | Cấu hình/hợp đồng tùy hàm; các giá trị được truyền rõ ràng từ caller. |
| `service` | Giá trị kiểu AuthenticationService dùng trong Refresh. |
| `clock` | Giá trị kiểu TimeProvider dùng trong Refresh. |

Lời gọi chính: `options.Get`, `RefreshTokenProtector.Unprotect`, `clock.GetUtcNow`, `Guid.TryParse`, `Principal.FindFirstValue`, `Results.Unauthorized`, `service.ValidateSession`, `Results.SignIn`, `AuthenticationService.Principal`.

## Horse_BackEnd/Infrastructure/ErrorMiddleware.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### ErrorMiddleware.InvokeAsync(context)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Chuyển lỗi nghiệp vụ, validation, trùng dữ liệu và lỗi hệ thống thành phản hồi thống nhất; giữ chi tiết nội bộ khỏi response client.

| Đầu vào | Ý nghĩa |
|---|---|
| `context` | Giá trị kiểu HttpContext dùng trong InvokeAsync. |

Lời gọi chính: `DatabaseFailures.IsDeadlock`, `Messages.Get`, `logger.LogError`, `Response.Clear`, `Response.WriteAsJsonAsync`.

## Horse_BackEnd/Infrastructure/KeyProtection.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### KeyProtection.AddClubKeyProtection(builder)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Đăng ký kho Data Protection key và cơ chế mã hóa theo môi trường; giữ key ổn định giữa các lần chạy để token còn giải mã được.

| Đầu vào | Ý nghĩa |
|---|---|
| `builder` | Giá trị kiểu WebApplicationBuilder dùng trong AddClubKeyProtection. |

Lời gọi chính: `Services.AddDataProtection`, `OperatingSystem.IsWindows`.

### ConfiguredCertificateEncryptor.Encrypt(plaintextElement)

**Kết quả:** `EncryptedXmlInfo`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Mã hóa XML Data Protection bằng certificate đã cấu hình và chỉ định decryptor dùng khi khôi phục key.

| Đầu vào | Ý nghĩa |
|---|---|
| `plaintextElement` | Giá trị kiểu XElement dùng trong Encrypt. |

### ConfiguredCertificateDecryptor.Decrypt(encryptedElement)

**Kết quả:** `XElement`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Giải mã XML key bằng certificate gốc từ DI; không thể khôi phục token nếu thiếu private key tương ứng.

| Đầu vào | Ý nghĩa |
|---|---|
| `encryptedElement` | Giá trị kiểu XElement dùng trong Decrypt. |

Lời gọi chính: `decryptionServices.AddDataProtection`, `decryptionServices.BuildServiceProvider`.

### KeyMaterial.KeyMaterial(config, environment)

**Kết quả:** Khởi tạo instance với dependency/configuration đã truyền.

Đọc cấu hình đường dẫn/key encryption, kiểm môi trường và certificate private key; tạo kho key cần được giữ khi dọn build.

| Đầu vào | Ý nghĩa |
|---|---|
| `config` | Giá trị kiểu IConfiguration dùng trong KeyMaterial. |
| `environment` | Giá trị kiểu IWebHostEnvironment dùng trong KeyMaterial. |

Lời gọi chính: `config.GetSection`, `Path.GetFullPath`, `Path.Combine`, `Directory.CreateDirectory`, `section.GetValue`, `environment.IsDevelopment`, `OperatingSystem.IsWindows`, `X509CertificateLoader.LoadPkcs12FromFile`, `certificate.Dispose`.

### KeyMaterial.Dispose()

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Giải phóng certificate/nguồn lực mật mã khi DI scope/container kết thúc.

## Horse_BackEnd/Infrastructure/OperationResultMapper.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### OperationResultMapper.ToHttpResult(result)

**Kết quả:** `IResult`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển kết quả thuần nghiệp vụ thành JSON, Created, NoContent hoặc file stream tại layer API.

| Đầu vào | Ý nghĩa |
|---|---|
| `result` | Giá trị kiểu OperationResult dùng trong ToHttpResult. |

Lời gọi chính: `Results.File`, `Results.Created`, `Results.NoContent`, `Results.Json`.

## Horse_BackEnd/Infrastructure/TransactionMiddleware.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### TransactionMiddleware.InvokeAsync(context, db, uploads)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Bao các request ghi trong transaction Serializable và buffer response; commit thành công hoặc lượt thử đăng nhập, rollback lỗi và dọn blob chưa được commit.

| Đầu vào | Ý nghĩa |
|---|---|
| `context` | Giá trị kiểu HttpContext dùng trong InvokeAsync. |
| `db` | Giá trị kiểu ClubDbContext dùng trong InvokeAsync. |
| `uploads` | Giá trị kiểu UploadStorage dùng trong InvokeAsync. |

- Hàm tự mở transaction; lock và dữ liệu cùng được giải phóng khi transaction kết thúc.

Lời gọi chính: `context.GetEndpoint`, `HttpMethods.IsGet`, `HttpMethods.IsHead`, `HttpMethods.IsOptions`, `Database.BeginTransactionAsync`, `Items.ContainsKey`, `tx.CommitAsync`, `uploads.Commit`, `tx.RollbackAsync`, `bufferedBody.CopyToAsync`, `uploads.CleanupUncommitted`.

## Horse_BackEnd/Infrastructure/UploadRequestAdapter.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### UploadRequestAdapter.Create(request)

**Kết quả:** `UploadRequest`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chuyển HttpRequest/form/files thành abstraction UploadRequest của BLL, giữ HTTP phụ thuộc ở layer API.

| Đầu vào | Ý nghĩa |
|---|---|
| `request` | Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP. |

Lời gọi chính: `request.ReadFormAsync`, `form.ToDictionary`, `Value.ToString`, `Files.Select`.

## Horse_BackEnd/Infrastructure/ValidationFilter.cs

Adapter HTTP, middleware transaction/lỗi, validation, bearer token, bảo vệ key và chuẩn hóa OpenAPI.

### ValidationFilter.InvokeAsync(context, next)

**Kết quả:** `ValueTask<object?>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm tra DataAnnotations và cấu trúc request trước khi endpoint thực hiện nghiệp vụ; trả lỗi validation khi dữ liệu không hợp lệ.

| Đầu vào | Ý nghĩa |
|---|---|
| `context` | Giá trị kiểu EndpointFilterInvocationContext dùng trong InvokeAsync. |
| `next` | Giá trị kiểu EndpointFilterDelegate dùng trong InvokeAsync. |

Lời gọi chính: `Ensure.Validate`.
