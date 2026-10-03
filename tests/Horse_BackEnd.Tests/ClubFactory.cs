using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class ClubFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> extraSettings;
    public ClubFactory(IReadOnlyDictionary<string, string?>? settings = null) => extraSettings = settings ?? new Dictionary<string, string?>();
    public const string Password = "TestPassword123!";
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly string directory = Path.Combine(Path.GetTempPath(), "horseclub-tests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(directory);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite", ["Database:AutoMigrate"] = "true",
            ["ConnectionStrings:Sqlite"] = $"Data Source={Path.Combine(directory, "test.db")}",
            ["DataProtection:Path"] = Path.Combine(directory, "keys"), ["Storage:Path"] = Path.Combine(directory, "uploads"),
            ["Workers:Enabled"] = "false", ["Bootstrap:ManagerEmail"] = "manager@example.test", ["Bootstrap:ManagerPassword"] = Password,
            ["Business:TimeZoneId"] = "UTC"
        }).AddInMemoryCollection(extraSettings));
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
        var message = db.EmailMessages.Single(x => x.Recipient == email && x.Subject == $"HorseClub {purpose}");
        await Task.CompletedTask; return message.Body.Split('\n')[0].Split(": ")[1];
    }
    public static async Task<JsonElement> Post(HttpClient c, string path, object body)
    {
        var response = await c.PostAsJsonAsync(path, body, Json); Assert.True(response.IsSuccessStatusCode, $"{path}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        if (response.Content.Headers.ContentLength == 0 || response.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
