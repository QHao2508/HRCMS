using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class WebsiteManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebsiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    LogoName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LogoType = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HeroName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HeroType = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BackgroundName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BackgroundType = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebsiteSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebsiteSettings");
        }
    }
}
