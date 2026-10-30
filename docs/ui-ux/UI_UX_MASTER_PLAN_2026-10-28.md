# Kế hoạch chỉnh UI/UX toàn bộ HRCMS

Ngày lập: 28/10/2026. Phạm vi: backend `D:/SWP391/HRCMS/HRCMS` và frontend `D:/SWP391/HRCMS/HRCMS-Frontend` tại thời điểm kiểm kê. Đây là bản kế hoạch gốc. Sau yêu cầu triển khai, frontend đã được cập nhật; phạm vi thực hiện, kiểm thử và giới hạn nghiệm thu được ghi tại `D:/SWP391/HRCMS/HRCMS-Frontend/docs/UI_UX_DELIVERY_2026-10-28.md`. Dữ liệu vận hành không bị thay đổi.

## 1. Phạm vi đọc và mức độ kiểm chứng

| Phạm vi | Số file | Kết quả |
|---|---:|---|
| Backend: source, contracts, database/migrations, tests, tools, docs, cấu hình | 551 | Đọc đầy đủ byte bằng công cụ; 551 file giải mã được UTF-8 |
| Frontend: source, tests, docs, assets, lockfile, CI/cấu hình | 245 | Đọc đầy đủ byte bằng công cụ; 241 file UTF-8 và 4 file ảnh/font nhị phân |
| Nội bộ Git của hai repository | 208 | Đọc byte, lập hash; không dùng làm căn cứ thiết kế |
| Tổng file dự án, không tính nội bộ Git | **796** | Có từng đường dẫn trong `FILE_INVENTORY.csv` |

Frontend có 160 file trong `src`. Tổng số dòng văn bản do công cụ đếm: backend 90.747, frontend 23.769; số này có cả tài liệu, lockfile, snapshot, migration và contract sinh tự động, không phải số dòng logic sản phẩm.

`FILE_INVENTORY.csv` lưu repository, đường dẫn, phân loại, dung lượng, định dạng, số dòng, SHA-256 và kết quả đọc từng file. Lượt kiểm kê không có file đọc thất bại. Chỉ loại chính thư mục báo cáo `docs/ui-ux` đang được tạo để tránh tự đếm đầu ra. Không có `node_modules`, `bin`, `obj`, `dist` trong kết quả kiểm kê. Các file thêm sau thời điểm kiểm kê không nằm trong snapshot này.

**Phân biệt rõ:** đọc byte/lập chỉ mục toàn bộ file không đồng nghĩa đã phân tích thủ công từng dòng. Phân tích sâu cho kế hoạch tập trung vào routing/quyền, layout, component chung, các page nghiệp vụ, hook mutation/resource, CSS, contract và các guard backend liên quan. Một số lượt xuất file dài bị giới hạn đầu ra; không xem việc quét file là bằng chứng đã hiểu hết mọi đoạn mã. Chưa chạy ứng dụng/browser, chưa có ảnh chụp mới, chưa kiểm thử tương tác hoặc UAT. Các kết luận dưới đây là kết luận từ mã nguồn; các vấn đề thị giác cần xác nhận khi chạy ở giai đoạn 0.

Không lấy ghi chú “đã đạt” trong tài liệu ngày 11–13/10 làm bằng chứng kiểm chứng bản hiện tại. Code hiện tại có thay đổi sau các tài liệu đó, đặc biệt duration/overlap, phân quyền chăm sóc và ngày kết thúc điều trị tùy chọn.

## 2. Hướng thiết kế

Giữ nhận diện nâu, kem, xanh của HRCMS và font Manrope/Lora đang có. Làm rõ việc cần xử lý, trạng thái ngựa và hành động tiếp theo trước khi thêm trang trí. Dùng Manrope cho không gian vận hành có mật độ thông tin cao; Lora dành cho nhận diện và nội dung giới thiệu.

Ba mục tiêu:

1. Người dùng thấy ngay tác vụ đúng vai trò, đúng ngựa, đúng trạng thái.
2. Mỗi thao tác lưu, duyệt, giao việc, thực hiện, kết thúc có phản hồi và đường phục hồi rõ ràng.
3. Mọi màn hình dùng cùng quy tắc typography, khoảng cách, bảng, form, trạng thái và điều hướng.

Giữ API và enum hiện hành trong các thay đổi bố cục. Chức năng mới cần dữ liệu chưa được API cấp phải có đầu việc backend riêng. Không tự đưa chẩn đoán, thuốc, hồ sơ khám hoặc số liệu toàn hệ thống vào màn hình chỉ có quyền/tập dữ liệu hạn chế.

## 3. Phát hiện cụ thể và thứ tự ưu tiên

P0: làm sai kết quả/lệch quyền hoặc gây hiểu nhầm khi thao tác. P1: cản trở luồng chính hoặc tính nhất quán toàn hệ thống. P2: tối ưu sau khi luồng chính ổn định. Đây là mức ưu tiên của đợt UI/UX, không phải đánh giá mức độ sự cố bảo mật.

| ID | Mức | Bằng chứng từ mã hiện tại | Việc cần làm và nghiệm thu |
|---|---|---|---|
| UX-01 | P0 | `MedicalTreatment.jsx`: `statusFilter` dùng `All/Active/Completed`; `<option>` không có `value`, nội dung lấy nhãn tiếng Việt | Tách giá trị enum và nhãn. Chọn “Đang hiệu lực” chỉ ra active, chọn hoàn tất chỉ ra completed, “Tất cả” trả toàn bộ; có test tương tác |
| UX-02 | P0 | `permissions.js` chỉ cấp `careAssign` cho Vet; `CareService.CreateTask` cấp Manager/Vet và tách loại việc; tài liệu 23/10 xác nhận Manager giao chăm sóc thường ngày | Đối chiếu lại quyết định quyền với người phụ trách nghiệp vụ tại bước 0. Lập ma trận FE/BE/test; nếu giữ contract hiện tại thì khôi phục UI cho Manager với đúng 5 loại việc và nguồn dữ liệu phù hợp. Không chỉ bật nút rồi tái sử dụng nhánh Vet |
| UX-03 | P1 | `Dashboard.jsx` render cùng 6 KPI cho mọi role; `DashboardResponse`/service có thêm `careTasksToday` | Bố cục ưu tiên theo role; dùng dữ liệu thật sẵn có, thêm careTasksToday cho Groom. Không tự suy số ca tái khám/chờ duyệt từ endpoint chưa trả |
| UX-04 | P1 | `global.css` có token 14/16px, `workspace.css` override nhiều thành 12/14px; medical/ops có hệ class riêng | Chốt token semantic và component chung, đưa override theo module vào phạm vi rõ. Đo computed style, không chỉ nhìn giá trị khai báo |
| UX-05 | P1 | Bảng chung min-width 640px, bảng từ 7 cột 960px; care/session/treatment có nhiều cột và văn bản dài | Giữ cột quyết định ở danh sách, chuyển nội dung dài sang chi tiết. Mobile dùng card cho tác vụ, vùng cuộn riêng cho bảng cần so sánh |
| UX-06 | P1 | `HorseDetail` nối overview, health, measurement, assignment, availability, archive, history theo chiều dọc | Tổ chức các vùng Tổng quan/Sức khỏe/Huấn luyện/Nhân sự/Lịch sử theo quyền; chọn rõ tab hoặc anchor, không trộn hành vi dưới cùng kiểu hiển thị |
| UX-07 | P1 | `SupportForm` mặc định nút “Lưu”; nhiều trang đặt form tạo mới trước bảng; chọn row mở thêm form xa ngữ cảnh | Cho truyền tên hành động cụ thể; danh sách là điểm vào chính, CTA mở form/drawer/dialog có ngữ cảnh và đường đóng |
| UX-08 | P1 | `ResourceState` là text loading; `SupportTable` vẫn render data khi loading/error; `OperationsTable` chỉ render khi ready | Thống nhất initial loading/refetch/stale/error/empty. Giữ dữ liệu cũ khi refresh nếu phù hợp, nhưng đánh dấu đang cập nhật và chặn thao tác không an toàn |
| UX-09 | P1 | Y tế dùng `new Date(...).toLocaleString()` ở một số nơi không chỉ định timezone; training/report dùng timezone CLB rõ | Định dạng instant theo Asia/Ho_Chi_Minh. Date-only giữ nguyên ý nghĩa lịch; kiểm tra máy người dùng ở múi giờ khác và quanh 00:00 |
| UX-10 | P1 | `WebsitePage` preview chỉ render nội dung thô; không dùng bố cục Home thực tế | Tạo preview dùng cùng section renderer với Home, thấy đúng ảnh/tỷ lệ/cắt ảnh, responsive và dữ liệu chưa lưu; tách “Lưu nội dung” với upload đang ghi ngay |
| UX-11 | P1 | `RegistrationCreate` đổi bước không kiểm từng bước; lỗi xác thực cuối form chưa tự đưa về trường lỗi | Giữ nháp được thiếu trường; chỉ kiểm hợp lệ phần đã nhập. Hiển thị bước có lỗi, summary liên kết/focus tới field; submit vẫn kiểm đủ toàn bộ |
| UX-12 | P1 | `SessionContent` đồng thời chứa nhiều vùng chỉnh sửa, bắt đầu, kết quả, đánh giá, bỏ qua | Thanh tiến trình và hành động chính theo state/role; đọc kế hoạch và thực thi tách rõ, không tự đổi trạng thái khi chưa có response |
| UX-13 | P2 | `AuditPage` render UUID/reference và `pre` detail; lịch sử có cấu trúc đã tồn tại ở training | Bản trình bày ai/làm gì/khi nào/đối tượng; raw detail trong mục mở rộng. Nhãn/đường dẫn suy từ dữ liệu hợp lệ; không suy tên từ UUID |
| UX-14 | P2 | Chưa có route lịch tổng hợp; hệ thống có list session và availability thực tế | Có thể thêm lựa chọn “Danh sách/Lịch” sau khi kiểm tra API date range, paging, timezone. Không cam kết calendar như chức năng đã có |

Các nền tảng cần kế thừa: menu theo role, skip link, label/help/error ARIA của Field, chống double submit, uncertain mutation, patient context qua URL, trap/restore focus của modal, notification read-all/realtime, ảnh riêng qua endpoint kiểm scope, lịch sử và versioning. Không viết lại các phần này chỉ vì thay diện mạo.

## 4. Không gian làm việc theo 7 vai trò

| Vai trò | Nội dung ưu tiên | Tác vụ đầu trang | Giới hạn cần giữ |
|---|---|---|---|
| HorseOwner | Ngựa của tôi, tình trạng vận hành, hồ sơ đăng ký, lịch tập | Tạo hồ sơ, xem yêu cầu bổ sung, mở ngựa | Chỉ ngựa thuộc scope; không thêm quyền sửa clinical |
| ClubManager | Chờ duyệt, phân công, nhân sự, tồn kho/chuồng, báo cáo | Duyệt hồ sơ, phân công, mời nhân sự | Phân biệt quyền đọc và ghi; quyền chăm sóc phải đối chiếu UX-02 |
| HeadTrainer | Ngựa phụ trách, giáo án, ứng viên Trainer, lịch liên quan | Giao Trainer, giao giáo án | Chỉ phân công Trainer cho ngựa mình phụ trách; không biến Manager thành người sửa giáo án |
| Trainer | Kế hoạch, lịch tập, kết quả chờ đánh giá, ràng buộc lịch/sức khỏe | Tạo kế hoạch/buổi tập, đánh giá | Kiểm assignment hiện tại và plan/session state |
| WorkRider | Buổi tập được giao, giờ/địa điểm/bài tập, buổi đang thực hiện | Bắt đầu, ghi kết quả, báo bất thường | Chỉ Rider được giao thực hiện; lịch sử không cấp quyền sửa |
| Veterinarian | Ngựa cần theo dõi, bệnh án, điều trị, hạn chế, tái khám/dự phòng | Khám, tái khám, giao việc điều trị | Clinical scope của backend; clearance theo từng vấn đề được chọn |
| Groom | Việc hôm nay, việc đang làm, khẩu phần, hướng dẫn, vệ sinh chuồng | Ghi thực hiện, báo sự cố, xuất vật tư | Chỉ thực hiện việc/chuồng được phép; không mở rộng sang toàn bệnh án |

Đợt đầu chỉ sắp xếp lại KPI và liên kết từ API hiện có. Các danh sách “cần xử lý” phải có response/endpoint thật phù hợp, kiểm scope và tổng số; nếu chưa có thì ghi rõ dependency backend thay vì tạo con số giả.

## 5. Design system và component dùng chung

| Thành phần | Quy tắc đề xuất | Vị trí triển khai |
|---|---|---|
| Màu | Semantic: primary, success, warning, danger, info, muted; giữ nâu/kem/xanh; trạng thái luôn có chữ | Tách token từ `global.css`, `workspace.css`, CSS module |
| Chữ | Nội dung 14–16px; input mobile 16px; metadata 12–14px; tiêu đề workspace 26–32px; không dùng chữ nhỏ cho hướng dẫn quan trọng | Typography chung; đo trên desktop/mobile |
| Khoảng cách | Thang 4/8/12/16/24/32/48; card 16–24px; layout rõ header/toolbar/body/actions | Page shell, card, form section |
| Button | Một hành động chính mỗi vùng tác vụ; verb cụ thể; trạng thái pending ngay trên nút; mobile target khoảng 44px | Button và action bar chung |
| Form | Label rõ; required/optional thống nhất; đơn vị sát field; error cạnh field và summary; giữ input khi lỗi | Mở rộng Field/AuthInput, bổ sung fieldset/section |
| Tìm kiếm/lọc | Search, reset, filter summary, tổng kết quả; về trang 1 khi lọc; form và filter đang áp dụng tách rõ | FilterBar và URL state |
| Bảng | Caption/heading, scope cột, numeric align, cột thao tác có tên truy cập; dữ liệu dài xem chi tiết | Phát triển từ WorkflowUI/SupportTable/OperationsTable theo từng bước |
| Chi tiết | Tiêu đề + định danh + trạng thái + hành động; phần tóm tắt trước lịch sử | DetailHeader/DetailSection |
| Modal/drawer | Tên đối tượng luôn thấy; initial focus, trap, Escape, restore focus; cảnh báo bỏ thay đổi; footer không che field | Kế thừa `dialogFocus`/`useMedicalDialog` |
| Trạng thái | Initial skeleton; background updating; empty do chưa có dữ liệu khác empty do filter; error retry đúng loại; read-only/archived rõ | ResourceState/EmptyState/ErrorState |
| Mutation | Lưu một lần; không báo thành công trước server; uncertain giải thích “chưa xác nhận kết quả” và tải lại đối soát | Giữ `useMutation`, `mutationOutcome`, module medical cần tương đương |
| Phân trang | Giữ bộ lọc khi quay lại; summary nhất quán; page size chỉ thêm nếu endpoint cho phép | Pagination chung |
| Ngày/giờ | DD/MM/YYYY khi hiển thị ngày, instant theo giờ CLB; không biến date-only thành instant | Helpers định dạng chung |
| Thông điệp | Tiếng Việt từ MSG/vi.js; enum API giữ nguyên; fallback không hiện stack trace | messages, enumLabels, serverMessages |

Không hợp nhất toàn bộ medical/ops thành một component khổng lồ. Dùng primitive chung và giữ bố cục chuyên biệt khi nghiệp vụ cần. Không làm đợt refactor lớn đồng thời với sửa state machine.

## 6. Ma trận chỉnh sửa toàn bộ 41 route

Đếm theo 40 path khai báo và một fallback `*` trong `AppRoutes.jsx`. Một route có thể gồm nhiều modal, trạng thái và khác biệt quyền; phần 7 bao phủ các bề mặt đó.

| # | Route | Thay đổi UI/UX dự kiến | Nghiệm thu trọng tâm |
|---:|---|---|---|
| 1 | `/` | Giới thiệu ngắn, lợi ích theo người dùng, CTA đăng nhập/đăng ký rõ; giảm câu chữ mang tính tài liệu dự án; ảnh đúng tỷ lệ | CTA tới đúng nơi khi đã/chưa đăng nhập; preview CMS khớp Home |
| 2 | `/login` | Form tập trung; hiển thị/ẩn password; thông báo theo ngữ cảnh; liên kết lấy lại tài khoản | Autofill, keyboard, sai mật khẩu, restore session, redirect an toàn |
| 3 | `/register` | Chia nhóm thông tin cá nhân/tài khoản/bảo mật; giảm chiều dài bằng lưới desktop | Không mất dữ liệu khi lỗi; rule password/CCCD đúng contract; chỉ tạo HorseOwner |
| 4 | `/forgot-password` | Làm rõ ba bước lấy lại tài khoản; trạng thái đã yêu cầu mã và cooldown | Không tiết lộ email có tồn tại; không cho gửi liên tục |
| 5 | `/verify-email` | Email, ô OTP hỗ trợ paste/autofill, resend phụ, đường đổi email | Mã sai/hết hạn, cooldown; thành công về login có notice |
| 6 | `/reset-password` | Phân nhóm mã xác thực và mật khẩu mới; kiểm khớp password rõ | Token/mã không vào URL; hết hạn có đường xin mã khác |
| 7 | `/accept-invitation` | Stepper xác minh lời mời → đặt mật khẩu; ngữ cảnh nhân sự rõ | Setup token ở bộ nhớ, email khóa sau verify, lỗi không lặp ghi |
| 8 | `/dashboard` | Dashboard theo vai trò, việc ưu tiên trước, giảm card tài khoản; KPI có đường mở danh sách phù hợp | Số liệu thật, không suy KPI toàn hệ thống từ một trang, có careTasksToday khi phù hợp |
| 9 | `/registrations` | Danh sách trạng thái dễ quét, CTA tạo mới, giải thích hồ sơ cần bổ sung | Nháp/chờ duyệt/cần sửa khác nhau; bộ lọc và phân trang thống nhất |
| 10 | `/registrations/new` | Wizard 5 bước, checklist hoàn thành, lỗi theo bước, ảnh preview, thanh lưu nháp | Nháp được thiếu trường; không ghi là autosave; upload lỗi không tạo nháp trùng |
| 11 | `/registrations/:id` | Header tình trạng, lý do yêu cầu sửa, tài liệu và bước gửi rõ ràng | PendingReview chỉ đọc; dirty/missing có hướng dẫn; hủy có xác nhận |
| 12 | `/reviews` | Hàng đợi duyệt, tên chủ/ngựa/mã rõ; lọc mặc định chờ duyệt | Không lẫn nháp với hồ sơ cần xử lý; empty theo filter |
| 13 | `/reviews/:id` | Bố cục hồ sơ + tài liệu + quyết định; checklist, lý do sửa; vùng sửa hành chính riêng | Giữ allowlist; không duyệt nhầm khi còn chỉnh sửa chưa lưu; thành công mở horse thật |
| 14 | `/horses` | Định danh ngựa, health badge, search/reset rõ, card mobile | Tên trùng vẫn phân biệt bằng mã; search không gây request mỗi phím không cần thiết |
| 15 | `/horses/:id` | Header nhận diện, tóm tắt sức khỏe, tab/section theo vai trò, tác vụ nguy cơ tách riêng | Quyền giữ nguyên; archive/lịch sử rõ; không tải dữ liệu clinical chỉ để ẩn bằng CSS |
| 16 | `/training/templates` | Thư viện mặc định/được giao/tùy chỉnh; editor chia nhóm, yêu cầu năng lực dễ đọc | Mẫu mặc định chỉ sao chép; chỉ HeadTrainer sửa/giao/lưu trữ; Trainer chỉ xem được giao |
| 17 | `/training/plans` | Tập trung ngựa, mục tiêu, trạng thái, giai đoạn; ngày tháng gọn, filter có ngữ cảnh ngựa | horseId được giữ; tạo mới đúng role |
| 18 | `/training/plans/new` | Luồng ngựa → giáo án → phương án → thời gian; xem trước trước khi tạo | Phân biệt tham khảo/tùy chỉnh; lỗi giữ form; không nói đã tự sinh buổi tập |
| 19 | `/training/plans/:id` | Summary gọn, hạn chế vận động nổi bật, sessions/history thống nhất, action theo state | Active/Paused/Completed/Archived đúng quyền; đổi trạng thái giải thích ảnh hưởng |
| 20 | `/training/sessions` | Danh sách ưu tiên lịch gần/được giao; filter trạng thái và search gọn; calendar là P2 | Không tự thay sort nghiệp vụ nếu API không hỗ trợ; timezone CLB |
| 21 | `/training/plans/:id/sessions/new` | Chọn bài → ngày/giờ/thời lượng → Rider → availability; cảnh báo ngay cạnh lựa chọn | Giới hạn ngày plan; giải thích không có slot, thiếu Rider, medical block, conflict |
| 22 | `/training/sessions/:id` | Tiến trình Planned/Assigned/InProgress/kết quả/đánh giá; một CTA chính theo role | Chỉ Rider được giao start/result; Trainer evaluate; abnormal/skip có lý do |
| 23 | `/medical` | Tổng quan bệnh nhân, search/status, thẻ ưu tiên rõ scope; giảm header lặp | Phân biệt total và “trên trang”; mở đúng ngựa qua horseId |
| 24 | `/medical/examination` | Context ngựa cố định, bệnh án theo vấn đề, modal khám chia nhóm, lịch sử phiên bản rõ | Tạo mới khác correction; correction giữ ngày đánh giá; chọn đúng bản mới nhất |
| 25 | `/medical/injury` | Mức độ/trạng thái/vị trí nổi bật, lựa chọn có giải thích, xem liên kết bệnh án | Filter chạy đúng; ngày chấn thương/review hợp lệ; không gỡ chấn thương do archive |
| 26 | `/medical/treatment` | Sửa filter UX-01; danh sách phác đồ gọn; chi tiết thuốc/hướng dẫn và tiến độ chăm sóc; liên kết giao việc | Active/Completed đúng; ngày kết thúc không bị ép bắt buộc; task hoàn thành không tự kết luận phác đồ đã xong |
| 27 | `/medical/restrictions` | Phân cấp khóa tập/chặn toàn bộ/giới hạn; diễn giải tác động và thời gian hiệu lực | HealthStatus khác TrainingLock; không đổi giới hạn thành chẩn đoán; giữ payload |
| 28 | `/medical/follow-up` | Timeline theo vấn đề; so sánh trước/sau; clearance thành vùng xác nhận có chọn đối tượng | Chỉ đóng injury/treatment/restriction được chọn; không tự xóa mọi hạn chế |
| 29 | `/medical/preventive-care` | Lịch tiêm/tẩy giun/móng theo ngựa; phân biệt sắp đến/quá hạn/đã làm | Chỉ Vet lập/hoàn thành; người khác xem theo scope; hoàn thành không ngày tương lai |
| 30 | `/care/tasks` | Groom thấy việc theo thời gian/trạng thái; chọn task mở chi tiết/thực hiện; form giao việc theo quyền | Khẩu phần kg có đơn vị; hướng dẫn điều trị đúng phạm vi; giải quyết UX-02 trước mở Manager |
| 31 | `/care/incidents` | Mức độ/tình trạng/đối tượng nhận rõ; báo sự cố ngắn; sau lưu mở ảnh và chi tiết | Quyền báo/chuyển/đóng khác nhau; báo cáo lỗi upload vẫn giữ sự cố đã tạo |
| 32 | `/care/stables` | Chọn chuồng → danh sách/card ô → chi tiết, vệ sinh và lịch sử; sơ đồ chỉ khi có dữ liệu phù hợp | Trống/đang ở và sạch/cần dọn là hai trạng thái; Groom chỉ dọn ô được phép |
| 33 | `/inventory` | Danh sách tồn, thiếu tồn, đơn vị; tạo vật tư qua CTA; lịch sử xem riêng | Không nhầm movement với số dư; chỉ archive tồn 0 và hết yêu cầu mở theo API |
| 34 | `/inventory/issue` | Chọn vật tư, số lượng, đơn vị, tồn dự kiến, lý do; phân biệt nhập của Manager và xuất | Nhập số dương; payload xuất âm; server kiểm lại tồn; không hiển thị tồn dự kiến như đã lưu |
| 35 | `/inventory/replenishments` | Luồng đề nghị → duyệt → nhận một phần/đủ; action rõ theo state | Duyệt chưa tăng tồn; nhận mới tăng; số còn phải nhận đúng; không nhận hai lần khi lỗi mạng |
| 36 | `/reports` | Bộ lọc thời gian nhanh, phạm vi đang áp dụng, KPI có đơn vị/%; chart xu hướng kèm bảng | Dữ liệu cùng filter; giới hạn 413 hướng dẫn thu hẹp; non-Vet không thấy clinical table |
| 37 | `/manager/staff` | Danh sách là trọng tâm; mời qua form riêng; activation/active tách rõ; hồ sơ năng lực dễ đọc | Preview workload trước vô hiệu hóa; emergency có lý do; phân biệt gửi lời mời và đã kích hoạt |
| 38 | `/manager/audit` | Timeline/bảng dễ hiểu, filter search/from/to, detail mở rộng | `ReportingEndpoints` hiện chuyển filter sang `IManagementService`; kiểm kết quả lọc và khoảng ngày bằng browser/API, không nhầm với hàm ListAudit cũ trong ReportingService |
| 39 | `/manager/website` | Editor + preview đúng Home; asset hiện tại và ảnh mới cạnh nhau; trạng thái lưu rõ | Upload ảnh đang ghi ngay phải thể hiện rõ; version conflict không ghi đè âm thầm |
| 40 | `/permission-denied` | Lý do ngắn, quay lại/tổng quan, phân biệt hết quyền sau đổi phân công | Không loop; không lộ dữ liệu tài nguyên bị chặn |
| 41 | `*` | Trang không tìm thấy với quay lại/tổng quan/trang chủ theo session | URL sai không thành trắng màn hình; không giả dữ liệu hoặc nhầm thành 403 |

## 7. Bề mặt không có route riêng cũng phải chỉnh

| Bề mặt | Thay đổi | Ca kiểm tra bắt buộc |
|---|---|---|
| Header, account, logout | Giảm thông tin phụ, nút rõ trên mobile, logout đang xử lý | Tên dài, zoom, session expired, logout server lỗi |
| Sidebar/nhóm menu | Nhóm theo công việc, active path rõ, mobile mở/đóng dễ; cân nhắc nhớ nhóm người dùng mở | 7 vai trò; quyền thay đổi; `/inventory` không active cùng `/inventory/issue` nếu gây nhầm |
| NotificationBell | Nội dung tác vụ + đối tượng + thời gian; thao tác đã đọc, retry rõ | Read-all nhiều trang, pending, item lỗi, đường đích hết quyền, realtime duplicate, mất kết nối |
| Wizard, ảnh và attachments | Progress từng bước; file đã lưu khác file vừa chọn; retry đúng bước | Sai định dạng/kích thước, upload thất bại sau đã tạo nháp, metadata reload lỗi |
| Review admin edits/decision | Dirty state, tóm tắt thay đổi, hành động quyết định có ngữ cảnh | Chỉnh chưa lưu rồi duyệt; revision cần lý do; 409 version |
| AssignmentForm/TrainerCandidates | Tên/chuyên môn/đủ điều kiện/lý do không phù hợp; bàn giao rõ | Manager không giao Trainer; HeadTrainer không thuộc assignment; đổi chuyên môn reset người đã chọn |
| HorseMeasurements/HealthPanel | Tóm tắt ưu tiên, lịch sử thu gọn, đơn vị sát giá trị | Không có ảnh/số đo; clinical summary theo role; archived |
| HorseTrainingAvailability/TrainingAvailability | Chọn slot có tiêu chí rõ, giải thích slot bị chặn, giữ input khi refresh | Time off, thiếu năng lực, trùng lịch, medical lock, response cũ về muộn |
| TemplateCurriculumEditor/assignment | Chia section, preview yêu cầu chuyên môn/bài tập; lý do Trainer chưa đủ chuẩn | Copy mặc định; thiếu required types; assigned nhưng không còn matches |
| Medical create/edit/detail/history/merge | Modal có patient context, section rõ, field error; phân biệt lần khám và phiên bản | Close/Escape/Tab, đang lưu, dirty, chọn hồ sơ bản cũ, merge không mất lịch sử |
| Follow-up/clearance | Checklist các đối tượng sẽ kết thúc, tác động nhìn thấy trước xác nhận | Không chọn gì, đối tượng thuộc bệnh án khác, đã được clearance, constraint còn lại |
| Care record/Incident detail/photos | Chi tiết gần dòng đã chọn; action gọi đúng việc; focus về trigger sau đóng | Ghi khẩu phần, ghi skipped/issue, đổi ngựa không giữ task cũ, ảnh private lỗi |
| Stock movements/archive/review/receive | Đơn vị, hướng dịch chuyển, lượng còn nhận; xác nhận theo đối tượng | 0 tồn, archived, replenishment mở, concurrent receipt |
| Stall occupancy/vacate/cleaning/history | Hành động theo occupancy và canClean; tên ngựa/ô luôn hiện | Ngựa đã có ô khác, chưa chọn, vacant/occupied stale, không có quyền dọn |
| Staff invitation/status/training profile | Form mời, preview workload, hồ sơ năng lực và giờ làm tách nhóm | Gửi lại lời mời, email trùng, staff có booking, hạ chuyên môn làm mất điều kiện |
| Website asset uploads/preview | Hiện ảnh đang dùng, ảnh mới, tỷ lệ khuyến nghị theo renderer, tiến độ và lỗi | Object URL cleanup, sai loại file, xung đột version, hủy phần chữ không giả là hoàn tác ảnh |
| Error/loading/empty/read-only | Một ngôn ngữ thiết kế xuyên module | 401/403/404/409/413/429/network; refetch không trắng trang; empty filter khác chưa có dữ liệu |

## 8. Ràng buộc nghiệp vụ phải giữ khi đổi UX

- Backend là nơi xác nhận quyền và scope. Ma trận FE dùng để trình bày; không thay thế kiểm quyền API. Tài liệu cũ mâu thuẫn code phải được ghi vào decision log, không âm thầm chọn một bản.
- Nháp đăng ký được thiếu thông tin; gửi mới cần đủ field/ảnh/chứng nhận. Không thêm autosave nếu chưa triển khai và kiểm chứng. Approval tạo Horse từ response thật.
- Phân biệt mã hệ thống và số đăng ký ngoài; không dùng UUID thay mã hiển thị mặc định.
- Manager giao HeadTrainer/Groom/Vet; HeadTrainer đang phụ trách giao Trainer. Giáo án mặc định không chỉnh trực tiếp. Lập kế hoạch không đồng nghĩa tự sinh lịch.
- Session có duration/overlap trong code hiện tại. Dùng service scheduling thật; không dựa tài liệu baseline cũ để loại bỏ thời lượng hoặc tự đặt mặc định 30/60 phút.
- HealthStatus, hạn chế vận động và TrainingLock không gộp thành một trạng thái. Hiển thị lý do vận hành được API cấp, giữ quyết định clinical ở đúng vai trò.
- Correction bệnh án giữ lịch sử và thời điểm đánh giá theo guard hiện tại. Clearance chỉ tác động đối tượng lựa chọn hợp lệ. Ngày kết thúc điều trị có thể null.
- Archive không phải xóa lịch sử hoặc xác nhận khỏi bệnh. Giải thích công việc còn mở; không tạo kết quả hoàn thành giả.
- Duyệt bổ sung vật tư không tăng stock; receipt mới tăng stock. Tồn dự kiến chỉ là preview.
- Request ghi timeout không được retry POST tự động. Giữ cơ chế uncertain/đối soát; không dùng toast thành công để che lỗi refresh.
- Lịch/notification/report không được mở rộng quyền clinical. Code `RolePolicy` hiện có quyền clinical cho Manager trong một số API, khác một số tài liệu baseline; phải ghi nhận và chốt với nghiệp vụ trước bất kỳ thay đổi quyền nào.

## 9. Lộ trình triển khai và đầu ra

Ước lượng sơ bộ theo **ngày công**, chưa phải cam kết lịch. Tổng khoảng **28–43 ngày công** cho một người triển khai có hỗ trợ kiểm thử/nghiệp vụ; thay đổi API và UAT thực tế có thể làm tăng. Hoàn tất giai đoạn 0 mới chốt estimate.

| Giai đoạn | Ước lượng | Đầu ra cụ thể | Điều kiện hoàn tất |
|---|---:|---|---|
| 0. Baseline + xác nhận | 2–3 | Screenshot 41 route/bề mặt quan trọng; role/action/field matrix; decision log UX-02/clinical; baseline audit filter; danh sách API dependency | Có tài khoản/fixture phù hợp 7 role; mỗi route có bằng chứng và trạng thái dữ liệu, không dùng dữ liệu thật để tạo tình huống giả |
| 1. Foundation + lỗi chặn | 4–6 | Token, page shell, button/field/filter/resource/table primitives; sửa UX-01; xử lý quyền đã chốt; bộ trang mẫu list/detail/form | Ba mẫu đại diện horse list, session detail và medical form ổn trên mobile/desktop; token không phá public pages |
| 2. Auth + Owner + tiếp nhận | 4–6 | Home, 6 auth routes, dashboard bản đầu, registrations/reviews/horses và bề mặt liên quan | Luồng đăng ký → xác thực → nháp → gửi → revision/approve → horse chạy trọn |
| 3. Training + phân công | 5–8 | Templates/curriculum, assignment/candidates, plans/sessions, availability, kết quả/đánh giá | Luồng giao giáo án → plan → session → Rider → Trainer; ca conflict/lock/history |
| 4. Medical + chăm sóc | 6–9 | 6 route medical workspace, preventive, care tasks, incident/photos; patient context thống nhất | Luồng khám → chấn thương/hạn chế → điều trị → giao việc → tái khám/clearance; đúng scope |
| 5. Kho/chuồng + quản trị | 4–6 | Inventory 3 route, stables, staff, audit, website preview, reports | Nhập/xuất/receipt, occupancy/cleaning, staff workload và CMS version chạy đúng |
| 6. Hoàn thiện + UAT | 3–5 | Bộ ảnh đối chiếu, kiểm accessibility/responsive, browser workflow, tài liệu nghiệm thu | Tất cả route/bề mặt có kết quả pass/fail cụ thể; lỗi P0/P1 đã giải quyết hoặc chặn phát hành |

Thứ tự trong mỗi module: list → detail → create/edit → confirmation → loading/error/empty → mobile → quyền → kiểm thử luồng. Chia commit/PR theo phần có thể kiểm chứng; không đổi hàng loạt CSS cuối file rồi coi như đã sửa toàn hệ thống.

## 10. Backlog kỹ thuật để bắt đầu ngay sau baseline

| Ticket | Nội dung | Phụ thuộc | File/module chính |
|---|---|---|---|
| UI-001 | Baseline screenshot + route/state matrix | Không | AppRoutes, navigation, fixtures |
| UI-002 | Chốt parity quyền/chăm sóc/clinical | UI-001 | permissions, RolePolicy, CareService, tests quyền |
| UI-003 | Sửa treatment status value và regression interaction | Không | MedicalTreatment, medical tests |
| UI-004 | Token semantic, typography, spacing, button | UI-001 | global/workspace/medical/training/ops CSS |
| UI-005 | Shell, sidebar active, header/mobile, breadcrumb | UI-004 | AppLayout, SystemHeader, navigation |
| UI-006 | Resource/empty/error/refetch chung | UI-004 | WorkflowUI, SupportUI, InventoryStableUI |
| UI-007 | Form actions/field summary/dirty protection | UI-004 | Field, AuthInput, SupportForm, wizard/dialog helpers |
| UI-008 | Dashboard role priority + careTasksToday | UI-002/004 | Dashboard, ReportingService contract |
| UI-009 | Home/auth toàn bộ trạng thái | UI-004/007 | Home, pages/auth, components/auth |
| UI-010 | Registration/review/attachments | UI-006/007 | pages/registrations, pages/reviews |
| UI-011 | Horse detail sections/assignment/health | UI-002/006/007 | HorsePages, AssignmentForm, HorseHealthPanel |
| UI-012 | Templates/plans/curriculum | UI-007/011 | TemplatePages, PlanPages, curriculum components |
| UI-013 | Session workflow/availability | UI-012 | SessionPages, TrainingAvailability |
| UI-014 | Medical shell, selectors, records/history | UI-003/006/007 | medical pages/components/services |
| UI-015 | Injury/restriction/treatment/follow-up | UI-014 | medical module, clearance helpers |
| UI-016 | Preventive/care/incidents/photos | UI-002/015 | support pages, permissions, care UI |
| UI-017 | Inventory/issue/replenishment | UI-006/007 | InventoryPage, inventory display, operations components |
| UI-018 | Stables/occupancy/cleaning/history | UI-006/007 | StablePage, inventory-stable CSS |
| UI-019 | Staff/profile/workload/invitation | UI-007/013 | StaffPage, StaffTrainingProfile |
| UI-020 | Audit filters/structured detail | UI-006 + kiểm filter hiện có | AuditPage, managerService, ReportingEndpoints, ManagementService |
| UI-021 | CMS/Home shared preview | UI-009 | WebsitePage, Home, websiteStore |
| UI-022 | Reports/chart/filter scope | UI-006 + contract hiện có | ReportsPage, ReportingService |
| UI-023 | Notifications/error routes/realtime UX | UI-005/006 | NotificationBell, feed, context, 403/404 pages |
| UI-024 | Cross-module UAT và regression | Các ticket trên | 41 route + bề mặt phần 7 |

UI-003 có thể thực hiện riêng ngay; UI-002 cần quyết định quyền cụ thể trước khi bật chức năng. Không gộp việc chốt quyền vào sửa màu/nút.

## 11. Checklist nghiệm thu

**Mỗi màn hình** có: role và scope, route trực tiếp, tiêu đề và hành động chính, dữ liệu bình thường, dữ liệu dài/tên trùng, không dữ liệu, filter không kết quả, loading/refetch, lỗi và retry, read-only/archived nếu áp dụng. Màn hình ghi thêm: validation, double click, timeout/uncertain, 409, response cũ, dirty khi đóng/chuyển trang, thành công và quay lại đúng danh sách.

**Responsive:** kiểm ở 390×844, 768×1024, 1440×900 và vùng desktop rộng; kiểm thêm 320px/reflow và zoom 200% ở bề mặt quan trọng. Không tràn ngang toàn document; bảng dài chỉ cuộn trong vùng riêng có tên và keyboard focus. Action bar/modal không che field, footer, error hoặc keyboard focus.

**Keyboard và khả năng đọc:** Tab/Shift+Tab theo thứ tự, visible focus, label đúng input, required/error/help liên kết, modal trap/restore/Escape, đổi tab/state thông báo phù hợp. Trạng thái không truyền bằng màu đơn độc. Đo contrast bằng công cụ ở baseline và sau đổi token; không khẳng định đã đạt từ việc nhìn mã màu.

**Workflow bắt buộc:**

1. Owner đăng ký/xác thực, tạo nháp thiếu trường, upload, gửi, Manager yêu cầu sửa, Owner gửi lại, Manager duyệt, mở đúng Horse.
2. Manager phân công HeadTrainer/Groom/Vet, HeadTrainer chọn Trainer đúng điều kiện, Trainer lập plan/session, Rider start/result, Trainer evaluate và sửa future session theo ý định đã ghi.
3. Vet khám/correction, injury/restriction, treatment/care, Groom record, Vet follow-up/clearance chọn đối tượng; kiểm restriction còn lại.
4. Groom báo sự cố, upload ảnh, Manager chuyển người nhận, đúng người đóng với resolution.
5. Groom/Manager đề nghị vật tư, Manager duyệt rồi nhận từng phần; xuất kho; concurrent stock/receipt và archive constraint.
6. Manager xếp ngựa vào ô, Groom đúng quyền cập nhật vệ sinh, chuyển/rời ô, lịch sử giữ đúng.
7. Manager mời nhân sự, nhân sự kích hoạt, chỉnh năng lực/giờ làm, xem workload trước disable; xung đột booking có đường xử lý.
8. Report cùng scope/date, notification tới đúng đối tượng, CMS preview/lưu/upload/version conflict, route 403/404 và session hết hạn.

**Kiểm thử code khi bắt đầu sửa:** `npm run lint`, `npm test`, `npm run build` tại frontend. Thêm test tương tác cho filter phác đồ, wizard error navigation, modal focus/dirty, task action theo role/state, calendar nếu triển khai. SSR/unit test hiện tại không thay browser workflow. Một số test đang cố định 6 KPI/dashboard và toàn bộ quick action; khi đổi thiết kế cần cập nhật assertion hành vi có chủ đích, giữ kiểm số liệu/quyền. Không sửa test để hợp thức hóa lệch quyền.

Backend chỉ chạy test liên quan khi đổi contract/quyền/logic. Database test dùng database tạm theo hướng dẫn repository, không chạy migration/seed/write trên Azure để thử UI. Lượt lập kế hoạch này chưa chạy các lệnh test/build vì chưa có thay đổi mã ứng dụng và frontend chưa có dependency cài tại thư mục hiện tại.

## 12. Cách xác nhận hoàn tất toàn bộ đợt

Một route không được đánh dấu xong chỉ vì CSS đã thay. Theo dõi bảng nghiệm thu với cột: route/bề mặt, role, dữ liệu/fixture, trạng thái, viewport, screenshot, test liên quan, kết quả, lỗi còn lại, người xác nhận. Chụp trước/sau với cùng bộ dữ liệu. Mọi quyết định quyền và API bổ sung có người chốt và lý do.

Đầu ra cuối đợt gồm UI đã sửa, token/component được tài liệu hóa, đủ 41 route và bề mặt phần 7 có bằng chứng, luồng liên phân hệ đã kiểm, và danh sách vấn đề còn lại có mức ưu tiên. Không đánh dấu chức năng mới hoặc API dependency là đã hoàn tất khi chỉ có mockup.
