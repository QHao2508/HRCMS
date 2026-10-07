namespace HorseClub.DAL.Enums;

public enum AuditAction
{
    StaffCreated = 0, StaffActiveChanged = 1, RegistrationDraftCreated = 2, RegistrationEdited = 3,
    RegistrationSubmitted = 4, RegistrationRevisionRequested = 5, RegistrationApproved = 6,
    HorseStaffAssigned = 7, RegistrationCancelled = 8, HorseMeasurementAdded = 9, HorseArchived = 10,
    TrainingPlanCreated = 11, TrainingSessionCreated = 12, TrainingSessionEdited = 13,
    TrainingSessionStarted = 14, TrainingResultSubmitted = 15, TrainingTemplateCreated = 16,
    TrainingTemplateEdited = 17, TrainingTemplateArchived = 18, TrainingPlanEdited = 19,
    TrainingPlanStatus = 20, TrainingSessionSkipped = 21, TrainingEvaluated = 22,
    MedicalExaminationCreated = 23, MedicalInjuryCreated = 24, MedicalRestrictionCreated = 25,
    MedicalTreatmentCreated = 26, MedicalClearanceIssued = 27, MedicalFollowUp = 28,
    MedicalPreventiveScheduled = 29, MedicalPreventiveCompleted = 30, CareTaskCreated = 31,
    CareTaskRecorded = 32, IncidentCreated = 33, IncidentResolved = 34, StableCreated = 35,
    StallCreated = 36, StallOccupied = 37, StallVacated = 38, StallCleaned = 39,
    InventoryCreated = 40, InventoryStockMoved = 41, InventoryArchived = 42,
    InventoryReplenishmentRequested = 43, InventoryReplenishmentReviewed = 44, AttachmentUploaded = 45, MedicalRecordCorrected = 46
}
