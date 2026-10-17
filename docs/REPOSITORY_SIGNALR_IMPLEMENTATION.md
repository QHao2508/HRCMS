# Kết quả tách repository, interface và tích hợp SignalR

Ngày: 08/10/2026. Mã nguồn đang ở working tree, chưa commit/push. Theo yêu cầu tiếp theo của người dùng, đã áp dụng ba migration bổ sung lên Azure SQL dùng chung HRCMS và kiểm tra thành công.

## Cấu trúc để trình bày với thầy

| Thành phần | Trách nhiệm | Ví dụ để trả lời |
|---|---|---|
| API | HTTP, binding, authentication, mapping response | Endpoint nhận IInventoryService qua DI. |
| Service interface tại BLL/Abstractions/Services | Hợp đồng thao tác ứng dụng | IInventoryService quy định thao tác kho mà API có thể gọi. |
| BLL service | Quyền theo đối tượng, validation, quy tắc và trạng thái | InventoryService kiểm quyền và số lượng trước khi lưu. |
| DAL/Abstractions | Hợp đồng truy cập dữ liệu, transaction | IInventoryRepository, IUnitOfWork. |
| DAL/Repositories và Queries | EF Core, lọc/phân trang, truy vấn báo cáo | InventoryRepository; ReportingQueries giữ dashboard một SQL command. |
| EfUnitOfWork | SaveChanges và transaction trên DbContext chung | Nhiều repository trong request cùng commit hoặc rollback. |
| Realtime outbox | Lưu tín hiệu cùng transaction nghiệp vụ | Rollback nghiệp vụ cũng rollback sự kiện. |
| Dispatcher và SignalR hub | Gửi sau commit, định tuyến theo phiên hợp lệ | Không giữ transaction SQL trong lúc gửi qua mạng. |
| React realtime provider | Một kết nối mỗi tab, reconnect, chống trùng | Nhận tín hiệu rồi tải lại REST; form nhập liệu không tự bị ghi đè. |

Luồng: React → endpoint → I…Service → service → I…Repository → EF Core → SQL. IUnitOfWork quản lý lưu/giao dịch. Sau commit, worker đọc outbox → hub → React tải lại dữ liệu bằng REST để backend kiểm quyền hiện tại.

Interface không phải project thứ tư. Repository không quyết định trạng thái nghiệp vụ. Unit of Work không tạo DbContext riêng cho mỗi repository. API contract, route cũ và cơ chế bearer/refresh/security stamp được giữ.

## Phạm vi realtime

Hub `/hubs/club` phát NotificationCreated, NotificationsChanged và DataChanged. Các nhóm hồ sơ, phân công, ngựa, training, medical, care, kho và file phát tín hiệu thay đổi cho đối tượng liên quan. Payload không chứa nội dung hồ sơ/bệnh án; REST vẫn là nguồn dữ liệu và nơi kiểm quyền.

Frontend bổ sung chuông thông báo chưa đọc/đánh dấu đã đọc; dashboard và danh sách hồ sơ, duyệt, ngựa, kế hoạch, buổi tập tải lại khi có tín hiệu. Những màn hình khác vẫn tải dữ liệu theo thao tác hiện có. Event trùng được bỏ qua, burst gom 250 ms, reconnect tải lại dữ liệu. Không cam kết exactly-once hoặc phát lại mọi event đã mất; dữ liệu thông báo bền vững vẫn đọc bằng REST.

## Migration và bật tính năng

Ba migration bổ sung lần lượt tạo RealtimeOutboxMessages, thêm sự kiện đọc và đổi tên trường nguồn để dùng chung tín hiệu nghiệp vụ. Không sửa migration cũ hoặc reset dữ liệu. ClubFactory đã dùng migration trên các database `HRCMS_Test_<GUID>` của SQL Express; database này được dọn sau test.

1. Sao lưu database đích và xuất SQL để review bằng `dotnet ef migrations script --idempotent --project HorseClub.DAL --startup-project Horse_BackEnd --context SqlServerClubDbContext --output TestResults/realtime-migrations.sql`.
2. Thử script trên bản sao database đích trước khi dùng chung; SQL Express tests không thay thế kiểm tra cấu hình Azure thực tế.
3. Áp dụng migration đã review trước khi chạy binary mới. Outbox vẫn được ghi khi dispatcher tắt, nên schema là điều kiện bắt buộc.
4. Bật backend `Realtime:Enabled=true`; frontend đặt `VITE_REALTIME_ENABLED=true` rồi build lại. Mặc định cả hai tắt.
5. Cấu hình `Cors:Origins` đúng origin frontend, reverse proxy hỗ trợ WebSocket và `/hubs`; không ghi query `access_token` vào access log của proxy/hosting.

Không cần Azure SignalR cho bản self-host này. Chỉ triển khai một API instance; scale nhiều instance cần bổ sung backplane/dịch vụ SignalR vì registry kết nối nằm trong memory mỗi host.

## Kiểm thử và vận hành

Kết quả: solution Release build thành công, 0 warning/0 error; toàn bộ backend 113 passed, 1 skipped (Azure upload live); kiểm thử repository/realtime bổ sung 3 passed; frontend lint/build thành công, 161 tests passed. Vite cảnh báo bundle chính khoảng 558 kB; chưa tách chunk SignalR. Script migration tại TestResults/realtime-migrations.sql và docs/azure-sql-schema.sql đã áp dụng lên database dùng chung theo yêu cầu người dùng.

Kiểm tra Azure SQL dùng chung ngày 08/10/2026: 32 bảng ứng dụng, đủ 7 migration. API Release chạy tạm tại cổng 5398 với worker/realtime dispatcher tắt và AutoMigrate=false; health, login, me, dashboard, horses, notifications trả 200; SignalR negotiate và WebSocket JSON handshake thành công bằng token hiện có. Ghi/update/read outbox trong transaction rồi rollback thành công, xác minh index unique và không còn dòng test. Không chạy toàn bộ fixture test trên HRCMS, không sửa dữ liệu nghiệp vụ hoặc logout tài khoản quản lý; đăng nhập có thể cập nhật metadata bảo mật theo hành vi hiện có. Phiên API kiểm tra đã dừng; chưa bật realtime cho phiên ứng dụng dùng chung, chưa kiểm tra phát event end-to-end trên Azure. Hai lần đầu Azure báo database chưa sẵn sàng, lần sau ONLINE và migration thành công.

Backend kiểm workflow cũ, SQL concurrency, hiệu năng truy vấn, dependency layers và service contracts. Test mới kiểm UoW rollback, notification outbox rollback, claim đồng thời một winner, anonymous hub bị từ chối, origin lạ bị từ chối, token hiện có kết nối WebSocket và gửi đúng phiên. Frontend có test gom burst/chống trùng/hủy đăng ký, cùng các test authentication và contract hiện có. Chưa chạy E2E trình duyệt với backend thật hoặc test upload Azure thật.

Outbox claim bằng lease một phút; mỗi lần gửi có timeout 10 giây, retry sau 30 giây, tối đa tám lần. Theo dõi log dispatch lỗi và hàng SentAt null/Attempts >= 8. Chưa có trang quản trị replay hoặc retention tự động: chỉ reset Attempts/NextAttemptAt/LockedUntil/LeaseOwner có chọn lọc sau khi sửa nguyên nhân và review event; chỉ dọn hàng đã gửi theo chính sách lưu trữ. Không xóa toàn bộ outbox để xử lý lỗi.

Token hết hạn bị đóng kết nối; stamp đã thu hồi không nhận publication tiếp theo. Reconnect kiểm lại `/api/auth/me`; logout dừng kết nối. Worker không gửi nội dung riêng tư vào log hoặc payload realtime.
