> Lưu trữ lịch sử trước đợt dọn tài liệu 08/10/2026. Kiến trúc và trạng thái hiện hành: [mục lục docs](../README.md).

# Backlog triển khai

Mỗi hàng là issue dự kiến để tạo trên GitHub. ID HC dùng để liên kết kế hoạch, không phải số issue GitHub đã được tạo. A–E là vai trò mẫu trong kế hoạch; chưa phải tên thành viên thật. Effort S/M/L dùng để chia việc, không phải ước lượng giờ.

| ID | Tuần / Owner | Ưu tiên / Effort | Công việc và tiêu chí nghiệm thu | Dependency |
| --- | --- | --- | --- | --- |
| HC-001 | 1 / A | P0 / M | Repo/CI/conventions: PR build backend; secrets và outputs bị ignore; hướng dẫn clone/run rõ | Không |
| HC-002 | 1 / B | P0 / M | Figma/sitemap/design system: map 7 role, frame và loading/empty/error/denied; review với nhóm | Không |
| HC-003 | 1 / A | P0 / L | RBAC/ERD/OpenAPI: ma trận resource/action/field và medical rules được chốt; FE có contract/mock | Không |
| HC-004 | 2 / A+B | P0 / L | Owner auth: register, email OTP, login, forgot/reset; OTP expired/wrong/reused bị xử lý; rate limit | HC-003 |
| HC-005 | 2 / A+B | P0 / M | Staff accounts: Manager tạo/gán role; staff không tự đăng ký; activation/reset theo policy | HC-004 |
| HC-006 | 2 / C+B | P0 / L | Intake: draft/submit đầy đủ pedigree/documents/physical/health/boarding; không có chọn Trainer | HC-003,004 |
| HC-007 | 2 / A | P0 / M | Upload: limits/type/authorized download; không có public access vào tài liệu; lỗi upload hiện rõ | HC-003 |
| HC-008 | 3 / C+B | P0 / L | Review/revision/resubmit/approval: có reason và audit; approval tạo một Horse; pending chưa có official profile | HC-006,007 |
| HC-009 | 3 / C+B | P0 / M | Horse Profile: tabs, latest measurement và history; Owner khác bị từ chối | HC-008 |
| HC-010 | 3 / C+B | P0 / M | Official staff/Trainer assignment: đúng role; Head Trainer chọn Trainer; history không bị mất | HC-009,005 |
| HC-011 | 3 / C+E | P0 / M | Standard templates: Head Trainer create/edit/archive; goal/phase/distance/intensity/surface/frequency | HC-003 |
| HC-012 | 4 / C+E | P0 / L | Training plans: tạo từ template theo Horse, timeline và health context, pause/complete/history | HC-010,011 |
| HC-013 | 4 / D+E | P0 / L | Medical records/health: Vet khám/chẩn đoán, history; field-level visibility đúng policy | HC-009,003 |
| HC-014 | 4 / D+C | P0 / L | Restrictions/lock: expiry/intensity/distance/no sprint; Trainer không override; race updates được xử lý | HC-013 |
| HC-015 | 4 / C+E | P0 / L | Session scheduling: planned/assigned; no rider lưu Planned; guard ở create/edit/assign/start | HC-012,014 |
| HC-016 | 5 / C+E | P0 / L | Rider execution/results: chỉ session được giao; actual distance/time/speed/intensity, HR tùy chọn; chống submit trùng | HC-015 |
| HC-017 | 5 / C+E | P0 / M | Evaluation: Trainer so target/actual, feedback, comment và điều chỉnh future sessions có history | HC-016 |
| HC-018 | 5 / D+E | P0 / L | Injury/treatment: body location/severity; medication/instructions/frequency; Groom được giao task, không chẩn đoán | HC-013 |
| HC-019 | 5 / D+E | P0 / L | Follow-up/clearance: Vet cập nhật lock/restrictions theo outcome; Trainer được notify; không tự mất history | HC-014,018 |
| HC-020 | 6 / D+B | P1 / L | Daily care/stall/feeding: schedule/complete/skip/issue; approved vs actual portion; occupancy không trùng | HC-009,018 |
| HC-021 | 6 / D+E | P1 / M | Incidents: photo/observation; abnormal rider feedback có issue và chuyển role đúng; idempotent notification | HC-016,020 |
| HC-022 | 6 / D+B | P1 / M | Preventive care: vaccination/deworming/farrier schedules, due/overdue và ghi completion | HC-013 |
| HC-023 | 6 / D+B | P1 / M | Inventory: item/category/movement/minimum stock; low-stock; request replenishment nếu policy yêu cầu | HC-003 |
| HC-024 | 6 / A+B | P0 / M | Notifications: review/revision/assignment/session/medical/follow-up/incident; recipient scope và read state | HC-008,015,019 |
| HC-025 | 7 / A+E | P0 / L | Reports/dashboard: từng role chỉ đúng scope; horse/date/group filters, KPI/table/chart/drill-down; no-data state | HC-017,019,020 |
| HC-026 | 7 / A+B | P0 / M | Audit/archive: actor/action/time, assignment/approval/medical changes; archive giữ history; audit quyền Manager | HC-008,014 |
| HC-027 | 7–8 / E+cả nhóm | P0 / L | UAT/security/regression: 7 role; cross-owner/cross-assignment direct API; lock sau schedule và UI edge cases | HC-004 đến 026 |
| HC-028 | 8 / A+cả nhóm | P0 / M | Staging/release: seed demo, smoke, backup/rollback, runbook; core demo hoàn chỉnh và reviewer ký nghiệm thu | HC-027 |

P0 là bắt buộc cho core hoặc cho an toàn/tích hợp; P1 là supporting và phạm vi có thể thu nhỏ khi thiếu thời gian. Những dependency đi qua P1 chỉ áp dụng cho nghiệm thu supporting/report care; không chặn bàn giao core khi nhóm đã đồng ý cắt scope.

## Kịch bản demo nghiệm thu

1. Owner tạo account, verify OTP và gửi intake; Manager yêu cầu sửa; Owner sửa và gửi lại; Manager approve.
2. Management xác nhận Head Trainer/Groom/Vet; Head Trainer assign Trainer; Trainer tạo plan từ template và assign session cho Rider.
3. Vet tạo restriction; session không tương thích bị chặn bằng API và UI; session phù hợp được thực hiện; Rider nộp result/feedback; Trainer evaluate.
4. Vet follow-up/clearance; training cập nhật theo health context mới; Groom ghi care/feeding/treatment tasks và incident.
5. Mỗi role xem report đúng phạm vi; Owner khác không đọc được horse/report/attachments; mọi quyết định có history/audit.
