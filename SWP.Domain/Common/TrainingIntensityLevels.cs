namespace SWP.Domain.Common;

/// <summary> Mức cường độ tập, sắp xếp từ nhẹ đến nặng. </summary>
public static class TrainingIntensityLevels
{
    public const string Recovery = "Recovery";

    public const string Low = "Low";

    public const string Medium = "Medium";

    public const string High = "High";

    public static readonly string[] All =
    [
        Recovery,
        Low,
        Medium,
        High,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Recovery] = "Phục hồi",
        [Low] = "Nhẹ",
        [Medium] = "Trung bình",
        [High] = "Cao",
    };

    public static bool IsValid(string? level) => Rank(level) > 0;

    public static bool TryNormalize(string? level, out string normalized) =>
        CodeLists.TryNormalize(All, level, out normalized);

    /// <summary> 1 = Recovery … 4 = High; 0 nếu không hợp lệ. </summary>
    public static int Rank(string? level) =>
        TryNormalize(level, out var normalized) ? Array.IndexOf(All, normalized) + 1 : 0;

    /// <summary> Ngược của <see cref="Rank"/>; null nếu rank ngoài khoảng 1..4. </summary>
    public static string? FromRank(int rank) =>
        rank >= 1 && rank <= All.Length ? All[rank - 1] : null;

    /// <summary> true nếu <paramref name="level"/> không nặng hơn <paramref name="limit"/>. </summary>
    public static bool IsWithin(string? level, string? limit) =>
        IsValid(level) && IsValid(limit) && Rank(level) <= Rank(limit);

    public static string Min(string a, string b) => FromRank(Math.Min(Rank(a), Rank(b)))!;
}
