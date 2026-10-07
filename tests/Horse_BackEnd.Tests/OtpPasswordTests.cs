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
        await using var factory = new ClubFactory();
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
            Assert.Equal(TimeSpan.FromMinutes(10), challenge.ExpiresAt - challenge.CreatedAt);
            challenge.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });
        async Task<bool> Submit(string otp) => (await ClubFactory.Post(client, route,
            new { email, code = otp, password = ClubFactory.Password, confirmPassword = ClubFactory.Password })).GetProperty("changed").GetBoolean();
        Assert.False(await Submit(code));
        await WorkerTests.Read(factory, async db =>
        {
            var challenge = await db.Challenges.SingleAsync(c => c.Purpose == purpose);
            challenge.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);
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
}
