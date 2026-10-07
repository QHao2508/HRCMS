using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Common;

public sealed class WorkerOptions
{
    public const string Section = "Workers";
    public bool Enabled { get; set; } = true;
    [Range(1, 3600)] public int AccountCleanupPollSeconds { get; set; } = 60;
    [Range(1, 1000)] public int AccountCleanupBatchSize { get; set; } = 100;
    [Range(1, 3600)] public int EmailPollSeconds { get; set; } = 5;
    [Range(1, 3600)] public int ReminderPollSeconds { get; set; } = 60;
    [Range(1, 100)] public int MaxEmailAttempts { get; set; } = 8;
    [Range(1, 100)] public int EmailBatchSize { get; set; } = 10;
    [Range(1, 300)] public int EmailSendTimeoutSeconds { get; set; } = 30;
    [Range(1, 1000)] public int ReminderBatchSize { get; set; } = 100;
}
