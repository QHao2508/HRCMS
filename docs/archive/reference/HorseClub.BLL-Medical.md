> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../../README.md).

# Giải thích function: HorseClub.BLL-Medical

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Medical/MedicalService.cs

Khám, chấn thương, hạn chế vận động, điều trị, tái khám và xác nhận đủ điều kiện.

### MedicalService.GetSummary(horseId)

**Kết quả:** `Task<MedicalSummaryResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết tóm tắt y tế trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `clock.GetUtcNow`, `Restrictions.Where`.

### MedicalService.ListExaminations(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<MedicalRecord>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListExaminations. |
| `pageSize` | Giá trị kiểu int? dùng trong ListExaminations. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.MedicalDetails`, `pager.Page`, `MedicalRecords.Where`.

### MedicalService.CreateExamination(horseId, r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `current.Get`, `MedicalRecords.Add`, `access.Horse`, `events.Audit`, `events.HorseStaff`, `db.SaveChangesAsync`, `OperationResult.Created`.

### MedicalService.ListInjuries(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<Injury>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang chấn thương trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListInjuries. |
| `pageSize` | Giá trị kiểu int? dùng trong ListInjuries. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.MedicalDetails`, `pager.Page`, `Injuries.Where`.

### MedicalService.CorrectExamination(horseId, id, r)

**Kết quả:** `Task<MedicalRecord>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo bản đính chính lần khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.Found`, `MedicalRecords.SingleOrDefaultAsync`, `Ensure.That`, `MedicalRecords.AnyAsync`, `Messages.Get`, `current.Get`, `MedicalRecords.Add`, `access.Horse`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.RecordInjury(horseId, r)

**Kết quả:** `Task<Injury>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận chấn thương trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.That`, `calendar.Today`, `Messages.Get`, `Injuries.Add`, `access.Horse`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.ListRestrictions(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<MedicalRestriction>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang hạn chế vận động trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListRestrictions. |
| `pageSize` | Giá trị kiểu int? dùng trong ListRestrictions. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `pager.Page`, `Restrictions.Where`.

### MedicalService.CreateRestriction(horseId, r)

**Kết quả:** `Task<MedicalRestriction>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới hạn chế vận động trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.That`, `Messages.Get`, `ValidFrom.ToUniversalTime`, `Restrictions.Add`, `events.HorseStaff`, `Sessions.Where`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.ListTreatments(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<TreatmentPlan>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang phác đồ điều trị trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListTreatments. |
| `pageSize` | Giá trị kiểu int? dùng trong ListTreatments. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.MedicalDetails`, `pager.Page`, `Treatments.Where`.

### MedicalService.CreateTreatment(horseId, r)

**Kết quả:** `Task<TreatmentPlan>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới phác đồ điều trị trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.That`, `Messages.Get`, `Injuries.AnyAsync`, `Treatments.Add`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.ListFollowUps(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<MedicalFollowUp>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang tái khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListFollowUps. |
| `pageSize` | Giá trị kiểu int? dùng trong ListFollowUps. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.MedicalDetails`, `pager.Page`, `FollowUps.Where`.

### MedicalService.RecordFollowUp(horseId, r)

**Kết quả:** `Task<MedicalFollowUp>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận tái khám trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.Validate`, `Ensure.That`, `Messages.Get`, `current.Get`, `MedicalRecords.Add`, `access.Horse`, `FollowUps.Add`, `Restrictions.Where`, `Injuries.Where`, `Treatments.Where`, `events.HorseStaff`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.ListPreventiveCare(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<PreventiveCareSummaryResponse>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListPreventiveCare. |
| `pageSize` | Giá trị kiểu int? dùng trong ListPreventiveCare. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horse`, `pager.Page`, `PreventiveCare.Where`.

### MedicalService.SchedulePreventiveCare(horseId, r)

**Kết quả:** `Task<PreventiveCare>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lên lịch chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.That`, `Enum.IsDefined`, `Messages.Get`, `PreventiveCare.Add`, `events.Audit`, `db.SaveChangesAsync`.

### MedicalService.CompletePreventiveCare(horseId, id, r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Hoàn tất chăm sóc phòng bệnh trong MedicalService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Vet`, `Ensure.Found`, `PreventiveCare.SingleOrDefaultAsync`, `Ensure.That`, `calendar.Today`, `Messages.Get`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### MedicalService.MedicalRecordFor(db, horseId, id)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Xác nhận bản ghi y tế thuộc đúng ngựa trước khi tạo liên kết chấn thương/hạn chế/điều trị.

| Đầu vào | Ý nghĩa |
|---|---|
| `db` | Giá trị kiểu ClubDbContext dùng trong MedicalRecordFor. |
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `Ensure.That`, `MedicalRecords.AnyAsync`, `Messages.Get`.

### MedicalService.ValidateExamination(r, clock)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Kiểm thời gian và các trường khám y tế bắt buộc trước lưu, tránh dữ liệu không hợp lệ liên kết với ngựa.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |
| `clock` | Giá trị kiểu TimeProvider dùng trong ValidateExamination. |

Lời gọi chính: `Ensure.That`, `clock.GetUtcNow`, `Messages.Get`.

### MedicalService.Record(horseId, vetId, r)

**Kết quả:** `MedicalRecord`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi dữ liệu khám/theo dõi vào MedicalService sau khi kiểm quyền và quan hệ ngựa/hồ sơ y tế.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `vetId` | Giá trị kiểu Guid dùng trong Record. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `ExaminationAt.ToUniversalTime`.
