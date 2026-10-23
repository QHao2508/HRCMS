using System.Text.Json;
using HorseClub.DAL.Data;
using HorseClub.OperationalData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// No credentials on command line; no migration/create-database/startup seeding.
if (args.Length is < 3 or > 4 || args.Length == 4 && args[3] != "--apply")
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/HorseClub.OperationalData -- <manifest.json> <expected-server> <expected-database> [--apply]");
    return 2;
}
try
{
    var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("HRCMS_DATA_CONNECTION")
        ?? throw new InvalidOperationException("Set HRCMS_DATA_CONNECTION privately before running."));
    if (!string.Equals(connection.DataSource, args[1], StringComparison.OrdinalIgnoreCase) || connection.InitialCatalog != args[2]
        || !connection.Encrypt || !connection.IntegratedSecurity && connection.TrustServerCertificate)
        throw new InvalidOperationException("Connection must match the explicit server/database and require encrypted SQL.");
    var manifest = JsonSerializer.Deserialize<OperationalManifest>(await File.ReadAllTextAsync(args[0]), new JsonSerializerOptions(JsonSerializerDefaults.Web))
        ?? throw new InvalidOperationException("Manifest is empty.");
    await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
    var summary = await OperationalDataImporter.Import(db, manifest, args.Length == 4);
    Console.WriteLine(JsonSerializer.Serialize(new { server = connection.DataSource, database = connection.InitialCatalog, summary }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    return 0;
}
catch (Exception error)
{
    // SQL/transport exceptions may include account/server details; do not print raw connection diagnostics.
    Console.Error.WriteLine(error is InvalidOperationException ? error.Message : $"Import failed ({error.GetType().Name}); credentials omitted.");
    return 1;
}
