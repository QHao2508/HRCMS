# BE-004 — Giáo án, buổi tập, kết quả và đánh giá

Ngày kiểm chứng: **05/10/2026**. Phụ trách BE02: **Trần Nguyễn Anh Khoa**. Phạm vi theo bảng phân công Leader: Template → Plan → Session → Rider → Result → Evaluation. Đã hoàn thiện và kiểm chứng backend; chờ BE03 review medical integration và demo qua frontend. Không tự thực hiện BE-005/BE-006 hoặc đổi policy clearance/archive ngựa.

## Luồng nghiệp vụ để trình bày

HeadTrainer tạo **Standard Training Template**, là khung tham khảo. Trainer đang được phân công cho ngựa tạo **Training Plan** cá nhân hóa từ template, nhập goal/phase/date/notes. Trainer tạo session với type/distance/intensity/surface/target/time, chưa có Rider thì **Planned**. Giao Rider hợp lệ thì **Assigned**, Rider start khi tới giờ và guard hợp lệ thì **InProgress**. Result bình thường thành **Completed**, abnormal hoặc medical conflict giữa buổi thành **IssueReported** có incident/notify. Trainer evaluate kết quả, quyết định tiếp tục hoặc yêu cầu chỉnh future sessions.

Template không tự sinh lịch/frequency cho horse; AdjustFutureSessions không tự đổi lịch. Trainer chỉnh rõ từng future session qua PUT, giữ revision và kiểm guard lại. Không thêm duration/end time hoặc interval overlap trong task này theo phạm vi core của [T02](T02_POLICY_BASELINE.md).

## Phần đã hoàn thiện

| Hạng mục | Behavior và bằng chứng |
| --- | --- |
| Template và Plan | Chỉ HeadTrainer tạo/edit template; chỉ current Trainer tạo/edit Plan. Archive template không phá Plan đã tạo; template archived không dùng tạo Plan mới. Có test các role bị từ chối. |
| Planned/Assigned | Chưa có Rider vẫn Planned, không start; assignment dùng Rider active đúng role, giữ notify/history. |
| State guards | Paused không cho create/start; Completed/Archived không reopen; không complete khi còn pending sessions hoặc archive khi InProgress. Skip có reason, không skip lại final session. |
| Medical integration | Guard được kiểm create/edit/assign/start. TrainingLock chặn Heavy nhưng Light phù hợp vẫn có thể chạy; Trainer không override. Dùng policy hiện tại của BE03, không sửa follow-up/clearance. |
| Rider scope | Plan list chỉ có Plan có session giao cho Rider đó; Plan detail không có session của Rider trả 403 kể cả cùng horse. Sessions trong Plan detail lọc theo Rider. Owner đúng horse đọc, cross-owner bị chặn, Owner không tạo session. |
| Plan session paging | Bỏ Take(100) âm thầm; GET detail nhận sessionPage/sessionPageSize, trả Sessions cùng SessionPage/SessionPageSize/SessionTotal. Dùng PageReader và Business limits, sort ScheduledAt rồi Id. Test 105 records đọc đủ qua hai trang và reject page=0. |
| Result | Chỉ Rider được giao/InProgress gửi result; time > 0; speed tự tính distance metres / seconds, không nhận speed từ client. Precision từ Business:SpeedDecimalPlaces. Duplicate không ghi lại. |
| Evaluation | Chỉ current Trainer, session Completed/IssueReported và có source Result thực tế; không evaluate trùng hoặc record final giả thiếu result. Flag chỉnh tương lai không tự sửa session. |
| History | Thêm revision khi start/result/skip/evaluation; giữ actor, PlanId, SessionId và snapshot. Existing create/edit/status history vẫn giữ. Result/evaluation history có payload để đọc lại thời điểm quyết định. |

Plan completion/skip/evaluation được kiểm thêm cùng các test đã có về restriction-vs-start, lock giữa buổi, clearance không resume, reassignment và scope Trainer cũ. Rule luồng y tế mới của T02 vẫn cần task BE03 riêng; suite không tự xác nhận code clearance đã theo policy mới.

## Cấu trúc ba layer và cấu hình

API TrainingEndpoints chỉ bind/query parameters, route/status và gọi TrainingPlanService/TrainingSessionService/TrainingSessionService trong BLL. BLL xử lý quyền/state, EF queries, paging, history, tính speed và notification theo quy ước dự án. DAL giữ enum/entity/DbContext/migrations; không tham chiếu ngược BLL/API. Task này **không đổi DAL model và không thêm migration**.

Dùng Role, PlanStatus, SessionStatus, Intensity, TrainingType, AuditAction, NotificationType và catalog MessageKey hiện có. Không tạo literal message người dùng trong business code. SpeedDecimalPlaces mặc định 3, cấu hình 0–3 để phù hợp DAL decimal scale 3; phép tính decimal và midpoint rounding giữ behavior hiện tại. Page limits từ Business:DefaultPageSize/MaxPageSize, start window từ StartEarlyMinutes, schedule grace từ ScheduleGraceMinutes; không thêm ngưỡng riêng trong workflow.

## Contract frontend cần biết

```http
GET /api/training/plans/{id}?sessionPage=2&sessionPageSize=20
```

Response giữ `plan`, `sessions`, `restrictions`, bổ sung `sessionPage`, `sessionPageSize`, `sessionTotal`. Không có query thì page=1 và size theo Business:DefaultPageSize (default core 20), thay cho cap cũ 100; frontend cần chuyển trang để đọc đủ. Total đã lọc quyền Rider, không tiết lộ số session của Rider khác.

TrainingRevision.Snapshot là chuỗi JSON: revision cũ/plan/session vẫn giữ dạng object Plan hoặc Session; mốc result/evaluation mới dùng `{ session, result, evaluation }` với enum string camelCase fields, evaluation có thể null ở mốc result. Không rewrite historical snapshots. Client đọc snapshot cần nhận biết shape, không mặc định mọi snapshot đều là raw Session; ActorId/CreatedAt/PlanId/SessionId nằm ngoài snapshot. Session history vẫn không cấp quyền mới cho Rider vào route history toàn Plan.

Các route/status không đổi; OpenAPI current và inventory đã xuất lại: **73 paths, 96 operations, 139 schemas, 87 secured operations, không response schema rỗng**. Ví dụ gọi API: [BE-004-training.http](http/BE-004-training.http), cần token/id thật trong môi trường test. Mẫu này không thay collection toàn hệ thống BE-012.

## Kiểm chứng

Thêm **12 ca** trong TrainingTests (có role theory). Build Release **0 warning/error**. Toàn suite **85 ca**:

- SQL Server Express: **84 pass, 1 provider-specific skip trong lần kiểm lịch sử, 0 fail**.
- TRX: TestResults/BE004/sqlserver, Git ignored.
- LayerAndMessageTests và OpenAPI contract tests chạy trong cùng suite; database SQL test tạo/dọn riêng, không đổi database cá nhân.

```powershell
dotnet build HorseClub.slnx -c Release
dotnet test HorseClub.slnx -c Release --filter 'FullyQualifiedName~TrainingTests|FullyQualifiedName~LayerAndMessageTests'
$env:HRCMS_TEST_SQLSERVER='Server=.\SQLEXPRESS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test HorseClub.slnx -c Release --logger trx --results-directory TestResults/BE004/sqlserver
Remove-Item Env:HRCMS_TEST_SQLSERVER
```

Các ca thời điểm dùng offset đủ rộng với clock thật; chưa phải kiểm exhaustively mọi biên mili giây/TTL bằng deterministic clock. Không tuyên bố đã load test, chạy CI GitHub mới hoặc UI E2E. Scope lịch sử Horse Profile của Rider và archive ngựa sâu hơn vẫn là phối hợp BE01/chủ module, không tự hoàn thành qua sửa scope Plan.

## Checklist review BE03 và bàn giao

BE03 kiểm lock/restriction trên session scheduled và actual start, privacy trong lý do/reference và việc lưu observation sau lock giữa buổi. BE01 kiểm Owner/Rider/current Trainer scope và contract pagination. FE demo Planned → Assigned → InProgress → Completed/IssueReported → Evaluation, hiển thị errors đúng, đọc history nhiều shape và không nói lịch đã thay đổi chỉ vì flag AdjustFutureSessions=true. Chỉ đánh dấu nghiệm thu nhóm sau reviewer và UI integration; backend đã có bằng chứng test.
