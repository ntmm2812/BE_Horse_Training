namespace SWP.Domain.Common;

public static class RaceStatuses
{
    public const string Scheduled = "Scheduled";
    public const string OpenForRegistration = "OpenForRegistration";
    public const string RegistrationClosed = "RegistrationClosed";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
    [
        Scheduled,
        OpenForRegistration,
        RegistrationClosed,
        InProgress,
        Completed,
        Cancelled
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);
}
