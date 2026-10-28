using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <summary>
    /// Mã đăng ký ngựa do câu lạc bộ cấp (HRC-000001…), duy nhất.
    /// Ngựa đang có mà chưa có mã (hoặc mã rỗng / trùng / quá dài) được cấp mã mới theo thứ tự tạo; ngựa đã có mã hợp lệ giữ nguyên.
    /// </summary>
    [DbContext(typeof(SqlServerClubDbContext))]
    [Migration("20261028100000_HorseRegistrationNumberCodes")]
    public partial class HorseRegistrationNumberCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(name: "HorseRegistrationNumbers");

            migrationBuilder.Sql("""
                UPDATE Horses SET RegistrationNumber = NULL
                WHERE RegistrationNumber IS NOT NULL AND (LTRIM(RTRIM(RegistrationNumber)) = N'' OR LEN(RegistrationNumber) > 100);

                -- Mã trùng: giữ cho ngựa tạo sớm nhất, các ngựa còn lại được cấp mã mới.
                WITH ranked AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY RegistrationNumber ORDER BY CreatedAt, Id) AS rn
                    FROM Horses WHERE RegistrationNumber IS NOT NULL)
                UPDATE h SET RegistrationNumber = NULL FROM Horses h JOIN ranked r ON r.Id = h.Id WHERE r.rn > 1;

                DECLARE @id uniqueidentifier, @code nvarchar(100), @n int;
                DECLARE pending CURSOR LOCAL FAST_FORWARD FOR
                    SELECT Id FROM Horses WHERE RegistrationNumber IS NULL ORDER BY CreatedAt, Id;
                OPEN pending;
                FETCH NEXT FROM pending INTO @id;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @code = NULL;
                    WHILE @code IS NULL OR EXISTS (SELECT 1 FROM Horses WHERE RegistrationNumber = @code)
                    BEGIN
                        SET @n = NEXT VALUE FOR [HorseRegistrationNumbers];
                        SET @code = N'HRC-' + CASE WHEN @n > 999999 THEN CAST(@n AS nvarchar(10)) ELSE RIGHT(N'000000' + CAST(@n AS nvarchar(10)), 6) END;
                    END
                    UPDATE Horses SET RegistrationNumber = @code WHERE Id = @id;
                    FETCH NEXT FROM pending INTO @id;
                END
                CLOSE pending;
                DEALLOCATE pending;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Horses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horses_RegistrationNumber",
                table: "Horses",
                column: "RegistrationNumber",
                unique: true,
                filter: "[RegistrationNumber] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Horses_RegistrationNumber", table: "Horses");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationNumber",
                table: "Horses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropSequence(name: "HorseRegistrationNumbers");
        }
    }
}
