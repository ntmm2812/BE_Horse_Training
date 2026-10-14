using SWP.Domain.Common;

namespace SWP.Application.Training;

/// <summary> Ngưỡng nghiệp vụ của luồng Lập &amp; Thực hiện Giáo án. </summary>
public static class TrainingRules
{
    // Giáo án / giai đoạn
    public const int MaxPhasesPerPlan = 12;
    public const int MaxPlanDays = 366;
    public const int MinSessionsPerWeek = 1;
    public const int MaxSessionsPerWeek = 6;
    public const decimal MaxDistanceKm = 15m;
    public const int MinDurationMinutes = 5;
    public const int MaxDurationMinutes = 240;

    // Buổi tập
    public const int MaxSessionsPerDay = 2;
    public const int MaxQueryRangeDays = 62;
    public static readonly TimeOnly DefaultStartTime = new(6, 0);

    // Kết quả buổi tập
    public const decimal MaxPerformanceScore = 10m;
    public const decimal MaxActualDistanceKm = 50m;
    public const int MaxActualDurationMinutes = 300;
    public const decimal MinHeartRate = 20m;
    public const decimal MaxHeartRate = 260m;
    public const decimal MaxSpeedKmh = 80m;

    // Gợi ý giáo án
    public const int DefaultSuggestionWeeks = 8;
    public const int MaxSuggestionWeeks = 26;

    // Đánh giá thể trạng
    public const int VitalsWindowDays = 3;
    public const int LoadWindowDays = 7;
    public const int HighLoadSessionThreshold = 3;
    public const decimal ElevatedTemperatureC = 38.5m;
    public const decimal FeverTemperatureC = 39.0m;
    public const decimal ElevatedRestingHeartRate = 44m;
    public const decimal HighRestingHeartRate = 60m;
    public const decimal ElevatedRespiratoryRate = 20m;
    public const int YoungHorseAge = 2;
    public const int SeniorHorseAge = 18;

    /// <summary> Khối lượng mặc định mỗi buổi khi phải hạ cường độ xuống mức này. </summary>
    public static (decimal DistanceKm, int DurationMinutes) DefaultVolume(string intensity) => intensity switch
    {
        TrainingIntensityLevels.Recovery => (2.0m, 30),
        TrainingIntensityLevels.Low => (3.0m, 35),
        TrainingIntensityLevels.Medium => (3.0m, 40),
        _ => (2.0m, 30),
    };

    /// <summary> Thứ Hai của tuần chứa <paramref name="date"/>. </summary>
    public static DateOnly WeekStartOf(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    public static string DayOfWeekLabel(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ Hai",
        DayOfWeek.Tuesday => "Thứ Ba",
        DayOfWeek.Wednesday => "Thứ Tư",
        DayOfWeek.Thursday => "Thứ Năm",
        DayOfWeek.Friday => "Thứ Sáu",
        DayOfWeek.Saturday => "Thứ Bảy",
        _ => "Chủ Nhật",
    };
}
