using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Horse_BackEnd.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Authentication").RequireRateLimiting("auth");
        auth.MapPost("/register", async (RegisterRequest r, AuthenticationService service) =>
            Results.Created("/api/auth/me", AuthenticationService.View(await service.Register(r))));
        auth.MapPost("/login", async (LoginRequest r, AuthenticationService service, HttpContext context) =>
        {
            var user = await service.Login(r);
            context.Items["commit-auth-attempt"] = true;
            return user is null ? Results.Json(new { error = "invalid_credentials" }, statusCode: 401)
                : Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme);
        });
        auth.MapPost("/refresh", async (RefreshRequest r, IOptionsMonitor<BearerTokenOptions> options, ClubDbContext db, TimeProvider clock) =>
        {
            var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(r.RefreshToken);
            if (ticket?.Properties.ExpiresUtc <= clock.GetUtcNow() || ticket is null || !Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return Results.Unauthorized();
            var user = await db.Users.FindAsync(id);
            if (user is not { Active: true, EmailVerified: true } || user.SecurityStamp != ticket.Principal.FindFirstValue("stamp")) return Results.Unauthorized();
            return Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme);
        });
        auth.MapPost("/verify-email", async (VerifyRequest r, AuthenticationService s, ClubDbContext db) =>
        {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            var ok = u is { Active: true, EmailVerified: false, Role: Role.HorseOwner } && await s.Consume(u, ChallengePurpose.Verify, r.Code);
            if (ok) u!.EmailVerified = true;
            await db.SaveChangesAsync();
            return Results.Ok(new { verified = ok });
        });
        auth.MapPost("/resend-verification", async (EmailRequest r, AuthenticationService s, ClubDbContext db) =>
        {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            if (u is { Active: true, EmailVerified: false, Role: Role.HorseOwner }) await s.Challenge(u, ChallengePurpose.Verify);
            await db.SaveChangesAsync(); return Results.Ok(new { message = "If eligible, a code will be emailed." });
        });
        auth.MapPost("/forgot-password", async (EmailRequest r, AuthenticationService s, ClubDbContext db) =>
        {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            if (u is { Active: true, EmailVerified: true }) await s.Challenge(u, ChallengePurpose.Reset);
            await db.SaveChangesAsync(); return Results.Ok(new { message = "If eligible, a reset code will be emailed." });
        });
        MapPassword(auth, "/reset-password", ChallengePurpose.Reset);
        MapPassword(auth, "/accept-invitation", ChallengePurpose.Invite);
        auth.MapGet("/me", async (CurrentUser current) => AuthenticationService.View(await current.Get())).RequireAuthorization();
        auth.MapPost("/logout", async (CurrentUser current, ClubDbContext db) =>
        {
            (await current.Get()).SecurityStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(); return Results.NoContent();
        }).RequireAuthorization();

        var staff = api.MapGroup("/staff").WithTags("Staff").RequireAuthorization();
        staff.MapGet("", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, Role? role, PageReader pager) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var q = db.Users.Where(x => x.Role != Role.HorseOwner);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            return await pager.Page(q.OrderBy(x => x.UserName).Select(x => new { x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.Role, x.Active, x.EmailVerified }), page, pageSize);
        });
        staff.MapGet("/directory", async (CurrentUser current, ClubDbContext db, Role? role, int? page, int? pageSize, PageReader pager) =>
        {
            await current.Get();
            var q = db.Users.Where(x => x.Active && x.Role != Role.HorseOwner && x.Role != Role.ClubManager);
            if (role.HasValue) q = q.Where(x => x.Role == role);
            return await pager.Page(q.OrderBy(x => x.UserName).Select(x => new { x.Id, x.FirstName, x.LastName, x.Role }), page, pageSize);
        });
        staff.MapPost("", async (StaffRequest r, CurrentUser current, AuthenticationService s, ClubEvents events, ClubDbContext db) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var u = await s.CreateStaff(r);
            await events.Audit(AuditAction.StaffCreated, u.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/staff/{u.Id}", AuthenticationService.View(u));
        });
        staff.MapPut("/{id:guid}/active", async (Guid id, ActiveRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) =>
        {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var u = Ensure.Found(await db.Users.FindAsync(id));
            Ensure.That(u.Role != Role.ClubManager && u.Role != Role.HorseOwner, "Only staff accounts can be changed here.");
            u.Active = r.Active; u.SecurityStamp = Guid.NewGuid().ToString();
            await events.Audit(AuditAction.StaffActiveChanged, id);
            await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
    private static void MapPassword(RouteGroupBuilder auth, string route, ChallengePurpose purpose)
    {
        auth.MapPost(route, async (ResetRequest r, AuthenticationService s, ClubDbContext db) =>
        {
            Ensure.That(r.Password == r.ConfirmPassword, "Passwords do not match.");
            s.ValidatePassword(r.Password);
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            var ok = u is { Active: true } && (purpose != ChallengePurpose.Invite || !u.EmailVerified) && await s.Consume(u, purpose, r.Code);
            if (ok) s.SetPassword(u!, r.Password);
            await db.SaveChangesAsync(); return Results.Ok(new { changed = ok });
        });
    }
}
