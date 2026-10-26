using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace HorseClub.DAL.Data;

public sealed class SqlServerDesignFactory : IDesignTimeDbContextFactory<SqlServerClubDbContext>
{
    /// <summary>
    /// Tạo DbContext design-time cho dotnet-ef từ cấu hình; chỉ dùng để tạo/áp dụng migration SQL Server.
    /// </summary>
    /// <param name="args">Giá trị kiểu string[] dùng trong CreateDbContext.</param>
    public SqlServerClubDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SqlServerClubDbContext>()
        .UseSqlServer(Connection()).Options);

    private static string Connection()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var secretsRoot = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");
        var path = Path.Combine(secretsRoot, "horseclub-backend", "secrets.json");
        if (File.Exists(path))
        {
            using var secrets = JsonDocument.Parse(File.ReadAllText(path));
            if (secrets.RootElement.TryGetProperty("ConnectionStrings:SqlServer", out var flat)
                && !string.IsNullOrWhiteSpace(flat.GetString())) return flat.GetString()!;
            if (secrets.RootElement.TryGetProperty("ConnectionStrings", out var section)
                && section.TryGetProperty("SqlServer", out var nested)
                && !string.IsNullOrWhiteSpace(nested.GetString())) return nested.GetString()!;
        }
        // Keep the no-configuration fallback local; never choose an Azure database implicitly.
        return @"Server=.\SQLEXPRESS;Database=HRCMS;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    }
}
