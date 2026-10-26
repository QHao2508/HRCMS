using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <summary>
    /// Lưu mã MessageKey cho thông báo để client dịch theo mã thay vì so khớp câu tiếng Anh.
    /// Thông báo cũ được điền mã một lần từ nội dung đã lưu; nếu không khớp thì lấy mã mặc định theo loại.
    /// </summary>
    [DbContext(typeof(SqlServerClubDbContext))]
    [Migration("20261026020000_NotificationMessageCodes")]
    public partial class NotificationMessageCodes : Migration
    {
        // Bản chụp cố định nội dung đã lưu tại thời điểm migration; không đọc catalog đang chạy vì catalog có thể đổi về sau.
        private static readonly (string Code, string Message)[] StoredMessages =
        [
                ("HorseIsArchived", "Horse is archived."),
                ("AHorseRegistrationRequiresReview", "A horse registration requires review."),
                ("YourHorseRegistrationRequiresRevision", "Your horse registration requires revision."),
                ("YourHorseRegistrationWasApproved", "Your horse registration was approved."),
                ("YouHaveBeenAssignedToAHorse", "You have been assigned to a horse."),
                ("OfficialHorseStaffAssignmentChanged", "Official horse staff assignment changed."),
                ("ATrainingSessionWasAssignedToYou", "A training session was assigned to you."),
                ("ATrainingAssignmentWasRemoved", "A training assignment was removed."),
                ("ARiderSubmittedASessionResult", "A rider submitted a session result."),
                ("ATrainingObservationRequiresReview", "A training observation requires review."),
                ("ANewIncidentRequiresReview", "A new incident requires review."),
                ("ASessionWasSkipped", "A session was skipped."),
                ("HorseHealthStatusChangedReviewCurrentTrainingLimits", "Horse health status changed. Review current training limits."),
                ("TrainingRestrictionsChangedReviewPlannedSessions", "Training restrictions changed. Review planned sessions."),
                ("AMedicalRestrictionChangedDuringYourSessionStopIncompatible", "A medical restriction changed during your session. Stop incompatible activity and contact the Veterinarian."),
                ("MedicalClearanceIssuedReviewTheTrainingPlanBeforeResuming", "Medical clearance issued. Review the training plan before resuming."),
                ("MedicalFollowUpCompletedReviewCurrentRestrictions", "Medical follow-up completed. Review current restrictions."),
                ("PreventiveCareIsDue", "Preventive care is due."),
                ("ATrainingSessionIsOverdue", "A training session is overdue."),
                ("MedicalFollowUpIsDue", "Medical follow-up is due."),
                ("ACareTaskWasAssignedToYou", "A care task was assigned to you."),
                ("ACareTaskRequiresReview", "A care task requires review."),
                ("AnInventoryItemReachedItsMinimumStock", "An inventory item reached its minimum stock."),
                ("AReplenishmentRequestNeedsReview", "A replenishment request needs review."),
                ("YourReplenishmentRequestWasReviewed", "Your replenishment request was reviewed."),
                ("TreatmentTaskCompletedReviewPlan", "A groom completed a treatment care task. Review the treatment plan status."),
                ("ThePlanWasArchivedAndThisSessionWasCancelled", "The plan was archived and this session was cancelled."),
                ("StaffDisabledWithOpenWork", "A staff account was disabled with open work requiring reassignment.")
        ];
        private static readonly (int Type, string Code)[] TypeDefaults =
        [
                (0, "AHorseRegistrationRequiresReview"),
                (1, "YourHorseRegistrationRequiresRevision"),
                (2, "YourHorseRegistrationWasApproved"),
                (3, "OfficialHorseStaffAssignmentChanged"),
                (4, "ATrainingSessionWasAssignedToYou"),
                (5, "ATrainingAssignmentWasRemoved"),
                (6, "ARiderSubmittedASessionResult"),
                (7, "ANewIncidentRequiresReview"),
                (8, "ASessionWasSkipped"),
                (9, "HorseHealthStatusChangedReviewCurrentTrainingLimits"),
                (10, "TrainingRestrictionsChangedReviewPlannedSessions"),
                (11, "MedicalFollowUpCompletedReviewCurrentRestrictions"),
                (12, "PreventiveCareIsDue"),
                (13, "ATrainingSessionIsOverdue"),
                (14, "MedicalFollowUpIsDue"),
                (15, "ACareTaskWasAssignedToYou"),
                (16, "ACareTaskRequiresReview"),
                (17, "AnInventoryItemReachedItsMinimumStock"),
                (18, "AReplenishmentRequestNeedsReview"),
                (19, "YourReplenishmentRequestWasReviewed"),
                (20, "TreatmentTaskCompletedReviewPlan")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageCode",
                table: "Notifications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            foreach (var (code, message) in StoredMessages)
                migrationBuilder.Sql($"UPDATE [Notifications] SET [MessageCode] = N'{code}' WHERE [MessageCode] = N'' AND [Message] = N'{message.Replace("'", "''")}';");
            foreach (var (type, code) in TypeDefaults)
                migrationBuilder.Sql($"UPDATE [Notifications] SET [MessageCode] = N'{code}' WHERE [MessageCode] = N'' AND [Type] = {type};");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageCode",
                table: "Notifications");
        }
    }
}
