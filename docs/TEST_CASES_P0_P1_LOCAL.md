# Bảng test case P0/P1 — HRCMS local

Môi trường: frontend http://127.0.0.1:5173, API http://localhost:5299, SQL `.\SQLEXPRESS / HRCMS`. Ngày lập: 26/10/2026.

Bảng này dùng để người dùng thực hiện kiểm thử thủ công. **Chưa đánh dấu PASS cho các case UI**. Việc API/login hoặc test tự động đã qua không thay thế kiểm thử UI.

## Tài khoản và dữ liệu

| Username | Vai trò |
|---|---|
| p1.manager | Quản lý |
| p1.owner | Chủ ngựa |
| p1.head | HLV trưởng |
| trainer.an / trainer.bao / trainer.son | HLV |
| p1.rider | Kỵ sĩ |
| p1.vet | Bác sĩ thú y |
| p1.groom | Nhân viên chăm sóc |

Mật khẩu: [file credentials local](../TestResults/P1-local/demo-credentials.json). Không chép mật khẩu vào bảng lỗi hoặc ảnh báo cáo.

Dữ liệu khởi đầu sau seed: 5 giáo án mặc định, 17 mặt hàng có tồn, 3 khu/14 ô chuồng, 10 user gồm 9 tài khoản test và quản lý cũ. Chưa tạo ngựa/lịch tập. Những case yêu cầu ngựa cần chạy quy trình chủ ngựa gửi hồ sơ, quản lý duyệt và phân công trước; nếu chưa có điều kiện thì ghi BLOCKED, không ghi FAIL.

## Cách chạy

- Ghi lại số tồn S trước từng case; không giả định tồn còn đúng số seed sau khi đã test xuất/nhập.
- Chạy kiểm kê ENV/INV-01/STB-01 và DATA-02 trước các case sửa dữ liệu.
- Chuỗi giao giáo án: TPL-03 → TPL-04 → TPL-07. Chạy TPL-05/TPL-06 trên cùng giáo án để kiểm tra hai trường hợp bị chặn.
- Chuỗi nhập kho: INV-05 → INV-06 → INV-07 → INV-08 → INV-09 → INV-10 → INV-11.
- Không cần sửa trực tiếp SQL để vượt qua quy tắc nghiệp vụ. Case gửi API trực tiếp dùng API/Swagger có sẵn nếu muốn kiểm tra thêm.
- Kết quả dùng PASS / FAIL / BLOCKED. Khi FAIL, ghi kết quả thực tế, role, URL, bước tái hiện và ảnh hoặc mã lỗi; không đưa token/password vào ghi chú.
- P0 là lỗi chặn luồng, phân quyền hoặc tính đúng dữ liệu; P1 là yêu cầu còn lại cần nghiệm thu trước deploy.

## Test case

| ID | Ưu tiên | Nhóm | Tài khoản | Nội dung | Bước thực hiện / tiền điều kiện | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|---|---|---|
| ENV-01 | P0 | Môi trường | — | Mở /health trực tiếp và qua frontend | API http://localhost:5299/health; proxy http://127.0.0.1:5173/health | Cả hai trả HTTP 200 và status healthy. | — | Chưa chạy thủ công |
| ENV-02 | P0 | Môi trường | SQL local | Đối chiếu schema HRCMS | Kiểm tra bảng và __EFMigrationsHistory trên .\SQLEXPRESS / HRCMS | 38 bảng nghiệp vụ + 1 bảng migration; 15 migration, mới nhất 20261026070938_LocalizeDefaultTrainingCurricula. | — | Chưa chạy thủ công |
| AUTH-01 | P0 | Đăng nhập | Cả 9 tài khoản | Đăng nhập lần lượt rồi đăng xuất | Dùng tài khoản trong bảng bên dưới và mật khẩu từ file credentials | Đăng nhập thành công; tên và vai trò đúng; menu phù hợp từng vai trò. | — | Chưa chạy thủ công |
| AUTH-02 | P1 | Đăng nhập | p1.owner | Thử truy cập trang quản lý | Sau đăng nhập, nhập trực tiếp URL /manager/staff hoặc /inventory | Không đọc/sửa được dữ liệu quản lý; API từ chối quyền. UI có thông báo hoặc điều hướng phù hợp. | — | Chưa chạy thủ công |
| AUTH-03 | P1 | Đăng nhập | p1.manager | Đăng xuất rồi tải lại trang được bảo vệ | Đăng xuất; truy cập lại /inventory | Không còn truy cập dưới phiên cũ; yêu cầu đăng nhập lại. | — | Chưa chạy thủ công |
| FONT-01 | P1 | Font | p1.manager, p1.vet, p1.head | Kiểm tra font ở kho, y tế, giáo án | Quan sát chữ có dấu; dùng DevTools xem computed font của tiêu đề, nội dung, input | Tiêu đề dùng HRCMS Lora; nội dung/control dùng HRCMS Manrope; không mất dấu hoặc cắt chữ. | — | Chưa chạy thủ công |
| FONT-02 | P1 | Font | p1.vet | Mở form ghi nhận thăm khám | Cần có ngựa đã duyệt và được phân công cho bác sĩ; mở form nhưng không lưu | Font đồng nhất với trang; label, dấu *, input, nút và tiêu đề đọc rõ; không đè nhau. | — | Chưa chạy thủ công |
| FONT-03 | P1 | Responsive | Các vai trò tương ứng | Kiểm tra login, y tế, kho, giáo án ở 390×844 và 1440×1000 | Thu/phóng viewport về hai kích thước; cuộn bảng/modal | Không tràn ngang toàn trang; bảng cuộn trong vùng riêng; modal cuộn được và thao tác đóng/lưu vẫn tiếp cận được. | — | Chưa chạy thủ công |
| TPL-01 | P1 | Giáo án | p1.head | Xem bộ giáo án mặc định | Mở Giáo án mẫu | Có 5 giáo án mặc định; hiển thị yêu cầu chuyên môn và cấp tối thiểu. | — | Chưa chạy thủ công |
| TPL-02 | P1 | Giáo án | p1.head | Kiểm tra Việt hóa nội dung mẫu | Xem mục tiêu và mở lựa chọn bài tập của giáo án chạy ổn định/tốc độ | Nhãn hiển thị đã Việt hóa; không còn Canter/Gallop/Trainer trong nội dung tự do đã sửa. Enum và technical key trong payload vẫn được giữ. | — | Chưa chạy thủ công |
| TPL-03 | P0 | Chuyên môn HLV | p1.head | Mở Giao giáo án cho Duy trì nhịp chạy ổn định | Giáo án steady-canter, yêu cầu Canter cấp tối thiểu 2 | Trần Gia Bảo: Đủ điều kiện; Nguyễn Minh An: Thiếu chuyên môn; Lê Hoàng Sơn: Chưa đủ cấp độ. | — | Chưa chạy thủ công |
| TPL-04 | P0 | Chuyên môn HLV | p1.head | Giao giáo án cho trainer.bao | Ở bảng ứng viên, chọn Bảo rồi xác nhận giao | Giao thành công; tải lại vẫn thấy đã giao; không tạo bản ghi phân công trùng khi thao tác lại. | — | Chưa chạy thủ công |
| TPL-05 | P0 | Chuyên môn HLV | p1.head | Thử chọn trainer.an | Ở cùng giáo án steady-canter | Ứng viên bị vô hiệu hóa/không được chọn; nếu gửi trực tiếp API thì bị từ chối; không phát sinh assignment. | — | Chưa chạy thủ công |
| TPL-06 | P0 | Chuyên môn HLV | p1.head | Thử chọn trainer.son | Ở cùng giáo án steady-canter | Báo chưa đủ cấp độ; không cho giao; API cũng từ chối nếu gửi trực tiếp. | — | Chưa chạy thủ công |
| TPL-07 | P1 | Bảo toàn điều kiện | p1.manager | Bỏ Canter khỏi profile của Bảo sau khi đã giao giáo án | Chạy sau TPL-04; giữ nguyên giờ làm/nghỉ, chỉ thử bỏ chuyên môn cần thiết | Thao tác bị từ chối do làm mất điều kiện giáo án; profile cũ và assignment được giữ. | — | Chưa chạy thủ công |
| TPL-08 | P1 | Giáo án | p1.head | Tạo bản riêng từ giáo án mặc định | Đặt tên QA-P1-GiaoAn-01, sửa nội dung bản riêng, lưu rồi tải lại | Có bản riêng với ID khác; bản mặc định giữ nguyên; nhãn có dấu lưu/đọc đúng. | — | Chưa chạy thủ công |
| INV-01 | P0 | Kho | p1.manager | Kiểm kê dữ liệu khởi đầu | Chạy trước các case làm thay đổi tồn | Có 17 mặt hàng, đúng tên/nhóm/đơn vị; cả 17 có tồn demo lớn hơn 0. | — | Chưa chạy thủ công |
| INV-02 | P0 | Kho | p1.groom | Xuất 2 kg yến mạch | Ghi lại tồn trước thao tác S; mở Xuất vật tư, chọn yến mạch, nhập 2 và lý do QA-P1-Xuat-01 | Tồn còn S−2; có đúng 1 movement xuất tương ứng; tải lại vẫn giữ đúng số. | — | Chưa chạy thủ công |
| INV-03 | P0 | Kho | p1.groom | Xuất vượt tồn | Nhập số lượng lớn hơn tồn hiện tại | Bị chặn; tồn không âm; không có movement hoặc audit thành công dở dang. | — | Chưa chạy thủ công |
| INV-04 | P1 | Kho | p1.groom | Nhập số lượng xuất bằng 0 | Chọn vật tư và nhập 0 | Có validation hoặc API từ chối; không thay đổi tồn/lịch sử. | — | Chưa chạy thủ công |
| INV-05 | P0 | Bổ sung kho | p1.groom | Tạo yêu cầu bổ sung 5 kg yến mạch | Ghi tồn hiện tại S; nhập ghi chú QA-P1-Nhap-01 | Yêu cầu chờ duyệt; tồn vẫn là S. | — | Chưa chạy thủ công |
| INV-06 | P0 | Bổ sung kho | p1.manager | Duyệt yêu cầu vừa tạo | Chạy sau INV-05; ghi lại tồn trước duyệt | Yêu cầu được duyệt; tồn chưa tăng. | — | Chưa chạy thủ công |
| INV-07 | P0 | Nhận hàng | p1.manager | Nhận một phần 3 kg | Chạy sau INV-06; lý do QA-P1-Nhan-01 | Tồn tăng đúng 3; ReceivedQuantity=3; trạng thái nhận một phần; movement liên kết đúng yêu cầu. | — | Chưa chạy thủ công |
| INV-08 | P0 | Nhận hàng | p1.manager | Thử nhận vượt phần còn lại | Sau INV-07 còn 2 kg; thử nhận 3 kg | Bị từ chối; tồn và ReceivedQuantity không đổi. | — | Chưa chạy thủ công |
| INV-09 | P0 | Nhận hàng | p1.manager | Nhận nốt 2 kg | Chạy sau INV-08; lý do QA-P1-Nhan-02 | Tổng đã nhận=5; trạng thái đã nhận đủ; tổng tồn tăng đúng 5 so với trước khi nhận. | — | Chưa chạy thủ công |
| INV-10 | P1 | Nhận hàng | p1.manager | Thử nhận thêm trên yêu cầu đã đủ | Chạy sau INV-09 | Không nhận thêm được; không cộng tồn hoặc tạo movement trùng. | — | Chưa chạy thủ công |
| INV-11 | P0 | Lịch sử kho | p1.manager | Đối chiếu tồn và movement | Xem lịch sử yến mạch; đối chiếu SQL hoặc báo cáo API nếu cần | Tồn hiện tại bằng tổng movement; đúng dấu nhập/xuất, số lượng, người thực hiện và phiếu nhận. | — | Chưa chạy thủ công |
| STB-01 | P1 | Chuồng | p1.manager | Kiểm kê chuồng ban đầu | Xem lần lượt Khu A, Khu B, Khu theo dõi sức khỏe | 3 khu; A01–A06, B01–B06, TD01–TD02; tổng 14 ô; dữ liệu khởi đầu cần vệ sinh. | — | Chưa chạy thủ công |
| STB-02 | P1 | Chuồng | p1.manager | Đánh dấu A01 đã vệ sinh | Thao tác trong dữ liệu local demo; tải lại danh sách và xem lịch sử | A01 chuyển sạch và có ghi nhận thao tác; các ô khác không bị thay đổi. | — | Chưa chạy thủ công |
| STB-03 | P0 | Chuồng | p1.manager | Thử bố trí ngựa vào ô chưa vệ sinh | Cần có ngựa đã được duyệt, đủ điều kiện boarding; chọn ô khác A01 còn bẩn | Bị chặn; không phát sinh occupancy mở cho ô đó. | — | Chưa chạy thủ công |
| STB-04 | P0 | Chuồng | p1.manager | Bố trí ngựa vào ô sạch rồi thử bố trí thêm | Cần hai ngựa hợp lệ; dùng A01 đã sạch | Ngựa đầu được bố trí; ngựa thứ hai không vào được ô đang sử dụng; không có hai occupancy mở trên cùng ô. | — | Chưa chạy thủ công |
| NTF-01 | P0 | Thông báo | p1.manager | Mở chuông có thông báo | Có thông báo phát sinh từ các case bổ sung kho | Không có nút đã đọc riêng từng dòng; có nút Đánh dấu tất cả đã đọc; thông báo mới phân biệt bằng chấm/trạng thái hiển thị. | — | Chưa chạy thủ công |
| NTF-02 | P0 | Thông báo | p1.manager | Bấm một thông báo mới rồi tải lại | Ghi lại số chưa xem trước khi bấm | Server xác nhận đã xem; mở đúng màn hình phù hợp; trạng thái vẫn được giữ sau tải lại. | — | Chưa chạy thủ công |
| NTF-03 | P1 | Thông báo | p1.manager | Giả lập mạng chậm khi tải thông báo | DevTools Network throttling; tải lại trang | Có trạng thái đang tải; chưa có dữ liệu thì không hiển thị số 0 hoặc thông báo danh sách rỗng giả. | — | Chưa chạy thủ công |
| NTF-04 | P0 | Đọc tất cả | p1.manager | Đánh dấu tất cả khi có nhiều trang | Cần hơn 10 thông báo mới; bấm nút một lần | Toàn bộ thông báo hiện có của tài khoản được đánh dấu, kể cả trang chưa tải; số mới và dashboard được cập nhật theo server. | — | Chưa chạy thủ công |
| NTF-05 | P1 | Đọc tất cả | p1.manager | Thử bấm khi đang xử lý hoặc không có thông báo mới | Bấm liên tục lúc request đang chạy; sau thành công thử lại | Nút bị vô hiệu hóa, không gửi lặp; khi hết thông báo mới thì badge biến mất và nút bị vô hiệu hóa. | — | Chưa chạy thủ công |
| NTF-06 | P1 | Đọc tất cả | p1.manager | Giả lập lỗi mạng khi bấm | DevTools chặn POST /api/notifications/read-all rồi bấm nút | Có thông báo lỗi; không tự đưa count về 0; có thể tải lại để kiểm tra và thử lại. | — | Chưa chạy thủ công |
| NTF-07 | P0 | Phân quyền thông báo | Hai tài khoản | Kiểm tra phạm vi thao tác | Ghi số thông báo mới của B; đăng nhập A và đánh dấu tất cả; xem lại B | Chỉ A bị thay đổi; B giữ nguyên. API không cho truyền recipient để thao tác thay tài khoản khác. | — | Chưa chạy thủ công |
| NTF-08 | P1 | Thông báo mới | p1.manager | Nhận thông báo mới sau khi đánh dấu tất cả | Hoàn tất NTF-04; tạo sự kiện phát sinh thông báo mới rồi tải lại | Thông báo phát sinh sau đó vẫn là mới; thao tác trước không đánh dấu tự động các sự kiện tương lai. | — | Chưa chạy thủ công |
| DATA-01 | P1 | Công cụ dữ liệu | Người vận hành | Chạy lại seeder tài khoản P1 | Dùng lệnh trong LOCAL_SQL_SETUP.md; không chọn reset password | Tạo 0 tài khoản mới; giữ nguyên user, password và số lượng. | — | Chưa chạy thủ công |
| DATA-02 | P1 | Công cụ dữ liệu | Người vận hành | Preview lại batch demo P1 | Chạy trước TPL-04 và trước khi tự sửa profile; dùng runbook, đúng SQL local | updatedProfiles=0, receivedItems=0; không ghi thay đổi. Sau xuất kho cũng không tự bù lại tồn đã tiêu thụ. | — | Chưa chạy thủ công |
| DATA-03 | P1 | Bảo toàn dữ liệu | Người vận hành | Tải lại trang và khởi động lại backend | Sau khi hoàn tất các case ghi dữ liệu | Giáo án riêng, profile hợp lệ, receipt, tồn kho và occupancy đã lưu không mất; backend vẫn dùng HRCMS local. | — | Chưa chạy thủ công |


## Ghi nhận kiểm chứng trước khi bàn giao

Đã kiểm tra ở lượt chuẩn bị dữ liệu: 9/9 tài khoản đăng nhập API đúng role; API inventory có 17 mặt hàng; ứng viên steady-canter trả đúng Eligible / MissingSpecialty / BelowMinimumLevel; không lệch tồn so với movement; seeder chạy lại tạo 0 tài khoản và demo operations preview không nhập lặp. Đây là bằng chứng kiểm tra chuẩn bị, không phải kết quả người dùng chạy toàn bộ bảng trên.

## Mẫu ghi lỗi

| Mã lỗi | Test case | Vai trò | URL | Bước tái hiện | Kết quả thực tế | Kỳ vọng | Ảnh/log đã che secrets | Mức độ |
|---|---|---|---|---|---|---|---|---|
| BUG-001 |  |  |  |  |  |  |  |  |
