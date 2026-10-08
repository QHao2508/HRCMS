using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class RealtimeNotificationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RealtimeOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<long>(type: "bigint", nullable: true),
                    LockedUntil = table.Column<long>(type: "bigint", nullable: true),
                    LeaseOwner = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SentAt = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealtimeOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeOutboxMessages_NotificationId",
                table: "RealtimeOutboxMessages",
                column: "NotificationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RealtimeOutboxMessages_SentAt_NextAttemptAt_LockedUntil",
                table: "RealtimeOutboxMessages",
                columns: new[] { "SentAt", "NextAttemptAt", "LockedUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealtimeOutboxMessages");
        }
    }
}
