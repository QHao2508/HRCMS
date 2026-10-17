# Khoa — kiểm chứng task #5 đến #10

Ngày kiểm chứng: 10/10/2026. Phạm vi đối chiếu là sáu task backend giao Khoa trong bảng Total Project Tracking. Đã sửa các gap backend và nối phần lịch sử training bị ảnh hưởng ở FE. Trạng thái dưới đây là **Implemented + automated tests verified**, chưa thay cho reviewer/UAT của nhóm hoặc nghiệm thu release.

## Kết quả từng task

| Task | Phần đã xác nhận | Bằng chứng |
|---|---|---|
| #5 Registration / approval | Draft thiếu dữ liệu; kiểm field/file khi submit; revision/resubmit; Manager allowlist/audit; approval tạo một Horse và measurement; scope và rollback | IntakeTests 12/12; SQL concurrency/approval rollback trong full regression |
| #6 Profile / assignment | Phân công giữ lịch sử, nhân sự cũ mất quyền ghi. Archive chặn session/care InProgress; đóng active assignments, Active/Paused plans, Planned/Assigned sessions, Pending care và occupancy; giữ medical state. Owner/Manager đọc profile archived, nhân sự cũ chỉ đọc lịch sử phân công của mình qua route riêng; Rider đọc session/plan của mình mà không có current Horse/clinical scope | HorseLifecycleTests 9/9; AdditionalTests, reassignment và SQL concurrency regression |
| #7 Templates / plans | Role/state guards, template archive giữ plan cũ, plan history/paging. Trainer hiện tại tiếp nhận quyền sửa; creator ID không đổi. Tên và tìm kiếm Trainer phụ trách dùng active assignment; hết assignment thì tên fallback về creator. History-only access không trả hạn chế hiện tại của horse ngoài quyền hiện tại | TrainingTests 12/12; reassignment và ManagerWorkflowTests trong full regression |
| #8 Sessions / results / evaluation | Planned → Assigned → InProgress → Completed/IssueReported, result/speed, duplicate guards, evaluation/history và sửa tương lai thủ công. Medical guards kiểm create/edit/assign/start; active/recovering injury tiếp tục chặn Heavy dù có assessment Fit. Giữ observation khi lock xuất hiện giữa buổi | TrainingTests, DatabaseWorkflowTests, WorkflowTests và SQL concurrency regression |
| #9 Medical / treatment | Clearance theo các ID được chọn; không đóng issue khác/treatment chưa chọn hoặc restriction tương lai. Không bắt buộc toàn ngựa Fit. Backdated assessment/follow-up/injury không ghi đè assessment mới hơn; correction giữ ngày và chuỗi supersedes. Tie-break ExaminationAt/CreatedAt/Id dùng thứ tự SQL Server. Clinical list/write chỉ Vet hiện tại trong scope; audit clearance liên kết follow-up với ID đã đóng | MedicalPolicyTests 11/11; clearance/training integration và layer/message tests |
| #10 Preventive care | Vet schedule/complete; reject future/duplicate completion; summary không lộ notes; scope, paging >100; ngày Asia/Ho_Chi_Minh; due reminder chỉ tới active current Vet, không lặp, không nhắc archived/completed records, chưa có recipient thì chưa đánh dấu sent | PreventiveCareTests 3/3; WorkerTests/WorkerSqlServerTests trong full regression |

Ngày kết thúc plan đi qua **không tự chuyển Completed**. Current Trainer xác nhận trạng thái theo buổi đã hoàn thành/bỏ qua; buổi chưa xử lý chặn complete. AdjustFutureSessions là ý định chỉnh, không tự đổi lịch.

## Contract thay đổi

- `GET /api/horses/{id}/assignment-history` trả `horseId`, `horseName`, `archived`, `assignments`. Owner của horse/Manager đọc toàn lịch sử phân công; nhân sự đã từng được giao chỉ nhận assignment của chính mình. Không trả medical records, preferences hoặc current profile. Route này không cấp quyền ghi hay quyền clinical.
- Owner/Manager có thể đọc profile, measurements và horse photo của horse archived; mutation vẫn bị chặn. Danh sách ngựa hiện tại tiếp tục chỉ liệt kê horse chưa archived.
- Training list/detail giữ history của Rider trên sessions thuộc Rider; current horse list/profile/medical summary yêu cầu Assigned/InProgress work. Plan detail chỉ có sessions của Rider đó. Former Trainer giữ read access cho plan do mình tạo và history; quyền ghi thuộc current assignment.
- Plan/session detail thêm `horseArchived` để FE hiển thị lịch sử chỉ đọc. FE dùng tên trong response training đã được cấp quyền khi profile bị từ chối; không giả lập assignment, không nuốt 401/404/500 hoặc lỗi mạng. Các nút ghi tắt với archived/history-only.
- `trainerName` là tên current assigned Trainer khi còn assignment; `TrainingPlan.TrainerId` vẫn là creator để giữ lịch sử. Search nhận cả tên current Trainer và creator, trong scope đã kiểm và trước phân trang.
- `POST /api/horses/{horseId}/medical/follow-ups`: khi `clearance=true`, bắt buộc chọn ít nhất một `restrictionIds`, `injuryIds` hoặc `treatmentIds`. ID phải unique, cùng horse, còn mở và đã có hiệu lực tại ngày khám. `clearance=false` không được kèm lựa chọn để đóng. Khám tái khám không trước lần khám được tham chiếu.
- Treatment chỉ Completed khi ID được Vet chọn rõ; Active/Recovering injury được chọn chuyển Recovered. Không tự resume plan. Audit reference là follow-up ID, detail giữ previous/current record và từng danh sách ID ảnh hưởng.
- `PUT /api/horses/{horseId}/medical/records/{id}` tạo correction mới, giữ nguyên `examinationAt`; đổi ngày phải tạo assessment riêng. Hồ sơ cũ không bị ghi đè/xóa.

Ví dụ follow-up (thay ID bằng dữ liệu thật trong scope Vet):

```json
{
  "previousRecordId": "<medical-record-id>",
  "examination": {
    "examinationAt": "<instant-khong-o-tuong-lai>",
    "reason": "Tái khám",
    "symptoms": "<Vet-nhap>",
    "findings": "<Vet-nhap>",
    "diagnosis": "<Vet-nhap>",
    "healthStatus": "Monitoring",
    "notes": "<Vet-nhap>"
  },
  "clearance": true,
  "outcome": "Kết thúc các mục được chọn",
  "restrictionIds": ["<restriction-id-can-ket-thuc>"],
  "injuryIds": [],
  "treatmentIds": []
}
```

Client cũ chỉ gửi `clearance=true` mà không có ID sẽ nhận 400. Nhóm làm màn hình Vet (#25) cần nối lựa chọn này; không dùng checkbox duy nhất để đóng toàn bộ ngựa.

## Kiểm chứng và phạm vi dữ liệu

- Full backend regression: **145 passed, 1 skipped, 0 failed, tổng 146**. Skip là upload ảnh live Azure cần cấu hình riêng. TRX: `TestResults/task-completion/final/task-completion.trx`; log `TestResults/task-completion-all.log`.
- Sau chỉnh wording lỗi archive-care: kiểm lại HorseLifecycleTests + LayerAndMessageTests **14/14 pass**; `TestResults/archive-message-check.log`.
- FE: **170/170 pass**, lint/build thành công. `TestResults/task-completion-fe.log`. Kiểm history fallback bảo toàn read scope, không cấp write scope hoặc che lỗi session/network.
- Contract current: **80 paths, 105 operations, 151 schemas, 91 secured operations**; không response schema rỗng; upload multipart được giữ.
- SQL tests dùng `HRCMS_Test_<UUID>` do fixture tạo/dọn, worker nền tắt; không sửa dữ liệu Azure HRCMS hay gửi email thật. Không thêm schema/migration cho lần sửa này: scope clearance được lưu trong audit gắn follow-up đã có.

## Bàn giao và nghiệm thu còn lại

Các thay đổi archive/reassignment có phần giao nhau với #22/#23; đối chiếu cùng Đức trước merge để tránh làm đè. Không tự đánh dấu các task của Đức hoặc màn hình của Lạc/Bảo Done.

#9 cũng xử lý gap backend của #21 về selected clearance và temporal assessment. Chưa xác nhận toàn bộ privacy của Care/Report (#24) hoặc E2E của các màn hình Vet/care mới; phần đó cần owner/reviewer riêng. Medical/preventive API có bằng chứng, không đồng nghĩa màn hình #25 đã hoàn thành.

UAT còn cần browser đúng bảy role, SMTP thật/staging và upload live Azure; reviewer ghi bằng chứng rồi cập nhật trạng thái nhóm. Khi demo backend, restart BE để dùng binary mới; không cần migrate hoặc sửa database thật cho các thay đổi này.
