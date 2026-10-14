namespace SWP.Domain.Common;

/// <summary> Scheduled → InProgress → Completed; Scheduled/InProgress → Cancelled. </summary>
public static class TrainingSessionStatuses
{
    public const string Scheduled = "Scheduled";

    public const string InProgress = "InProgress";

    public const string Completed = "Completed";

    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
    [
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Scheduled] = "Đã lên lịch",
        [InProgress] = "Đang tập",
        [Completed] = "Hoàn thành",
        [Cancelled] = "Đã hủy",
    };

    public static bool TryNormalize(string? status, out string normalized) =>
        CodeLists.TryNormalize(All, status, out normalized);

    public static bool Is(string? status, string expected) =>
        string.Equals(status?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
