using System.Security.Claims;
using System.Security.Cryptography;
using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;

namespace Horse_BackEnd.Services;

public sealed class AuthenticationService(ClubDbContext db, TimeProvider clock, IOptions<SecurityOptions> options, IDataProtectionProvider protection)
{
    private readonly SecurityOptions settings = options.Value;
    private readonly PasswordHasher<User> hasher = new();
    private readonly PasswordHasher<EmailChallenge> challengeHasher = new();
    public static string Normalize(string value) => value.Trim().ToLowerInvariant();
    public static object View(User u) => new { u.Id, u.Email, u.UserName, u.FirstName, u.LastName, u.Phone, u.Address, u.Role, u.EmailVerified, u.Active };

    public async Task<User> Register(RegisterRequest r)
    {
        Ensure.That(r.Password == r.ConfirmPassword, "Passwords do not match.");
        ValidatePassword(r.Password);
        Ensure.That(!settings.RequireNationalId || !string.IsNullOrWhiteSpace(r.NationalId), "National ID is required by club policy.");
        if (!string.IsNullOrWhiteSpace(r.NationalId)) Ensure.That(r.NationalId.Length == settings.NationalIdDigits && r.NationalId.All(char.IsAsciiDigit), "Invalid national ID format.");
        var email = Normalize(r.Email); var name = Normalize(r.UserName);
        Ensure.That(!await db.Users.AnyAsync(x => x.Email == email || x.UserName == name), "Email or username is already registered.", 409, "account_exists");
        var user = new User { Email = email, UserName = name, FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Phone = r.Phone.Trim(), Address = r.Address.Trim(), Role = Role.HorseOwner };
        user.PasswordHash = hasher.HashPassword(user, r.Password);
        if (!string.IsNullOrWhiteSpace(r.NationalId)) user.NationalIdProtected = protection.CreateProtector("HorseClub.PersonalData.NationalId").Protect(r.NationalId);
        db.Users.Add(user);
        await Challenge(user, ChallengePurpose.Verify);
        await db.SaveChangesAsync();
        return user;
    }
    public async Task<User> CreateStaff(StaffRequest r)
    {
        Ensure.That(r.Role != Role.HorseOwner && r.Role != Role.ClubManager, "Use this endpoint for internal staff roles only.");
        var email = Normalize(r.Email); var name = Normalize(r.UserName);
        Ensure.That(!await db.Users.AnyAsync(x => x.Email == email || x.UserName == name), "Email or username is already registered.", 409, "account_exists");
        var user = new User { Email = email, UserName = name, FirstName = r.FirstName.Trim(), LastName = r.LastName.Trim(), Phone = r.Phone, Address = r.Address, Role = r.Role };
        // Staff choose their own password with a one-use email invitation; no shared initial password.
        db.Users.Add(user);
        await Challenge(user, ChallengePurpose.Invite);
        await db.SaveChangesAsync();
        return user;
    }
    public async Task Challenge(User user, ChallengePurpose purpose)
    {
        var now = clock.GetUtcNow();
        // A generic response also applies to known/throttled accounts, avoiding email enumeration.
        if (await db.Challenges.AnyAsync(x => x.UserId == user.Id && x.Purpose == purpose && x.CreatedAt > now.AddSeconds(-settings.ResendSeconds))) return;
        var previous = await db.Challenges.Where(x => x.UserId == user.Id && x.Purpose == purpose && !x.Consumed).ToListAsync();
        foreach (var p in previous) p.Consumed = true;
        var code = purpose == ChallengePurpose.Verify ? RandomNumberGenerator.GetInt32(100000, 1000000).ToString()
            : Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var minutes = purpose switch { ChallengePurpose.Invite => settings.InvitationMinutes, ChallengePurpose.Reset => settings.ResetMinutes, _ => settings.VerificationMinutes };
        var challenge = new EmailChallenge { UserId = user.Id, Purpose = purpose, CreatedAt = now, ExpiresAt = now.AddMinutes(minutes) };
        challenge.CodeHash = challengeHasher.HashPassword(challenge, code);
        db.Challenges.Add(challenge);
        db.EmailMessages.Add(new EmailMessage { Recipient = user.Email, Subject = $"HorseClub {purpose}", Body = $"Your {purpose} code: {code}\nExpires at {challenge.ExpiresAt:O}. Never share this code." });
    }
    public async Task<bool> Consume(User user, ChallengePurpose purpose, string code)
    {
        var c = await db.Challenges.Where(x => x.UserId == user.Id && x.Purpose == purpose && !x.Consumed).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        if (c is null || c.ExpiresAt <= clock.GetUtcNow() || c.Attempts >= settings.MaxCodeAttempts) return false;
        c.Attempts++;
        var ok = challengeHasher.VerifyHashedPassword(c, c.CodeHash, code) != PasswordVerificationResult.Failed;
        if (ok || c.Attempts >= settings.MaxCodeAttempts) c.Consumed = true;
        return ok;
    }
    public async Task<User?> Login(LoginRequest r)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == Normalize(r.Email));
        if (user is null)
        {
            // Hash verification work is still performed for unknown email addresses.
            var dummy = new User(); hasher.HashPassword(dummy, r.Password);
            return null;
        }
        if (user.LockedUntil > clock.GetUtcNow()) return null;
        if (!user.Active || !user.EmailVerified || string.IsNullOrEmpty(user.PasswordHash) || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;
            if (user.FailedLogins >= settings.MaxLoginAttempts) { user.LockedUntil = clock.GetUtcNow().AddMinutes(settings.LockoutMinutes); user.FailedLogins = 0; }
            await db.SaveChangesAsync(); return null;
        }
        user.FailedLogins = 0; user.LockedUntil = null;
        await db.SaveChangesAsync(); return user;
    }
    public void SetPassword(User user, string password)
    {
        ValidatePassword(password);
        user.PasswordHash = hasher.HashPassword(user, password);
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.EmailVerified = true; user.FailedLogins = 0; user.LockedUntil = null;
    }
    public static ClaimsPrincipal Principal(User user) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("stamp", user.SecurityStamp)
    ], IdentityConstants.BearerScheme));
    public void ValidatePassword(string password) => CheckPassword(password, settings);
    public static void CheckPassword(string password, SecurityOptions settings) => Ensure.That(password.Length >= settings.PasswordMinLength && password.Length <= settings.PasswordMaxLength && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit),
        $"Password needs {settings.PasswordMinLength}–{settings.PasswordMaxLength} characters including uppercase, lowercase and a digit.");
}
