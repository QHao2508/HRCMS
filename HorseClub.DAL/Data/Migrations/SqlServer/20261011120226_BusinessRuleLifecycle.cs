using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class BusinessRuleLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplenishmentRequestId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ScheduledEndAt",
                table: "Sessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReceivedAt",
                table: "Replenishments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedBy",
                table: "Replenishments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedQuantity",
                table: "Replenishments",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "Replenishments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReviewedAt",
                table: "Replenishments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                table: "Replenishments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "Incidents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResolvedAt",
                table: "Incidents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedBy",
                table: "Incidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ArchiveCategory",
                table: "Horses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchiveReason",
                table: "Horses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ArchivedAt",
                table: "Horses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedBy",
                table: "Horses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReplenishmentRequestId",
                table: "StockMovements",
                column: "ReplenishmentRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Replenishments_ReplenishmentRequestId",
                table: "StockMovements",
                column: "ReplenishmentRequestId",
                principalTable: "Replenishments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Replenishments_ReplenishmentRequestId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ReplenishmentRequestId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ReplenishmentRequestId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "ReceivedQuantity",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Replenishments");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ArchiveCategory",
                table: "Horses");

            migrationBuilder.DropColumn(
                name: "ArchiveReason",
                table: "Horses");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Horses");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "Horses");
        }
    }
}
