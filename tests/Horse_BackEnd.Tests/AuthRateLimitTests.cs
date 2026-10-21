using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class AuthRateLimitTests
{
    // P6-BUG-001: repeated session checks after page loads must not hit the anonymous credential limiter.
    [Fact]
    public async Task SessionChecksAreNotRateLimitedButCredentialEndpointsStillAre()
    {
        await using var factory = new ClubFactory(new Dictionary<string, string?> { ["Security:AuthRequestsPerMinute"] = "3" });
        using var client = await factory.Client();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var anonymous = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            statuses.Add((await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@example.test" })).StatusCode);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
