# Flow 3 và Flow 4 — triển khai và kiểm chứng 13/10/2026

Phạm vi theo policy hiện có: Flow 3 là thú y, chăm sóc và sự cố; Flow 4 là chuồng, kho, báo cáo và nhật ký. Các module đã có được giữ và kiểm chứng cùng các phần bổ sung dưới đây.

## Hành vi hoàn thiện

- Flow 3: kiểm chứng thăm khám → phác đồ → giao chăm sóc cho Groom → ghi nhận vấn đề → Vet xử lý → tái khám kết thúc đúng phác đồ đã chọn. Owner/Manager không đọc hướng dẫn và ghi chú của công việc điều trị; Groom chỉ đọc công việc của mình. Clearance không tự kết thúc mục khác hoặc tự tiếp tục kế hoạch tập.
- Flow 4: ngựa chuyển ô, rời ô hoặc được archive đều làm ô cũ chuyển `NeedsCleaning`. Groom được vệ sinh ô trống khi ngựa trong lần sử dụng mới nhất vẫn thuộc phân công hiện hành; Manager xử lý các ô còn lại, gồm ô của ngựa đã archive. Quyền được kiểm lại ở server.
- `GET /api/care/stalls/{id}/history`: phân trang, tên ngựa, thời điểm vào/rời ô. Manager thấy cả lịch sử ngựa archive; Groom thấy ngựa thuộc phạm vi hiện hành. Không tiết lộ tên/ID ngựa ngoài phạm vi.
- Danh sách ô thêm `canClean` để FE hiển thị đúng thao tác khi ô đã trống.
- Groom có biểu mẫu xuất vật tư với lượng âm; không được nhập kho. Manager vẫn nhập/xuất và tiếp nhận bổ sung từng phần. Tồn kho và lịch sử cập nhật sau lưu.
- Báo cáo mở chi tiết buổi tập/công việc chăm sóc, chọn đúng ngựa; thiếu tên không hiển thị UUID. Chi tiết thú y dùng `Business:MaxReportRecords`, không âm thầm cắt ở 100 dòng; vượt giới hạn trả 413 để thu hẹp bộ lọc.
- Dashboard dùng cùng ngưỡng `Business:OverdueAfterMinutes` với reminder: Assigned, plan Active, ngựa chưa archive, thời gian lịch nhỏ hơn hiện tại trừ ngưỡng. Đúng 60 phút chưa quá hạn khi ngưỡng là 60.
- SupportForm kiểm điều kiện disabled cả khi submit, bảo vệ thao tác đang xử lý/không chắc kết quả và biểu mẫu chưa đủ điều kiện.

## Kiểm chứng

| Lớp | Kết quả |
|---|---|
| BE regression HTTP + SQL Server riêng | 170 pass, 1 skip (upload Azure Blob live chưa bật), 0 fail; 171 tổng |
| FE | 224 pass; lint và build pass |
| Azure SQL HRCMS | 23 assertions pass, trong đó 6 assertion nhóm mới cho Flow 3/4; transaction rollback, không commit fixture |
| Trình duyệt FE → API thật → SQL Server riêng | Groom xuất 2 kg: 20 → 18; vệ sinh ô trống; cho ăn Completed với thực tế 2 kg; báo cáo nhận kết quả, mở đúng ngựa; Groom báo sự cố → Vet xử lý; Vet tạo khám, phác đồ, tái khám chọn phác đồ → Hoàn thành |

Azure chạy lúc 13:06 ngày 13/10 (UTC+7). Kiểm tra residual theo ID fixture trên toàn bộ 36 bảng: 0 dòng. Users 21, Horses 7, Sessions 7 giữ nguyên trước/sau. Azure smoke gọi BLL/repository trực tiếp; kiểm thử HTTP/trình duyệt dùng database SQL Server tạm riêng. Không khởi động worker hay thay đổi tiến trình BE đang chạy của dự án.

Minh chứng local (thư mục TestResults được ignore): `TestResults/Flow34/azure-smoke-result.json`, `backend-final.log`, `inventory-groom.png`, `stall-cleaned.png`, `care-completed.png`, `report-groom.png`, `incident-resolved.png`, `treatment-completed.png`. Test nghiệp vụ được commit ở `tests/Horse_BackEnd.Tests/Flow34CompletionTests.cs`.

## Tích hợp

Không thay đổi schema SQL trong lượt này. Deploy BE và FE cùng nhau vì FE dùng endpoint lịch sử mới và field `canClean`. Với ô cũ đã trống trước bản cập nhật nhưng chưa đánh dấu bẩn, Manager cần đối chiếu thực tế rồi xử lý; không suy đoán trạng thái vệ sinh từ lịch sử cũ.

Thay đổi được đưa lên nhánh `codex/trainer-availability` ở hai repository. Merge main vẫn theo PR/review của `CONTRIBUTING.md`. Kiểm chứng trên không thay thế nghiệm thu của người dùng CLB hoặc chứng nhận chuyên môn y tế. Build FE còn cảnh báo chunk lớn khoảng 803 kB.
