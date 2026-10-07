using System.ComponentModel.DataAnnotations;
using Horse_BackEnd.Domain;

namespace Horse_BackEnd.Infrastructure;

public sealed class SecurityOptions
{
    public const string Section = "Security";
    [Range(8, 128)] public int PasswordMinLength { get; set; } = 12;
    [Range(8, 256)] public int PasswordMaxLength { get; set; } = 128;
    [Range(1, 1440)] public int VerificationMinutes { get; set; } = 10;
    [Range(1, 1440)] public int ResetMinutes { get; set; } = 10;
    [Range(1, 10080)] public int InvitationMinutes { get; set; } = 60;
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
public sealed class StorageOptions
{
    public const string Section = "Storage";
    public string? Path { get; set; }
    [Range(1, 104857600)] public long MaxFileBytes { get; set; } = 10485760;
    [Range(1, 100)] public int MaxAttachmentsPerRecord { get; set; } = 20;
    [Range(1, 1000)] public int RequestsPerMinute { get; set; } = 20;
}
public sealed class BusinessOptions
{
    public const string Section = "Business";
    [Required] public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    [Range(1, 1000)] public int MaxPageSize { get; set; } = 100;
    [Range(1, 1000)] public int DefaultPageSize { get; set; } = 20;
    [Range(1, 100000)] public int MaxReportRecords { get; set; } = 5000;
    [Range(1, 3660)] public int MaxReportDays { get; set; } = 366;
    [Range(1, 3660)] public int DefaultReportDays { get; set; } = 30;
    [Range(0, 60)] public int ScheduleGraceMinutes { get; set; } = 5;
    [Range(0, 1440)] public int StartEarlyMinutes { get; set; } = 15;
    [Range(1, 10080)] public int OverdueAfterMinutes { get; set; } = 60;
}
public sealed class WorkerOptions
{
    public const string Section = "Workers";
    public bool Enabled { get; set; } = true;
    [Range(1, 3600)] public int EmailPollSeconds { get; set; } = 5;
    [Range(1, 3600)] public int ReminderPollSeconds { get; set; } = 60;
    [Range(1, 100)] public int MaxEmailAttempts { get; set; } = 8;
    [Range(1, 100)] public int EmailBatchSize { get; set; } = 10;
    [Range(1, 300)] public int EmailSendTimeoutSeconds { get; set; } = 30;
    [Range(1, 1000)] public int ReminderBatchSize { get; set; } = 100;
}
public sealed class EmailOptions
{
    public const string Section = "Email";
    public EmailDeliveryMode Mode { get; set; } = EmailDeliveryMode.Smtp;
    public string? Host { get; set; }
    [Range(1, 65535)] public int Port { get; set; } = 587;
    public string? From { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }

    public bool CanDeliver(bool isDevelopment) => Mode == EmailDeliveryMode.DevelopmentFile
        ? isDevelopment
        : Mode == EmailDeliveryMode.Smtp && !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(From) && new EmailAddressAttribute().IsValid(From);
}
