using SWP.Domain.Common;

namespace SWP.Application.Training;

public sealed record SuggestedPhase(
    string PhaseName,
    string Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    string IntensityLevel,
    int SessionsPerWeek,
    decimal TargetDistanceKm,
    int SessionDurationMinutes,
    string Surface);

public sealed record TrainingPlanSuggestion(
    bool CanCreatePlan,
    string? PlanType,
    IReadOnlyList<SuggestedPhase> Phases,
    IReadOnlyList<string> Rationale);

/// <summary>
/// Đề xuất giáo án nhiều giai đoạn theo mức cường độ khuyến nghị của thể trạng:
/// càng sẵn sàng thì càng nhiều giai đoạn tăng tiến (nền tảng → phát triển → tăng tốc → giảm tải).
/// </summary>
public static class TrainingPlanSuggester
{
    public const int ReducedVolumeYoungAge = 3;
    public const int ReducedVolumeSeniorAge = 15;
    public const decimal ReducedDistanceFactor = 0.8m;

    private sealed record PhaseTemplate(
        string Name,
        string Goal,
        string Intensity,
        decimal Ratio,
        int SessionsPerWeek,
        decimal DistanceKm,
        int DurationMinutes,
        string Surface);

    private static readonly PhaseTemplate Foundation = new(
        "Nền tảng thể lực",
        "Xây dựng sức bền nền, làm quen khối lượng tập",
        TrainingIntensityLevels.Low, 0, 4, 3.0m, 35, TrackSurfaces.Sand);

    private static readonly IReadOnlyDictionary<string, (string PlanType, PhaseTemplate[] Phases)> Templates =
        new Dictionary<string, (string, PhaseTemplate[])>
        {
            [TrainingIntensityLevels.High] = (TrainingPlanTypes.RacePreparation,
            [
                Foundation with { Ratio = 0.25m, SessionsPerWeek = 5 },
                new("Phát triển sức bền", "Tăng cự ly phi chậm, nâng ngưỡng sức bền",
                    TrainingIntensityLevels.Medium, 0.35m, 5, 3.5m, 40, TrackSurfaces.Dirt),
                new("Tăng tốc độ", "Bài phi nước đại trên sân cỏ, rèn tốc độ thi đấu",
                    TrainingIntensityLevels.High, 0.25m, 5, 2.0m, 30, TrackSurfaces.Turf),
                new("Giảm tải trước thi đấu", "Giữ phong độ, giảm khối lượng để hồi sức",
                    TrainingIntensityLevels.Medium, 0.15m, 4, 2.5m, 30, TrackSurfaces.Turf),
            ]),
            [TrainingIntensityLevels.Medium] = (TrainingPlanTypes.Endurance,
            [
                Foundation with { Ratio = 0.4m },
                new("Phát triển sức bền", "Tăng dần cự ly phi chậm",
                    TrainingIntensityLevels.Medium, 0.6m, 5, 3.0m, 40, TrackSurfaces.Dirt),
            ]),
            [TrainingIntensityLevels.Low] = (TrainingPlanTypes.Conditioning,
            [
                new("Phục hồi vận động", "Đi bộ trên sân mềm, ổn định nhịp tim",
                    TrainingIntensityLevels.Recovery, 0.25m, 3, 2.0m, 30, TrackSurfaces.Sand),
                Foundation with { Ratio = 0.75m },
            ]),
            [TrainingIntensityLevels.Recovery] = (TrainingPlanTypes.Rehabilitation,
            [
                new("Phục hồi chức năng", "Vận động nhẹ có kiểm soát theo chỉ định thú y",
                    TrainingIntensityLevels.Recovery, 1m, 3, 1.5m, 25, TrackSurfaces.Sand),
            ]),
        };

    public static TrainingPlanSuggestion Suggest(
        string? recommendedIntensity,
        int? ageYears,
        DateOnly startDate,
        int weeks)
    {
        var rationale = new List<string>();
        if (!TrainingIntensityLevels.TryNormalize(recommendedIntensity, out var intensity))
        {
            rationale.Add("Thể trạng hiện tại không cho phép tập – chưa thể lập giáo án.");
            return new TrainingPlanSuggestion(false, null, Array.Empty<SuggestedPhase>(), rationale);
        }

        var (planType, templates) = Templates[intensity];
        rationale.Add(
            $"Cường độ khuyến nghị theo thể trạng: {TrainingIntensityLevels.Labels[intensity]} – giáo án '{TrainingPlanTypes.Labels[planType]}'.");

        var distanceFactor = 1m;
        if (ageYears is { } age && (age < ReducedVolumeYoungAge || age >= ReducedVolumeSeniorAge))
        {
            distanceFactor = ReducedDistanceFactor;
            rationale.Add($"Ngựa {age} tuổi – giảm 20% cự ly mỗi buổi.");
        }

        var allocation = AllocateWeeks(templates.Select(t => t.Ratio).ToArray(), weeks);
        var phases = new List<SuggestedPhase>();
        var cursor = startDate;
        for (var i = 0; i < templates.Length; i++)
        {
            if (allocation[i] == 0)
            {
                continue;
            }

            var template = templates[i];
            var end = cursor.AddDays(allocation[i] * 7 - 1);
            phases.Add(new SuggestedPhase(
                template.Name,
                template.Goal,
                cursor,
                end,
                template.Intensity,
                template.SessionsPerWeek,
                Math.Round(template.DistanceKm * distanceFactor, 1),
                template.DurationMinutes,
                template.Surface));
            cursor = end.AddDays(1);
        }

        if (phases.Count < templates.Length)
        {
            rationale.Add($"Thời lượng {weeks} tuần ngắn – đã lược bớt giai đoạn cuối.");
        }

        return new TrainingPlanSuggestion(true, planType, phases, rationale);
    }

    /// <summary> Chia <paramref name="weeks"/> tuần theo tỉ lệ (phương pháp phần dư lớn nhất). </summary>
    public static int[] AllocateWeeks(decimal[] ratios, int weeks)
    {
        var total = ratios.Sum();
        var exact = ratios.Select(r => r / total * weeks).ToArray();
        var result = exact.Select(e => (int)Math.Floor(e)).ToArray();
        var remaining = weeks - result.Sum();

        var byRemainder = exact
            .Select((value, index) => (Remainder: value - Math.Floor(value), Index: index))
            .OrderByDescending(x => x.Remainder)
            .ThenBy(x => x.Index)
            .ToList();

        for (var i = 0; i < remaining; i++)
        {
            result[byRemainder[i % byRemainder.Count].Index]++;
        }

        return result;
    }
}
