namespace SWP.Domain.Common;

public static class RaceResultStatuses
{
    public const string Finished = "Finished";
    public const string DidNotFinish = "DNF";
    public const string Disqualified = "DQ";
    public const string DidNotStart = "DNS";

    public static readonly string[] All =
    [
        Finished,
        DidNotFinish,
        Disqualified,
        DidNotStart
    ];

    public static bool IsValid(string? status) =>
        !string.IsNullOrWhiteSpace(status)
        && All.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase);
}
