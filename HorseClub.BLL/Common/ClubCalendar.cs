using Microsoft.Extensions.Options;

namespace HorseClub.BLL.Common;

public sealed class ClubCalendar(IOptions<BusinessOptions> options)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    /// <summary>
    /// Chuyển thời điểm UTC sang múi giờ nghiệp vụ đã cấu hình.
    /// </summary>
    /// <param name="instant">Giá trị kiểu DateTimeOffset dùng trong Local.</param>
    public DateTime Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, zone).DateTime;
    /// <summary>
    /// Lấy ngày nghiệp vụ tại thời điểm truyền vào theo múi giờ câu lạc bộ, không dựa vào múi giờ máy chủ.
    /// </summary>
    /// <param name="instant">Giá trị kiểu DateTimeOffset dùng trong DateAt.</param>
    public DateOnly DateAt(DateTimeOffset instant) => DateOnly.FromDateTime(Local(instant));
    /// <summary>
    /// Tính ngày hiện tại từ TimeProvider và múi giờ nghiệp vụ để dùng thống nhất khi kiểm tra lịch.
    /// </summary>
    /// <param name="clock">Giá trị kiểu TimeProvider dùng trong Today.</param>
    public DateOnly Today(TimeProvider clock) => DateAt(clock.GetUtcNow());
    /// <summary>
    /// Tạo cặp mốc UTC đầu/cuối của một ngày nghiệp vụ để lọc thời gian trong database.
    /// </summary>
    /// <param name="instant">Giá trị kiểu DateTimeOffset dùng trong DayRange.</param>
    public (DateTimeOffset Start, DateTimeOffset End) DayRange(DateTimeOffset instant)
    {
        var day = Local(instant).Date;
        return (new DateTimeOffset(day, zone.GetUtcOffset(day)).ToUniversalTime(),
            new DateTimeOffset(day.AddDays(1), zone.GetUtcOffset(day.AddDays(1))).ToUniversalTime());
    }
    /// <summary>
    /// Kiểm tra hệ thống nhận diện được timezone đã cấu hình trước khi ứng dụng khởi động.
    /// </summary>
    /// <param name="id">ID đối tượng được thao tác; quyền/phạm vi được kiểm trước khi đọc hoặc ghi.</param>
    public static bool IsValidZone(string id)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
}
