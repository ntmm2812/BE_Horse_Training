namespace SWP.Tests.Support;

/// <summary> Đồng hồ cố định (múi giờ UTC) để các test về ngày/tuần luôn xác định. </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;

    public void SetNow(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan by) => _now += by;
}
