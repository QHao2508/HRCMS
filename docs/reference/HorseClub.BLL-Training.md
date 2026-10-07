# Giải thích function: HorseClub.BLL-Training

Các function có tên trong source nghiệp vụ/UI. Callback anonymous của LINQ/React và getter tự động được giải thích trong function bao ngoài; migration sinh tự động được giữ nguyên.

## HorseClub.BLL/Training/TrainingPlanService.cs

Giáo án → kế hoạch cá nhân hóa → buổi tập → kết quả/đánh giá; kiểm trạng thái và hạn chế sức khỏe.

### TrainingPlanService.CreatePlan(r)

**Kết quả:** `Task<TrainingPlan>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `access.Trainer`, `current.Get`, `Ensure.Found`, `Templates.FindAsync`, `Ensure.That`, `Messages.Get`, `Plans.Add`, `events.TrainingHistory`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingPlanService.Create(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Điểm vào tạo dữ liệu của TrainingPlanService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `OperationResult.Created`.

### TrainingPlanService.ListPlans(horseId, page, pageSize)

**Kết quả:** `Task<PageResponse<TrainingPlan>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `page` | Giá trị kiểu int? dùng trong ListPlans. |
| `pageSize` | Giá trị kiểu int? dùng trong ListPlans. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `access.Horses`, `Plans.Where`, `horses.Contains`, `current.Get`, `q.Where`, `Sessions.Any`, `access.Horse`, `pager.Page`, `q.OrderByDescending`.

### TrainingPlanService.GetPlan(id, sessionPage, sessionPageSize)

**Kết quả:** `Task<PlanDetailResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `sessionPage` | Giá trị kiểu int? dùng trong GetPlan. |
| `sessionPageSize` | Giá trị kiểu int? dùng trong GetPlan. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `Ensure.Found`, `Plans.FindAsync`, `access.Horse`, `current.Get`, `Sessions.Where`, `sessions.Where`, `Ensure.That`, `sessions.AnyAsync`, `Messages.Get`, `pager.Page`, `sessions.OrderBy`, `Restrictions.Where`.

### TrainingPlanService.UpdatePlan(id, r)

**Kết quả:** `Task<TrainingPlan>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cập nhật kế hoạch huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Plans.FindAsync`, `access.Trainer`, `Ensure.That`, `Messages.Get`, `Sessions.Where`, `sessions.All`, `calendar.DateAt`, `events.TrainingHistory`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingPlanService.SetPlanStatus(id, r)

**Kết quả:** `Task<TrainingPlan>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Thiết lập trạng thái kế hoạch trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Plans.FindAsync`, `access.Trainer`, `Ensure.That`, `Messages.Get`, `Sessions.AnyAsync`, `Sessions.Where`, `events.Notify`, `events.TrainingHistory`, `events.Audit`, `Status.ToString`, `db.SaveChangesAsync`.

### TrainingPlanService.ListHistory(id, page, pageSize)

**Kết quả:** `Task<PageResponse<TrainingRevision>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang lịch sử huấn luyện trong TrainingPlanService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `page` | Giá trị kiểu int? dùng trong ListHistory. |
| `pageSize` | Giá trị kiểu int? dùng trong ListHistory. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `Plans.FindAsync`, `access.Horse`, `pager.Page`, `TrainingRevisions.Where`.

## HorseClub.BLL/Training/TrainingSessionService.cs

Giáo án → kế hoạch cá nhân hóa → buổi tập → kết quả/đánh giá; kiểm trạng thái và hạn chế sức khỏe.

### TrainingSessionService.Guard(horseId, distance, intensity, type, at)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Kiểm tình trạng ngựa và hạn chế y tế tại thời điểm buổi tập; chặn bài tập, cường độ hoặc quãng đường không phù hợp.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `distance` | Giá trị kiểu decimal dùng trong Guard. |
| `intensity` | Giá trị kiểu Intensity dùng trong Guard. |
| `type` | Giá trị kiểu TrainingType dùng trong Guard. |
| `at` | Thời điểm nghiệp vụ được kiểm, tính theo UTC rồi quy đổi múi giờ khi cần. |

Lời gọi chính: `Ensure.Found`, `Horses.FindAsync`, `Ensure.That`, `Messages.Get`, `Restrictions.Where`.

### TrainingSessionService.CreateSession(planId, r)

**Kết quả:** `Task<TrainingSession>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `planId` | ID kế hoạch chứa buổi tập hoặc dữ liệu huấn luyện được thao tác. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Plans.FindAsync`, `access.Trainer`, `Ensure.That`, `Messages.Get`, `Sessions.Add`, `events.TrainingHistory`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingSessionService.EditSession(id, r)

**Kết quả:** `Task<TrainingSession>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Chỉnh sửa buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Sessions.FindAsync`, `access.Trainer`, `Ensure.That`, `Messages.Get`, `Plans.FindAsync`, `events.TrainingHistory`, `events.Notify`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingSessionService.ApplySession(s, p, r)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Kiểm thời gian trong kế hoạch/múi giờ, sức khỏe và lịch người cưỡi; áp dụng dữ liệu và chọn trạng thái Planned hoặc Assigned.

| Đầu vào | Ý nghĩa |
|---|---|
| `s` | Giá trị kiểu TrainingSession dùng trong ApplySession. |
| `p` | Giá trị kiểu TrainingPlan dùng trong ApplySession. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `calendar.DateAt`, `Ensure.That`, `Messages.Get`, `clock.GetUtcNow`, `access.Staff`, `Sessions.AnyAsync`, `ScheduledAt.ToUniversalTime`.

### TrainingSessionService.Start(id)

**Kết quả:** Task hoàn tất thao tác; lỗi/cancellation được truyền về caller, không trả entity.

Chỉ người cưỡi được giao mới bắt đầu; kiểm lịch, xung đột, kế hoạch hoạt động và sức khỏe ngay trước lúc chuyển InProgress.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `Sessions.FindAsync`, `Ensure.That`, `Messages.Get`, `access.Horse`, `clock.GetUtcNow`, `Plans.FindAsync`, `calendar.Today`, `Sessions.AnyAsync`, `events.TrainingHistory`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingSessionService.SubmitResult(id, r)

**Kết quả:** `Task<SessionResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Kiểm quyền người cưỡi và buổi tập đã bắt đầu, tránh kết quả trùng; lưu kết quả đo, chuyển trạng thái và thông báo huấn luyện viên.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `current.Get`, `Ensure.Role`, `Ensure.Found`, `Sessions.FindAsync`, `Ensure.That`, `Messages.Get`, `access.Horse`, `Results.AnyAsync`, `clock.GetUtcNow`, `decimal.Round`, `Results.Add`, `events.HorseStaff`, `Incidents.Add`, `Plans.FindAsync`, `events.TrainingHistory`, `events.Audit`.

### TrainingSessionService.Create(id, r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Điểm vào tạo dữ liệu của TrainingSessionService; chuyển dữ liệu request vào hàm nghiệp vụ rồi đóng gói kết quả API.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `OperationResult.Created`.

### TrainingSessionService.ListSessions(horseId, status, from, to, page, pageSize)

**Kết quả:** `Task<PageResponse<TrainingSession>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `horseId` | ID ngựa cần kiểm sở hữu/phân công và phạm vi nghiệp vụ. |
| `status` | Trạng thái enum API, tách khỏi nhãn tiếng Việt. |
| `from` | Giá trị kiểu DateTimeOffset? dùng trong ListSessions. |
| `to` | URL nội bộ mà liên kết/redirect hướng đến. |
| `page` | Giá trị kiểu int? dùng trong ListSessions. |
| `pageSize` | Giá trị kiểu int? dùng trong ListSessions. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `current.Get`, `access.Horses`, `Sessions.Where`, `horses.Contains`, `q.Where`, `access.Horse`, `pager.Page`, `q.OrderBy`.

### TrainingSessionService.GetSession(id)

**Kết quả:** `Task<SessionDetailResponse>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc chi tiết buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

Lời gọi chính: `Ensure.Found`, `Sessions.FindAsync`, `access.Horse`, `current.Get`, `Ensure.That`, `Messages.Get`, `Results.SingleOrDefaultAsync`, `Evaluations.SingleOrDefaultAsync`.

### TrainingSessionService.Update(id, r)

**Kết quả:** `Task<TrainingSession>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cập nhật dữ liệu của module trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

### TrainingSessionService.AssignRider(id, r)

**Kết quả:** `Task<TrainingSession>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Phân công Rider trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

Lời gọi chính: `Ensure.Found`, `Sessions.FindAsync`.

### TrainingSessionService.StartSession(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Bắt đầu buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

Lời gọi chính: `OperationResult.NoContent`.

### TrainingSessionService.RecordResult(id, r)

**Kết quả:** `Task<SessionResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Ghi nhận kết quả buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

### TrainingSessionService.SkipSession(id, r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Bỏ qua buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Sessions.FindAsync`, `current.Get`, `access.Horse`, `Ensure.That`, `Messages.Get`, `access.Trainer`, `events.TrainingHistory`, `Plans.FindAsync`, `events.HorseStaff`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### TrainingSessionService.EvaluateSession(id, r)

**Kết quả:** `Task<TrainerEvaluation>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đánh giá buổi tập trong TrainingSessionService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- ClubAccess giới hạn dữ liệu theo user/phân công; không chỉ dựa vào role hoặc ID client gửi.

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Found`, `Sessions.FindAsync`, `access.Trainer`, `Ensure.That`, `Messages.Get`, `Evaluations.AnyAsync`, `Results.SingleOrDefaultAsync`, `current.Get`, `Evaluations.Add`, `events.TrainingHistory`, `Plans.FindAsync`, `events.Audit`, `db.SaveChangesAsync`.

## HorseClub.BLL/Training/TrainingTemplateService.cs

Giáo án → kế hoạch cá nhân hóa → buổi tập → kết quả/đánh giá; kiểm trạng thái và hạn chế sức khỏe.

### TrainingTemplateService.ListTemplates(page, pageSize)

**Kết quả:** `Task<PageResponse<TrainingTemplate>>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Đọc danh sách có lọc/phân trang giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `page` | Giá trị kiểu int? dùng trong ListTemplates. |
| `pageSize` | Giá trị kiểu int? dùng trong ListTemplates. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

Lời gọi chính: `Ensure.Role`, `current.Get`, `pager.Page`, `Templates.Where`.

### TrainingTemplateService.CreateTemplate(r)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Tạo mới giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Templates.Add`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.Created`.

### TrainingTemplateService.UpdateTemplate(id, r)

**Kết quả:** `Task<TrainingTemplate>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Cập nhật giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Templates.FindAsync`, `Ensure.That`, `Messages.Get`, `events.Audit`, `db.SaveChangesAsync`.

### TrainingTemplateService.ArchiveTemplate(id)

**Kết quả:** `Task<OperationResult>`: kết quả theo kiểu của chữ ký. DTO/entity chứa dữ liệu được phép; null/bool phản ánh điều kiện mô tả ở mục đích và nhánh return.

Lưu trữ giáo án trong TrainingTemplateService; áp dụng quyền, điều kiện và lưu dữ liệu theo phần thân hàm.

| Đầu vào | Ý nghĩa |
|---|---|
| `id` | ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi. |

- Quyền role được kiểm trong thân hàm (các tên enum không dịch khi gửi API).

- Có ghi dữ liệu SQL Server; transaction của API/worker quyết định commit hoặc rollback.

- Audit/thông báo/lịch sử được xếp và lưu cùng thao tác, tránh phản ánh một mutation chưa commit.

Lời gọi chính: `Ensure.Role`, `current.Get`, `Ensure.Found`, `Templates.FindAsync`, `events.Audit`, `db.SaveChangesAsync`, `OperationResult.NoContent`.

### TrainingTemplateService.Apply(t, r)

**Kết quả:** Không trả dữ liệu; tác động lên đối tượng/DI/endpoint/configuration được mô tả ở trên.

Kiểm và áp dụng mục tiêu, giai đoạn, quãng đường, cường độ và tần suất lên giáo án.

| Đầu vào | Ý nghĩa |
|---|---|
| `t` | Giá trị kiểu TrainingTemplate dùng trong Apply. |
| `r` | DTO request của thao tác; các field được kiểm ở API và quy tắc BLL. |
