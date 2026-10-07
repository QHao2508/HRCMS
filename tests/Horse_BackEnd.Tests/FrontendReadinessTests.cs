using System.Net;
using HorseClub.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class FrontendReadinessTests
{
    [Fact]
    public async Task FrontendPreflightAllowsConfiguredOriginAndBearerHeader()
    {
        await using var factory = new ClubFactory(new Dictionary<string, string?> { ["Cors:Origins:0"] = "http://localhost:5173" });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/horses");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("authorization", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")), StringComparison.OrdinalIgnoreCase);
        using var rejected = new HttpRequestMessage(HttpMethod.Options, "/api/horses");
        rejected.Headers.Add("Origin", "http://unconfigured.example.test");
        rejected.Headers.Add("Access-Control-Request-Method", "GET");
        using var denied = await client.SendAsync(rejected);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RuntimeUsesSqlServerAndHealthReportsConnectivity()
    {
        await using var factory = new ClubFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClubDbContext>();
        Assert.IsType<SqlServerClubDbContext>(db);
        Assert.True(db.Database.IsSqlServer());
        Assert.All(db.Database.GetMigrations(), migration => Assert.DoesNotContain("Sqlite", migration, StringComparison.OrdinalIgnoreCase));
    }
}
