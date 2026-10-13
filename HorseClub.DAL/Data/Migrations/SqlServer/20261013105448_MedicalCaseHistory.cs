using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class MedicalCaseHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_HorseId",
                table: "MedicalRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "CaseRootId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialSnapshotJson",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                maxLength: 200000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentSnapshotJson",
                table: "FollowUps",
                type: "nvarchar(max)",
                maxLength: 200000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousSnapshotJson",
                table: "FollowUps",
                type: "nvarchar(max)",
                maxLength: 200000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_HorseId_CaseRootId",
                table: "MedicalRecords",
                columns: new[] { "HorseId", "CaseRootId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_HorseId_CaseRootId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "CaseRootId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "InitialSnapshotJson",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "CurrentSnapshotJson",
                table: "FollowUps");

            migrationBuilder.DropColumn(
                name: "PreviousSnapshotJson",
                table: "FollowUps");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_HorseId",
                table: "MedicalRecords",
                column: "HorseId");
        }
    }
}
