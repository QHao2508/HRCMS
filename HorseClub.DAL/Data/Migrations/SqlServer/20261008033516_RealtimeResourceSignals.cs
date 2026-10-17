using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RealtimeResourceSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NotificationVersion",
                table: "RealtimeOutboxMessages",
                newName: "SourceVersion");

            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "RealtimeOutboxMessages",
                newName: "SourceId");

            migrationBuilder.RenameIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId_NotificationVersion",
                table: "RealtimeOutboxMessages",
                newName: "IX_RealtimeOutboxMessages_SourceId_SourceVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SourceVersion",
                table: "RealtimeOutboxMessages",
                newName: "NotificationVersion");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "RealtimeOutboxMessages",
                newName: "NotificationId");

            migrationBuilder.RenameIndex(
                name: "IX_RealtimeOutboxMessages_SourceId_SourceVersion",
                table: "RealtimeOutboxMessages",
                newName: "IX_RealtimeOutboxMessages_NotificationId_NotificationVersion");
        }
    }
}
