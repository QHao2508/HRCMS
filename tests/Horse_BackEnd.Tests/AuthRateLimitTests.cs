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
        // The limiter reads Security:AuthRequestsPerMinute (default 30) before test settings apply, so exceed the default.
        await using var factory = new ClubFactory();
        using var client = await factory.Client();
        for (var i = 0; i < 40; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        using var anonymous = factory.CreateClient();
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 35; i++)
            statuses.Add((await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@example.test" })).StatusCode);
        Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
    }
}
