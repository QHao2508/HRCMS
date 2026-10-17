using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RealtimeNotificationReadSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId",
                table: "RealtimeOutboxMessages");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "RealtimeOutboxMessages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "NotificationVersion",
                table: "RealtimeOutboxMessages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId_NotificationVersion",
                table: "RealtimeOutboxMessages",
                columns: new[] { "NotificationId", "NotificationVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId_NotificationVersion",
                table: "RealtimeOutboxMessages");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "RealtimeOutboxMessages");

            migrationBuilder.DropColumn(
                name: "NotificationVersion",
                table: "RealtimeOutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId",
                table: "RealtimeOutboxMessages",
                column: "NotificationId",
                unique: true);
        }
    }
}
