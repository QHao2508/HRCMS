using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class QueryPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingRevisions_PlanId",
                table: "TrainingRevisions");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ItemId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_HorseId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_PlanId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_RiderId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_OwnerId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Plans_HorseId",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_HorseId",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Horses_OwnerId",
                table: "Horses");

            migrationBuilder.DropIndex(
                name: "IX_CareTasks_HorseId",
                table: "CareTasks");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRevisions_PlanId_CreatedAt",
                table: "TrainingRevisions",
                columns: new[] { "PlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ItemId_CreatedAt",
                table: "StockMovements",
                columns: new[] { "ItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HorseId_Status_ScheduledAt",
                table: "Sessions",
                columns: new[] { "HorseId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlanId_ScheduledAt",
                table: "Sessions",
                columns: new[] { "PlanId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RiderId_Status_ScheduledAt",
                table: "Sessions",
                columns: new[] { "RiderId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_OwnerId_Status_CreatedAt",
                table: "Registrations",
                columns: new[] { "OwnerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_HorseId_Status",
                table: "Plans",
                columns: new[] { "HorseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_Read_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "Read", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_HorseId_Date",
                table: "Measurements",
                columns: new[] { "HorseId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Horses_OwnerId_Archived",
                table: "Horses",
                columns: new[] { "OwnerId", "Archived" });

            migrationBuilder.CreateIndex(
                name: "IX_CareTasks_GroomId_ScheduledAt",
                table: "CareTasks",
                columns: new[] { "GroomId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CareTasks_HorseId_ScheduledAt",
                table: "CareTasks",
                columns: new[] { "HorseId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Audit_ReferenceId_CreatedAt",
                table: "Audit",
                columns: new[] { "ReferenceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_StaffId_Active_HorseId",
                table: "Assignments",
                columns: new[] { "StaffId", "Active", "HorseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingRevisions_PlanId_CreatedAt",
                table: "TrainingRevisions");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ItemId_CreatedAt",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_HorseId_Status_ScheduledAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_PlanId_ScheduledAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_RiderId_Status_ScheduledAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_OwnerId_Status_CreatedAt",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Plans_HorseId_Status",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_Read_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Measurements_HorseId_Date",
                table: "Measurements");

            migrationBuilder.DropIndex(
                name: "IX_Horses_OwnerId_Archived",
                table: "Horses");

            migrationBuilder.DropIndex(
                name: "IX_CareTasks_GroomId_ScheduledAt",
                table: "CareTasks");

            migrationBuilder.DropIndex(
                name: "IX_CareTasks_HorseId_ScheduledAt",
                table: "CareTasks");

            migrationBuilder.DropIndex(
                name: "IX_Audit_ReferenceId_CreatedAt",
                table: "Audit");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_StaffId_Active_HorseId",
                table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRevisions_PlanId",
                table: "TrainingRevisions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ItemId",
                table: "StockMovements",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_HorseId",
                table: "Sessions",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_PlanId",
                table: "Sessions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RiderId",
                table: "Sessions",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_OwnerId",
                table: "Registrations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_HorseId",
                table: "Plans",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_HorseId",
                table: "Measurements",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_OwnerId",
                table: "Horses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CareTasks_HorseId",
                table: "CareTasks",
                column: "HorseId");
        }
    }
}
