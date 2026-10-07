namespace HorseClub.DAL.Enums;

public enum NotificationType
{
    RegistrationReview = 0, RegistrationRevision = 1, RegistrationApproved = 2, HorseAssignment = 3,
    SessionAssigned = 4, SessionUnassigned = 5, SessionResult = 6, IncidentReported = 7, SessionSkipped = 8,
    MedicalHealthChanged = 9, MedicalRestrictionCreated = 10, MedicalFollowUp = 11, MedicalPreventiveDue = 12,
    SessionOverdue = 13, MedicalFollowUpDue = 14, CareAssigned = 15, CareIssue = 16,
    InventoryLowStock = 17, InventoryReplenishment = 18, InventoryReplenishmentReviewed = 19
}
