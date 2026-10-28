# Kế hoạch tách mã giấy tờ và mã ngựa trong hệ thống

Ngày: 28/10/2026. Tài liệu kế hoạch và tiến độ triển khai tách hai loại mã đăng ký ngựa.

Tiến độ triển khai: đã bổ sung API rõ nghĩa, chuyển frontend sang trường mới và thêm tìm kiếm mã giấy tờ. Giữ tên cột/property lưu trữ cũ thay vì đổi schema; các property API mới không được EF map. Local có 0 ngựa nên không cần chuyển dữ liệu. Chi tiết tương thích/phát hành tại `contracts/horse-identifiers.md`; script đối soát tại `tools/Audit-HorseRegistrationIdentifiers.sql`. Chưa triển khai lên Azure.

## 1. Định nghĩa nghiệp vụ đề xuất

| Thuộc tính | Mã đăng ký trên giấy tờ | Mã ngựa trong hệ thống |
| --- | --- | --- |
| API đích | externalRegistrationNumber | systemRegistrationCode |
| Nguồn | Chủ ngựa khai theo giấy tờ, quản lý đối chiếu | Backend tự cấp |
| Ví dụ | Giá trị đúng theo giấy tờ thực tế | HRC-000001 |
| Thời điểm | Lưu từ hồ sơ nháp | Cấp khi duyệt và tạo Horse |
| Bắt buộc | Giữ tùy chọn như hiện tại; thiếu hiển thị “Chưa cung cấp” | Bắt buộc với ngựa đã được duyệt |
| Quyền sửa | Giữ quyền/vòng đời sửa hồ sơ hiện tại | Không cho client nhập hoặc sửa |
| Tính duy nhất | Chưa áp unique toàn hệ thống khi chưa có thông tin cơ quan cấp | Duy nhất toàn hệ thống, không tái sử dụng |
| Hiển thị | Thông tin nhận diện/giấy tờ trong hồ sơ ngựa | Danh sách, lựa chọn ngựa, nghiệp vụ và một dòng riêng trong hồ sơ |

Giữ Horse.Id (GUID) làm khóa liên kết và URL. RegistrationId là khóa hồ sơ đăng ký; không dùng thay mã ngựa. Không thêm loại mã thứ ba cho đơn đăng ký trong phạm vi này.

Giả định: giữ thời điểm cấp mã nội bộ khi duyệt, không cấp từ lúc tạo nháp; “đăng ký với hệ thống” ở đây là ngựa được tiếp nhận chính thức. Mã giấy tờ tiếp tục không bắt buộc. Nếu cần cấp mã từ khi gửi đơn hoặc bắt buộc giấy tờ, đó là thay đổi nghiệp vụ riêng cần chốt.

## 2. Hiện trạng đã kiểm tra trong mã nguồn

- HorseRegistration.RegistrationNumber lưu mã bên ngoài do chủ ngựa khai; RegistrationDraftRequest/RegistrationRequest dùng cùng tên này.
- HorseRegistrationService.Review tạo Horse và gọi NextHorseRegistrationNumberAsync để cấp HRC-xxxxxx vào Horse.RegistrationNumber.
- RegistrationRepository sử dụng SQL sequence; model có unique index trên Horses.RegistrationNumber.
- HorseRepository.GetDetailAsync đã lấy hồ sơ đăng ký liên quan; không cần tạo thêm một bản sao mã giấy tờ trên Horse chỉ để hiển thị.
- HorseRepository.ListAsync hiện tìm theo tên và Horse.RegistrationNumber, chưa tìm mã giấy tờ ở hồ sơ đăng ký.
- Frontend RegistrationForm/registrationWizard gọi trường nhập là mã bên ngoài; HorsePages hiển thị Horse.RegistrationNumber dưới nhãn chung “Mã đăng ký”, cả trên danh sách và hồ sơ.
- careUi.js và MedicalExamination.jsx có fallback giữa registrationNumber/identifier; cần rà soát để không đánh đồng hai loại mã.
- Migration 20261028100000_HorseRegistrationNumberCodes giữ giá trị cũ không rỗng, không trùng, dài <=100 ở Horses.RegistrationNumber, kể cả giá trị nhập tay không phải mã hệ thống. Đây là khả năng dữ liệu bị lẫn, chưa phải kết luận về database đang chạy.

## 3. Hướng kỹ thuật

### Backend và hợp đồng API

1. Dùng tên nghiệp vụ rõ trong code: Horse.SystemRegistrationCode và HorseRegistration.ExternalRegistrationNumber. Có thể map tới cột RegistrationNumber cũ của từng bảng để giảm thay đổi vật lý; không đổi tên cột trực tiếp trong lần phát hành đầu.
2. Response DTO cho danh sách/chi tiết ngựa trả rõ systemRegistrationCode và externalRegistrationNumber; chi tiết lấy mã giấy tờ từ hồ sơ đăng ký đã duyệt. Không trả thêm toàn bộ dữ liệu riêng tư của hồ sơ chỉ để lấy một mã.
3. Request tạo/sửa hồ sơ nhận externalRegistrationNumber; không nhận trường mã nội bộ. Giữ validation độ dài hiện có, trim khoảng trắng đầu/cuối, chuyển chuỗi trống thành null, giữ số 0 đầu và ký tự hợp lệ theo giấy tờ.
4. Giai đoạn tương thích: registrationNumber cũ trên response Horse vẫn có nghĩa mã nội bộ; trên hồ sơ đăng ký vẫn có nghĩa mã giấy tờ. Giữ alias này trong DTO, đánh dấu deprecated trong OpenAPI. Request cũ vẫn đọc được; nếu gửi cả hai tên với giá trị khác nhau thì trả lỗi rõ ràng.
5. Đổi tên helper/repository sinh mã cho đúng nghĩa; giữ sequence, unique index và nguyên tắc mã nội bộ không đổi khi đổi tên ngựa, phân công, lưu trữ hoặc sửa giấy tờ.
6. Tìm kiếm hỗ trợ tên, mã nội bộ và mã giấy tờ, giữ phân quyền/phạm vi, phân trang và tránh truy vấn riêng theo từng ngựa.
7. Cập nhật audit, thông báo, OpenAPI và tài liệu hợp đồng để thể hiện đúng loại mã.

### Frontend

1. Form đăng ký chỉ nhập “Mã đăng ký trên giấy tờ (nếu có)”; hướng dẫn “Nhập đúng theo giấy tờ của ngựa”. Không có ô nhập mã hệ thống.
2. Hồ sơ ngựa có hai dòng riêng: “Mã ngựa trong hệ thống” và “Mã đăng ký trên giấy tờ”; thiếu mã giấy tờ hiển thị “Chưa cung cấp”. Tuyệt đối không dùng mã nội bộ thay thế khi mã giấy tờ trống.
3. Danh sách và lựa chọn ngựa dùng mã hệ thống làm mã nhận diện vận hành; nhãn tìm kiếm nêu rõ hỗ trợ cả hai mã. Không bắt buộc thêm cột giấy tờ vào mọi bảng.
4. Trang duyệt, bản tóm tắt hồ sơ, y tế, chăm sóc, phân công và các màn hình còn dùng mã phải lấy đúng trường. Loại bỏ fallback đánh đồng mã giấy tờ với mã hệ thống.
5. Cập nhật catalog ngôn ngữ và kiểm tra các vai trò đang có quyền xem hồ sơ.

## 4. Đối soát và chuyển dữ liệu cũ

1. Đọc trạng thái migration thực tế trên từng môi trường trước khi quyết định script; không sửa migration đã phát hành.
2. Xuất báo cáo đối chiếu Horse.Id, RegistrationId, mã trên Horse và mã trên HorseRegistration; lưu bản chụp dữ liệu trước khi chuyển đổi.
3. Dữ liệu mới đã tách đúng: giữ cả hai giá trị, không cấp lại mã.
4. Ngựa thiếu mã nội bộ: cấp qua sequence, tránh mã đã tồn tại, lưu ánh xạ trước/sau để truy vết. Chạy lại không tạo mã khác.
5. Giá trị cũ có dấu hiệu lẫn mã: đối chiếu hồ sơ, audit và nguồn dữ liệu. Không chỉ dựa vào tiền tố HRC- để kết luận là mã nội bộ hay mã giấy tờ.
6. Không ghi đè mã giấy tờ đang có hoặc xóa mã cũ khi chưa xác định nguồn. Đưa các trường hợp mâu thuẫn vào danh sách cần xử lý; nếu phải đổi mã vận hành thì giữ lịch sử mã cũ và khả năng tra cứu.
7. Không thể tự phục hồi giá trị từng bị migration cũ xóa nếu không còn trong hồ sơ/audit/bản sao lưu; báo cáo rõ trường hợp này.
8. Sau đối soát mới áp ràng buộc bắt buộc mã nội bộ phù hợp với dữ liệu thực tế. Không tạo unique toàn cục cho mã giấy tờ trong phạm vi hiện tại.

## 5. Thứ tự triển khai

1. Chốt thuật ngữ và giả định tại mục 1; kiểm tra dữ liệu và lập danh sách ảnh hưởng backend/frontend.
2. Backend bổ sung hợp đồng API rõ nghĩa và tương thích cũ; giữ hành vi sinh mã. Viết kiểm thử cùng thay đổi.
3. Chuyển dữ liệu cần thiết bằng migration/script mới đã kiểm thử trên database thử nghiệm, có ánh xạ và khả năng chạy lại.
4. Phát hành backend tương thích trước; sau đó cập nhật frontend dùng trường mới và hai nhãn riêng.
5. Kiểm tra tích hợp từ tạo nháp → gửi duyệt → yêu cầu sửa → duyệt → xem hồ sơ → tìm kiếm.
6. Triển khai staging và đối soát; triển khai production theo quy trình phát hành của dự án, xác minh migration và bản frontend/backend thực tế.
7. Chỉ loại bỏ alias cũ trong đợt sau khi mọi client đã chuyển. Khi cần quay lại bản ứng dụng cũ, giữ cột/alias tương thích; không chạy Down để xóa dữ liệu mã đã cấp.

## 6. Tiêu chí nghiệm thu

- Mã giấy tờ chủ ngựa nhập được giữ nguyên ý nghĩa sau duyệt và hiển thị trong hồ sơ ngựa.
- Mã hệ thống được cấp riêng, duy nhất, không bị người dùng ghi đè; duyệt đồng thời không cấp trùng và không tạo nhiều Horse cho cùng hồ sơ.
- Ngựa không có mã giấy tờ vẫn có mã hệ thống; giao diện hiển thị đúng trạng thái thiếu giấy tờ.
- Các mã khác nhau không bị fallback thay thế lẫn nhau; tìm theo cả hai mã trả đúng ngựa trong phạm vi được phép.
- Client cũ vẫn hoạt động trong giai đoạn chuyển tiếp; kiểm thử cả request alias và trường hợp hai tên mâu thuẫn.
- Kiểm thử dữ liệu cũ: mã giấy tờ không mang tiền tố HRC, mã giấy tờ tình cờ có tiền tố HRC, mã nội bộ thiếu, giá trị trùng/mâu thuẫn và chạy lại script.
- Xác minh liên kết buổi tập, y tế, chăm sóc và lịch sử vẫn giữ đúng Horse.Id.
- Backend chạy kiểm thử intake/registration/search/concurrency liên quan; frontend chạy kiểm thử form/hồ sơ/tìm kiếm và build, kèm kiểm tra giao diện hai mã.

## 7. Điểm triển khai chính

Backend: Horse.cs, HorseRegistration.cs, RegistrationDraftRequest.cs, RegistrationRequest.cs, HorseRegistrationService.cs, RegistrationRepository.cs, HorseRepository.cs, ClubModelConfiguration.cs, DTO phản hồi liên quan, OpenAPI, migration mới nếu cần và IntakeTests.cs.

Frontend: src/pages/horses/HorsePages.jsx, src/components/registrations/RegistrationForm.jsx, src/constants/registrationWizard.js, services/validation và trang duyệt hồ sơ, src/services/careUi.js, src/pages/medical/MedicalExamination.jsx, các màn y tế/chăm sóc dùng registrationNumber và catalog thông báo.
