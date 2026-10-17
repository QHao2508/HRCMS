using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Common;

public sealed class SecurityOptions
{
    public const string Section = "Security";
    [Range(8, 128)] public int PasswordMinLength { get; set; } = 12;
    [Range(8, 256)] public int PasswordMaxLength { get; set; } = 128;
    [Range(1, 1440)] public int VerificationMinutes { get; set; } = 10;
    [Range(1, 168)] public int PendingRegistrationHours { get; set; } = 24;
    [Range(1, 1440)] public int ResetMinutes { get; set; } = 10;
    [Range(1, 10080)] public int InvitationMinutes { get; set; } = 10;
    [Range(1, 3600)] public int ResendSeconds { get; set; } = 60;
    [Range(1, 20)] public int MaxCodeAttempts { get; set; } = 5;
    [Range(1, 20)] public int MaxLoginAttempts { get; set; } = 5;
    [Range(1, 1440)] public int LockoutMinutes { get; set; } = 15;
    [Range(1, 1440)] public int AccessTokenMinutes { get; set; } = 60;
    [Range(1, 10080)] public int RefreshTokenMinutes { get; set; } = 480;
    [Range(1, 1000)] public int AuthRequestsPerMinute { get; set; } = 30;
    public bool RequireNationalId { get; set; }
    [Range(1, 30)] public int NationalIdDigits { get; set; } = 12;
}
