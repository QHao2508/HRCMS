using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.DAL.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var resetDemoPassword = args.Length == 3 && args[2] == "--reset-demo-password";
if (args.Length != 2 && !resetDemoPassword)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/HorseClub.DemoSeed -- <repository-path> <existing-demo-database> [--reset-demo-password]");
    return 2;
}
var root = args[0];
var expectedDatabase = args[1];
try
{
    root = Path.GetFullPath(root);
    var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(root, "Horse_BackEnd", "appsettings.json")).Build();
    var connection = new SqlConnectionStringBuilder(config.GetConnectionString("SqlServer"));
    // Explicit demo target and Windows authentication only; never migrate or create a database.
    if (connection.InitialCatalog != expectedDatabase || (expectedDatabase != "HRCMS" && !expectedDatabase.EndsWith("_Test", StringComparison.Ordinal))
        || !connection.IntegratedSecurity || connection.DataSource is not (@".\SQLEXPRESS" or @"localhost\SQLEXPRESS"))
        throw new InvalidOperationException("Select local SQL Express HRCMS or a demo database ending in _Test. Target must match appsettings.json.");
    var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    { WriteIndented = true, Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    var accounts = JsonSerializer.Deserialize<List<DemoAccountSeeder.Account>>(
        await File.ReadAllTextAsync(Path.Combine(root, "docs", "demo", "BE02-accounts.json")), json)
        ?? throw new InvalidOperationException("Demo accounts configuration missing.");
    if (accounts.Count == 0 || accounts.Select(a => a.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Count
        || accounts.Select(a => a.UserName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Count
        || accounts.Any(a => !a.Email.EndsWith("@example.test", StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException("Demo accounts must have unique names/emails in example.test.");
    var credentialPath = Path.Combine(root, "TestResults", "BE02", "demo-credentials.json");
    Directory.CreateDirectory(Path.GetDirectoryName(credentialPath)!);
    string password;
    if (resetDemoPassword)
    {
        password = Environment.GetEnvironmentVariable("HRCMS_DEMO_PASSWORD")
            ?? throw new InvalidOperationException("Set HRCMS_DEMO_PASSWORD for the explicit demo password reset.");
    }
    else if (File.Exists(credentialPath))
    {
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(credentialPath));
        if (credentials.RootElement.GetProperty("database").GetString() != expectedDatabase)
            throw new InvalidOperationException("Existing credentials belong to another database.");
        password = credentials.RootElement.GetProperty("password").GetString()!;
    }
    else
    {
        password = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)) + "aA1!";
        // Write before the DB commit so a process interruption cannot lose the chosen password.
        await File.WriteAllTextAsync(credentialPath, JsonSerializer.Serialize(new { database = expectedDatabase, password, accounts }, json));
    }
    var security = config.GetSection(SecurityOptions.Section).Get<SecurityOptions>() ?? new();
    await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
    var count = await new DemoAccountSeeder(new HorseClub.DAL.Repositories.AuthRepository(db), new HorseClub.DAL.Data.EfUnitOfWork(db)).Seed(accounts, password, security, resetDemoPassword);
    if (resetDemoPassword)
        await File.WriteAllTextAsync(credentialPath, JsonSerializer.Serialize(new { database = expectedDatabase, password, accounts }, json));
    Console.WriteLine(resetDemoPassword
        ? $"Created {count} accounts; reset passwords for matching demo accounts."
        : $"Created {count} accounts; existing matching accounts kept unchanged.");
    foreach (var account in accounts) Console.WriteLine($"{account.Role}: {account.Email}");
    Console.WriteLine($"Local credentials (Git ignored): {credentialPath}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"Demo seed failed: {error.Message}");
    return 1;
}
