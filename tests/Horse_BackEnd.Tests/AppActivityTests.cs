using HorseClub.BLL.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class AppActivityTests
{
    [Fact]
    public void ActivityExpiresAfterWindowAndZeroWindowMeansAlwaysActive()
    {
        var clock = new WorkerClock(); var activity = new AppActivity(clock);
        Assert.True(activity.IsRecent(TimeSpan.FromMinutes(10)));
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.False(activity.IsRecent(TimeSpan.FromMinutes(10)));
        Assert.True(activity.IsRecent(TimeSpan.Zero));
        activity.Touch();
        Assert.True(activity.IsRecent(TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public async Task UserRequestsCountAsActivityButHealthChecksDoNot()
    {
        var clock = new WorkerClock();
        await using var factory = new ClubFactory(clock: clock);
        using var client = factory.CreateClient();
        var activity = factory.Services.GetRequiredService<AppActivity>();
        clock.Advance(TimeSpan.FromMinutes(30));
        Assert.False(activity.IsRecent(TimeSpan.FromMinutes(10)));
        await client.GetAsync("/health");
        Assert.False(activity.IsRecent(TimeSpan.FromMinutes(10)));
        await client.GetAsync("/api/website");
        Assert.True(activity.IsRecent(TimeSpan.FromMinutes(10)));
    }
}
