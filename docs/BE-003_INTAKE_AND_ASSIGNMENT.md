# BE-003 — Hồ sơ ngựa, duyệt và phân công

Ngày thực hiện: **05/10/2026**. Phụ trách BE02: **Trần Nguyễn Anh Khoa**. Trạng thái: triển khai và kiểm chứng backend, chờ BE01 review và demo tích hợp frontend. Phạm vi theo BACKEND_TASK_ASSIGNMENT.md; policy Draft/review theo [T02](T02_POLICY_BASELINE.md).

## Cách hiểu luồng

Owner lưu **Draft**, có thể chưa đủ thông tin và file. Khi điền đủ thì **Submit** chuyển sang PendingReview. Manager chọn **Request Revision** có lý do hoặc **Approve**. RevisionRequired cho Owner sửa và submit lại. Chỉ Approve thành công mới tạo Horse Profile và measurement ban đầu. Preferences không tự trở thành official assignment. Manager phân công HeadTrainer/Groom/Vet; HeadTrainer đang phụ trách mới chọn Trainer.

Không có bước đăng ký staff hoặc chỉnh medical clearance trong BE-003. Owner khai báo khỏe không làm ngựa được cấp Fit: Horse mới giữ Monitoring như baseline.

## Phân lớp và thay đổi

| Layer | File chính | Việc thực hiện |
| --- | --- | --- |
| API | HorseEndpoints.cs | Binding RegistrationDraftRequest cho POST/PUT; gọi workflow BLL; không query EF hoặc xử lý duyệt tại endpoint |
| BLL | Requests.cs, HorseWorkflow.cs, HorseService.cs | DTO partial, quyền/state, kiểm dữ liệu đã nhập, ValidateReady khi submit/approve, manager allowlist và audit |
| BLL | ClubOptions.cs, MessageKey.cs, messages.en.json | Giới hạn height/weight từ Business options; thông báo dùng enum key/catalog; không nối câu lỗi nghiệp vụ trong service |
| DAL | Entities.cs, migrations hai provider | Nullable dữ liệu chưa nhập của HorseRegistration; Horse/Measurement chính thức vẫn yêu cầu dữ liệu đầy đủ |

Namespace Horse_BackEnd.* trong BLL/DAL được giữ như quy ước dự án. Dependency vẫn API → BLL → DAL. Enum dùng RegistrationStatus, HorseGender, Role, AttachmentType, AuditAction, NotificationType; không dùng số role/status để quyết định nghiệp vụ.

### Draft và submit

- POST /api/registrations chấp nhận intake thiếu field, kể cả Draft chưa có nội dung; owner identity lấy từ token, không nhận OwnerId từ request. Field chưa nhập lưu null, không thay bằng ngày 0001, height 0 hoặc Gender mặc định.
- PUT của Owner thay thế nội dung Draft/RevisionRequired; field bị bỏ khỏi body thành null. Đây là PUT, không phải autosave PATCH; client cần gửi toàn bộ dữ liệu muốn giữ.
- Field đã nhập vẫn phải đúng enum, date, length, measurement range hoặc staff role/active. DateOfBirth/MeasurementDate không ở tương lai; MeasurementDate không trước birth nếu cả hai có; BoardingEnd không trước start.
- Submit/resubmit yêu cầu Name/Sire/Dam/Breed/DeclaredHealth, DateOfBirth/Gender/HeightCm/WeightKg/MeasurementDate/BoardingStart và cả HorsePhoto + Certificate. HealthNotes, registration number, BoardingEnd và preferences vẫn optional theo baseline.
- PendingReview/Approved/Cancelled không cho Owner edit hoặc submit lại. Cancel chỉ Draft/RevisionRequired, giữ history.
- Approval kiểm lại intake và attachments, không tin rằng mọi record PendingReview cũ đều đầy đủ. Lỗi không tạo Horse/measurement/audit/notification dở dang; approval trùng trả conflict.

### Manager review

- Chỉ Manager sửa thông tin quản lý của PendingReview: **Name, RegistrationNumber, BoardingStart, BoardingEnd**. Giá trị null/omitted trong body của Manager giữ giá trị hiện tại; không xóa field ngầm. Owner có thể xóa field optional qua PUT khi được revision.
- Body đầy đủ kiểu cũ vẫn được nhận nếu non-admin fields không đổi; non-admin field khác hiện tại bị từ chối 403. Không cấp quyền Manager sửa pedigree/physical/health/preferences toàn form.
- Lưu audit RegistrationEdited có actor/time/reference và JSON Before/After của field quản lý. Không ghi dữ liệu clinical vào audit này.
- Approve=false bắt buộc reason, chuyển RevisionRequired, notify Owner. Approve=true mới tạo Horse; chỉ một Horse trên RegistrationId nhờ DB unique và transaction.

### Official assignment

- Owner không chọn Trainer hoặc tạo official assignment. PreferredHeadTrainer/Groom/Veterinarian chỉ là preference, không tự thêm assignment sau approval.
- Manager xác nhận HeadTrainer/Groom/Veterinarian; Manager không trực tiếp chọn Trainer. Chỉ HeadTrainer Active đang assigned đúng horse được chọn Trainer.
- Staff được chọn phải Active và đúng role. Future assignment chưa thuộc core. Reassignment kết thúc assignment cũ, tạo record mới, không ghi đè history. Trainer cũ mất quyền sửa; plan creator giữ nguyên.
- Concurrency/rollback tiếp tục được kiểm trên SQL Server với hai host có gate độc lập.

## Cấu hình giới hạn

Business:MinHorseHeightCm=1, MaxHorseHeightCm=300, MinHorseWeightKg=1, MaxHorseWeightKg=2000 là default core. Có thể override qua configuration; startup kiểm min <= max. Không hardcode bounds ở HorseService. MaxLength của DTO là giới hạn cấu trúc contract, HTTP status/error code/field identifiers là định danh kỹ thuật theo quy ước layer/messages.

## Migration và ảnh hưởng frontend

Thêm **PartialRegistrationDraft** cho SQLite và SQL Server; SQL idempotent [database.sql](database.sql) đã sinh lại. Migration Up chỉ đổi nullability intake, không xóa dữ liệu đầy đủ trước đây; đã kiểm downgrade/upgrade trên fixture chứa intake đầy đủ. Chưa áp migration vào database cá nhân/production.

Trước dùng binary mới, BE05 review và áp migration đúng provider trong môi trường có backup. **Không tự downgrade khi còn partial Draft:** Down được EF sinh có default để thay null khi quay về schema cũ, có thể làm mất ý nghĩa “chưa nhập”. Rollback an toàn cần restore bộ backup tương ứng hoặc xử lý dữ liệu trước theo quy trình BE05, không chạy Down tùy tiện.

Frontend cần đọc nullable fields, dùng [OpenAPI hiện tại](contracts/openapi.current.json) với RegistrationDraftRequest, phân biệt Owner PUT replacement và Manager edit giữ field null/omitted. Tên route/JSON/status cũ vẫn giữ; chưa nghiệm thu Figma/UI. Giới hạn cụ thể từ cấu hình cần được bàn giao cho frontend, không suy ra giới hạn deployment từ example JSON.

## Kiểm chứng và checklist bàn giao

Thêm 12 trường hợp trong IntakeTests (gồm theory): partial Draft/persistence, thiếu field/file, revision/resubmit/approval/assignment, Manager allowlist và before/after audit, invalid enum/numeric enum/date/height/Trainer injection, cross-owner/cancel, configured bounds/wrong preferred role, incomplete legacy PendingReview, migration giữ intake cũ, wrong actor/HeadTrainer/inactive staff assignment.

Giữ các test cũ về approval đồng thời, rollback sau SQL trigger, reassignment và Trainer cũ mất scope. LayerAndMessageTests kiểm dependency project và đầy đủ message templates. SQL concurrency fixture nay có intake/files metadata đầy đủ để validation không chặn trước khi kiểm lỗi DB.

Kết quả cuối: **Release build 0 warning/0 error; SQL Server 72 pass/1 SQLite-only skip; SQLite 60 pass/13 SQL-only skip, tổng 73 ca, không fail.** TRX ở TestResults/BE003/sqlite-reviewed và TestResults/BE003/sqlserver-reviewed, Git ignored. OpenAPI giữ 96 operations, 139 schemas, 87 secured operations; POST/PUT intake mô tả DTO nullable mới.

Chạy lại:

```powershell
dotnet build HorseClub.slnx -c Release
dotnet test HorseClub.slnx -c Release --filter 'FullyQualifiedName~IntakeTests|FullyQualifiedName~LayerAndMessageTests'
$env:HRCMS_TEST_SQLSERVER='Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx -c Release --logger trx --results-directory TestResults/BE003/sqlserver
Remove-Item Env:HRCMS_TEST_SQLSERVER
```

Checklist review: partial fields thật trong DB; no Horse trước approval; role/prefs/history đúng; Manager không sửa intake ngoài allowlist; catalog/options đúng; migration/script đồng bộ; tests pass; BE01 review và frontend demo luồng. HTTP mẫu: [BE-003-intake.http](http/BE-003-intake.http). Chưa tự chuyển sang BE-004, BE-005 hoặc sửa archive/medical policy.

HTTP mẫu chỉ phục vụ BE-003, cần token/id và upload file thật của môi trường test; không phải collection toàn hệ thống và không tự hoàn thành BE-012.
