using HorseClub.BLL.Messaging;
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

namespace HorseClub.BLL.Workflows;

public static class AuthWorkflow
{
    public static async Task<object> PostRegister(RegisterRequest r, AuthenticationService service)
    { return Results.Created("/api/auth/me", AuthenticationService.View(await service.Register(r))); }

    public static async Task<object> PostLogin(LoginRequest r, AuthenticationService service, HttpContext context)
    {
            var user = await service.Login(r);
            context.Items["commit-auth-attempt"] = true;
            return user is null ? Results.Json(new { error = "invalid_credentials" }, statusCode: 401)
                : Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme);
        }

    public static async Task<object> PostRefresh(RefreshRequest r, IOptionsMonitor<BearerTokenOptions> options, ClubDbContext db, TimeProvider clock)
    {
            var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(r.RefreshToken);
            if (ticket?.Properties.ExpiresUtc <= clock.GetUtcNow() || ticket is null || !Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return Results.Unauthorized();
            var user = await db.Users.FindAsync(id);
            if (user is not { Active: true, EmailVerified: true } || user.SecurityStamp != ticket.Principal.FindFirstValue("stamp")) return Results.Unauthorized();
            return Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme);
        }

    public static async Task<object> PostVerifyEmail(VerifyRequest r, AuthenticationService s, ClubDbContext db)
    {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            var ok = u is { Active: true, EmailVerified: false, Role: Role.HorseOwner } && await s.Consume(u, ChallengePurpose.Verify, r.Code);
            if (ok) u!.EmailVerified = true;
            await db.SaveChangesAsync();
            return Results.Ok(new { verified = ok });
        }

    public static async Task<object> PostResendVerification(EmailRequest r, AuthenticationService s, ClubDbContext db)
    {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            if (u is { Active: true, EmailVerified: false, Role: Role.HorseOwner }) await s.Challenge(u, ChallengePurpose.Verify);
            await db.SaveChangesAsync(); return Results.Ok(new { message = Messages.Get(MessageKey.IfEligibleACodeWillBeEmailed) });
        }

    public static async Task<object> PostForgotPassword(EmailRequest r, AuthenticationService s, ClubDbContext db)
    {
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            if (u is { Active: true, EmailVerified: true }) await s.Challenge(u, ChallengePurpose.Reset);
            await db.SaveChangesAsync(); return Results.Ok(new { message = Messages.Get(MessageKey.IfEligibleAResetCodeWillBeEmailed) });
        }

    public static async Task<object> GetMe(CurrentUser current)
    { return AuthenticationService.View(await current.Get()); }

    public static async Task<object> PostLogout(CurrentUser current, ClubDbContext db)
    {
            (await current.Get()).SecurityStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> GetList(CurrentUser current, ClubDbContext db, int? page, int? pageSize, Role? role, PageReader pager)
    {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var q = db.Users.Where(x => x.Role != Role.HorseOwner);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            return await pager.Page(q.OrderBy(x => x.UserName).Select(x => new { x.Id, x.UserName, x.FirstName, x.LastName, x.Email, x.Role, x.Active, x.EmailVerified }), page, pageSize);
        }

    public static async Task<object> GetDirectory(CurrentUser current, ClubDbContext db, Role? role, int? page, int? pageSize, PageReader pager)
    {
            await current.Get();
            var q = db.Users.Where(x => x.Active && x.Role != Role.HorseOwner && x.Role != Role.ClubManager);
            if (role.HasValue) q = q.Where(x => x.Role == role);
            return await pager.Page(q.OrderBy(x => x.UserName).Select(x => new { x.Id, x.FirstName, x.LastName, x.Role }), page, pageSize);
        }

    public static async Task<object> PostList(StaffRequest r, CurrentUser current, AuthenticationService s, ClubEvents events, ClubDbContext db)
    {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var u = await s.CreateStaff(r);
            await events.Audit(AuditAction.StaffCreated, u.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/staff/{u.Id}", AuthenticationService.View(u));
        }

    public static async Task<object> PutByIdActive(Guid id, ActiveRequest r, CurrentUser current, ClubDbContext db, ClubEvents events)
    {
            Ensure.Role(await current.Get(), Role.ClubManager);
            var u = Ensure.Found(await db.Users.FindAsync(id));
            Ensure.That(u.Role != Role.ClubManager && u.Role != Role.HorseOwner, Messages.Get(MessageKey.OnlyStaffAccountsCanBeChangedHere));
            u.Active = r.Active; u.SecurityStamp = Guid.NewGuid().ToString();
            await events.Audit(AuditAction.StaffActiveChanged, id);
            await db.SaveChangesAsync(); return Results.NoContent();
        }

    public static async Task<object> PostPassword(ResetRequest r, AuthenticationService s, ClubDbContext db, ChallengePurpose purpose)
    {
            Ensure.That(r.Password == r.ConfirmPassword, Messages.Get(MessageKey.PasswordsDoNotMatch));
            s.ValidatePassword(r.Password);
            var u = await db.Users.SingleOrDefaultAsync(x => x.Email == AuthenticationService.Normalize(r.Email));
            var ok = u is { Active: true } && (purpose != ChallengePurpose.Invite || !u.EmailVerified) && await s.Consume(u, purpose, r.Code);
            if (ok) s.SetPassword(u!, r.Password);
            await db.SaveChangesAsync(); return Results.Ok(new { changed = ok });
        }

}
