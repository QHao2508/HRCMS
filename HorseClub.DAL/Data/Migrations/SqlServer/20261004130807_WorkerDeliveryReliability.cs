using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class WorkerDeliveryReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChallengeId",
                table: "EmailMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DiscardedAt",
                table: "EmailMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExpiresAt",
                table: "EmailMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ReferenceId_Type",
                table: "Notifications",
                columns: new[] { "ReferenceId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ChallengeId",
                table: "EmailMessages",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SentAt_DiscardedAt_NextAttemptAt",
                table: "EmailMessages",
                columns: new[] { "SentAt", "DiscardedAt", "NextAttemptAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_EmailMessages_Challenges_ChallengeId",
                table: "EmailMessages",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailMessages_Challenges_ChallengeId",
                table: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ReferenceId_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_ChallengeId",
                table: "EmailMessages");

            migrationBuilder.DropIndex(
                name: "IX_EmailMessages_SentAt_DiscardedAt_NextAttemptAt",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "ChallengeId",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "DiscardedAt",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "EmailMessages");
        }
    }
}
