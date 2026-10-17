using System.ComponentModel.DataAnnotations;

namespace HorseClub.BLL.Common;

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
    [Range(0, 3)] public int SpeedDecimalPlaces { get; set; } = 3;
    [Range(1, 300)] public decimal MinHorseHeightCm { get; set; } = 1;
    [Range(1, 300)] public decimal MaxHorseHeightCm { get; set; } = 300;
    [Range(1, 2000)] public decimal MinHorseWeightKg { get; set; } = 1;
    [Range(1, 2000)] public decimal MaxHorseWeightKg { get; set; } = 2000;
}
