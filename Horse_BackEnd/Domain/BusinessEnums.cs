namespace Horse_BackEnd.Domain;

// Persisted identifiers are explicit. Never reorder/reuse numeric values after release.
public enum HorseGender { Male = 0, Female = 1, Gelding = 2 }
public enum TrainingType { Walk = 0, Trot = 1, Canter = 2, Gallop = 3, Sprint = 4, Recovery = 5 }
public enum InjurySeverity { Mild = 0, Moderate = 1, Severe = 2, Critical = 3 }
public enum InjuryType { Muscle = 0, Tendon = 1, Ligament = 2, Bone = 3, Joint = 4, Hoof = 5, Wound = 6, Other = 7 }
public enum InjuryStatus { Active = 0, Recovering = 1, Recovered = 2 }
public enum IncidentType { TrainingObservation = 0, CareObservation = 1, Health = 2, Training = 3, Stable = 4, Feeding = 5, Other = 6 }
public enum IncidentSeverity { NeedsReview = 0, Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum PreventiveCareType { Vaccination = 0, Deworming = 1, Farrier = 2 }
public enum CleaningStatus { NeedsCleaning = 0, Clean = 1 }
public enum ReplenishmentStatus { Pending = 0, Approved = 1, Rejected = 2 }
public enum AttachmentType { HorsePhoto = 0, Certificate = 1, MedicalDocument = 2, IncidentPhoto = 3 }
public enum ChallengePurpose { Verify = 0, Reset = 1, Invite = 2 }
public enum EmailDeliveryMode { Smtp = 0, DevelopmentFile = 1 }
public enum DatabaseProvider { Sqlite = 0, SqlServer = 1 }
public enum ReportGrouping { Day = 0, Week = 1, Month = 2, Custom = 3 }
public enum NotificationType
{
    RegistrationReview = 0, RegistrationRevision = 1, RegistrationApproved = 2, HorseAssignment = 3,
    SessionAssigned = 4, SessionUnassigned = 5, SessionResult = 6, IncidentReported = 7, SessionSkipped = 8,
    MedicalHealthChanged = 9, MedicalRestrictionCreated = 10, MedicalFollowUp = 11, MedicalPreventiveDue = 12,
    SessionOverdue = 13, MedicalFollowUpDue = 14, CareAssigned = 15, CareIssue = 16,
    InventoryLowStock = 17, InventoryReplenishment = 18, InventoryReplenishmentReviewed = 19
}
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
