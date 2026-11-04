namespace SWP.Domain.Common;

public static class RaceCatalog
{
    public const string Registered = "Registered";

    public const string PrizeReview = "PrizeReview";

    public static readonly string[] ClosedRaceStatuses = ["Cancelled", "Canceled", "Đã hủy"];

    public static readonly string[] InactiveRegistrationStatuses = ["Cancelled", "Canceled", "Withdrawn", "Rejected"];

    public static readonly string[] BlockingHealthStatuses =
    [
        HorseHealthStatuses.ChanThuong,
        HorseHealthStatuses.CachLy,
        "Đang điều trị",
    ];

    public static bool IsClosedRace(string? status) =>
        ClosedRaceStatuses.Contains(status?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    public static bool IsInactiveRegistration(string? status) =>
        InactiveRegistrationStatuses.Contains(status?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    public static bool BlocksEntry(string? healthStatus) =>
        BlockingHealthStatuses.Contains(healthStatus?.Trim() ?? string.Empty, StringComparer.Ordinal);
}
