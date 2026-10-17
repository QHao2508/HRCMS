# Contract cho các hạng mục còn thiếu — giai đoạn 1

Ngày 11/10/2026. Trích snapshot OpenAPI current trong repo; kiểm kê tĩnh, chưa chạy test mới. Source service/DTO trên main là nguồn ưu tiên nếu snapshot lệch. Các route dưới đây là API đã có, không phải đề xuất tạo lại.

## Quy tắc tích hợp bắt buộc

- Bearer/session qua api.js hiện có; metadata enums giữ mã API, chỉ dịch label.
- List dùng items/page/pageSize/total, pageSize tối đa 100. Không cắt âm thầm ở trang đầu. Report là response tổng hợp, không giả lập PageResponse.
- Lỗi 400/401/403/404/409/413/429/500 và kết quả ghi chưa rõ phải hiển thị đúng; không tự replay mutation không idempotent.
- Medical clinical chỉ Vet current scope; Owner/Manager dùng summary. Follow-up clearance chọn explicit restrictionIds/injuryIds/treatmentIds; correction giữ exact examinationAt.
- Care: list Owner/Manager/Groom/Vet; Groom chỉ task mình. Create Manager/Vet, Vet chỉ Treatment/IceBath; record chỉ Groom được giao. Policy privacy #24 còn cần BE sửa trước nghiệm thu.
- Inventory list Manager/Groom; create/movement/archive Manager; request bổ sung Manager/Groom, review chỉ Manager và request còn Pending. Duyệt không tự nhập stock.
- Staff active endpoint đã có, FE chưa nối. Không thay active Manager/Owner; inactive đổi SecurityStamp.
- Stalls response hiện chỉ id/stableId/name/cleaningStatus; thiếu occupancy/horse cho UI quản lý ô. Hào/Đức quyết định mở rộng projection hoặc endpoint đọc occupancy, cập nhật contract tương ứng; không đoán horse từ frontend.
- Không đưa clinical raw vào Owner/Manager qua audit hoặc UI khi policy không cho phép; Hào/Khoa review field matrix.

## Endpoint và DTO hiện có

| Method | Path | Query | Request | Success |
|---|---|---|---|---|
| GET | /api/staff | page, pageSize, role | — | 200 PageResponseOfStaffResponse |
| POST | /api/staff | — | application/json: StaffRequest | 201 UserResponse |
| GET | /api/staff/directory | role, page, pageSize | — | 200 PageResponseOfStaffDirectoryResponse |
| POST | /api/staff/{id}/resend-invitation | — | — | 204 no body |
| PUT | /api/staff/{id}/active | — | application/json: ActiveRequest | 204 no body |
| GET | /api/horses/{horseId}/medical/follow-ups | page, pageSize | — | 200 PageResponseOfMedicalFollowUp |
| POST | /api/horses/{horseId}/medical/follow-ups | — | application/json: FollowUpRequest | 200 MedicalFollowUp |
| GET | /api/horses/{horseId}/medical/preventive-care | page, pageSize | — | 200 PageResponseOfPreventiveCareSummaryResponse |
| POST | /api/horses/{horseId}/medical/preventive-care | — | application/json: PreventiveRequest | 200 PreventiveCare |
| POST | /api/horses/{horseId}/medical/preventive-care/{id}/complete | — | application/json: PreventiveCompletionRequest | 204 no body |
| GET | /api/care/tasks | horseId, status, from, to, page, pageSize | — | 200 PageResponseOfCareTaskSummaryResponse |
| POST | /api/care/tasks | — | application/json: CareRequest | 200 CareTask |
| POST | /api/care/tasks/{id}/record | — | application/json: CareCompletionRequest | 200 CareTask |
| GET | /api/care/incidents | horseId, page, pageSize | — | 200 PageResponseOfIncident |
| POST | /api/care/incidents | — | application/json: IncidentRequest | 200 Incident |
| POST | /api/care/incidents/{id}/resolve | — | — | 204 no body |
| GET | /api/care/stables | page, pageSize | — | 200 PageResponseOfStable |
| POST | /api/care/stables | — | application/json: NameRequest | 200 Stable |
| GET | /api/care/stalls | stableId, page, pageSize | — | 200 PageResponseOfStallSummaryResponse |
| POST | /api/care/stalls | — | application/json: StallRequest | 200 Stall |
| POST | /api/care/stalls/{id}/occupancy | — | application/json: OccupancyRequest | 200 StallOccupancy |
| POST | /api/care/stalls/{id}/vacate | — | — | 204 no body |
| POST | /api/care/stalls/{id}/cleaned | — | — | 204 no body |
| GET | /api/inventory | lowStock, page, pageSize | — | 200 PageResponseOfInventoryItem |
| POST | /api/inventory | — | application/json: InventoryRequest | 200 InventoryItem |
| GET | /api/inventory/{id}/movements | page, pageSize | — | 200 PageResponseOfStockMovement |
| POST | /api/inventory/{id}/movements | — | application/json: MovementRequest | 200 StockMovementResponse |
| POST | /api/inventory/{id}/archive | — | — | 204 no body |
| GET | /api/inventory/replenishments | page, pageSize | — | 200 PageResponseOfReplenishmentRequest |
| POST | /api/inventory/replenishments | — | application/json: ReplenishmentRequestDto | 200 ReplenishmentRequest |
| POST | /api/inventory/replenishments/{id}/review | — | application/json: ReplenishmentReviewRequest | 200 ReplenishmentRequest |
| GET | /api/audit | referenceId, search, from, to, page, pageSize | — | 200 PageResponseOfAuditReadModel |
| GET | /api/reports | horseId, from, to, groupBy | — | 200 ReportResponse |
| GET | /api/care/incidents/{incidentId}/photos | — | — | 200 array(IncidentPhotoResponse) |
| POST | /api/care/incidents/{incidentId}/photos | — | multipart/form-data: object | 201 IncidentPhotoCreatedResponse |
| GET | /api/care/incidents/{incidentId}/photos/{id} | — | — | 200 string, string |

## Champs DTO từ snapshot

Required ở đây là schema-required; validation BLL vẫn có thêm rule ngày/trạng thái/phân công. Nullable không đồng nghĩa bỏ qua business validation.

### PageResponseOfStaffResponse

| Field | Type | Required |
|---|---|---|
| items | array(StaffResponse) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### StaffRequest

| Field | Type | Required |
|---|---|---|
| email | string | yes |
| userName | string | yes |
| firstName | string | yes |
| lastName | string | yes |
| phone | string | yes |
| address | string | yes |
| role | Role | yes |

### UserResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| email | string | yes |
| userName | string | yes |
| firstName | string | yes |
| lastName | string | yes |
| phone | string | yes |
| address | string | yes |
| role | Role | yes |
| emailVerified | boolean | yes |
| active | boolean | yes |

### PageResponseOfStaffDirectoryResponse

| Field | Type | Required |
|---|---|---|
| items | array(StaffDirectoryResponse) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### ActiveRequest

| Field | Type | Required |
|---|---|---|
| active | boolean | yes |

### PageResponseOfMedicalFollowUp

| Field | Type | Required |
|---|---|---|
| items | array(MedicalFollowUp) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### FollowUpRequest

| Field | Type | Required |
|---|---|---|
| previousRecordId | string | yes |
| examination | MedicalRequest | yes |
| clearance | boolean | yes |
| outcome | string | yes |
| restrictionIds | null/array | no |
| injuryIds | null/array | no |
| treatmentIds | null/array | no |

### MedicalFollowUp

| Field | Type | Required |
|---|---|---|
| horseId | string | no |
| previousRecordId | string | no |
| currentRecordId | string | no |
| clearance | boolean | no |
| outcome | string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PageResponseOfPreventiveCareSummaryResponse

| Field | Type | Required |
|---|---|---|
| items | array(PreventiveCareSummaryResponse) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### PreventiveRequest

| Field | Type | Required |
|---|---|---|
| type | PreventiveCareType | yes |
| dueDate | string | yes |
| notes | string | yes |

### PreventiveCare

| Field | Type | Required |
|---|---|---|
| horseId | string | no |
| type | PreventiveCareType | no |
| dueDate | string | no |
| completedDate | null/string | no |
| notes | string | no |
| reminderSent | boolean | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PreventiveCompletionRequest

| Field | Type | Required |
|---|---|---|
| completedDate | string | yes |
| notes | string | yes |

### PageResponseOfCareTaskSummaryResponse

| Field | Type | Required |
|---|---|---|
| items | array(CareTaskSummaryResponse) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### CareRequest

| Field | Type | Required |
|---|---|---|
| horseId | string | yes |
| groomId | string | yes |
| treatmentPlanId | null/string | yes |
| type | CareType | yes |
| scheduledAt | string | yes |
| instructions | string | yes |
| approvedPortionKg | null/number/string | yes |

### CareTask

| Field | Type | Required |
|---|---|---|
| horseId | string | no |
| groomId | string | no |
| treatmentPlanId | null/string | no |
| type | CareType | no |
| scheduledAt | string | no |
| instructions | string | no |
| approvedPortionKg | null/number/string | no |
| actualPortionKg | null/number/string | no |
| status | CareStatus | no |
| completedAt | null/string | no |
| notes | string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### CareCompletionRequest

| Field | Type | Required |
|---|---|---|
| status | CareStatus | yes |
| actualPortionKg | null/number/string | yes |
| notes | string | yes |

### PageResponseOfIncident

| Field | Type | Required |
|---|---|---|
| items | array(Incident) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### IncidentRequest

| Field | Type | Required |
|---|---|---|
| horseId | string | yes |
| occurredAt | string | yes |
| type | IncidentType | yes |
| description | string | yes |
| severity | IncidentSeverity | yes |
| routedTo | Role | yes |

### Incident

| Field | Type | Required |
|---|---|---|
| horseId | string | no |
| reporterId | string | no |
| sessionId | null/string | no |
| occurredAt | string | no |
| type | IncidentType | no |
| description | string | no |
| severity | IncidentSeverity | no |
| routedTo | Role | no |
| resolved | boolean | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PageResponseOfStable

| Field | Type | Required |
|---|---|---|
| items | array(Stable) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### NameRequest

| Field | Type | Required |
|---|---|---|
| name | string | yes |

### Stable

| Field | Type | Required |
|---|---|---|
| name | string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PageResponseOfStallSummaryResponse

| Field | Type | Required |
|---|---|---|
| items | array(StallSummaryResponse) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### StallRequest

| Field | Type | Required |
|---|---|---|
| stableId | string | yes |
| name | string | yes |

### Stall

| Field | Type | Required |
|---|---|---|
| stableId | string | no |
| name | string | no |
| cleaningStatus | CleaningStatus | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### OccupancyRequest

| Field | Type | Required |
|---|---|---|
| horseId | string | yes |

### StallOccupancy

| Field | Type | Required |
|---|---|---|
| stallId | string | no |
| horseId | string | no |
| endedAt | null/string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PageResponseOfInventoryItem

| Field | Type | Required |
|---|---|---|
| items | array(InventoryItem) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### InventoryRequest

| Field | Type | Required |
|---|---|---|
| name | string | yes |
| category | string | yes |
| unit | string | yes |
| minimumStock | number/string | yes |

### InventoryItem

| Field | Type | Required |
|---|---|---|
| name | string | no |
| category | string | no |
| unit | string | no |
| stock | number/string | no |
| minimumStock | number/string | no |
| archived | boolean | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### PageResponseOfStockMovement

| Field | Type | Required |
|---|---|---|
| items | array(StockMovement) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### MovementRequest

| Field | Type | Required |
|---|---|---|
| quantity | number/string | yes |
| reason | string | yes |

### StockMovementResponse

| Field | Type | Required |
|---|---|---|
| item | InventoryItem | yes |
| movement | StockMovement | yes |

### PageResponseOfReplenishmentRequest

| Field | Type | Required |
|---|---|---|
| items | array(ReplenishmentRequest) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### ReplenishmentRequestDto

| Field | Type | Required |
|---|---|---|
| itemId | string | yes |
| quantity | number/string | yes |
| notes | string | yes |

### ReplenishmentRequest

| Field | Type | Required |
|---|---|---|
| itemId | string | no |
| requestedBy | string | no |
| quantity | number/string | no |
| status | ReplenishmentStatus | no |
| notes | string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### ReplenishmentReviewRequest

| Field | Type | Required |
|---|---|---|
| approve | boolean | yes |
| notes | string | yes |

### PageResponseOfAuditReadModel

| Field | Type | Required |
|---|---|---|
| items | array(AuditReadModel) | yes |
| page | integer/string | yes |
| pageSize | integer/string | yes |
| total | integer/string | yes |

### ReportResponse

| Field | Type | Required |
|---|---|---|
| from | string | yes |
| to | string | yes |
| groupBy | ReportGrouping | yes |
| noData | boolean | yes |
| kpi | ReportKpiResponse | yes |
| series | array(ReportSeriesResponse) | yes |
| training | array(ReportTrainingResponse) | yes |
| care | array(ReportCareResponse) | yes |
| clinical | null/array | yes |

### IncidentPhotoResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| fileName | string | yes |
| contentType | string | yes |
| length | integer/string | yes |

### IncidentPhotoCreatedResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| fileName | string | yes |
| length | integer/string | yes |

### StaffResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| userName | string | yes |
| firstName | string | yes |
| lastName | string | yes |
| email | string | yes |
| role | Role | yes |
| active | boolean | yes |
| emailVerified | boolean | yes |

### Role

HorseOwner, ClubManager, HeadTrainer, Trainer, WorkRider, Veterinarian, Groom

### StaffDirectoryResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| firstName | string | yes |
| lastName | string | yes |
| role | Role | yes |

### MedicalRequest

| Field | Type | Required |
|---|---|---|
| examinationAt | string | yes |
| reason | string | yes |
| symptoms | string | yes |
| findings | string | yes |
| diagnosis | string | yes |
| healthStatus | HealthStatus | yes |
| notes | string | yes |

### PreventiveCareSummaryResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| horseId | string | yes |
| type | PreventiveCareType | yes |
| dueDate | string | yes |
| completedDate | null/string | yes |

### PreventiveCareType

Vaccination, Deworming, Farrier

### CareTaskSummaryResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| horseId | string | yes |
| groomId | string | yes |
| type | CareType | yes |
| scheduledAt | string | yes |
| status | CareStatus | yes |
| completedAt | null/string | yes |
| approvedPortionKg | null/number/string | yes |
| actualPortionKg | null/number/string | yes |
| instructions | null/string | yes |
| notes | null/string | yes |

### CareType

Feeding, Cleaning, Bathing, Grooming, WaterCheck, IceBath, Treatment

### CareStatus

Pending, InProgress, Completed, Skipped, IssueReported

### IncidentType

TrainingObservation, CareObservation, Health, Training, Stable, Feeding, Other

### IncidentSeverity

NeedsReview, Low, Medium, High, Critical

### StallSummaryResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| stableId | string | yes |
| name | string | yes |
| cleaningStatus | CleaningStatus | yes |

### CleaningStatus

NeedsCleaning, Clean

### StockMovement

| Field | Type | Required |
|---|---|---|
| itemId | string | no |
| actorId | string | no |
| quantity | number/string | no |
| reason | string | no |
| id | string | no |
| createdAt | string | no |
| version | integer/string | no |

### ReplenishmentStatus

Pending, Approved, Rejected

### AuditReadModel

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| createdAt | string | yes |
| actorId | string | yes |
| actorName | string | yes |
| action | AuditAction | yes |
| referenceId | string | yes |
| detail | string | yes |

### ReportGrouping

Day, Week, Month, Custom

### ReportKpiResponse

| Field | Type | Required |
|---|---|---|
| sessions | integer/string | yes |
| completed | integer/string | yes |
| completionRate | number/string | yes |
| actualDistanceMetres | number/string | yes |
| actualTimeSeconds | number/string | yes |
| careTasks | integer/string | yes |
| completedCare | integer/string | yes |
| medicalExaminations | null/integer/string | yes |

### ReportSeriesResponse

| Field | Type | Required |
|---|---|---|
| period | string | yes |
| sessions | integer/string | yes |
| completed | integer/string | yes |
| plannedDistanceMetres | number/string | yes |
| actualDistanceMetres | number/string | yes |

### ReportTrainingResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| horseId | string | yes |
| scheduledAt | string | yes |
| status | SessionStatus | yes |
| distanceMetres | number/string | yes |
| target | string | yes |
| result | object | yes |

### ReportCareResponse

| Field | Type | Required |
|---|---|---|
| id | string | yes |
| horseId | string | yes |
| type | CareType | yes |
| status | CareStatus | yes |
| scheduledAt | string | yes |
| approvedPortionKg | null/number/string | yes |
| actualPortionKg | null/number/string | yes |

### HealthStatus

Fit, Monitoring, Injured, Isolated

### AuditAction

StaffCreated, StaffActiveChanged, RegistrationDraftCreated, RegistrationEdited, RegistrationSubmitted, RegistrationRevisionRequested, RegistrationApproved, HorseStaffAssigned, RegistrationCancelled, HorseMeasurementAdded, HorseArchived, TrainingPlanCreated, TrainingSessionCreated, TrainingSessionEdited, TrainingSessionStarted, TrainingResultSubmitted, TrainingTemplateCreated, TrainingTemplateEdited, TrainingTemplateArchived, TrainingPlanEdited, TrainingPlanStatus, TrainingSessionSkipped, TrainingEvaluated, MedicalExaminationCreated, MedicalInjuryCreated, MedicalRestrictionCreated, MedicalTreatmentCreated, MedicalClearanceIssued, MedicalFollowUp, MedicalPreventiveScheduled, MedicalPreventiveCompleted, CareTaskCreated, CareTaskRecorded, IncidentCreated, IncidentResolved, StableCreated, StallCreated, StallOccupied, StallVacated, StallCleaned, InventoryCreated, InventoryStockMoved, InventoryArchived, InventoryReplenishmentRequested, InventoryReplenishmentReviewed, AttachmentUploaded, MedicalRecordCorrected, StaffInvitationResent, WebsiteContentUpdated, WebsiteAssetUpdated

### SessionStatus

Planned, Assigned, InProgress, Completed, Skipped, IssueReported
