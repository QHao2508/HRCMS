using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HorseClub.DAL.Data.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class LocalizeDefaultTrainingCurricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000002"),
                columns: new[] { "CurriculumJson", "Goal" },
                values: new object[] { "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh\",\"goal\":\"Ph\\u00E1t tri\\u1EC3n s\\u1EE9c b\\u1EC1n v\\u00E0 kh\\u1EA3 n\\u0103ng gi\\u1EEF nh\\u1ECBp ch\\u1EA1y n\\u01B0\\u1EDBc \\u0111\\u1EA1i ch\\u1EADm.\",\"phase\":\"Build\"}],\"sessionOptions\":[{\"key\":\"canter-light\",\"label\":\"Ch\\u1EA1y n\\u01B0\\u1EDBc \\u0111\\u1EA1i ch\\u1EADm nh\\u1EB9\",\"trainingType\":\"Canter\",\"distanceMetres\":800,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh v\\u00E0 ki\\u1EC3m so\\u00E1t h\\u01B0\\u1EDBng v\\u1EADn \\u0111\\u1ED9ng.\"},{\"key\":\"canter-steady\",\"label\":\"Ch\\u1EA1y n\\u01B0\\u1EDBc \\u0111\\u1EA1i ch\\u1EADm \\u0111\\u1EC1u\",\"trainingType\":\"Canter\",\"distanceMetres\":1200,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":25,\"target\":\"Gi\\u1EEF nh\\u1ECBp ch\\u1EA1y n\\u01B0\\u1EDBc \\u0111\\u1EA1i ch\\u1EADm \\u0111\\u1EC1u, kh\\u1EDFi \\u0111\\u1ED9ng v\\u00E0 th\\u1EA3 l\\u1ECFng \\u0111\\u1EA7y \\u0111\\u1EE7.\"}]}", "Phát triển sức bền và khả năng giữ nhịp chạy nước đại chậm." });

            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000003"),
                column: "CurriculumJson",
                value: "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Ph\\u00E1t tri\\u1EC3n t\\u1ED1c \\u0111\\u1ED9 theo t\\u1EEBng \\u0111o\\u1EA1n\",\"goal\":\"R\\u00E8n kh\\u1EA3 n\\u0103ng t\\u0103ng t\\u1ED1c c\\u00F3 ki\\u1EC3m so\\u00E1t xen k\\u1EBD h\\u1ED3i ph\\u1EE5c.\",\"phase\":\"Peak\"}],\"sessionOptions\":[{\"key\":\"gallop-interval\",\"label\":\"\\u0110o\\u1EA1n ch\\u1EA1y n\\u01B0\\u1EDBc \\u0111\\u1EA1i c\\u00F3 ki\\u1EC3m so\\u00E1t\",\"trainingType\":\"Gallop\",\"distanceMetres\":600,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"V\\u1EADn \\u0111\\u1ED9ng theo t\\u1EEBng \\u0111o\\u1EA1n, xen k\\u1EBD h\\u1ED3i ph\\u1EE5c v\\u00E0 quan s\\u00E1t ph\\u1EA3n \\u1EE9ng.\"},{\"key\":\"sprint-short\",\"label\":\"\\u0110o\\u1EA1n t\\u0103ng t\\u1ED1c ng\\u1EAFn\",\"trainingType\":\"Sprint\",\"distanceMetres\":300,\"intensity\":\"Heavy\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"T\\u0103ng t\\u1ED1c ng\\u1EAFn theo h\\u01B0\\u1EDBng d\\u1EABn chuy\\u00EAn m\\u00F4n, kh\\u00F4ng \\u00E1p d\\u1EE5ng khi c\\u00F3 h\\u1EA1n ch\\u1EBF s\\u1EE9c kh\\u1ECFe.\"}]}");

            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000005"),
                column: "CurriculumJson",
                value: "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 gi\\u1EEFa c\\u00E1c bu\\u1ED5i n\\u1EB7ng\",\"goal\":\"C\\u00E2n b\\u1EB1ng kh\\u1ED1i l\\u01B0\\u1EE3ng hu\\u1EA5n luy\\u1EC7n b\\u1EB1ng v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9.\",\"phase\":\"Recovery\"}],\"sessionOptions\":[{\"key\":\"recovery-easy\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9\",\"trainingType\":\"Recovery\",\"distanceMetres\":300,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"Theo d\\u00F5i v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 theo \\u0111\\u00E1nh gi\\u00E1 c\\u1EE7a hu\\u1EA5n luy\\u1EC7n vi\\u00EAn v\\u00E0 b\\u00E1c s\\u0129.\"},{\"key\":\"walk-recovery\",\"label\":\"\\u0110i b\\u1ED9 th\\u1EA3 l\\u1ECFng\",\"trainingType\":\"Walk\",\"distanceMetres\":500,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"\\u0110i b\\u1ED9 \\u0111\\u1EC1u \\u0111\\u1EC3 th\\u1EA3 l\\u1ECFng, ghi nh\\u1EADn ph\\u1EA3n \\u1EE9ng sau bu\\u1ED5i t\\u1EADp.\"}]}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000002"),
                columns: new[] { "CurriculumJson", "Goal" },
                values: new object[] { "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh\",\"goal\":\"Ph\\u00E1t tri\\u1EC3n s\\u1EE9c b\\u1EC1n v\\u00E0 kh\\u1EA3 n\\u0103ng gi\\u1EEF nh\\u1ECBp canter.\",\"phase\":\"Build\"}],\"sessionOptions\":[{\"key\":\"canter-light\",\"label\":\"Canter nh\\u1EB9\",\"trainingType\":\"Canter\",\"distanceMetres\":800,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"Duy tr\\u00EC nh\\u1ECBp ch\\u1EA1y \\u1ED5n \\u0111\\u1ECBnh v\\u00E0 ki\\u1EC3m so\\u00E1t h\\u01B0\\u1EDBng v\\u1EADn \\u0111\\u1ED9ng.\"},{\"key\":\"canter-steady\",\"label\":\"Canter \\u0111\\u1EC1u\",\"trainingType\":\"Canter\",\"distanceMetres\":1200,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":25,\"target\":\"Gi\\u1EEF nh\\u1ECBp canter \\u0111\\u1EC1u, kh\\u1EDFi \\u0111\\u1ED9ng v\\u00E0 th\\u1EA3 l\\u1ECFng \\u0111\\u1EA7y \\u0111\\u1EE7.\"}]}", "Phát triển sức bền và khả năng giữ nhịp canter." });

            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000003"),
                column: "CurriculumJson",
                value: "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"Ph\\u00E1t tri\\u1EC3n t\\u1ED1c \\u0111\\u1ED9 theo t\\u1EEBng \\u0111o\\u1EA1n\",\"goal\":\"R\\u00E8n kh\\u1EA3 n\\u0103ng t\\u0103ng t\\u1ED1c c\\u00F3 ki\\u1EC3m so\\u00E1t xen k\\u1EBD h\\u1ED3i ph\\u1EE5c.\",\"phase\":\"Peak\"}],\"sessionOptions\":[{\"key\":\"gallop-interval\",\"label\":\"\\u0110o\\u1EA1n gallop ki\\u1EC3m so\\u00E1t\",\"trainingType\":\"Gallop\",\"distanceMetres\":600,\"intensity\":\"Moderate\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"V\\u1EADn \\u0111\\u1ED9ng theo t\\u1EEBng \\u0111o\\u1EA1n, xen k\\u1EBD h\\u1ED3i ph\\u1EE5c v\\u00E0 quan s\\u00E1t ph\\u1EA3n \\u1EE9ng.\"},{\"key\":\"sprint-short\",\"label\":\"\\u0110o\\u1EA1n t\\u0103ng t\\u1ED1c ng\\u1EAFn\",\"trainingType\":\"Sprint\",\"distanceMetres\":300,\"intensity\":\"Heavy\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"T\\u0103ng t\\u1ED1c ng\\u1EAFn theo h\\u01B0\\u1EDBng d\\u1EABn chuy\\u00EAn m\\u00F4n, kh\\u00F4ng \\u00E1p d\\u1EE5ng khi c\\u00F3 h\\u1EA1n ch\\u1EBF s\\u1EE9c kh\\u1ECFe.\"}]}");

            migrationBuilder.UpdateData(
                table: "Templates",
                keyColumn: "Id",
                keyValue: new Guid("9b590cbd-cc33-4f4d-8f03-000000000005"),
                column: "CurriculumJson",
                value: "{\"planOptions\":[{\"key\":\"standard\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 gi\\u1EEFa c\\u00E1c bu\\u1ED5i n\\u1EB7ng\",\"goal\":\"C\\u00E2n b\\u1EB1ng kh\\u1ED1i l\\u01B0\\u1EE3ng hu\\u1EA5n luy\\u1EC7n b\\u1EB1ng v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9.\",\"phase\":\"Recovery\"}],\"sessionOptions\":[{\"key\":\"recovery-easy\",\"label\":\"V\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9\",\"trainingType\":\"Recovery\",\"distanceMetres\":300,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":15,\"target\":\"Theo d\\u00F5i v\\u1EADn \\u0111\\u1ED9ng nh\\u1EB9 theo \\u0111\\u00E1nh gi\\u00E1 c\\u1EE7a Trainer v\\u00E0 b\\u00E1c s\\u0129.\"},{\"key\":\"walk-recovery\",\"label\":\"\\u0110i b\\u1ED9 th\\u1EA3 l\\u1ECFng\",\"trainingType\":\"Walk\",\"distanceMetres\":500,\"intensity\":\"Light\",\"surface\":\"C\\u00E1t\",\"durationMinutes\":20,\"target\":\"\\u0110i b\\u1ED9 \\u0111\\u1EC1u \\u0111\\u1EC3 th\\u1EA3 l\\u1ECFng, ghi nh\\u1EADn ph\\u1EA3n \\u1EE9ng sau bu\\u1ED5i t\\u1EADp.\"}]}");
            // The preceding hand-written migration has no target model; rollback needs explicit SQL types.
            foreach (var operation in migrationBuilder.Operations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.UpdateDataOperation>())
            {
                operation.KeyColumnTypes = ["uniqueidentifier"];
                operation.ColumnTypes = operation.Columns.Select(_ => "nvarchar(max)").ToArray();
            }
        }
    }
}
