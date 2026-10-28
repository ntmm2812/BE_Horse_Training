namespace SWP.Domain.Common;

public static class RaceRegistrationStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
    [
        Pending,
        Confirmed,
        Rejected,
        Cancelled
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);
}
