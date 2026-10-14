namespace SWP.Domain.Common;

/// <summary> Loại bài tập của một buổi tập; mỗi loại có cường độ và mặt sân mặc định. </summary>
public static class TrainingTypes
{
    public const string Walk = "Walk";

    public const string Swimming = "Swimming";

    public const string Trot = "Trot";

    public const string Canter = "Canter";

    public const string Interval = "Interval";

    public const string Gallop = "Gallop";

    public const string TrialRun = "TrialRun";

    public static readonly string[] All =
    [
        Walk,
        Swimming,
        Trot,
        Canter,
        Interval,
        Gallop,
        TrialRun,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Walk] = "Đi bộ",
        [Swimming] = "Bơi",
        [Trot] = "Nước kiệu",
        [Canter] = "Phi chậm (canter)",
        [Interval] = "Chạy biến tốc",
        [Gallop] = "Phi nước đại",
        [TrialRun] = "Chạy thử",
    };

    private static readonly Dictionary<string, string> Intensities = new()
    {
        [Walk] = TrainingIntensityLevels.Recovery,
        [Swimming] = TrainingIntensityLevels.Recovery,
        [Trot] = TrainingIntensityLevels.Low,
        [Canter] = TrainingIntensityLevels.Medium,
        [Interval] = TrainingIntensityLevels.High,
        [Gallop] = TrainingIntensityLevels.High,
        [TrialRun] = TrainingIntensityLevels.High,
    };

    private static readonly Dictionary<string, string> DefaultSurfaces = new()
    {
        [Walk] = TrackSurfaces.Sand,
        [Swimming] = TrackSurfaces.Pool,
        [Trot] = TrackSurfaces.Sand,
        [Canter] = TrackSurfaces.Dirt,
        [Interval] = TrackSurfaces.Dirt,
        [Gallop] = TrackSurfaces.Turf,
        [TrialRun] = TrackSurfaces.Turf,
    };

    public static bool TryNormalize(string? trainingType, out string normalized) =>
        CodeLists.TryNormalize(All, trainingType, out normalized);

    /// <summary> Cường độ của loại bài tập; null nếu loại không thuộc danh mục (dữ liệu cũ). </summary>
    public static string? GetIntensity(string? trainingType) =>
        TryNormalize(trainingType, out var normalized) ? Intensities[normalized] : null;

    public static string GetDefaultSurface(string trainingType) =>
        TryNormalize(trainingType, out var normalized) ? DefaultSurfaces[normalized] : TrackSurfaces.Sand;
}
