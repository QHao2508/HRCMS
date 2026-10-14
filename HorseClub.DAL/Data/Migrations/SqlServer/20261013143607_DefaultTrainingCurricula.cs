using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class DefaultTrainingCurricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurriculumJson",
                table: "Templates",
                type: "nvarchar(max)",
                maxLength: 16000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemDefault",
                table: "Templates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinimumExpertiseLevel",
                table: "Templates",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "RequiredTrainingTypesJson",
                table: "Templates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Templates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "Templates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TemplateExerciseKey",
                table: "Sessions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseCustomExercise",
                table: "Sessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TemplatePlanOptionKey",
                table: "Plans",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseCustomPlan",
                table: "Plans",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TrainerTemplateAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerTemplateAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerTemplateAssignments_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerTemplateAssignments_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainerTemplateAssignments_Users_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Templates",
                columns: new[] { "Id", "Archived", "CreatedAt", "CurriculumJson", "DistanceMetres", "FrequencyPerWeek", "Goal", "Intensity", "IsSystemDefault", "MinimumExpertiseLevel", "Name", "Notes", "Phase", "RequiredTrainingTypesJson", "SourceUrl", "Surface", "SystemKey", "Version" },
                values: new object[,]
                {
                    { new Guid("9b590cbd-cc33-4f4d-8f03-000000000001"), false, 639274464000000000L, "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"X\\u00E2y d\\u1EF1ng th\\u1EC3 l\\u1EF1c n\\u1EC1n t\\u1EA3ng\",\"goal\":\"X\\u00E2y th\\u1EC3 l\\u1EF1c v\\u00E0 l\\u00E0m quen nh\\u1ECBp v\\u1EADn \\u0111\\u1ED9ng t\\u1EEBng b\\u01B0\\u1EDBc.\",\"phase\":\"Base\"}],\"sessionOptions\":[{\"key\":\"walk-easy\",\"label\":\"\\u0110i b\\u1ED9 nh\\u1EB9\",\"trainingType\":\"Walk\",\"distanceMetres\":400,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"Gi\\u1EEF b\\u01B0\\u1EDBc \\u0111i \\u0111\\u1EC1u v\\u00E0 ph\\u1EA3n h\\u1ED3i \\u1ED5n \\u0111\\u1ECBnh v\\u1EDBi ng\\u01B0\\u1EDDi c\\u01B0\\u1EE1i.\"},{\"key\":\"trot-base\",\"label\":\"N\\u01B0\\u1EDBc ki\\u1EC7u n\\u1EC1n t\\u1EA3ng\",\"trainingType\":\"Trot\",\"distanceMetres\":800,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"Duy tr\\u00EC nh\\u1ECBp n\\u01B0\\u1EDBc ki\\u1EC7u nh\\u1EB9 v\\u00E0 theo d\\u00F5i h\\u1ED3i ph\\u1EE5c.\"}]}", 800m, 3, "Xây thể lực và làm quen nhịp vận động từng bước.", 0, true, 1, "Xây dựng thể lực nền tảng", "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.", "Base", "[\"Walk\",\"Trot\"]", "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/", "Cát", "foundation", 0L },
                    { new Guid("9b590cbd-cc33-4f4d-8f03-000000000002"), false, 639274464000000000L, "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh\",\"goal\":\"Ph\\u00E1t tri\\u1EC3n s\\u1EE9c b\\u1EC1n v\\u00E0 kh\\u1EA3 n\\u0103ng gi\\u1EEF nh\\u1ECBp canter.\",\"phase\":\"Build\"}],\"sessionOptions\":[{\"key\":\"canter-light\",\"label\":\"Canter nh\\u1EB9\",\"trainingType\":\"Canter\",\"distanceMetres\":800,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh v\\u00E0 ki\\u1EC3m so\\u00E1t h\\u01B0\\u1EDBng v\\u1EADn \\u0111\\u1ED9ng.\"},{\"key\":\"canter-steady\",\"label\":\"Canter \\u0111\\u1EC1u\",\"trainingType\":\"Canter\",\"distanceMetres\":1200,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":25,\"target\":\"Gi\\u1EEF nh\\u1ECBp canter \\u0111\\u1EC1u, kh\\u1EDFi \\u0111\\u1ED9ng v\\u00E0 th\\u1EA3 l\\u1ECFng \\u0111\\u1EA7y \\u0111\\u1EE7.\"}]}", 1200m, 3, "Phát triển sức bền và khả năng giữ nhịp canter.", 0, true, 2, "Duy trì nhịp chạy ổn định", "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.", "Build", "[\"Canter\"]", "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/", "Cát", "steady-canter", 0L },
                    { new Guid("9b590cbd-cc33-4f4d-8f03-000000000003"), false, 639274464000000000L, "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Ph\\u00E1t tri\\u1EC3n t\\u1ED1c \\u0111\\u1ED9 theo t\\u1EEBng \\u0111o\\u1EA1n\",\"goal\":\"R\\u00E8n kh\\u1EA3 n\\u0103ng t\\u0103ng t\\u1ED1c c\\u00F3 ki\\u1EC3m so\\u00E1t xen k\\u1EBD h\\u1ED3i ph\\u1EE5c.\",\"phase\":\"Peak\"}],\"sessionOptions\":[{\"key\":\"gallop-interval\",\"label\":\"\\u0110o\\u1EA1n gallop ki\\u1EC3m so\\u00E1t\",\"trainingType\":\"Gallop\",\"distanceMetres\":600,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"V\\u1EADn \\u0111\\u1ED9ng theo t\\u1EEBng \\u0111o\\u1EA1n, xen k\\u1EBD h\\u1ED3i ph\\u1EE5c v\\u00E0 quan s\\u00E1t ph\\u1EA3n \\u1EE9ng.\"},{\"key\":\"sprint-short\",\"label\":\"\\u0110o\\u1EA1n t\\u0103ng t\\u1ED1c ng\\u1EAFn\",\"trainingType\":\"Sprint\",\"distanceMetres\":300,\"intensity\":\"Heavy\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"T\\u0103ng t\\u1ED1c ng\\u1EAFn theo h\\u01B0\\u1EDBng d\\u1EABn chuy\\u00EAn m\\u00F4n, kh\\u00F4ng \\u00E1p d\\u1EE5ng khi c\\u00F3 h\\u1EA1n ch\\u1EBF s\\u1EE9c kh\\u1ECFe.\"}]}", 600m, 3, "Rèn khả năng tăng tốc có kiểm soát xen kẽ hồi phục.", 0, true, 3, "Phát triển tốc độ theo từng đoạn", "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.", "Peak", "[\"Gallop\",\"Sprint\"]", "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/", "Cát", "speed-intervals", 0L },
                    { new Guid("9b590cbd-cc33-4f4d-8f03-000000000004"), false, 639274464000000000L, "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"L\\u00E0m quen c\\u1ED5ng xu\\u1EA5t ph\\u00E1t\",\"goal\":\"X\\u00E2y s\\u1EF1 b\\u00ECnh t\\u0129nh v\\u00E0 ph\\u1EA3n h\\u1ED3i khi v\\u00E0o, ra c\\u1ED5ng xu\\u1EA5t ph\\u00E1t.\",\"phase\":\"Technique\"}],\"sessionOptions\":[{\"key\":\"gate-familiar\",\"label\":\"L\\u00E0m quen c\\u1ED5ng\",\"trainingType\":\"StartingGate\",\"distanceMetres\":100,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"L\\u00E0m quen khu v\\u1EF1c c\\u1ED5ng v\\u00E0 gi\\u1EEF ph\\u1EA3n h\\u1ED3i b\\u00ECnh t\\u0129nh.\"},{\"key\":\"gate-exit\",\"label\":\"Ph\\u1EA3n h\\u1ED3i ra c\\u1ED5ng\",\"trainingType\":\"StartingGate\",\"distanceMetres\":200,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"Th\\u1EF1c h\\u00E0nh v\\u00E0o v\\u00E0 ra c\\u1ED5ng theo t\\u00EDn hi\\u1EC7u c\\u1EE7a ng\\u01B0\\u1EDDi h\\u01B0\\u1EDBng d\\u1EABn.\"}]}", 100m, 3, "Xây sự bình tĩnh và phản hồi khi vào, ra cổng xuất phát.", 0, true, 2, "Làm quen cổng xuất phát", "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.", "Technique", "[\"StartingGate\"]", "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/", "Cát", "starting-gate", 0L },
                    { new Guid("9b590cbd-cc33-4f4d-8f03-000000000005"), false, 639274464000000000L, "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 gi\\u1EEFa c\\u00E1c bu\\u1ED5i n\\u1EB7ng\",\"goal\":\"C\\u00E2n b\\u1EB1ng kh\\u1ED1i l\\u01B0\\u1EE3ng hu\\u1EA5n luy\\u1EC7n b\\u1EB1ng v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9.\",\"phase\":\"Recovery\"}],\"sessionOptions\":[{\"key\":\"recovery-easy\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9\",\"trainingType\":\"Recovery\",\"distanceMetres\":300,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"Theo d\\u00F5i v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 theo \\u0111\\u00E1nh gi\\u00E1 c\\u1EE7a Trainer v\\u00E0 b\\u00E1c s\\u0129.\"},{\"key\":\"walk-recovery\",\"label\":\"\\u0110i b\\u1ED9 th\\u1EA3 l\\u1ECFng\",\"trainingType\":\"Walk\",\"distanceMetres\":500,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"\\u0110i b\\u1ED9 \\u0111\\u1EC1u \\u0111\\u1EC3 th\\u1EA3 l\\u1ECFng, ghi nh\\u1EADn ph\\u1EA3n \\u1EE9ng sau bu\\u1ED5i t\\u1EADp.\"}]}", 300m, 3, "Cân bằng khối lượng huấn luyện bằng vận động nhẹ.", 0, true, 1, "Vận động nhẹ giữa các buổi nặng", "Khung tham khảo. Các lựa chọn quãng đường, thời lượng và cường độ là thông số minh họa của câu lạc bộ; điều chỉnh theo từng ngựa và chỉ định chuyên môn trước khi áp dụng.", "Recovery", "[\"Walk\",\"Recovery\"]", "https://www.greatbritishracing.com/features/the-equine-fitness-plan-how-racehorses-get-fit/", "Cát", "light-recovery", 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_SystemKey",
                table: "Templates",
                column: "SystemKey",
                unique: true,
                filter: "[SystemKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerTemplateAssignments_AssignedBy",
                table: "TrainerTemplateAssignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TrainerTemplateAssignments_TemplateId_TrainerId",
                table: "TrainerTemplateAssignments",
                columns: new[] { "TemplateId", "TrainerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerTemplateAssignments_TrainerId",
                table: "TrainerTemplateAssignments",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerTemplateAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Templates_SystemKey",
                table: "Templates");

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000001"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000002"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000003"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000004"));

            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000005"));

            migrationBuilder.DropColumn(
                name: "CurriculumJson",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "IsSystemDefault",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "MinimumExpertiseLevel",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "RequiredTrainingTypesJson",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "TemplateExerciseKey",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "UseCustomExercise",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "TemplatePlanOptionKey",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "UseCustomPlan",
                table: "Plans");
        }
    }
}
