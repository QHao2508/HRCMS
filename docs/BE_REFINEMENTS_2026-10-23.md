# Cập nhật Backend ngày 23 tháng 10 năm 2026

## Phạm vi và quyền đã chốt

ClubManager xem thông tin được phép, quản lý tài khoản và chuyên môn nhân viên, duyệt hồ sơ và phân công HeadTrainer, Groom, Veterinarian phụ trách ngựa. Chỉ HeadTrainer đang phụ trách ngựa được giao Trainer. Manager không còn ngoại lệ giao Trainer bằng cách nhập lý do.

Theo xác nhận bổ sung, Manager **vẫn giao Feeding, Cleaning, Bathing, Grooming và WaterCheck**. Veterinarian giao Treatment và IceBath. Groom thực hiện công việc được giao. Các API tạo/sửa/lưu trữ/giao giáo án vẫn chỉ dành cho HeadTrainer; lập/sửa kế hoạch và buổi tập, giao Rider, đánh giá thuộc Trainer; bắt đầu và báo cáo kết quả thuộc Rider được giao. Backend kiểm tra quyền kể cả khi người dùng gọi trực tiếp API.

## Hợp đồng dành cho Frontend

### Tên chủ ngựa

`GET /api/registrations` và `GET /api/registrations/{id}` bổ sung `ownerName`, cùng cấp với `ownerId`. Giữ shape hiện tại và phân trang `{items,page,pageSize,total}`. Tên lấy từ Users bằng truy vấn theo lô cho trang danh sách, không lặp truy vấn từng hồ sơ. Không trả email, mật khẩu hay thông tin định danh khác của chủ ngựa. Kiểm tra phạm vi trước khi trả chi tiết.

`ownerName` chỉ được bổ sung khi đọc response, không lưu thêm cột; không cần migration.

### Điều kiện giao giáo án

`GET /api/training/templates/{id}/trainer-candidates` giữ các trường `id`, `name`, `matches`, `specialties`, `assigned` và bổ sung:

- `requiredTrainingTypes`: các chuyên môn bắt buộc của giáo án.
- `minimumExpertiseLevel`: cấp tối thiểu, 1 Cơ bản, 2 Thành thạo, 3 Chuyên sâu.
- `missingSpecialties`: chuyên môn chưa có trong hồ sơ Trainer.
- `belowLevelSpecialties`: đã có chuyên môn nhưng chưa đạt cấp tối thiểu.
- `eligibility`: `Eligible`, `MissingSpecialty`, `BelowMinimumLevel` hoặc `RequirementsMissing`.

`matches` chỉ true khi đủ tất cả chuyên môn và cấp tối thiểu. `assigned` độc lập với việc đang phù hợp. FE dùng các trường này để giải thích lý do, không tự nâng năng lực. Sửa hồ sơ chuyên môn không được làm mất điều kiện của giáo án đang giao: thu hồi giáo án hoặc bàn giao lịch trước khi giảm chuyên môn/cấp độ.

### Thông báo

`GET /api/notifications` bổ sung `messageCode` và `displayMessage` cho thông báo đã nhận diện. `displayMessage` có nội dung tiếng Việt rõ công việc cần làm; FE ưu tiên hiển thị trường này. Với thông báo không nhận diện, giữ nguyên nội dung gốc. `message`, `type`, `referenceId`, `read`, phân trang và phạm vi người nhận được giữ tương thích. Thông báo cũ không cần sửa hàng loạt.

Nhãn/nút đánh dấu đã đọc, font và bố cục thông báo thuộc task FE. Bản FE đang deploy cần được cập nhật để dùng `displayMessage`.

## Công cụ dữ liệu vận hành

Project `tools/HorseClub.OperationalData` tách khỏi migration và startup. Manifest `docs/data/operational-catalog.proposed.json` chứa tên, nhóm, đơn vị, URL nguồn, sơ đồ chuồng và danh sách đổi tên tùy chọn.

Chạy mặc định chỉ xem trước; chỉ ghi khi có `--apply`, connection phải khớp server/database truyền tường minh. Connection lấy từ biến môi trường riêng `HRCMS_DATA_CONNECTION`; không đặt mật khẩu trên command line hoặc commit source. Công cụ không migrate, không tạo database, không thay đổi cấu hình BE.

```powershell
dotnet run --project tools/HorseClub.OperationalData -c Release -- docs/data/operational-catalog.proposed.json 'tcp:hrcms.database.windows.net,1433' HRCMS-free
# Sau khi kiểm tra bản xem trước và được phép áp dụng
dotnet run --project tools/HorseClub.OperationalData -c Release -- docs/data/operational-catalog.proposed.json 'tcp:hrcms.database.windows.net,1433' HRCMS-free --apply
```

Công cụ dùng transaction Serializable, chạy lại không tạo trùng. Mặt hàng trùng tên nhưng khác nhóm/đơn vị, đã lưu trữ hoặc không xác định duy nhất sẽ bị từ chối. Đổi tên phải chỉ rõ ID và tên trước thay đổi; giữ nguyên ID, đơn vị, tồn kho và liên kết lịch sử. Không đổi tên QA sang một thuốc/thức ăn cụ thể khi chưa xác minh hàng đó là gì.

Mặt hàng mới có tồn kho và tồn tối thiểu bằng 0; chưa xác nhận có hàng thực tế. Không thêm số lô, hạn dùng, liều dùng hoặc movement giả. Ô chuồng mới chưa bố trí ngựa và có trạng thái NeedsCleaning để yêu cầu kiểm tra/vệ sinh; tên khu/ô là sơ đồ được người dùng đồng ý, không khẳng định đã khảo sát cơ sở.

### Kết quả đã áp vào HRCMS free

Ngày 23/10/2026, theo xác nhận của người dùng, đã thêm 6 mặt hàng, 3 khu và 14 ô chuồng vào `hrcms.database.windows.net / HRCMS-free`. Bản xem trước sau áp dụng cho kết quả 0 bản ghi mới. Không sửa database HRCMS cũ.

- Cỏ khô Timothy và Alfalfa; yến mạch hạt; bã củ cải đường khô.
- Vắc xin ProteqFlu-Te; Eqvalan ivermectin dạng uống cho ngựa. Đây là tên sản phẩm được tham khảo từ nguồn quản lý/nhà chuyên môn, không phải chỉ định điều trị hoặc xác nhận đã được cấp phép/mua tại câu lạc bộ ở Việt Nam.
- Khu chuồng A gồm A01 đến A06; khu B gồm B01 đến B06; khu theo dõi sức khỏe gồm TD01 và TD02.

Trước áp dụng có 5 mặt hàng QA, chưa có chuồng. Sau khi người dùng yêu cầu loại bỏ dữ liệu thử nghiệm trong kho, đã sao lưu rồi xóa đúng 5 mặt hàng QA cùng 11 biến động kho, 7 yêu cầu bổ sung và 18 thông báo liên quan. Sáu mặt hàng mới giữ nguyên, tồn kho bằng 0. Bản sao khôi phục lưu ở TestResults bị Git bỏ qua; thao tác dữ liệu này không tự chạy khi deploy code.

Theo yêu cầu cấu hình chuyên môn cho các Trainer hiện có, đã bổ sung 28 mục chuyên môn cho trainer.an, trainer.bao, trainer.nam và trainer.son; giữ nguyên trainer.duc. Cả 5 tài khoản có đủ 7 loại chuyên môn với cấp độ riêng: An thiên về nền tảng, Bảo về tốc độ, Nam về xuất phát, Sơn về phục hồi. Đây là cấu hình phục vụ demo, không phải chứng nhận năng lực thực tế. Không thay đổi lịch làm việc hay tự cấu hình cho tài khoản mới.

Nguồn tên sản phẩm và thức ăn:

- [UMN Extension về cỏ khô](https://extension.umn.edu/agriculture/animals-and-livestock/horse/hay-in-horse-diets).
- [UMN Extension về chọn và bảo quản cỏ khô](https://extension.umn.edu/agriculture/animals-and-livestock/horse/selecting-and-storing-horse-hay).
- [Extension Horses về các nguồn thức ăn](https://horses.extension.org/nutrients-and-common-feed-sources-for-horses/).
- [ANSES về ProteqFlu-Te](https://www.ircp.anmv.anses.fr/fiche.aspx?NomMedicament=PROTEQFLU-TE+SUSPENSION+INJECTABLE+POUR+CHEVAUX).
- [VMD về Eqvalan](https://www.vmd.defra.gov.uk/productinformationdatabase/product/A003145).

## Kiểm tra và triển khai

Kiểm thử dùng SQL Express và database tạm riêng `HRCMS_Test_<GUID>`. Có test tên chủ và phạm vi Owner, yêu cầu/cấp độ giáo án, thu hồi trước khi giảm năng lực, API Manager bị chặn, quyền chăm sóc thường ngày được giữ, thông báo tương thích và importer chạy lại/rollback/giữ lịch sử.

Kết quả toàn bộ suite: **198 đạt, 0 lỗi, 1 bỏ qua** (test Azure Blob live cần bật riêng), tổng 199. Build Release thành công. Test quyền directory được điều chỉnh để phản ánh đúng chính sách đã có: Owner phải lọc theo vai trò được phép và không được đọc danh bạ Trainer; không nới quyền thực tế để làm test đạt.

Thay đổi response không cần migration schema. Các thao tác dữ liệu trên HRCMS-free đã thực hiện theo yêu cầu riêng của người dùng; triển khai code không tự nhập lại dữ liệu. FE và việc triển khai BE không thuộc phạm vi commit này.
