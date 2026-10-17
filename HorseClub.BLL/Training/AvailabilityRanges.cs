using HorseClub.BLL.Contracts;

namespace HorseClub.BLL.Training;

/// <summary>Subtract blocked half-open intervals and intersect free windows without assuming a missing duration.</summary>
public static class AvailabilityRanges
{
    public static List<TimeRange> Subtract(IEnumerable<TimeRange> windows, IEnumerable<BusyRange> busy, int durationMinutes)
    {
        var result = new List<TimeRange>();
        foreach (var window in windows)
        {
            var cursor = window.Start;
            foreach (var block in busy.Where(b => b.Start < window.End && b.End > window.Start).OrderBy(b => b.Start))
            {
                if (block.Start > cursor && (block.Start - cursor).TotalMinutes >= durationMinutes)
                    result.Add(new(cursor, block.Start < window.End ? block.Start : window.End));
                if (block.End > cursor) cursor = block.End;
                if (cursor >= window.End) break;
            }
            if (cursor < window.End && (window.End - cursor).TotalMinutes >= durationMinutes) result.Add(new(cursor, window.End));
        }
        return result;
    }

    public static List<TimeRange> Intersect(IEnumerable<TimeRange> left, IEnumerable<TimeRange> right, int durationMinutes)
        => (from a in left from b in right let start = a.Start > b.Start ? a.Start : b.Start let end = a.End < b.End ? a.End : b.End
            where (end - start).TotalMinutes >= durationMinutes select new TimeRange(start, end)).OrderBy(x => x.Start).ToList();

    public static List<TimeRange> ForMinuteInput(IEnumerable<TimeRange> ranges, int durationMinutes)
        => ranges.Select(x => new TimeRange(
            new DateTimeOffset((x.Start.UtcTicks + TimeSpan.TicksPerMinute - 1) / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute, TimeSpan.Zero),
            new DateTimeOffset(x.End.UtcTicks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute, TimeSpan.Zero)))
            .Where(x => (x.End - x.Start).TotalMinutes >= durationMinutes).ToList();
}
