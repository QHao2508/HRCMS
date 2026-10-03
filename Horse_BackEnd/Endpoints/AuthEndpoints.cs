using Horse_BackEnd.Contracts;
using Horse_BackEnd.Data;
using Horse_BackEnd.Domain;
using Horse_BackEnd.Infrastructure;
using Horse_BackEnd.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;

using HorseClub.BLL.Workflows;

namespace Horse_BackEnd.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Authentication").RequireRateLimiting("auth");
        auth.MapPost("/register", async (RegisterRequest r, AuthenticationService service) => await AuthWorkflow.PostRegister(r, service));
        auth.MapPost("/login", async (LoginRequest r, AuthenticationService service, HttpContext context) => await AuthWorkflow.PostLogin(r, service, context));
        auth.MapPost("/refresh", async (RefreshRequest r, IOptionsMonitor<BearerTokenOptions> options, ClubDbContext db, TimeProvider clock) => await AuthWorkflow.PostRefresh(r, options, db, clock));
        auth.MapPost("/verify-email", async (VerifyRequest r, AuthenticationService s, ClubDbContext db) => await AuthWorkflow.PostVerifyEmail(r, s, db));
        auth.MapPost("/resend-verification", async (EmailRequest r, AuthenticationService s, ClubDbContext db) => await AuthWorkflow.PostResendVerification(r, s, db));
        auth.MapPost("/forgot-password", async (EmailRequest r, AuthenticationService s, ClubDbContext db) => await AuthWorkflow.PostForgotPassword(r, s, db));
        MapPassword(auth, "/reset-password", ChallengePurpose.Reset);
        MapPassword(auth, "/accept-invitation", ChallengePurpose.Invite);
        auth.MapGet("/me", async (CurrentUser current) => await AuthWorkflow.GetMe(current)).RequireAuthorization();
        auth.MapPost("/logout", async (CurrentUser current, ClubDbContext db) => await AuthWorkflow.PostLogout(current, db)).RequireAuthorization();

        var staff = api.MapGroup("/staff").WithTags("Staff").RequireAuthorization();
        staff.MapGet("", async (CurrentUser current, ClubDbContext db, int? page, int? pageSize, Role? role, PageReader pager) => await AuthWorkflow.GetList(current, db, page, pageSize, role, pager));
        staff.MapGet("/directory", async (CurrentUser current, ClubDbContext db, Role? role, int? page, int? pageSize, PageReader pager) => await AuthWorkflow.GetDirectory(current, db, role, page, pageSize, pager));
        staff.MapPost("", async (StaffRequest r, CurrentUser current, AuthenticationService s, ClubEvents events, ClubDbContext db) => await AuthWorkflow.PostList(r, current, s, events, db));
        staff.MapPut("/{id:guid}/active", async (Guid id, ActiveRequest r, CurrentUser current, ClubDbContext db, ClubEvents events) => await AuthWorkflow.PutByIdActive(id, r, current, db, events));
    }
    private static void MapPassword(RouteGroupBuilder auth, string route, ChallengePurpose purpose)
    {
        auth.MapPost(route, async (ResetRequest r, AuthenticationService s, ClubDbContext db) => await AuthWorkflow.PostPassword(r, s, db, purpose));
    }
}
