using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.BLL.Auth;
using HorseClub.BLL.Common;
using HorseClub.DAL.Data;
using HorseClub.DAL.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Explicit data tool: run from the repository root; credentials remain outside Git.
if (args.Length != 1 || args[0] != "--azure-hrcms") throw new InvalidOperationException("Run with --azure-hrcms to confirm the existing HRCMS Azure database.");
var root = Environment.CurrentDirectory;
var secretFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", "horseclub-backend", "secrets.json");
var secrets = JsonSerializer.Deserialize<Dictionary<string,string>>(await File.ReadAllTextAsync(secretFile))!;
var connection = new SqlConnectionStringBuilder(secrets["ConnectionStrings:SqlServer"]);
if (connection.InitialCatalog != "HRCMS" || connection.DataSource != "tcp:hrcms.database.windows.net,1433" || !connection.Encrypt || connection.TrustServerCertificate)
    throw new InvalidOperationException("Unexpected database target or insecure connection.");
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true, Converters = {new JsonStringEnumConverter(allowIntegerValues:false)} };
var accounts = JsonSerializer.Deserialize<List<DemoAccountSeeder.Account>>(await File.ReadAllTextAsync(Path.Combine(root,"docs/demo/STAFF_SAMPLE_ACCOUNTS.json")),json)!;
var roles = new[] {Role.Veterinarian,Role.HeadTrainer,Role.Trainer,Role.WorkRider,Role.Groom};
if (accounts.Count != 15 || accounts.Select(a=>a.Email).Distinct().Count()!=15 || accounts.Select(a=>a.UserName).Distinct().Count()!=15
    || roles.Any(role=>accounts.Count(a=>a.Role==role)!=3) || accounts.Any(a=>!a.Email.EndsWith("@example.test") || string.IsNullOrWhiteSpace(a.FirstName)||string.IsNullOrWhiteSpace(a.LastName)||string.IsNullOrWhiteSpace(a.Phone)||string.IsNullOrWhiteSpace(a.Address)))
    throw new InvalidOperationException("Expected exactly 3 complete sample accounts per staff role.");
var privateDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HRCMS","demo");
Directory.CreateDirectory(privateDirectory);
var credentialFile = Path.Combine(privateDirectory,"azure-staff-credentials.json");
string password;
if (File.Exists(credentialFile)) {
    using var existing = JsonDocument.Parse(await File.ReadAllTextAsync(credentialFile));
    if (existing.RootElement.GetProperty("database").GetString()!="HRCMS") throw new InvalidOperationException("Credential file belongs to another database.");
    password=existing.RootElement.GetProperty("password").GetString()!;
} else {
    password=Convert.ToHexString(RandomNumberGenerator.GetBytes(10))+"aA1!";
    await File.WriteAllTextAsync(credentialFile,JsonSerializer.Serialize(new {database="HRCMS",password,accounts},json));
}
await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(connection.ConnectionString).Options);
var created = await new DemoAccountSeeder(new HorseClub.DAL.Repositories.AuthRepository(db), new HorseClub.DAL.Data.EfUnitOfWork(db)).Seed(accounts,password,new SecurityOptions());
var emails=accounts.Select(a=>a.Email).ToArray();
var saved=await db.Users.AsNoTracking().Where(u=>emails.Contains(u.Email)).OrderBy(u=>u.Role).ThenBy(u=>u.UserName)
    .Select(u=>new {u.UserName,u.Email,u.Role,u.FirstName,u.LastName,u.Phone,u.Address,u.Active,u.EmailVerified}).ToListAsync();
if (saved.Count!=15 || saved.Any(u=>!u.Active||!u.EmailVerified||string.IsNullOrWhiteSpace(u.Phone)||string.IsNullOrWhiteSpace(u.Address))) throw new InvalidOperationException("Staff data verification failed.");
await File.WriteAllTextAsync(Path.Combine(privateDirectory,"azure-staff-summary.json"),JsonSerializer.Serialize(saved,json));
Console.WriteLine("Created "+created+" accounts; verified 15 complete active sample staff accounts.");
foreach(var role in roles)Console.WriteLine(role+": "+saved.Count(a=>a.Role==role));
Console.WriteLine("Credentials saved locally outside Git: "+credentialFile);
