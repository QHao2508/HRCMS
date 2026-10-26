using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HorseClub.OperationalData;

public static class DemoOperationsCommand
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length is < 4 or > 5 || args.Length == 5 && args[4] != "--apply")
        {
            Console.Error.WriteLine("Usage: --demo-operations <manifest.json> <expected-server> <expected-database> <manager-username> [--apply]");
            return 2;
        }
        try
        {
            var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HRCMS_DATA_CONNECTION")
                ?? throw new InvalidOperationException("Set HRCMS_DATA_CONNECTION privately."));
            if (!string.Equals(connection.DataSource, args[1], StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != args[2]
                || !connection.Encrypt || !connection.IntegratedSecurity && connection.TrustServerCertificate)
                throw new InvalidOperationException("Connection must match the explicit encrypted server/database.");
            var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
            var manifest = JsonSerializer.Deserialize<DemoOperationsManifest>(await File.ReadAllTextAsync(args[0]), json)
                ?? throw new InvalidOperationException("Manifest is empty.");
            await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
            var manager = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.UserName == args[3] && u.Role == Role.ClubManager && u.Active && u.EmailVerified);
            var password = Environment.GetEnvironmentVariable("HRCMS_DEMO_MANAGER_PASSWORD");
            if (manager is null || string.IsNullOrEmpty(password) || manager.LockedUntil > DateTimeOffset.UtcNow
                || new PasswordHasher<User>().VerifyHashedPassword(manager, manager.PasswordHash, password) == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("An existing active manager and valid HRCMS_DEMO_MANAGER_PASSWORD are required.");
            var summary = await DemoOperationsImporter.Import(db, manager, manifest, args.Length == 5);
            Console.WriteLine(JsonSerializer.Serialize(new { database = connection.InitialCatalog, summary }, json));
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error is InvalidOperationException ? error.Message : $"Demo operations failed ({error.GetType().Name}); credentials omitted.");
            return 1;
        }
    }
}
