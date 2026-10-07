# Bước 2 — Bản đồ chức năng, màn hình và API hiện tại

**Cập nhật nguồn Word — 05/10/2026:** đã đọc Racehorse_Frontend_Figma_Functional_Spec_Merged_V1_V2.docx để xử lý T02; kết luận nằm ở [T02_POLICY_BASELINE.md](T02_POLICY_BASELINE.md). Chưa có đối chiếu Figma/frontend đầy đủ; baseline dưới đây phản ánh ngày 04/10, không phải contract đã sửa theo toàn bộ policy mới.

Ngày: 04/10/2026. Trạng thái: baseline từ code và OpenAPI; chưa nghiệm thu khớp Figma/Word/frontend. Không thay đổi API hoặc nghiệp vụ ở bước này.

**Quyết định phạm vi của người dùng:** hoãn đối chiếu frontend vì bộ phận frontend chưa hoàn thành. C08 được hoãn tới giai đoạn tích hợp, không chặn công việc backend tiếp theo. Giữ behavior nghiệp vụ hiện có làm baseline kiểm thử; các thay đổi trong POLICY_DECISIONS vẫn là đề xuất riêng, chưa được chấp thuận bằng quyết định hoãn frontend.

**Cập nhật bước 4:** backend đã bổ sung typed response, DTO projection, status/error/security/multipart cho 96 operations. Những nhận xét thiếu OpenAPI dưới đây thuộc baseline bước 2. Dùng [contract hiện tại và hướng dẫn tích hợp](API_CONTRACT.md) cùng [OpenAPI hiện tại](contracts/openapi.current.json) khi xây frontend.

## Nguồn đã kiểm tra và giới hạn

- Backend checkout hiện tại, các Endpoint/Workflow/Service/Request DTO và cấu hình startup.
- OpenAPI lấy từ backend Development chạy tại cổng 5301, dùng SQL Server database test; snapshot tại [openapi.baseline.json](contracts/openapi.baseline.json).
- Frontend repository `https://github.com/QHao2508/HRCMS-Frontend`, HEAD `cdcc389b0b8be5b307e9492e63284d2178a1ce61`: checkout chỉ có README và .gitignore, không có package.json/src/app. Bản chỉ đọc nằm trong TestResults/step2/frontend-reference, bị Git ignore; không sửa/push frontend.
- Link Figma từ README chưa đọc được bằng công cụ web. Không có file Word nguồn trong checkout. Vì thế các tên màn hình bên dưới là nhóm chức năng suy ra từ API, không phải tên/frame Figma đã xác nhận.

Không diễn giải tình trạng branch HEAD frontend thành bằng chứng rằng nhóm chưa có code trên máy hoặc branch khác. Cần cung cấp đường dẫn/branch thực tế để hoàn tất đối chiếu T01.

## Quy ước dùng bảng

Mọi route nghiệp vụ dưới `/api`; bảng viết route tương đối cho ngắn. O=HorseOwner, M=ClubManager, H=HeadTrainer, T=Trainer, R=WorkRider, V=Veterinarian, G=Groom. HS=Horse scope: Owner đúng OwnerId, Manager, staff có assignment Active, Rider có session được giao. Route training loại Groom bằng filter dù Groom có HS. Session của Rider còn phải đúng RiderId.

Tên DTO là request trong `HorseClub.BLL/Contracts/`. Tên entity/shape response lấy từ source, chưa phải response DTO có schema đầy đủ trong OpenAPI. `Page<T>` viết tắt `{items,page,pageSize,total}`. HTTP success trong bảng lấy từ code; không có nghĩa mọi nhánh đã được probe runtime.

## Màn hình/hành động → API → quyền và contract

| Nhóm màn hình/hành động | Method và route | Quyền/phạm vi hiện tại | Request → success response theo code | Chỗ cần đối chiếu/hoàn thiện |
| --- | --- | --- | --- | --- |
| Owner đăng ký | POST `/auth/register` | Public; role luôn Owner | RegisterRequest → 201 AccountView | UI không gửi role; NationalId theo config; không nhận profile ngựa ở đây |
| Xác thực/resend | POST `/auth/verify-email`, `/auth/resend-verification` | Public; account Owner eligible | VerifyRequest → 200 `{verified}`; EmailRequest → 200 `{message}` | Sai/hết hạn trả verified=false; HTTP 200 không có nghĩa verify thành công |
| Đăng nhập/refresh | POST `/auth/login`, `/auth/refresh` | Public; kiểm account/token | LoginRequest/RefreshRequest → tokenType/accessToken/expiresIn/refreshToken | Opaque token; không decode JWT; có 401, refresh chưa rotate từng device |
| Forgot/reset/invitation | POST `/auth/forgot-password`, `/auth/reset-password`, `/auth/accept-invitation` | Public; challenge đúng purpose | EmailRequest → `{message}`; ResetRequest → `{changed}` | changed=false phải hiện lỗi; thiếu luồng resend invitation/profile/change-password nếu màn hình yêu cầu |
| Account/logout | GET `/auth/me`; POST `/auth/logout` | Account hiện tại | AccountView 200; logout 204 | Logout thu hồi toàn account; chưa có PUT profile |
| Quản lý staff | GET/POST `/staff`; PUT `/staff/{id}/active` | M; create không cho O/M, active chỉ staff | Page<UserSummary>; StaffRequest → 201 AccountView; ActiveRequest → 204 | Chưa có edit role/details; disable đổi security stamp |
| Chọn staff trong form | GET `/staff/directory` | Đăng nhập, mọi role | Query role/page/pageSize → Page<{id,firstName,lastName,role}> | Không chứa email/contact; metadata enums cũng cần đăng nhập |
| Hồ sơ intake list/detail | GET `/registrations`, `/{id}` | O của hồ sơ hoặc M | Query status/page/pageSize → Page<Registration>; detail Registration | Chưa có sort/search tùy ý; không dùng `/horses` cho hồ sơ pending |
| Tạo/lưu/sửa draft | POST `/registrations`; PUT `/{id}` | O tạo; O/M sửa trong state được phép | RegistrationRequest → 201/200 Registration | Draft vẫn phải đủ required fields; Manager có thể sửa PendingReview; chờ P04 |
| Submit/review/cancel | POST `/registrations/{id}/submit`, `/review`, `/cancel` | O submit/cancel; M review | Submit 204; ReviewRequest → 200 `{registration,horseId}`; cancel 204 | approve=false là RevisionRequired; không có Rejected; cancel chỉ Draft/RevisionRequired |
| Tài liệu intake | GET/POST `/registrations/{registrationId}/attachments`; GET `/{id}` | O của registration/M; upload theo state | Multipart file/type/certificateNumber/issueDate/expiryDate; 201 metadata; GET file | PNG/JPEG/PDF, max 10 MiB theo mặc định; chưa DELETE/replace; OpenAPI thiếu multipart |
| Danh sách/profile ngựa | GET `/horses`, `/{id}`, `/{horseId}/photo` | HS, chưa archive | search/healthStatus/page/pageSize; `{horse,latestMeasurement,assignments,currentStall,preferences}`; file | IDs staff/stall cần map tên; profile không trả clinical; ảnh cần Bearer dù dùng img |
| Phân công nhân sự | POST `/horses/{id}/assignments` | M giao H/V/G; H đang phụ trách giao T | AssignmentRequest → 200 StaffAssignment | Owner preferences không tự trở thành assignment; chưa future assignment/unassign |
| Measurements/archive | GET/POST `/horses/{id}/measurements`; POST `/{id}/archive` | HS đọc; M/V/G ghi; M archive | Page<Measurement>; MeasurementRequest → 200; archive 204 | Archive đóng occupancy, chưa đóng mọi pending work/assignment; chờ P06 |
| Templates | GET/POST `/training/templates`; PUT `/{id}`; POST `/{id}/archive` | M/H/T đọc; H ghi/archive | Page<Template>; TemplateRequest → 201/200 Template; archive 204 | Template không tự sinh session hoặc copy tất cả defaults sang plan |
| Plans/list/detail/edit | GET/POST `/training/plans`; GET/PUT `/{id}` | HS training đọc; T hiện tại ghi | PlanRequest → 201/200 Plan; detail `{plan,sessions,restrictions}` | Detail cắt 100 sessions; Rider chỉ session mình nhưng đọc plan; chưa detail riêng template |
| Plan status/history | PUT `/training/plans/{id}/status`; GET `/{id}/history` | T hiện tại đổi; M/O/H/T/V trong HS đọc history | PlanStatusRequest → 200 Plan; Page<TrainingRevision> | Completed/Archived không reopen; history chưa bao mọi transition |
| Schedule/edit/assign | POST `/training/plans/{id}/sessions`; PUT `/training/sessions/{id}`; POST `/{id}/assign` | T hiện tại | SessionRequest → 201/200 Session; RiderRequest → 200 | riderId=null thành Planned; guard y tế; trùng giờ Rider chưa phải overlap; chờ P05 |
| Session list/detail | GET `/training/sessions`, `/{id}` | HS training; R chỉ session mình | horseId/status/from/to/page/pageSize; `{session,result,evaluation}` | Không cho G; Rider không thấy session khác; status/enum phải đúng tên |
| Rider start/result/skip | POST `/training/sessions/{id}/start`, `/results`, `/skip` | R đúng người; T cũng skip được | Start 204; ResultRequest → 200 Result; ReasonRequest → skip 204 | Guard recheck lúc start; result vẫn nhận nếu lock giữa buổi; duplicate 409 |
| Trainer evaluation | POST `/training/sessions/{id}/evaluation` | T hiện tại | EvaluationRequest → 200 Evaluation | AdjustFutureSessions chỉ lưu cờ; UI không được báo lịch đã tự điều chỉnh |
| Health summary/restrictions | GET `/horses/{horseId}/medical/summary`, `/restrictions` | HS | `{healthStatus,restrictions}`; Page<Restriction> | summary chỉ active tại now; restriction list có history; reason là operational, không diagnosis |
| Khám/correction | GET/POST `/horses/{horseId}/medical/records`; PUT `/records/{id}` | V được giao ngựa | Page<Record>; MedicalRequest → 201 record, correction 200 | Correction thêm bản SupersedesRecordId; chốt cách health state theo thời gian ở P03 |
| Injury/treatment | GET/POST `/horses/{horseId}/medical/injuries`, `/treatments` | V được giao ngựa | InjuryRequest/TreatmentRequest → 200; Page<Entity> | Chưa API lifecycle riêng Recovering/Recovered/complete treatment |
| Follow-up/clearance | GET/POST `/horses/{horseId}/medical/follow-ups` | V được giao ngựa | FollowUpRequest (nested Examination) → 200 FollowUp; Page<FollowUp> | Clearance hiện đóng trên toàn horse; không resume plan; chờ P02 |
| Preventive care | GET/POST `/horses/{horseId}/medical/preventive-care`; POST `/{id}/complete` | HS đọc projected fields; V ghi | PreventiveRequest → 200; PreventiveCompletionRequest → 204 | Notes không trả trong list; chưa recurring schedule; reminder khi chưa có Vet cần fix |
| Care task list/assign/record | GET/POST `/care/tasks`; POST `/tasks/{id}/record` | O/M/V/G đọc; G chỉ task mình; M daily care, V Treatment/IceBath; G ghi task mình | Page<CareProjection>; CareRequest/CareCompletionRequest → 200 CareTask | Feeding approved/actual; privacy Treatment khác IceBath; chờ P07 |
| Incidents/photos/resolve | GET/POST `/care/incidents`; POST `/{id}/resolve`; GET/POST `/{incidentId}/photos`; GET `/{id}` | M/V/T/G/R list theo route/reporter; V/T/G/R tạo; routed role resolve; reporter upload | IncidentRequest → 200 Incident; resolve 204; multipart → 201 Photo | Chưa detail Incident riêng, kết luận xử lý; không O/H; list/photo phải cùng scope |
| Stables/stalls/occupancy | GET/POST `/care/stables`, `/care/stalls`; POST `/stalls/{id}/occupancy`, `/vacate`, `/cleaned` | M/G list; M tạo/occupy/vacate; M/G scoped cleaning | Page entities; Name/Stall/OccupancyRequest → 200; vacate/cleaned 204 | List stalls chưa trả current occupant; cleaning lifecycle/history cần đối chiếu |
| Inventory/movements | GET/POST `/inventory`; GET/POST `/{id}/movements`; POST `/{id}/archive` | M/G đọc/xuất; M tạo/nhập/archive | Page<Item/Movement>; InventoryRequest → 200; MovementRequest → `{item,movement}` | Quantity signed, không âm; archive stock=0; chưa edit item; care không tự trừ kho |
| Replenishments | GET/POST `/inventory/replenishments`; POST `/{id}/review` | M/G; G đọc request mình; M review | ReplenishmentRequestDto/ReviewRequest → 200 Request | Approval chưa tăng kho; receipt movement riêng, chưa link request |
| Notifications/audit | GET `/notifications`; POST `/{id}/read`; GET `/audit` | Notifications của account; audit M | Page<Notification/Audit>; read 204 | Filter unread/referenceId; chưa bulk read; link reference không luôn là id session |
| Dashboard/report | GET `/dashboard`, `/reports` | HS với các projection theo role | KPI; `{from,to,groupBy,noData,kpi,series,training,care,clinical}` | Dashboard shape chung; report clinical cắt 100; overdue khác worker; chưa export |
| Enum metadata/health | GET `/metadata/enums`; GET `/health` | Metadata account verified; health public | enum names map; health `{status}`/503 | Không dùng metadata auth trước login; health phản ánh DB connect, không SMTP |

## Quy ước contract frontend phải tuân thủ

| Nội dung | Hành vi hiện tại |
| --- | --- |
| Auth header | `Authorization: Bearer <accessToken>`; không đưa token vào query URL |
| Account response | id/email/userName/firstName/lastName/phone/address/role/emailVerified/active; không NationalId/hash/stamp |
| Pagination | page/pageSize dương; default 1/20, max pageSize=100 theo cấu hình; items và total |
| Enum | JSON tên enum, không ID số; field JSON không biết bị từ chối; case dùng đúng metadata |
| Date/time | DateOnly `yyyy-MM-dd`; timestamp ISO 8601 có offset; server lưu UTC, ngày Club theo Asia/Ho_Chi_Minh |
| Đơn vị | distance=m; time=s; speed=m/s; height=cm; weight/portion=kg; inventory unit theo item |
| UUID | id entity là GUID; không thay bằng tên hiển thị |
| Medical block | 409 kèm title/detail/referenceId/traceId; FE không retry tự động để vượt guard |
| File download | File bytes từ endpoint protected; ảnh render bằng fetch có header rồi Blob URL; thu hồi Blob URL khi bỏ view |
| Error | Exception nghiệp vụ có problem-like shape, nhưng 401 auth middleware/refresh có thể body rỗng; login sai có `{error}` |
| Verify/reset | Phải kiểm `verified`/`changed`, không chỉ response.ok |
| Retry | 409 reload hoặc xử lý state; 429 chờ; không tự nhân đôi POST tạo hồ sơ/result/movement |

### Hai lệch metadata request cần xác minh

Snapshot OpenAPI đánh dấu nhiều nullable positional record fields là required, ví dụ ReviewRequest.reason, RegistrationRequest.preferred* và SessionRequest.riderId. Tests hiện có gửi `{approve:true}` không có reason vẫn pass. Cần probe các field omit/null thực tế và chuẩn hóa schema theo behavior; không đồng nhất "required property" với "non-null" hoặc "required theo state".

OpenAPI mô tả decimal nhận number hoặc string. FE nên gửi JSON number và thống nhất hiển thị/rounding; DB scale=3. Không tự dùng locale string `1,5` cho số.

## Kết quả kiểm OpenAPI thực tế

| Chỉ số | Giá trị |
| --- | --- |
| Paths / operations / schemas | 73 / 96 / 53 (operations gồm health) |
| Operations chỉ khai báo 200 | 96 |
| Operations có response schema rỗng | 94 |
| Operations không khai báo response content | 2 (health, reports) |
| Security schemes / operations có security requirement | 0 / 0 |
| Upload POST thiếu multipart requestBody | 2 |

Lệch cụ thể: register/plan/session/create staff thực tế trả 201; logout/start/submit/archive/read thường 204 nhưng OpenAPI chỉ ghi 200. Photo/download đang bị mô tả application/json thay vì binary file. Đây là thiếu metadata contract, không phải bằng chứng endpoint runtime trả sai.

Đã probe GET `/health`=200, `/api/horses`=401 và `/api/auth/me`=401 khi không token; route protected vẫn được bảo vệ runtime dù OpenAPI chưa mô tả auth. Không tạo user/hồ sơ hay dữ liệu nghiệp vụ trong bước này.

Artifacts kiểm chứng:

- [API inventory CSV — toàn bộ 96 thao tác](contracts/api-inventory.csv): method/path/module/query/request schema/response/security theo snapshot.
- [Audit metrics](contracts/openapi-audit.json) và [anonymous probes](contracts/anonymous-probes.json).
- Chạy lại inventory: `pwsh -File docs/Export-ContractInventory.ps1`. Script dùng file snapshot, không tự khởi động hay sửa backend/database.

## Công việc thực hiện tiếp từ kết quả bước 2

| ID | Hạng mục | Tiêu chí hoàn thành |
| --- | --- | --- |
| C01 | Response DTO/typed response metadata | Mỗi operation có schema đúng success shape, nullability và content type; không trả nguyên entity chứa dữ liệu riêng |
| C02 | HTTP status/error contract | Khai báo 201/204/400/401/403/404/409/413/429 theo hành vi từng endpoint; không gắn tất cả status lên mọi route |
| C03 | Bearer metadata | Có security scheme và requirements chỉ cho protected routes; public auth/health không yêu cầu token |
| C04 | Upload/download metadata | Multipart file/type/certificate dates/limits, binary response; requests import Postman dùng được |
| C05 | Request required/nullability | Omit/null/enum/date/range tests khớp OpenAPI và state-specific validation |
| C06 | Lists/detail/report completeness | Pagination/total cho các phần cắt 100; sort/filter chỉ thêm nếu màn hình thật cần; error không âm thầm cắt |
| C07 | Screen-specific missing data/actions | Xác nhận current stall/name lookup/profile edit/resend invite/draft lifecycle trước khi thêm API |
| C08 | Đối chiếu Figma/Word/frontend — hoãn theo người dùng | Có screen/frame ID thật, field/action match và người nghiệm thu khi nguồn/FE sẵn sàng; không chặn backend |

Thứ tự đề xuất: C01–C05 trước khi sinh frontend client; C06/C07 theo nhu cầu thật; C08 hoãn theo quyết định người dùng. Quyết định domain ở [POLICY_DECISIONS.md](POLICY_DECISIONS.md) vẫn là đề xuất, chưa được phê duyệt bằng việc tạo tài liệu này. Có thể tiếp tục workflow/concurrency tests SQL Server theo baseline ngay.
