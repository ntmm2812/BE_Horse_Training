namespace SWP.Domain.Common;

/// <summary> Mục tiêu tổng của giáo án (cột TrainingPlans.TrainingType). </summary>
public static class TrainingPlanTypes
{
    public const string Conditioning = "Conditioning";

    public const string Endurance = "Endurance";

    public const string Speed = "Speed";

    public const string RacePreparation = "RacePreparation";

    public const string Rehabilitation = "Rehabilitation";

    public static readonly string[] All =
    [
        Conditioning,
        Endurance,
        Speed,
        RacePreparation,
        Rehabilitation,
    ];

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Conditioning] = "Thể lực nền",
        [Endurance] = "Sức bền",
        [Speed] = "Tốc độ",
        [RacePreparation] = "Chuẩn bị thi đấu",
        [Rehabilitation] = "Phục hồi chấn thương",
    };

    public static bool TryNormalize(string? planType, out string normalized) =>
        CodeLists.TryNormalize(All, planType, out normalized);
}
