using HorseClub.BLL.Contracts;
using HorseClub.DAL.Enums;

namespace Horse_BackEnd.Endpoints;

public static class AuthEndpoints
{
    /// <summary>
    /// Đăng ký endpoint HTTP của module Auth với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="api">Giá trị kiểu RouteGroupBuilder dùng trong MapAuth.</param>
    public static void MapAuth(this RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Authentication").RequireRateLimiting("auth");
        auth.MapPost("/register", async (RegisterRequest r, IAuthenticationService moduleService) => (await moduleService.RegisterAccount(r)).ToHttpResult()).Produces<UserResponse>(201);
        auth.MapPost("/login", BearerSessionHandler.Login).Produces<Microsoft.AspNetCore.Authentication.BearerToken.AccessTokenResponse>(200)
            .Produces<LoginErrorResponse>(401).Produces<LoginErrorResponse>(410);
        auth.MapPost("/refresh", BearerSessionHandler.Refresh).WithMetadata(new ReadOnlyOperation()).Produces<Microsoft.AspNetCore.Authentication.BearerToken.AccessTokenResponse>(200);
        auth.MapPost("/verify-email", async (VerifyRequest r, IAuthenticationService moduleService) => (await moduleService.VerifyEmail(r)).ToHttpResult()).Produces<VerificationResponse>(200);
        auth.MapPost("/resend-verification", async (EmailRequest r, IAuthenticationService moduleService) => (await moduleService.ResendVerification(r)).ToHttpResult()).Produces<MessageResponse>(200);
        auth.MapPost("/forgot-password", async (EmailRequest r, IAuthenticationService moduleService) => (await moduleService.ForgotPassword(r)).ToHttpResult()).Produces<MessageResponse>(200);
        MapPassword(auth, "/reset-password", ChallengePurpose.Reset);
        MapPassword(auth, "/accept-invitation", ChallengePurpose.Invite);
        auth.MapGet("/me", async (IAuthenticationService moduleService) => await moduleService.GetProfile()).RequireAuthorization().Produces<UserResponse>(200);
        auth.MapPost("/logout", async (IAuthenticationService moduleService) => (await moduleService.Logout()).ToHttpResult()).RequireAuthorization().Produces(204);

        var staff = api.MapGroup("/staff").WithTags("Staff").RequireAuthorization();
        staff.MapGet("", async (int? page, int? pageSize, Role? role, IAuthenticationService moduleService) => await moduleService.ListStaff(page, pageSize, role)).Produces<PageResponse<StaffResponse>>(200);
        staff.MapGet("/directory", async (Role? role, int? page, int? pageSize, IAuthenticationService moduleService) => await moduleService.ListStaffDirectory(role, page, pageSize)).Produces<PageResponse<StaffDirectoryResponse>>(200);
        staff.MapPost("", async (StaffRequest r, IAuthenticationService moduleService) => (await moduleService.InviteStaff(r)).ToHttpResult()).Produces<UserResponse>(201);
        staff.MapPut("/{id:guid}/active", async (Guid id, ActiveRequest r, IAuthenticationService moduleService) => (await moduleService.SetStaffActive(id, r)).ToHttpResult()).Produces(204);
    }
    /// <summary>
    /// Đăng ký endpoint HTTP của module Password với schema, role/rate limit; chuyển request vào service BLL rồi ánh xạ response.
    /// </summary>
    /// <param name="auth">Giá trị kiểu RouteGroupBuilder dùng trong MapPassword.</param>
    /// <param name="route">Giá trị kiểu string dùng trong MapPassword.</param>
    /// <param name="purpose">Enum mục đích OTP Verify/Reset/Invite; ngăn dùng mã của luồng khác.</param>
    private static void MapPassword(RouteGroupBuilder auth, string route, ChallengePurpose purpose)
    {
        auth.MapPost(route, async (ResetRequest r, IAuthenticationService moduleService) => (await moduleService.ChangePassword(r, purpose)).ToHttpResult()).Produces<PasswordChangedResponse>(200);
    }
}
