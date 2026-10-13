using System.Text.Json;
using HorseClub.DAL.Data;
using HorseClub.ReferenceImport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var path = args.FirstOrDefault(x => !x.StartsWith("--")) ?? "docs/data/flow34-public-reference-2026-10-13.json";
var json = await File.ReadAllTextAsync(path); var rows = ReferenceImporter.Parse(json);
Console.WriteLine(JsonSerializer.Serialize(new { mode = args.Contains("--apply") ? "apply" : "validate", rows = rows.Count, groups = rows.GroupBy(x => x.Kind).ToDictionary(x => x.Key, x => x.Count()) }));
if (!args.Contains("--apply")) return;
var secretPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "horseclub-backend", "secrets.json");
using var secrets = JsonDocument.Parse(await File.ReadAllTextAsync(secretPath));
var connection = new SqlConnectionStringBuilder(secrets.RootElement.GetProperty("ConnectionStrings:SqlServer").GetString());
if (connection.DataSource != "tcp:hrcms.database.windows.net,1433" || connection.InitialCatalog != "HRCMS" || !connection.Encrypt || connection.TrustServerCertificate)
    throw new InvalidOperationException("Unexpected Azure SQL target or TLS.");
await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
var result = await ReferenceImporter.Import(db, json);
Console.WriteLine(JsonSerializer.Serialize(result));
// Verify persisted evidence with a fresh connection, not tracked objects.
await using var verify = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
var keys = rows.Select(x => x.ExternalKey).ToArray();
var stored = await verify.PublicReferenceRecords.AsNoTracking().Where(x => keys.Contains(x.ExternalKey)).ToListAsync();
if (stored.Count != rows.Count || stored.Any(x => rows.Single(r => r.ExternalKey == x.ExternalKey).ContentSha256 != x.ContentSha256))
    throw new InvalidOperationException("Persisted reference verification failed.");
Console.WriteLine($"Verified {stored.Count} persisted public reference records in Azure HRCMS.");
