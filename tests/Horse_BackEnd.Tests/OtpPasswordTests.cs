using System.Net.Http.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class OtpPasswordTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetAndInvitationOtpsExpireAndStopAfterFiveWrongAttempts(bool invitation)
    {
        var clock = new WorkerClock();
        var setting = invitation ? "Security:InvitationMinutes" : "Security:ResetMinutes";
        await using var factory = new ClubFactory(
            new Dictionary<string, string?> { [setting] = "1" },
            clock: clock);
        var client = factory.CreateClient();
        var email = invitation ? "staff-otp@example.test" : "owner-otp@example.test";
        if (invitation)
        {
            var manager = await factory.Client();
            await ClubFactory.Post(manager, "/api/staff", new StaffRequest(email, "assigned-staff", "Test", "Staff", "0900", "Address", Role.Trainer));
        }
        else
        {
            await factory.User(Role.HorseOwner, email);
            await ClubFactory.Post(client, "/api/auth/forgot-password", new { email });
        }
        var purpose = invitation ? ChallengePurpose.Invite : ChallengePurpose.Reset;
        var route = invitation ? "/api/auth/accept-invitation" : "/api/auth/reset-password";
        var code = await factory.Code(email, purpose.ToString());
        Assert.Matches("^[0-9]{6}$", code);
        await WorkerTests.Read(factory, async db =>
        {
            var challenge = await db.Challenges.SingleAsync(c => c.Purpose == purpose);
            Assert.NotEqual(code, challenge.CodeHash);
            Assert.Equal(TimeSpan.FromMinutes(1), challenge.ExpiresAt - challenge.CreatedAt);
        });
        clock.Advance(TimeSpan.FromMinutes(2));
        async Task<bool> Submit(string otp) => (await ClubFactory.Post(client, route,
            new { email, code = otp, password = ClubFactory.Password, confirmPassword = ClubFactory.Password })).GetProperty("changed").GetBoolean();
        Assert.False(await Submit(code));
        await WorkerTests.Read(factory, async db =>
        {
            var challenge = await db.Challenges.SingleAsync(c => c.Purpose == purpose);
            challenge.ExpiresAt = clock.GetUtcNow().AddMinutes(10);
            await db.SaveChangesAsync();
        });
        for (var attempt = 0; attempt < 5; attempt++) Assert.False(await Submit("000000"));
        Assert.False(await Submit(code));
        await WorkerTests.Read(factory, async db =>
        {
            var challenge = await db.Challenges.SingleAsync(c => c.Purpose == purpose);
            Assert.Equal(5, challenge.Attempts);
            Assert.True(challenge.Consumed);
        });
    }

    [Fact]
    public async Task VerificationOtpExpiresAccordingToConfiguredLifetime()
    {
        var clock = new WorkerClock();
        await using var factory = new ClubFactory(
            new Dictionary<string, string?> { ["Security:VerificationMinutes"] = "1" },
            clock: clock);
        var client = factory.CreateClient();
        const string email = "verification-expiry@example.test";
        await ClubFactory.Post(client, "/api/auth/register",
            new RegisterRequest(email, "verification-expiry", "Test", "Owner", "0900", "Address",
                ClubFactory.Password, ClubFactory.Password));
        var code = await factory.Code(email, "Verify");

        await WorkerTests.Read(factory, async db =>
        {
            var challenge = await db.Challenges.SingleAsync(c => c.Purpose == ChallengePurpose.Verify);
            Assert.Equal(TimeSpan.FromMinutes(1), challenge.ExpiresAt - challenge.CreatedAt);
        });

        clock.Advance(TimeSpan.FromMinutes(2));
        var response = await ClubFactory.Post(client, "/api/auth/verify-email", new { email, code });
        Assert.False(response.GetProperty("verified").GetBoolean());
    }
}
