using SWP.Domain.Common;

namespace SWP.Application.Training;

public sealed record PhaseSpec(
    int PhaseId,
    string PhaseName,
    DateOnly StartDate,
    DateOnly EndDate,
    string IntensityLevel,
    int SessionsPerWeek,
    decimal TargetDistanceKm,
    int SessionDurationMinutes,
    string? Surface);

public sealed record WeeklyGenerationInput
{
    /// <summary> Ngày bất kỳ trong tuần; sẽ được chuẩn hóa về Thứ Hai. </summary>
    public required DateOnly WeekStart { get; init; }

    public required IReadOnlyList<PhaseSpec> Phases { get; init; }

    /// <summary> Mức trần theo thể trạng hiện tại; null = ngựa không được tập. </summary>
    public required string? MaxAllowedIntensity { get; init; }

    public required DateOnly Today { get; init; }

    /// <summary> Những ngày ngựa đã có buổi tập (chưa hủy). </summary>
    public IReadOnlySet<DateOnly> OccupiedDates { get; init; } = new HashSet<DateOnly>();

    public TimeOnly StartTime { get; init; } = TrainingRules.DefaultStartTime;
}

public sealed record GeneratedSession(
    DateOnly Date,
    int PhaseId,
    string PhaseName,
    string Title,
    string TrainingType,
    string IntensityLevel,
    decimal DistanceKm,
    int DurationMinutes,
    string Surface,
    TimeOnly StartTime);

public sealed record SkippedDay(DateOnly Date, string Reason);

public sealed record WeeklyGenerationOutput(
    DateOnly WeekStart,
    IReadOnlyList<GeneratedSession> Sessions,
    IReadOnlyList<SkippedDay> SkippedDays,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Sinh lịch tập 1 tuần từ các giai đoạn của giáo án:
/// rải buổi theo mẫu ngày cố định (Chủ Nhật luôn nghỉ), xen kẽ buổi nhẹ/buổi chính,
/// và hạ cường độ nếu thể trạng hiện tại không cho phép mức của giai đoạn.
/// </summary>
public static class WeeklyScheduleGenerator
{
    /// <summary> Số buổi/tuần → các ngày tập (0 = Thứ Hai). </summary>
    public static readonly IReadOnlyDictionary<int, int[]> DayPatterns = new Dictionary<int, int[]>
    {
        [1] = [2],
        [2] = [1, 4],
        [3] = [0, 2, 4],
        [4] = [0, 1, 3, 4],
        [5] = [0, 1, 2, 4, 5],
        [6] = [0, 1, 2, 3, 4, 5],
    };

    /// <summary> Cường độ → (bài chính, bài nhẹ xen kẽ). </summary>
    public static (string Primary, string Secondary) SessionTypesFor(string intensity) => intensity switch
    {
        TrainingIntensityLevels.High => (TrainingTypes.Gallop, TrainingTypes.Canter),
        TrainingIntensityLevels.Medium => (TrainingTypes.Canter, TrainingTypes.Trot),
        TrainingIntensityLevels.Low => (TrainingTypes.Trot, TrainingTypes.Walk),
        _ => (TrainingTypes.Walk, TrainingTypes.Walk),
    };

    public static WeeklyGenerationOutput Generate(WeeklyGenerationInput input)
    {
        var weekStart = TrainingRules.WeekStartOf(input.WeekStart);
        var sessions = new List<GeneratedSession>();
        var skipped = new List<SkippedDay>();
        var warnings = new List<string>();

        if (input.MaxAllowedIntensity is null)
        {
            warnings.Add("Thể trạng hiện tại không cho phép tập – không tạo buổi tập nào.");
            return new WeeklyGenerationOutput(weekStart, sessions, skipped, warnings);
        }

        foreach (var phase in input.Phases.OrderBy(p => p.StartDate))
        {
            if (!DayPatterns.TryGetValue(phase.SessionsPerWeek, out var pattern))
            {
                continue;
            }

            var effective = TrainingIntensityLevels.Min(phase.IntensityLevel, input.MaxAllowedIntensity);
            var capped = effective != phase.IntensityLevel;
            var (primary, secondary) = SessionTypesFor(effective);
            var (distanceKm, durationMinutes) = capped
                ? CappedVolume(phase, effective)
                : (phase.TargetDistanceKm, phase.SessionDurationMinutes);
            var touchesWeek = false;

            for (var i = 0; i < pattern.Length; i++)
            {
                var date = weekStart.AddDays(pattern[i]);
                if (date < phase.StartDate || date > phase.EndDate)
                {
                    continue;
                }

                touchesWeek = true;
                if (date < input.Today)
                {
                    skipped.Add(new SkippedDay(date, "Ngày đã qua."));
                    continue;
                }

                if (input.OccupiedDates.Contains(date))
                {
                    skipped.Add(new SkippedDay(date, "Ngựa đã có lịch tập trong ngày."));
                    continue;
                }

                // Buổi đầu tuần nhẹ để khởi động; một buổi/tuần thì là buổi chính.
                var isPrimary = pattern.Length == 1 || i % 2 == 1;
                var trainingType = isPrimary ? primary : secondary;
                sessions.Add(new GeneratedSession(
                    Date: date,
                    PhaseId: phase.PhaseId,
                    PhaseName: phase.PhaseName,
                    Title: $"{phase.PhaseName} - {TrainingTypes.Labels[trainingType]}",
                    TrainingType: trainingType,
                    IntensityLevel: TrainingTypes.GetIntensity(trainingType)!,
                    DistanceKm: distanceKm,
                    DurationMinutes: durationMinutes,
                    Surface: ResolveSurface(trainingType, phase.Surface),
                    StartTime: input.StartTime));
            }

            if (capped && touchesWeek)
            {
                warnings.Add(
                    $"Giai đoạn '{phase.PhaseName}' đặt cường độ {phase.IntensityLevel} nhưng thể trạng hiện tại chỉ cho phép {input.MaxAllowedIntensity} – lịch tuần đã được hạ cường độ.");
            }
        }

        return new WeeklyGenerationOutput(
            weekStart,
            sessions.OrderBy(s => s.Date).ToList(),
            skipped.OrderBy(s => s.Date).ToList(),
            warnings);
    }

    public static string ResolveSurface(string trainingType, string? preferredSurface)
    {
        if (TrackSurfaces.TryNormalize(preferredSurface, out var surface)
            && TrackSurfaces.IsCompatible(trainingType, surface))
        {
            return surface;
        }

        return TrainingTypes.GetDefaultSurface(trainingType);
    }

    private static (decimal DistanceKm, int DurationMinutes) CappedVolume(PhaseSpec phase, string effective)
    {
        var (defaultDistance, defaultDuration) = TrainingRules.DefaultVolume(effective);
        return (Math.Min(phase.TargetDistanceKm, defaultDistance), Math.Min(phase.SessionDurationMinutes, defaultDuration));
    }
}
