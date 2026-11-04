using System.Globalization;
using SWP.Domain.Common;

namespace SWP.Application.Training;

public static class FitnessReadiness
{
    public const string Ready = "Ready";

    public const string Limited = "Limited";

    public const string RecoveryOnly = "RecoveryOnly";

    public const string NotAllowed = "NotAllowed";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Ready] = "Sẵn sàng tập đủ cường độ",
        [Limited] = "Tập có giới hạn cường độ",
        [RecoveryOnly] = "Chỉ tập phục hồi",
        [NotAllowed] = "Không được tập",
    };
}

/// <summary> Dữ liệu thể trạng hiện tại của ngựa (đã gom từ DB). </summary>
public sealed record FitnessInput
{
    public int? AgeYears { get; init; }

    /// <summary> Trạng thái sức khỏe do bác sĩ thú y cập nhật; null nếu chưa có. </summary>
    public string? HealthStatus { get; init; }

    public bool IsTrainingLocked { get; init; }

    public string? LockReason { get; init; }

    public decimal? RestingHeartRate { get; init; }

    public decimal? RespiratoryRate { get; init; }

    public decimal? BodyTemperature { get; init; }

    public int HighIntensitySessionsLast7Days { get; init; }
}

public sealed record FitnessEvaluation(
    bool CanTrain,
    string? MaxAllowedIntensity,
    string? RecommendedIntensity,
    string Readiness,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Quy tắc "xem xét thể trạng": mỗi yếu tố đặt một mức trần cường độ, lấy mức thấp nhất.
/// Tải tập 7 ngày chỉ hạ mức <i>khuyến nghị</i>, không hạ mức trần bắt buộc.
/// </summary>
public static class FitnessEvaluator
{
    private const int NotAllowedRank = 0;
    private static readonly int RecoveryRank = TrainingIntensityLevels.Rank(TrainingIntensityLevels.Recovery);
    private static readonly int LowRank = TrainingIntensityLevels.Rank(TrainingIntensityLevels.Low);
    private static readonly int MediumRank = TrainingIntensityLevels.Rank(TrainingIntensityLevels.Medium);
    private static readonly int HighRank = TrainingIntensityLevels.Rank(TrainingIntensityLevels.High);

    public static FitnessEvaluation Evaluate(FitnessInput input)
    {
        var cap = HighRank;
        var warnings = new List<string>();

        void Limit(int rank, string message)
        {
            cap = Math.Min(cap, rank);
            warnings.Add(message);
        }

        switch (input.HealthStatus?.Trim())
        {
            case HorseHealthStatuses.DuDieuKien:
                break;
            case HorseHealthStatuses.CachLy:
                Limit(NotAllowedRank, "Ngựa đang cách ly – không được xếp lịch tập.");
                break;
            case HorseHealthStatuses.ChanThuong:
                Limit(RecoveryRank, "Ngựa đang chấn thương – chỉ cho phép bài tập phục hồi.");
                break;
            case HorseHealthStatuses.CanTheoDoi:
                Limit(LowRank, "Ngựa đang cần theo dõi sức khỏe – giới hạn cường độ nhẹ.");
                break;
            default:
                Limit(MediumRank, "Chưa có trạng thái sức khỏe từ bác sĩ thú y – tạm giới hạn cường độ trung bình.");
                break;
        }

        if (input.IsTrainingLocked)
        {
            var reason = string.IsNullOrWhiteSpace(input.LockReason) ? string.Empty : $" ({input.LockReason.Trim()})";
            Limit(RecoveryRank, $"Bác sĩ thú y đã khóa huấn luyện{reason} – chỉ cho phép bài tập phục hồi.");
        }

        if (input.BodyTemperature is { } temperature)
        {
            if (temperature >= TrainingRules.FeverTemperatureC)
            {
                Limit(NotAllowedRank, $"Thân nhiệt {Format(temperature)}°C – ngựa đang sốt, tạm dừng tập.");
            }
            else if (temperature > TrainingRules.ElevatedTemperatureC)
            {
                Limit(RecoveryRank, $"Thân nhiệt {Format(temperature)}°C cao hơn bình thường – chỉ tập phục hồi.");
            }
        }

        if (input.RestingHeartRate is { } heartRate)
        {
            if (heartRate > TrainingRules.HighRestingHeartRate)
            {
                Limit(RecoveryRank, $"Nhịp tim lúc nghỉ {Format(heartRate)} bpm rất cao – chỉ tập phục hồi.");
            }
            else if (heartRate > TrainingRules.ElevatedRestingHeartRate)
            {
                Limit(LowRank, $"Nhịp tim lúc nghỉ {Format(heartRate)} bpm cao hơn bình thường – giới hạn cường độ nhẹ.");
            }
        }

        if (input.RespiratoryRate is { } respiratoryRate && respiratoryRate > TrainingRules.ElevatedRespiratoryRate)
        {
            Limit(LowRank, $"Nhịp thở lúc nghỉ {Format(respiratoryRate)} lần/phút cao – giới hạn cường độ nhẹ.");
        }

        if (input.AgeYears is { } age)
        {
            if (age < TrainingRules.YoungHorseAge)
            {
                Limit(LowRank, $"Ngựa non {age} tuổi – chưa tập cường độ trung bình/cao.");
            }
            else if (age >= TrainingRules.SeniorHorseAge)
            {
                Limit(MediumRank, $"Ngựa {age} tuổi – không tập cường độ cao.");
            }
        }

        var recommended = cap;
        if (cap > MediumRank && input.HighIntensitySessionsLast7Days >= TrainingRules.HighLoadSessionThreshold)
        {
            recommended = MediumRank;
            warnings.Add(
                $"Đã có {input.HighIntensitySessionsLast7Days} buổi cường độ cao trong {TrainingRules.LoadWindowDays} ngày qua – nên giảm tải tuần tới.");
        }

        var readiness = cap switch
        {
            NotAllowedRank => FitnessReadiness.NotAllowed,
            _ when cap == RecoveryRank => FitnessReadiness.RecoveryOnly,
            _ when cap == HighRank => FitnessReadiness.Ready,
            _ => FitnessReadiness.Limited,
        };

        return new FitnessEvaluation(
            CanTrain: cap > NotAllowedRank,
            MaxAllowedIntensity: TrainingIntensityLevels.FromRank(cap),
            RecommendedIntensity: TrainingIntensityLevels.FromRank(recommended),
            Readiness: readiness,
            Warnings: warnings);
    }

    private static string Format(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
