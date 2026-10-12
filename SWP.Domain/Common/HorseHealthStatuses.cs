namespace SWP.Domain.Common;

public static class HorseHealthStatuses
{
    public const string DuDieuKien = "Đủ điều kiện";

    public const string CanTheoDoi = "Cần theo dõi";

    public const string ChanThuong = "Chấn thương";

    public const string CachLy = "Cách ly";

    public static readonly string[] All =
    [
        DuDieuKien,
        CanTheoDoi,
        ChanThuong,
        CachLy,
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.Ordinal);
}
