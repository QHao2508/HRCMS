namespace HorseClub.BLL.Common;

/// <summary>
/// Ghi nhận lần cuối có request người dùng để worker nền chỉ truy vấn SQL khi hệ thống đang được sử dụng.
/// Nhờ vậy Azure SQL serverless có thể auto-pause khi không ai dùng, kể cả khi backend vẫn đang chạy.
/// </summary>
public sealed class AppActivity
{
    private readonly TimeProvider clock;
    private long lastTicks;

    public AppActivity(TimeProvider clock)
    {
        this.clock = clock;
        // Khởi động được tính là một lần hoạt động để email/nhắc lịch tồn đọng được xử lý ngay sau khi app bật.
        lastTicks = clock.GetUtcNow().UtcTicks;
    }

    /// <summary>Đánh dấu vừa có request từ người dùng.</summary>
    public void Touch() => Interlocked.Exchange(ref lastTicks, clock.GetUtcNow().UtcTicks);

    /// <summary>
    /// Có hoạt động trong khoảng <paramref name="window"/> gần nhất hay không; window &lt;= 0 nghĩa là luôn hoạt động.
    /// </summary>
    public bool IsRecent(TimeSpan window) => window <= TimeSpan.Zero || clock.GetUtcNow().UtcTicks - Interlocked.Read(ref lastTicks) <= window.Ticks;
}
