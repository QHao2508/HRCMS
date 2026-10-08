# Bước 4 — Hợp đồng API backend

**BE-004 — 05/10/2026:** Plan detail nhận sessionPage/sessionPageSize và trả metadata sessionTotal, không còn cap âm thầm 100; Rider chỉ đọc Plan có session giao mình. History thêm start/result/skip/evaluation; outcome snapshot có shape session/result/evaluation. Xem [contract BE-004](BE-004_TRAINING.md). OpenAPI/inventory current đã xuất lại, số operations giữ nguyên.

**Cập nhật T02 — 05/10/2026:** xem [policy theo Word V1/V2 và gap code](T02_POLICY_BASELINE.md). API/OpenAPI dưới đây vẫn mô tả behavior hiện tại, không khẳng định các policy mới hoặc lựa chọn đang chờ đã được triển khai.

**BE-003 — 05/10/2026:** POST/PUT registrations dùng RegistrationDraftRequest với field optional/nullable. Owner PUT thay thế Draft; Submit/Approval yêu cầu intake đầy đủ + HorsePhoto/Certificate. Manager chỉ sửa Name/RegistrationNumber/BoardingStart/BoardingEnd của PendingReview, null/omitted giữ field hiện tại; có before/after audit. Xem [contract và migration BE-003](BE-003_INTAKE_AND_ASSIGNMENT.md). Snapshot OpenAPI/inventory current đã được xuất lại; không đổi số operations.

Ngày hoàn thiện: 04/10/2026. Frontend được hoãn theo yêu cầu của người dùng; tài liệu này mô tả backend hiện có, chưa phải nghiệm thu giao diện.

## Kết quả

- 73 paths, 96 operations; mọi success response có kiểu cụ thể hoặc 204 không body.
- 139 schemas, không còn response schema rỗng; đủ 87 operations yêu cầu Bearer token.
- Cả hai upload mô tả multipart, trường bắt buộc, file binary và mã 201; các download mô tả PNG/JPEG/PDF theo chức năng.
- Có operationId duy nhất để hỗ trợ công cụ sinh client. Các endpoint khai báo đúng 200/201/204, không giữ 200 rỗng do `Task<object>`.
- Có DTO có tên cho account, paging, error, các projection/summary/detail và report trong `HorseClub.BLL/Contracts/`. Service trả kiểu cụ thể hoặc `OperationResult`, API map sang HTTP; JSON giữ nguyên tên camelCase và shape hiện có.
- Follow-up yêu cầu `examination` khác null; validation kiểm DTO lồng nhau trước khi gọi workflow, trả 400 khi thiếu/sai dữ liệu.

Snapshot có thể import vào Postman hoặc công cụ sinh client: [openapi.current.json](contracts/openapi.current.json). Inventory đầy đủ: [api-inventory.current.csv](contracts/api-inventory.current.csv). Kết quả audit: [openapi-audit.current.json](contracts/openapi-audit.current.json). Baseline bước 2 được giữ riêng để đối chiếu; không dùng baseline làm contract hiện tại.

## Cách frontend gọi API

API local theo launch profile: `http://localhost:5299`. OpenAPI nằm tại `/openapi/v1.json` trong Development. Cấu hình `Cors:Origins` bằng origin frontend thực tế.

1. Gọi `/api/auth/login` bằng email hoặc username và password của tài khoản active, đã verify hoặc đã accept invitation. Field JSON vẫn là `email`, nhận cả username để giữ tương thích. Đăng ký/reset/invitation đều dùng OTP 6 chữ số; xem [hướng dẫn test](EMAIL_OTP_TEST_GUIDE.md).
2. Lưu `accessToken`, `refreshToken` theo chính sách của ứng dụng. Gửi `Authorization: Bearer <accessToken>` cho các operation có security Bearer.
3. Token là opaque ASP.NET Core Identity token; không decode như JWT. `expiresIn` là số giây, không phải timestamp.
4. Lấy profile bằng `/api/auth/me`, enum bằng `/api/metadata/enums`. Enum request dùng đúng tên chuỗi, không dùng số hoặc tên tự dịch.
5. Role chỉ là một điều kiện. Backend còn kiểm OwnerId, assignment active, RiderId, trạng thái ngựa/session và quyền xem clinical. Xem [bản đồ quyền theo hành động](archive/CONTRACT_AND_SCREEN_MAP.md).

List phân trang trả `{ items, page, pageSize, total }`; page bắt đầu từ 1, giới hạn pageSize theo `Business:MaxPageSize`. Không phải mọi list đều phân trang: attachments/photos trả mảng trực tiếp. DateOnly dùng `yyyy-MM-dd`; DateTimeOffset dùng ISO 8601 có offset. Trường nullable có thể trả null, ví dụ latestMeasurement/currentStall, result/evaluation và clinical bị giới hạn theo role.

Create không luôn là 201: giữ đúng status được khai báo cho từng operation. Với 204, không parse JSON. Verify/reset/accept invitation có thể trả 200 với `verified: false` hoặc `changed: false`; frontend phải kiểm cả giá trị đó.

## Xử lý lỗi

Lỗi qua ErrorMiddleware giữ cấu trúc:

```json
{
  "type": "urn:horseclub:error:validation_error",
  "title": "validation_error",
  "status": 400,
  "detail": "Thông báo lỗi",
  "referenceId": null,
  "traceId": "mã theo dõi request"
}
```

| HTTP | Cách xử lý |
| --- | --- |
| 400 | Kiểm body/query, trường bắt buộc, enum, ngày, giới hạn và detail. Unknown JSON member cũng bị từ chối. |
| 401 | Thiếu/hết hạn/bị thu hồi token; kiểm refresh và đưa về login nếu không còn hợp lệ. Login sai trả `{error: "invalid_credentials"}`; refresh sai và authentication middleware có thể không có body. |
| 403 | Không đủ role hoặc không thuộc scope ngựa/session/incident; không xử lý bằng cách chỉ ẩn nút UI. |
| 404 | Record/file không tồn tại; route không khớp có thể trả body rỗng. |
| 409 | State/data/concurrency conflict. Tải lại dữ liệu, đọc title/detail rồi quyết định gửi lại; không tự replay mọi POST. |
| 413 | Thu hẹp report; upload giảm kích thước theo cấu hình. Lỗi giới hạn từ server có thể không có JSON. |
| 429 | Rate limit; chờ trước khi thử lại. Body có thể rỗng. |
| 500 | Ghi traceId để tra log; không hiển thị stack trace cho người dùng. |

Frontend cần kiểm content type/body trước khi parse lỗi. Mã lỗi được khai báo là nhóm lỗi có thể gặp ở tầng API; không có nghĩa mọi operation phát sinh tất cả mã đó trong luồng thành công.

## Upload

- `POST /api/registrations/{registrationId}/attachments`: multipart `file`, `type`; tùy chọn `certificateNumber`, `issueDate`, `expiryDate`. Tên AttachmentType phân biệt hoa/thường; IncidentPhoto không hợp lệ ở endpoint này. HorsePhoto chỉ PNG/JPEG; các tài liệu khác cho phép PNG/JPEG/PDF.
- `POST /api/care/incidents/{incidentId}/photos`: multipart `file`, PNG/JPEG; chỉ reporter được upload khi incident chưa resolved.
- Không tự đặt header Content-Type khi gửi FormData trong browser; browser cần tạo boundary.
- Giới hạn kích thước/số file/rate limit lấy từ Storage options. Đuôi file phải khớp signature nội dung.

## Kiểm chứng và tái xuất

ApiContractTests kiểm operation/schema/security/status/multipart, shape runtime account/page/error/login và request sai trên SQL Server. Kết quả hiện tại xem [FRONTEND_READINESS.md](archive/FRONTEND_READINESS.md).

PowerShell tại repository root:

```powershell
$env:HRCMS_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:HRCMS_EXPORT_OPENAPI = Join-Path $PWD 'docs/contracts/openapi.current.json'
dotnet test HorseClub.slnx -c Release --filter FullyQualifiedName~ApiContractTests
Remove-Item Env:HRCMS_EXPORT_OPENAPI
Remove-Item Env:HRCMS_TEST_SQLSERVER
./docs/Export-ContractInventory.ps1 -InputPath docs/contracts/openapi.current.json -OutputPath docs/contracts/api-inventory.current.csv -SummaryPath docs/contracts/openapi-audit.current.json
```

Test dùng fixture/database riêng, không cần chạy API thủ công. Export là tùy chọn; CI không sửa snapshot trong checkout. Workflow test không thay thế load testing, worker nhiều replica hoặc UI E2E.

Các response đang trả entity nghiệp vụ tiếp tục giữ các trường hiện có, bao gồm Version/CreatedAt nếu entity có. Bước này bổ sung kiểu/schema và DTO cho projection, không tách toàn bộ contract khỏi DAL hoặc thay chính sách clinical/clearance/archive. Nếu đổi trường entity về sau, cần xem ảnh hưởng API và version contract. Các policy đề xuất vẫn nằm ở [POLICY_DECISIONS.md](archive/POLICY_DECISIONS.md).

Cơ chế transformer dựa trên [OpenAPI customization của ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/customize-openapi?view=aspnetcore-10.0).
