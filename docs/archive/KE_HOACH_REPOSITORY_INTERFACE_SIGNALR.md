> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../README.md).

# Kế hoạch tách Interface, Repository và tích hợp SignalR

Ngày lập: 08/10/2026. Kế hoạch đã được triển khai trong working tree backend và frontend theo xác nhận của người dùng. Xem REPOSITORY_SIGNALR_IMPLEMENTATION.md để biết kết quả, kiểm thử và giới hạn vận hành. Những bảng bên dưới lưu thiết kế dự kiến ban đầu. “Signar R” được hiểu là ASP.NET Core SignalR.

## 1. Mục tiêu và phạm vi

- API phụ thuộc service interface; BLL giữ quyết định nghiệp vụ, không trực tiếp truy vấn EF Core.
- DAL triển khai repository/query và Unit of Work; giữ SQL Server, schema nghiệp vụ, route/JSON/status hiện có.
- SignalR cập nhật thông báo và báo giao diện tải lại dữ liệu khi trạng thái đổi. Các lệnh ghi vẫn đi qua REST API và validation/phân quyền hiện tại.
- Giao dịch nghiệp vụ và sự kiện realtime cùng được lưu vào SQL; worker chỉ nhìn thấy sự kiện đã commit.
- Không chuyển sang microservices, không đổi cơ chế token thành JWT chỉ để dùng SignalR; không bắt buộc một repository cho mỗi bảng.

Tài liệu hiện tại chủ động cho phép BLL dùng DbContext. Việc đổi này là quy ước kiến trúc mới để phù hợp yêu cầu báo cáo/bảo trì, không phải bằng chứng kiến trúc ba tầng hiện tại sai. Cần cập nhật THREE_LAYER_AND_MESSAGES.md khi hoàn thành.

## 2. Căn cứ và điểm cần giữ

| Căn cứ trong code | Tác động tới kế hoạch |
|---|---|
| AddClubBusiness đăng ký service concrete | Đổi endpoint/adapter sang I…Service và đăng ký interface → implementation. |
| BLL service, ClubAccess, CurrentUser, ClubEvents, PageReader và workers dùng DbContext | Phải rà cả helpers/worker, không chỉ service module. |
| TransactionMiddleware dùng Serializable, response buffering, commit-auth-attempt và cleanup upload | Giữ hành vi; đưa transaction qua interface, loại endpoint hub khỏi transaction request. |
| Program dùng AddBearerToken/IdentityConstants.BearerScheme | Adapter xác thực SignalR phải tương thích token Data Protection hiện tại, không dùng JwtBearerEvents như thể token là JWT. |
| Dashboard gom bảy chỉ số trong một SQL command | Giữ query tổng hợp trong DAL, không biến thành bảy lần gọi repository. |
| DbContext.SaveChangesAsync tăng Version | Mọi repository/Unit of Work vẫn đi qua cơ chế này. |
| Worker hiện có SQL application lock | Realtime dispatcher phải phối hợp nhiều host và không giữ SQL transaction khi gửi mạng. |

## 3. Kiến trúc đích và trách nhiệm

```text
React → REST Endpoint → I…Service → …Service → I…Repository/I…Queries → EF Core → SQL
                                            └→ IRealtimeOutbox → SQL (cùng transaction)

SQL đã commit → RealtimeDispatchWorker → IRealtimePublisher → SignalR → React
React nhận sự kiện → tải lại REST API → backend kiểm quyền hiện tại → cập nhật UI
```

| Vị trí | Thành phần dự kiến | Trách nhiệm |
|---|---|---|
| BLL/Abstractions/Services | IAuthenticationService, IHorseRegistrationService, IHorseAssignmentService, IHorseProfileService; các interface training, medical, care, inventory, attachment, reporting, metadata | Hợp đồng ứng dụng; chỉ đưa public operation thật sự cần dùng vào interface. |
| DAL/Abstractions/Repositories | IUserRepository, IRegistrationRepository, IHorseRepository, IAssignmentRepository, IInventoryRepository, nhóm training/medical/care/file | Đọc/ghi dữ liệu, tracked entity cho mutation, không quyết định quyền/chuyển trạng thái. |
| DAL/Abstractions/Queries | IAccessQueries, IDashboardQueries, IReportQueries; filter/page/projection model độc lập HTTP | Truy vấn đặc thù, phân trang/lọc trên SQL, không trả IQueryable hoặc DbSet ra BLL. |
| DAL/Abstractions/Transactions | IUnitOfWork, IWriteTransaction | SaveChanges, begin/commit/rollback; repository không tự commit. |
| DAL/Repositories, Queries | EF implementation | LINQ/EF, tracking, projection, index-aware queries. |
| BLL/Realtime | IRealtimeOutbox, RealtimeEvent, chính sách xác định người nhận | Tạo sự kiện nghiệp vụ, không phụ thuộc Hub/IHubContext. |
| DAL/Entities và Data | RealtimeOutboxMessage, repository outbox, migration | Lưu sự kiện bền vững và trạng thái retry/lease. |
| API/Realtime | ClubHub, IClubClient, SignalRRealtimePublisher, adapter xác thực/phiên | Hub mỏng; publisher thực thi IRealtimePublisher qua IHubContext. |
| BLL/Workers hoặc project host hiện tại | RealtimeDispatchWorker | Dùng interface publisher/outbox; tạo scope theo batch, không phụ thuộc API implementation. |
| Frontend services/context | realtimeService.js, RealtimeProvider.jsx, mapping event→resource | Quản lý kết nối, đồng bộ session, tải lại có debounce, cleanup handler. |

Giữ hướng reference API → BLL → DAL. DAL không tham chiếu DTO của BLL; query trả read model của DAL, BLL map sang response DTO. IRealtimePublisher đặt ở BLL để API triển khai mà không tạo vòng phụ thuộc. Composition root đăng ký AddClubDataAccess, AddClubBusiness, AddClubRealtime ở API. Tất cả repository và Unit of Work trong một request dùng đúng một scoped DbContext; không inject scoped DbContext vào singleton worker.

## 4. Phạm vi realtime và người nhận

| Sự kiện | Khi nào tạo | Người nhận hợp lệ | UI cập nhật |
|---|---|---|---|
| NotificationCreated / NotificationsChanged | Tạo thông báo/đánh dấu đọc | Đúng RecipientId; mọi tab của cùng user | Badge và danh sách thông báo |
| RegistrationChanged | Gửi, yêu cầu sửa, duyệt, hủy | Owner hồ sơ và Manager phù hợp | Danh sách/chi tiết/review queue |
| HorseAssignmentChanged | Thay phân công | Owner, người cũ/người mới theo policy | Danh sách phạm vi và dữ liệu ngựa; người mất quyền xóa cache |
| TrainingPlanChanged / TrainingSessionChanged | Lập/sửa/trạng thái/giao rider/bắt đầu/kết quả/đánh giá | Owner và nhân sự hiện có quyền; Rider chỉ session của mình | Plan/session và dashboard liên quan |
| MedicalAvailabilityChanged | Hạn chế/sức khỏe thay đổi | Người có quyền biết khả năng tập của ngựa | Tải lại trạng thái có được tập; không gửi bệnh án |
| CareTaskChanged / IncidentChanged | Giao/ghi task hoặc sự cố | Groom được giao và người có quyền xử lý/xem | Task/incident tương ứng |
| InventoryChanged / ReplenishmentChanged | Movement hoặc đề nghị/duyệt bổ sung | Manager, Groom theo quyền từng resource | Tồn kho, yêu cầu, cảnh báo thấp |

Giai đoạn đầu ưu tiên NotificationCreated, RegistrationChanged và TrainingSessionChanged. Các màn hình chưa tồn tại chỉ kiểm thử backend/event contract; không tính là đã hoàn thành UI.

Envelope v1 dự kiến: eventId, schemaVersion, eventType, resourceType, resourceId, resourceVersion, occurredAtUtc. Đây là tín hiệu thay đổi tối thiểu; tránh gửi entity, OTP/token, file URL riêng tư hoặc nội dung bệnh án. EventId chống xử lý trùng; resourceVersion hỗ trợ bỏ bản cũ nếu có ý nghĩa với resource. Sự kiện cấp nhiều bảng không dùng Version của một entity để giả định thứ tự toàn hệ thống.

## 5. Xác thực và phân quyền SignalR

1. Hub `/hubs/club` yêu cầu đăng nhập; user identifier lấy từ NameIdentifier ổn định, không lấy email hoặc ID do client truyền.
2. Client dùng accessTokenFactory đọc token hiện tại qua session service; refresh phối hợp một luồng với REST, tránh nhiều tab/request refresh đua nhau. Không đổi token format.
3. WebSocket/SSE trên browser có thể truyền access_token qua query. Tích hợp với AddBearerToken bằng adapter/event hook phù hợp .NET 10, chỉ nhận query token trong hub path, giữ ưu tiên header hợp lệ và kiểm lại SecurityStamp/Active. Bước spike phải thử cả negotiate, WebSocket và fallback, không chỉ kết nối .NET client.
4. HTTPS; không log query chứa token ở app/proxy; chỉ allow origin frontend đã cấu hình. Kiểm WebSocket Origin riêng, CORS không đủ để bảo vệ WebSocket. Nếu dùng credentials thì cấu hình origin cụ thể, không wildcard.
5. Dùng CloseOnAuthenticationExpiration; kiểm hạn ticket được truyền đúng. Logout, disable account hoặc đổi stamp phải khiến kết nối cũ mất quyền nhận dữ liệu. Không chỉ dựa vào yêu cầu client tự stop.
6. Thiết kế connection registry theo user/session/stamp và kênh thu hồi phiên. Trước khi gửi kiểm account/stamp hiện tại; chỉ định tuyến tới các connection/group của phiên hợp lệ. Nếu token không có session ID, dùng stamp hiện có và registry server quản lý; không đổi contract token tùy tiện. Hub method phải kiểm dữ liệu phiên hiện tại, không chỉ cached claims. Kiểm cả thu hồi phiên ở tab/máy khác.
7. Ưu tiên định tuyến từng user/session hợp lệ. Không mở JoinGroup tùy ý bằng horseId/role; group chỉ là cơ chế phân phối, không phải bằng chứng quyền. Khi dùng subscription resource phải kiểm quyền lúc subscribe và cập nhật khi phân công đổi.
8. Dispatcher kiểm lại quyền khi phát; recipient snapshot có thể đã hết quyền trong thời gian retry. AssignmentChanged cho người bị gỡ chỉ là tín hiệu phạm vi thay đổi tối thiểu, không kèm dữ liệu ngựa/bệnh án hiện tại.

Đề xuất hub ban đầu chỉ nhận kết nối và phát sự kiện server→client, không có method ghi nghiệp vụ. Các read sau sự kiện vẫn qua REST kiểm quyền.

## 6. Outbox và giao dịch

Một thao tác ghi chạy theo thứ tự: kiểm quyền/rule → cập nhật entity → tạo audit/notification → thêm outbox → SaveChanges → commit transaction → dispatcher đọc và gửi. Rollback đồng nghĩa không có sự kiện để phát.

Bảng RealtimeOutboxMessages dự kiến gồm: Id, SchemaVersion, EventType, ResourceType/Id, PayloadJson, CreatedAtUtc, Status, Attempts, NextAttemptAtUtc, LockedUntilUtc, LeaseOwner, SentAtUtc, LastError. Bảng delivery/recipient riêng có khóa duy nhất (EventId, RecipientId) nếu cần retry từng người nhận; tránh đánh dấu toàn event thành công khi mới gửi được một phần. TTL giữ dữ liệu lỗi đủ điều tra; job dọn theo retention cấu hình.

Dispatcher claim batch trong transaction ngắn bằng locking/lease hoặc SQL application lock; commit claim rồi mới gửi mạng, cập nhật kết quả sau. Lease hết hạn cho phép worker khác tiếp quản. Retry có backoff, attempt limit và trạng thái lỗi cần xem; app crash sau send trước ack có thể phát trùng. Đích là at-least-once ở bước dispatch, không cam kết browser nhận đúng một lần. IHubContext gửi thành công không chứng minh user đã xem hay client offline đã nhận. Notification trong SQL và REST là nguồn khôi phục.

Hub/negotiate/transports phải được loại rõ khỏi TransactionMiddleware; không mở transaction dài cho kết nối hoặc request negotiate POST. Không loại mọi request theo một tiền tố không kiểm soát; dùng metadata/endpoint phân loại có kiểm thử.

Migration là bổ sung bảng/index outbox và delivery nếu chọn. Refactor repository tự nó không cần đổi schema nghiệp vụ. Không reset database hay sửa giá trị enum hiện có. AutoMigrate production giữ false; triển khai migration riêng trước bật dispatcher.

## 7. Frontend và phục hồi kết nối

- Một connection do provider cấp ứng dụng quản lý cho mỗi tab; không tạo connection ở mỗi page. Đăng ký handler trước start; cleanup đúng callback khi unmount/logout; tránh handler trùng do React StrictMode.
- withAutomaticReconnect cho mất kết nối sau khi đã start; retry start ban đầu bằng backoff riêng, dừng khi logout/unmount. 401/403 không retry vô hạn, xử lý session theo auth service.
- Khi reconnect: đăng ký lại subscription được phép nếu có, tải lại notifications/unread và resource đang mở. Không giả định mọi sự kiện trong lúc offline sẽ được replay.
- Gom nhiều sự kiện cùng resource bằng debounce; chỉ refetch trang/list/dashboard liên quan, tránh mỗi event kéo lại toàn ứng dụng. Hủy/bỏ qua response cũ nếu request mới đã hoàn thành.
- Event đến trước response REST của chính thao tác vẫn hợp lệ; dùng eventId/version và refetch làm UI hội tụ. Không ghi đè form người dùng đang sửa; hiện thông báo dữ liệu đã đổi và xử lý conflict.
- Khi quyền bị gỡ, xóa cache tương ứng và xử lý REST 403/404. Header có trạng thái reconnect nhẹ; tính năng REST vẫn dùng được khi realtime mất.
- Vite proxy `/hubs` bật WebSocket; production proxy hỗ trợ upgrade/timeouts. URL API/hub dùng cấu hình môi trường.

## 8. Work breakdown và tiêu chí nghiệm thu

| Mã | Công việc cụ thể | Phụ thuộc | Sản phẩm/điều kiện hoàn thành | Ước lượng ngày công |
|---|---|---|---|---|
| P01 | Inventory dependencies, query, contract; baseline tests; chốt quy ước | Không | Danh sách điểm dùng EF, API snapshot, test baseline và quyết định kiến trúc | 1–2 |
| P02 | Tạo service interfaces; sửa endpoint, auth adapter và DI; tách static helper khi cần | P01 | Endpoint/adapter không inject service implementation; build/contract tests đạt | 1–2 |
| P03 | Unit of Work, transaction abstraction, DAL registration; thí điểm kho | P02 | Repository không save/commit riêng; scope chung; stock/concurrency/rollback tests đạt | 2–3 |
| P04 | Spike SignalR + bearer auth + loại transaction hub + origin/proxy | P03 | Browser kết nối hợp lệ; anonymous/token sai bị chặn; logout/disable/reconnect tests chứng minh policy | 2–3 |
| P05 | Outbox schema, migration, enqueue và dispatcher; publisher abstraction/adapter | P03,P04 | Commit mới phát; rollback không phát; crash/retry/lease/dedup đạt | 2–3 |
| P06 | Tách intake, horse, assignment, attachment queries và helpers liên quan | P03 | Duyệt nguyên tử; quyền và JSON giữ nguyên; phát Registration/Assignment event | 2–3 |
| P07 | Tách training/medical; chuyên biệt conflict/restriction queries | P06 | Guards, kết quả bất thường, lịch sử và concurrency đạt; session events đúng người | 3–4 |
| P08 | Tách care, chuồng, incident, inventory hoàn chỉnh | P07 | Role/scope/stock/occupancy tests đạt; sự kiện từng module | 2–3 |
| P09 | Tách auth, CurrentUser/ClubAccess/ClubEvents, workers, storage metadata | P03,P08 | Không còn EF trực tiếp trong BLL; giữ OTP/outbox/stamp/lock/cleanup | 2–4 |
| P10 | Reporting queries và DTO mapping còn lại | P08,P09 | Phân trang trên SQL; dashboard giữ query count; contract tương thích | 1–2 |
| P11 | Frontend provider, notification badge và invalidation intake/training; mở rộng nơi có UI | P05,P06,P07 | Hai browser đồng bộ, reconnect phục hồi, không handler/request trùng | 2–3 |
| P12 | Regression, kiến trúc, performance, tài liệu và demo | Tất cả | Test matrix đạt, migration/rollback runbook, demo script, báo cáo giới hạn | 2–3 |

Ước lượng tổng: 22–35 ngày công cho một người đã quen code, gồm kiểm thử; không phải cam kết lịch giao. Spike auth và độ phủ test SQL có thể điều chỉnh số này. Không đổi tất cả module trong một lần; mỗi module phải có checkpoint độc lập. MVP đầu tiên: P01–P05 + phần intake của P06 + phần frontend P11, rồi hoàn thiện phạm vi còn lại.

## 9. Ma trận kiểm thử bắt buộc

| Nhóm | Trường hợp cần có |
|---|---|
| Kiến trúc/DI | Không DbContext/DbSet/EF extension trong BLL; DAL không reference BLL/API; scoped instance chung; hub adapter không bị inject vào BLL concrete. |
| Nghiệp vụ | Role/scope/state; thiếu attachment; duplicate review/result; medical guard lúc schedule/start và bất thường khi submit; replenish không tăng tồn. |
| SQL/transaction | Hai request tranh tồn/duyệt/start; Version/unique constraint; rollback giữa create Horse và audit/outbox; commit-auth-attempt; file cleanup. |
| SignalR auth | Anonymous, expired token, query token ngoài hub, negotiate/WS/SSE/long polling; sai Origin; logout tab khác, disable account, đổi stamp. |
| Phân phối | Owner A không nhận event Owner B; rider chỉ session của mình; gỡ assignment trước retry; không lộ clinical payload; nhiều tab đúng account. |
| Outbox | Rollback không gửi; restart trước/sau send; worker đồng thời; lease hết hạn; lỗi một người nhận; duplicate và dead-letter/retry. |
| Frontend | Start lỗi ban đầu, mạng mất/reconnect, token refresh race, handler cleanup, response REST cũ, dữ liệu đổi lúc sửa form. |
| Hiệu năng | Dashboard SQL count, report limit, phân trang/index; burst event không gây refetch storm; dispatcher không giữ SQL lock trong network send. |
| Tương thích | API routes/JSON/errors, OpenAPI, existing workflow tests; CI SQL Server, không dùng database thật cho test. |

Integration tests cần SQL Server thật cho hành vi SQL; mock repository không chứng minh transaction/concurrency. SignalR cần browser E2E, không chỉ test Hub method trực tiếp.

## 10. Triển khai, theo dõi và rollback

1. Refactor theo module, giữ API; checkpoint kiểm thử trước khi tiếp tục.
2. Deploy migration outbox/index có kiểm soát; bật feature flag hub/dispatcher riêng. Chạy API REST bình thường khi realtime tắt.
3. Bật cho môi trường demo/test trước; đo queue depth, age oldest pending, retries/failed count, dispatch latency, active connections/reconnect và 401/403. Logs dùng EventId/correlation ID, không token/clinical payload.
4. Một instance dùng SignalR self-host cho demo. Nhiều API instance phải chọn Azure SignalR Service hoặc backplane phù hợp; SQL outbox không tự chuyển message tới connection ở host khác. Nếu chọn Redis phải thiết kế affinity và kiểm chứng deployment; Azure SignalR phù hợp hướng Azure hiện tại nhưng cần cấu hình/chi phí riêng.
5. Bật frontend dần theo chức năng. Nếu có lỗi, tắt feature flag dispatcher/client; REST tiếp tục hoạt động. Giữ outbox data để điều tra; quyết định tuổi tối đa event được dispatch sau khi bật lại để tránh backlog cũ làm UI tải hàng loạt.
6. Không drop bảng hoặc Down migration phá dữ liệu chỉ để rollback ứng dụng; schema additive cho phép quay về bản app trước. Thay token hay payload breaking phải là công việc riêng có version/migration plan.

## 11. Thứ tự các đợt review

1. Baseline + quy ước + service interfaces.
2. Unit of Work + Inventory repository + transaction tests.
3. SignalR authentication spike + connection/session policy.
4. Outbox + dispatcher + Notification realtime.
5. Intake/assignment repositories + frontend realtime.
6. Training/medical repositories + frontend realtime.
7. Care/kho/chuồng và các helpers/workers/auth còn lại.
8. Reporting/DTO + architecture checks + regression/docs/deployment.

Definition of Done chung: code đúng hướng phụ thuộc, không đổi nghiệp vụ ngoài chủ đích, test liên quan đạt, API tương thích, event không lộ dữ liệu/không phát trước commit, frontend phục hồi khi mất mạng, và tài liệu phản ánh chức năng đã có. Không gọi hoàn tất toàn dự án khi mới tách interface hoặc mới kết nối được hub.

## 12. Tài liệu Microsoft dùng đối chiếu

- [SignalR authentication/authorization .NET 10](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-10.0): token browser, accessTokenFactory, hết hạn và cached principal.
- [JavaScript client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-10.0): reconnect, start và lifecycle.
- [Security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-10.0): token logging, origin và thông tin nhạy cảm.
- [Hosting/scaling](https://learn.microsoft.com/en-us/aspnet/core/signalr/scale?view=aspnetcore-10.0): nhiều server/backplane/Azure SignalR.

Outbox, ranh giới repository, event catalog, ước lượng và chính sách thu hồi phiên ở trên là thiết kế đề xuất cho HorseClub, không phải tính năng đã được SignalR tự cung cấp.
