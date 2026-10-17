# Dữ liệu thật cho Flow 3 và Flow 4

Thu thập ngày 13/10/2026. Bộ JSON: [flow34-public-reference-2026-10-13.json](data/flow34-public-reference-2026-10-13.json).

## Flow 3

Đã chọn 8 quan sát thú y công khai của ngựa thật từ [HKJC Veterinary Records Database](https://racing.hkjc.com/en-us/local/information/ovedatabase). Mỗi dòng có mã ngựa, tên, ngày, diễn giải tiếng Việt và ngày `Passed On` khi nguồn cung cấp. Đây là trích chọn, không phải toàn bộ database. Trang mở có ngày dữ liệu 10/10, dù kết quả tìm kiếm hiển thị 13/10; JSON giữ ngày của trang mở.

Giữ nguyên ý nghĩa trường nguồn. Không chuyển `Passed On` thành clearance trong HRCMS. Không biết bác sĩ, giờ khám, chỉ định, liều thuốc hoặc HealthStatus hiện tại: để null. Không tạo bệnh án hoặc giao việc Groom dưới tên một nhân viên CLB từ các quan sát này.

Nguồn bổ sung: [CHRB Veterinary](https://www.chrb.ca.gov/veterinary.html) có danh sách thú y và báo cáo sự cố; [HKJC quy trình và thuật ngữ thú y](https://www.hkjc.com/english/racing/ove_intro.asp) hỗ trợ diễn giải. Các nguồn này được phát hiện nhưng chưa trích xuất thành bản ghi trong bộ JSON.

## Flow 4

Đã thu thập 4 sản phẩm có thật từ nhà sản xuất: [Purina](https://www.purinamills.com/horse-feed/products/detail/purina-strategy-healthy-edge-horse-feed), [Standlee](https://www.standleeforage.com/products/alfalfa-pellets/), [Zoetis Core](https://www.zoetisequine.com/products/vaccines/core-eq-innovator), [Zoetis Fluvac](https://www.zoetisequine.com/products/vaccines/fluvac-innovator). Dùng cho danh mục tham chiếu; chưa biết CLB có mua, tồn bao nhiêu, giá, lô, hạn dùng hay ngưỡng tồn.

[Infographic chính thức Conghua](https://crc.hkjc.com/-/media/Sites/CRC/PDF/HKJCConghuaInfographicEng) cho số khối chuồng và số ô. Tổng 664 là phép tính từ cấu hình được công bố, không phải số ngựa đang lưu trú. Không có tên từng ô, lịch ngựa vào/rời hoặc nhật ký vệ sinh.

## Đưa vào dự án

- Lưu thư viện tham chiếu riêng với URL, ngày thu thập, mã ngoài và trường chưa biết null. Sau yêu cầu thêm vào database, đã nạp 13 bản ghi vào `dbo.PublicReferenceRecords` trên Azure HRCMS; xem [hướng dẫn nhập và truy vấn](PUBLIC_REFERENCE_IMPORT_2026-10-13.md).
- Bệnh án vận hành cần bản xuất từ phòng thú y: mã ngựa, bác sĩ, ngày giờ khám, đánh giá, điều trị và tái khám được xác nhận.
- Chăm sóc cần nhật ký phân công và ghi nhận thực tế của Groom.
- Chuồng cần danh sách ô thực tế, sổ vào/rời và vệ sinh.
- Kho cần kiểm kê, hóa đơn/phiếu nhập, phiếu xuất, lô và đơn vị đo; báo cáo tổng hợp từ các bản ghi đó.
- Không coi quyền xem trang công khai là giấy phép sao chép hàng loạt. Kiểm tra quyền sử dụng trước khi phân phối dataset hoặc khai thác tự động ở quy mô lớn.

Sản phẩm vaccine nước ngoài chỉ được ghi nhận tồn tại trong danh mục nguồn; không khẳng định được đăng ký/lưu hành tại Việt Nam và không dùng để tự tạo phác đồ điều trị.
