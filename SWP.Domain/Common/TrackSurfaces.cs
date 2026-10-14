namespace SWP.Domain.Common;

/// <summary> Mặt sân tập. </summary>
public static class TrackSurfaces
{
    public const string Turf = "Turf";

    public const string Dirt = "Dirt";

    public const string Sand = "Sand";

    public const string Synthetic = "Synthetic";

    public const string Pool = "Pool";

    public static readonly string[] All =
    [
        Turf,
        Dirt,
        Sand,
        Synthetic,
        Pool,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Turf] = "Sân cỏ",
        [Dirt] = "Sân đất",
        [Sand] = "Sân cát",
        [Synthetic] = "Sân tổng hợp",
        [Pool] = "Hồ bơi",
    };

    public static bool TryNormalize(string? surface, out string normalized) =>
        CodeLists.TryNormalize(All, surface, out normalized);

    /// <summary> Bơi chỉ tập ở hồ bơi, và hồ bơi chỉ dùng cho bài bơi. </summary>
    public static bool IsCompatible(string trainingType, string surface) =>
        string.Equals(trainingType, TrainingTypes.Swimming, StringComparison.Ordinal)
            == string.Equals(surface, Pool, StringComparison.Ordinal);
}
