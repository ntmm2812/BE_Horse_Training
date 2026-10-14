namespace SWP.Domain.Common;

/// <summary> Draft → Active → Completed; Draft/Active → Cancelled. </summary>
public static class TrainingPlanStatuses
{
    public const string Draft = "Draft";

    public const string Active = "Active";

    public const string Completed = "Completed";

    public const string Cancelled = "Cancelled";

    public static readonly string[] All =
    [
        Draft,
        Active,
        Completed,
        Cancelled,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Draft] = "Nháp",
        [Active] = "Đang áp dụng",
        [Completed] = "Hoàn thành",
        [Cancelled] = "Đã hủy",
    };

    public static bool TryNormalize(string? status, out string normalized) =>
        CodeLists.TryNormalize(All, status, out normalized);

    public static bool Is(string? status, string expected) =>
        string.Equals(status?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
