using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Data;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class AdditionalTests
{
    [Fact]
    public async Task OpenApiAndMetadata_DescribeEnumNamesAndRoutes()
    {
        await using var f = new ClubFactory(); var c = await f.Client();
        var metadata = await c.GetFromJsonAsync<JsonElement>("/api/metadata/enums");
        Assert.Contains("Veterinarian", metadata.GetProperty("Role").EnumerateArray().Select(x => x.GetString()));
        var document = await f.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/auth/login", out _));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/horses/{horseId}/medical/restrictions", out _));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/training/sessions/{id}/start", out _));
    }

    [Fact]
    public async Task NationalIdPolicy_IsConfigurable_AndIdentifierNeverReturnedInAccountView()
    {
        await using var f = new ClubFactory(new Dictionary<string, string?> { ["Security:RequireNationalId"] = "true" }); var c = f.CreateClient();
        var request = new RegisterRequest("identity@example.test", "identity", "Test", "Owner", "0900", "Here", ClubFactory.Password, ClubFactory.Password);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/register", request)).StatusCode);
        const string demoId = "000000000001";
        var account = await ClubFactory.Post(c, "/api/auth/register", request with { NationalId = demoId });
        Assert.False(account.TryGetProperty("nationalId", out _)); Assert.False(account.TryGetProperty("nationalIdProtected", out _));
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        var user = await db.Users.SingleAsync(x => x.Email == request.Email);
        Assert.False(string.IsNullOrWhiteSpace(user.NationalIdProtected)); Assert.NotEqual(demoId, user.NationalIdProtected);
    }

    [Theory]
    [InlineData(Intensity.Light, TrainingType.Trot, 400, true)]
    [InlineData(Intensity.Moderate, TrainingType.Trot, 400, false)]
    [InlineData(Intensity.Light, TrainingType.Trot, 501, false)]
    [InlineData(Intensity.Light, TrainingType.Sprint, 400, false)]
    public async Task RestrictionGuard_EnforcesIntensityDistanceAndNoSprint(Intensity intensity, TrainingType type, int distance, bool allowed)
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var trainer = await f.User(Role.Trainer); var head = await f.User(Role.HeadTrainer); var vet = await f.User(Role.Veterinarian);
        var horse = await f.Horse(owner, trainer, head, vet); var tc = await f.Client(trainer); var hc = await f.Client(head); var vc = await f.Client(vet); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var template = await ClubFactory.Post(hc, "/api/training/templates", new TemplateRequest("Recovery", "Goal", "Recovery", 400, Intensity.Light, "Sand", 2, ""));
        var plan = await ClubFactory.Post(tc, "/api/training/plans", new PlanRequest(horse.Id, template.GetProperty("id").GetGuid(), "Goal", "Recovery", today, today.AddDays(3), ""));
        var record = await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/records", new MedicalRequest(DateTimeOffset.UtcNow.AddSeconds(-1), "Check", "Normal", "Exam", "Monitoring", HealthStatus.Monitoring, ""));
        await ClubFactory.Post(vc, $"/api/horses/{horse.Id}/medical/restrictions", new RestrictionRequest(record.GetProperty("id").GetGuid(), false, false, Intensity.Light, 500, true, DateTimeOffset.UtcNow.AddMinutes(-1), null, "Light recovery only"));
        var response = await tc.PostAsJsonAsync($"/api/training/plans/{plan.GetProperty("id").GetGuid()}/sessions", new SessionRequest(DateTimeOffset.UtcNow.AddMinutes(20), type, distance, intensity, "Sand", "Goal", "", null), ClubFactory.Json);
        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict, response.StatusCode);
        if (allowed)
        {
            var session = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("Planned", session.GetProperty("status").GetString()); Assert.Equal(JsonValueKind.Null, session.GetProperty("riderId").ValueKind);
            var history = await tc.GetFromJsonAsync<JsonElement>($"/api/training/plans/{plan.GetProperty("id").GetGuid()}/history"); Assert.Equal(2, history.GetProperty("total").GetInt32());
        }
    }

    [Fact]
    public async Task StallsCannotDoubleBook_AndArchiveClosesOccupancy()
    {
        await using var f = new ClubFactory(); var owner = await f.User(Role.HorseOwner); var first = await f.Horse(owner); var second = await f.Horse(owner); var c = await f.Client();
        var stable = await ClubFactory.Post(c, "/api/care/stables", new NameRequest("Stable A")); var stall = await ClubFactory.Post(c, "/api/care/stalls", new StallRequest(stable.GetProperty("id").GetGuid(), "A1")); var id = stall.GetProperty("id").GetGuid();
        await ClubFactory.Post(c, $"/api/care/stalls/{id}/occupancy", new OccupancyRequest(first.Id));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/care/stalls/{id}/occupancy", new OccupancyRequest(second.Id))).StatusCode);
        await ClubFactory.Post(c, $"/api/horses/{first.Id}/archive", new { });
        await ClubFactory.Post(c, $"/api/care/stalls/{id}/occupancy", new OccupancyRequest(second.Id));
        Assert.Equal(HttpStatusCode.Conflict, (await c.GetAsync($"/api/horses/{first.Id}")).StatusCode);
    }

    [Fact]
    public void SqlServerModelAndMigration_CanGenerateDeploymentSql()
    {
        using var db = new SqlServerClubDbContext(new DbContextOptionsBuilder<SqlServerClubDbContext>()
            .UseSqlServer("Server=localhost;Database=HorseClubSchemaTest;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var script = db.Database.GenerateCreateScript();
        Assert.Contains("CREATE TABLE [Sessions]", script); Assert.Contains("CREATE TABLE [Restrictions]", script);
        Assert.Contains(db.Database.GetMigrations(), x => x.EndsWith("_QueryPerformanceIndexes", StringComparison.Ordinal));
        Assert.Contains("IX_Sessions_HorseId_Status_ScheduledAt", script);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void ClubCalendar_UsesConfiguredTimeZoneAcrossUtcMidnightBoundary()
    {
        var calendar = new ClubCalendar(Options.Create(new BusinessOptions { TimeZoneId = "Asia/Ho_Chi_Minh" }));
        var instant = new DateTimeOffset(2026, 10, 3, 18, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 10, 4), calendar.DateAt(instant));
        var (start, end) = calendar.DayRange(instant);
        Assert.Equal(new DateTimeOffset(2026, 10, 3, 17, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(TimeSpan.FromDays(1), end - start);
    }
}
