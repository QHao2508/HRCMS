using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HorseClub.DAL.Data;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class ClubFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> extraSettings;
    public ClubFactory(IReadOnlyDictionary<string, string?>? settings = null, IClubMailSender? mailSender = null, DbCommandInterceptor? interceptor = null, TimeProvider? clock = null)
    {
        this.mailSender = mailSender;
        this.interceptor = interceptor;
        this.clock = clock;
        extraSettings = settings ?? new Dictionary<string, string?>();
        var server = Environment.GetEnvironmentVariable("HRCMS_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(server))
            throw new InvalidOperationException("Set HRCMS_TEST_SQLSERVER to a SQL Server instance with permission to create temporary test databases.");
        // Never migrate or delete the database named by the supplied connection string.
        ownedDatabase = "HRCMS_Test_" + Guid.NewGuid().ToString("N");
        sqlConnection = new SqlConnectionStringBuilder(server) { InitialCatalog = ownedDatabase }.ConnectionString;
    }
    private ClubFactory(ClubFactory owner)
    {
        extraSettings = owner.extraSettings;
        mailSender = owner.mailSender;
        interceptor = owner.interceptor;
        clock = owner.clock;
        directory = owner.directory;
        sqlConnection = owner.sqlConnection;
    }
    public ClubFactory Replica()
    {
        if (sqlConnection is null) throw new InvalidOperationException("Replica tests require HRCMS_TEST_SQLSERVER.");
        return new ClubFactory(this);
    }
    private readonly string? sqlConnection;
    internal string SqlTestConnection => sqlConnection ?? throw new InvalidOperationException("SQL Server fixture required.");
    private readonly IClubMailSender? mailSender;
    private readonly DbCommandInterceptor? interceptor;
    private readonly TimeProvider? clock;
    private readonly string? ownedDatabase;
    private bool configured;
    public const string Password = "TestPassword123!";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly string directory = Path.Combine(Path.GetTempPath(), "horseclub-tests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        configured = true;
        // Keep startup failures observable without depending on Windows Event Log permissions.
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        Directory.CreateDirectory(directory);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["Database:AutoMigrate"] = "true",
            ["DataProtection:Path"] = Path.Combine(directory, "keys"),
            ["Storage:Path"] = Path.Combine(directory, "uploads"),
            ["Storage:Provider"] = "Local",
            ["Workers:Enabled"] = "false",
            ["Bootstrap:ManagerEmail"] = "manager@example.test",
            ["Bootstrap:ManagerPassword"] = Password,
            ["Business:TimeZoneId"] = "UTC"
        }).AddInMemoryCollection(extraSettings).AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "SqlServer",
            ["ConnectionStrings:SqlServer"] = sqlConnection
        }));
        // Override the production provider explicitly for the isolated test database. Minimal hosting
        // may register its DbContext before the test configuration callback is applied.
        // Tests must never inherit a developer's SQL Server connection from User Secrets.
        builder.ConfigureTestServices(services =>
        {
            if (clock is not null) { services.RemoveAll<TimeProvider>(); services.AddSingleton(clock); }
            services.RemoveAll<ClubDbContext>();
            services.RemoveAll<SqlServerClubDbContext>();
            services.RemoveAll<DbContextOptions<SqlServerClubDbContext>>();
            services.AddDbContext<SqlServerClubDbContext>(o =>
            {
                o.UseSqlServer(sqlConnection);
                if (interceptor is not null) o.AddInterceptors(interceptor);
            });
            services.AddScoped<ClubDbContext>(sp => sp.GetRequiredService<SqlServerClubDbContext>());
        });
        if (mailSender is not null)
            builder.ConfigureTestServices(services => { services.RemoveAll<IClubMailSender>(); services.AddSingleton(mailSender); });
    }
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!configured || ownedDatabase is null || sqlConnection is null) return;
        var target = new SqlConnectionStringBuilder(sqlConnection);
        if (target.InitialCatalog != ownedDatabase || !System.Text.RegularExpressions.Regex.IsMatch(ownedDatabase, "^HRCMS_Test_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing to delete a database not owned by this fixture.");
        await using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>().UseSqlServer(sqlConnection).Options);
        await db.Database.EnsureDeletedAsync();
    }
    public async Task<User> User(Role role, string? email = null)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var u = new User { Email = email ?? $"{Guid.NewGuid():N}@example.test", UserName = Guid.NewGuid().ToString("N"), FirstName = "Test", LastName = role.ToString(), Role = role, EmailVerified = true };
        u.PasswordHash = new PasswordHasher<User>().HashPassword(u, Password); db.Users.Add(u); await db.SaveChangesAsync(); return u;
    }
    public async Task<HttpClient> Client(User? u = null)
    {
        var client = CreateClient(); var login = await client.PostAsJsonAsync("/api/auth/login", new { email = u?.Email ?? "manager@example.test", password = Password });
        Assert.True(login.IsSuccessStatusCode, await login.Content.ReadAsStringAsync());
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString()); return client;
    }
    public async Task<Horse> Horse(User owner, params User[] staff)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var r = new HorseRegistration { OwnerId = owner.Id, Name = "Thunder", Status = RegistrationStatus.Approved }; db.Registrations.Add(r);
        var h = new Horse { OwnerId = owner.Id, RegistrationId = r.Id, Name = "Thunder", HealthStatus = HealthStatus.Fit, DateOfBirth = new DateOnly(2020, 1, 1) }; db.Horses.Add(h);
        foreach (var u in staff) db.Assignments.Add(new StaffAssignment { HorseId = h.Id, StaffId = u.Id, Role = u.Role, StartDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await db.SaveChangesAsync(); return h;
    }
    public async Task<string> Code(string email, string purpose)
    {
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var challengePurpose = Enum.Parse<ChallengePurpose>(purpose);
        var message = await db.EmailMessages.Where(x => x.Recipient == email
            && db.Challenges.Any(c => c.Id == x.ChallengeId && c.Purpose == challengePurpose))
            .OrderByDescending(x => x.CreatedAt).FirstAsync();
        await Task.CompletedTask; return message.Body.Split('\n')[0].Split(": ")[1];
    }
    public static async Task<JsonElement> Post(HttpClient c, string path, object body)
    {
        var response = await c.PostAsJsonAsync(path, body, Json); Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        if (response.Content.Headers.ContentLength == 0 || response.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
