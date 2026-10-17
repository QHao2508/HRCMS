namespace HorseClub.BLL.Contracts;

public sealed record LoginErrorResponse(
    string Error, string? Email = null)
{
    /// <summary>
    /// Chuyển enum LoginStatus thành mã lỗi API cố định, chỉ kèm email khi mật khẩu đúng và cần xác thực.
    /// </summary>
    /// <param name="status">Trạng thái enum API, tách khỏi nhãn tiếng Việt.</param>
    /// <param name="email">Email đầu vào hoặc email chuẩn của tài khoản; không dùng để suy luận tài khoản tồn tại từ phản hồi chung.</param>
    public static LoginErrorResponse From(LoginStatus status, string? email = null) => new(status switch
    {
        LoginStatus.VerificationRequired => "email_verification_required",
        LoginStatus.RegistrationExpired => "registration_expired",
        _ => "invalid_credentials"
    }, email);
}
