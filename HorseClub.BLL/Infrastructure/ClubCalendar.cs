using Microsoft.Extensions.Options;

namespace Horse_BackEnd.Infrastructure;

public sealed class ClubCalendar(IOptions<BusinessOptions> options)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    public DateTime Local(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, zone).DateTime;
    public DateOnly DateAt(DateTimeOffset instant) => DateOnly.FromDateTime(Local(instant));
    public DateOnly Today(TimeProvider clock) => DateAt(clock.GetUtcNow());
    public (DateTimeOffset Start, DateTimeOffset End) DayRange(DateTimeOffset instant)
    {
        var day = Local(instant).Date;
        return (new DateTimeOffset(day, zone.GetUtcOffset(day)).ToUniversalTime(),
            new DateTimeOffset(day.AddDays(1), zone.GetUtcOffset(day.AddDays(1))).ToUniversalTime());
    }
    public static bool IsValidZone(string id)
    {
        try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }
}
