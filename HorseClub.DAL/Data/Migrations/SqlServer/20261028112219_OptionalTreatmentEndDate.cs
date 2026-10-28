using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class OptionalTreatmentEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "Treatments",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM Treatments WHERE EndDate IS NULL) THROW 51000, 'Resolve open-ended treatment plans before reverting EndDate to NOT NULL.', 1;");
            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "Treatments",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
