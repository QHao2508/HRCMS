using System.Security.Claims;
using HorseClub.BLL.Contracts;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Infrastructure;

/// <summary>ASP.NET bearer-token protocol belongs to the HTTP boundary.</summary>
public static class BearerSessionHandler
{
    /// <summary>
    /// Chuyển LoginAttempt thành lỗi HTTP có mã để frontend điều hướng, hoặc cấp bearer token chỉ cho tài khoản đã xác thực. Cho phép transaction lưu bộ đếm đăng nhập dù trả 401.
    /// </summary>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <param name="service">Giá trị kiểu IAuthenticationService dùng trong Login.</param>
    /// <param name="context">Giá trị kiểu HttpContext dùng trong Login.</param>
    public static async Task<IResult> Login(LoginRequest request, IAuthenticationService service, HttpContext context)
    {
        var attempt = await service.Login(request);
        context.Items["commit-auth-attempt"] = true;
        return attempt.Status == LoginStatus.Authenticated && attempt.User is { EmailVerified: true } user
            ? Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme)
            : Results.Json(LoginErrorResponse.From(attempt.Status, attempt.Status == LoginStatus.VerificationRequired ? attempt.User?.Email : null),
                statusCode: attempt.Status == LoginStatus.RegistrationExpired ? 410 : 401);
    }

    /// <summary>
    /// Giải mã và kiểm hạn refresh token, xác thực user/stamp rồi cấp phiên mới; trả Unauthorized khi token hoặc tài khoản không hợp lệ.
    /// </summary>
    /// <param name="request">Request đã có hợp đồng; thao tác upload dùng abstraction tách khỏi HTTP.</param>
    /// <param name="options">Cấu hình/hợp đồng tùy hàm; các giá trị được truyền rõ ràng từ caller.</param>
    /// <param name="service">Giá trị kiểu IAuthenticationService dùng trong Refresh.</param>
    /// <param name="clock">Giá trị kiểu TimeProvider dùng trong Refresh.</param>
    public static async Task<IResult> Refresh(RefreshRequest request, IOptionsMonitor<BearerTokenOptions> options,
        IAuthenticationService service, TimeProvider clock)
    {
        var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(request.RefreshToken);
        if (ticket is null || ticket.Properties.ExpiresUtc is null || ticket.Properties.ExpiresUtc <= clock.GetUtcNow()
            || !Guid.TryParse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Results.Unauthorized();
        var user = await service.ValidateSession(id, ticket.Principal.FindFirstValue("stamp"));
        return user is null ? Results.Unauthorized()
            : Results.SignIn(AuthenticationService.Principal(user), authenticationScheme: IdentityConstants.BearerScheme);
    }
}
