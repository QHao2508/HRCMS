# Backend hỗ trợ màn hình mới — chưa UAT

Không chạy test nghiệp vụ theo yêu cầu người dùng. Release build đã thành công; các test cần chạy nằm trong PHASE_1_DEFERRED_TESTS_2026-10-11.md và docs SUPPORT_SCREENS_IMPLEMENTATION_2026-10-11.md của FE.

## Contract additive

GET /api/care/stalls thêm occupied:boolean và horseId:Guid?. Occupied phản ánh có current occupancy; horseId chỉ được trả khi ngựa thuộc HorseScope của caller. Groom có thể biết ô có ngựa nhưng không được đọc ID ngựa ngoài phân công. Dữ liệu lấy từ repository theo IDs của trang, không giả định từ frontend. Không có migration, giữ fields cũ và response page cũ.

## Care privacy

ListTasks chỉ trả instructions/notes Treatment/IceBath cho Vet hoặc Groom. Groom query tiếp tục chỉ trả task giao chính mình. Owner/Manager nhận summary và không nhận clinical text của hai loại này. Policy này không mở quyền mới. Scope/report/audit/privacy toàn hệ thống vẫn cần review tại #24.

## Cần test sau

- [ ] Stall empty/occupied, paging/filter; current occupancy không trả ID ngoài scope.
- [ ] Manager/Groom đúng scope và active assignment; xếp/chuyển/vacate/archive cập nhật trạng thái.
- [ ] Owner/Manager Treatment/IceBath text null; Groom chỉ own instructions, Vet đúng assigned horse.
- [ ] EF SQL translation dictionary projection trên SQL Server/Azure, dữ liệu nhiều trang và concurrent occupancy.
- [ ] Regression contract/layer boundaries, care, horse lifecycle và authorization.

Baseline trước thay đổi: BE d5c71bf, bản docs a88d9ff; FE a3f389a. Lần triển khai mới chưa có bằng chứng test hoàn chỉnh; không ghi Done tự động.

Azure SQL timeout tại ClubStartup khi khởi động lại (11/10/2026); API mới chưa chạy thành công trên Azure trong lần này. Snapshot OpenAPI được cập nhật từ DTO source cho occupied/horseId; chưa tái xuất từ runtime, cần đối chiếu khi UAT. Bản build thành công không phải bằng chứng kết nối Azure. Không thay đổi schema hoặc dữ liệu database trong đợt triển khai.
