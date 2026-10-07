using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HorseClub.BLL.Contracts;
using HorseClub.DAL.Entities;
using HorseClub.DAL.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class PendingRegistrationTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static AccountCleanupWorker Worker(ClubFactory factory, Clock clock) => new(factory.Services.GetRequiredService<IServiceScopeFactory>(), clock,
        NullLogger<AccountCleanupWorker>.Instance, Options.Create(new WorkerOptions()));
    private static async Task Pending(ClubFactory factory, User user, DateTimeOffset created)
    {
        await WorkerTests.Read(factory, async db =>
        {
            var row = (await db.Users.FindAsync(user.Id))!; row.EmailVerified = false; row.CreatedAt = created; await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task CorrectPasswordForPendingOwnerReturnsVerificationEmailWithoutTokensOrLockingOut()
    {
        var clock = new Clock(); await using var factory = new ClubFactory(clock: clock); var client = factory.CreateClient();
        var owner = await factory.User(Role.HorseOwner); await Pending(factory, owner, clock.Now.AddHours(-23));
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new { email = owner.Email, password = "WrongPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        var bad = await wrong.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal("invalid_credentials", bad.GetProperty("error").GetString());
        Assert.Equal(JsonValueKind.Null, bad.GetProperty("email").ValueKind);
        for (var index = 0; index < 6; index++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = owner.UserName, password = ClubFactory.Password });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("email_verification_required", body.GetProperty("error").GetString()); Assert.Equal(owner.Email, body.GetProperty("email").GetString());
            Assert.False(body.TryGetProperty("accessToken", out _)); Assert.False(body.TryGetProperty("refreshToken", out _));
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        await WorkerTests.Read(factory, async db => { var user = (await db.Users.FindAsync(owner.Id))!; Assert.Equal(0, user.FailedLogins); Assert.Null(user.LockedUntil); });
    }

    [Fact]
    public async Task Exact24HourBoundaryBlocksLoginResendAndVerificationEvenBeforeCleanup()
    {
        var clock = new Clock(); await using var factory = new ClubFactory(clock: clock); var client = factory.CreateClient();
        var owner = await factory.User(Role.HorseOwner); await Pending(factory, owner, clock.Now.AddHours(-24));
        await WorkerTests.Read(factory, async db =>
        {
            var challenge = new EmailChallenge { UserId = owner.Id, Purpose = ChallengePurpose.Verify, CreatedAt = clock.Now.AddMinutes(-1), ExpiresAt = clock.Now.AddMinutes(9) };
            challenge.CodeHash = new PasswordHasher<EmailChallenge>().HashPassword(challenge, "123456"); db.Challenges.Add(challenge); await db.SaveChangesAsync();
        });
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = owner.Email, password = ClubFactory.Password });
        Assert.Equal(HttpStatusCode.Gone, login.StatusCode); Assert.Equal("registration_expired", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString());
        await ClubFactory.Post(client, "/api/auth/resend-verification", new { email = owner.Email });
        var verification = await ClubFactory.Post(client, "/api/auth/verify-email", new { email = owner.Email, code = "123456" }); Assert.False(verification.GetProperty("verified").GetBoolean());
        await WorkerTests.Read(factory, async db => { Assert.False((await db.Users.FindAsync(owner.Id))!.EmailVerified); Assert.Equal(1, await db.Challenges.CountAsync(c => c.UserId == owner.Id)); Assert.Empty(await db.EmailMessages.ToListAsync()); });
    }

    [Fact]
    public async Task CleanupDeletesOnlyExpiredSelfRegistrationsAndRelatedOtpsAcrossConcurrentWorkers()
    {
        var clock = new Clock(); await using var factory = new ClubFactory(clock: clock);
        var expired = await factory.User(Role.HorseOwner); var inactive = await factory.User(Role.HorseOwner);
        var fresh = await factory.User(Role.HorseOwner); var verified = await factory.User(Role.HorseOwner); var staff = await factory.User(Role.Trainer);
        await Pending(factory, expired, clock.Now.AddHours(-24)); await Pending(factory, inactive, clock.Now.AddDays(-2));
        await Pending(factory, fresh, clock.Now.AddHours(-24).AddTicks(1)); await Pending(factory, staff, clock.Now.AddDays(-2));
        await WorkerTests.Read(factory, async db =>
        {
            (await db.Users.FindAsync(inactive.Id))!.Active = false;
            foreach (var user in new[] { expired, inactive })
            {
                var challenge = new EmailChallenge { UserId = user.Id, ExpiresAt = clock.Now.AddMinutes(10) }; db.Challenges.Add(challenge);
                db.EmailMessages.Add(new EmailMessage { Recipient = user.Email, ChallengeId = challenge.Id, Body = "otp-sensitive", ExpiresAt = challenge.ExpiresAt });
                db.EmailMessages.Add(new EmailMessage { Recipient = user.Email, Body = "legacy-sensitive" });
                db.Notifications.Add(new Notification { RecipientId = user.Id, Message = "pending" });
            }
            await db.SaveChangesAsync();
        });
        using var first = Worker(factory, clock); using var second = Worker(factory, clock);
        Assert.Equal(2, (await Task.WhenAll(first.RunOnce(), second.RunOnce())).Sum()); Assert.Equal(0, await first.RunOnce());
        await WorkerTests.Read(factory, async db =>
        {
            Assert.Null(await db.Users.FindAsync(expired.Id)); Assert.Null(await db.Users.FindAsync(inactive.Id));
            foreach (var id in new[] { fresh.Id, verified.Id, staff.Id }) Assert.NotNull(await db.Users.FindAsync(id));
            Assert.Empty(await db.Challenges.ToListAsync()); Assert.Empty(await db.EmailMessages.ToListAsync()); Assert.Empty(await db.Notifications.ToListAsync());
        });
    }

    [Fact]
    public async Task ExpiredEmailAndUsernameCanRegisterAgainWithoutWaitingForWorker()
    {
        var clock = new Clock(); await using var factory = new ClubFactory(clock: clock); var client = factory.CreateClient();
        var old = await factory.User(Role.HorseOwner); await Pending(factory, old, clock.Now.AddDays(-2));
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(old.Email, old.UserName, "Tên", "Họ", "0900000000", "Address", ClubFactory.Password, ClubFactory.Password));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await WorkerTests.Read(factory, async db =>
        {
            Assert.Null(await db.Users.FindAsync(old.Id)); var replacement = await db.Users.SingleAsync(u => u.Email == old.Email);
            Assert.NotEqual(old.Id, replacement.Id); Assert.Equal(clock.Now, replacement.CreatedAt); Assert.False(replacement.EmailVerified);
        });
    }

    [Fact]
    public async Task OtpCannotExtendRegistrationLifetimeAndSuccessfulVerificationSurvivesCleanup()
    {
        var clock = new Clock(); await using var factory = new ClubFactory(clock: clock); var client = factory.CreateClient();
        var owner = await factory.User(Role.HorseOwner); await Pending(factory, owner, clock.Now.AddHours(-24).AddSeconds(30));
        await ClubFactory.Post(client, "/api/auth/resend-verification", new { email = owner.Email }); var code = await factory.Code(owner.Email, "Verify");
        await WorkerTests.Read(factory, async db => Assert.Equal(clock.Now.AddSeconds(30), (await db.Challenges.SingleAsync(c => c.UserId == owner.Id)).ExpiresAt));
        var response = await ClubFactory.Post(client, "/api/auth/verify-email", new { email = owner.Email, code }); Assert.True(response.GetProperty("verified").GetBoolean());
        clock.Now = clock.Now.AddMinutes(1); using var worker = Worker(factory, clock); Assert.Equal(0, await worker.RunOnce());
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = owner.Email, password = ClubFactory.Password }); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
