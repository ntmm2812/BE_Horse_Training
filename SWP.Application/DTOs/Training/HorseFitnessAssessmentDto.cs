namespace SWP.Application.DTOs.Training;

public class HorseFitnessAssessmentDto
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public int? AgeYears { get; set; }

    public decimal? WeightKg { get; set; }

    /// <summary> Đủ điều kiện / Cần theo dõi / Chấn thương / Cách ly; null nếu chưa cập nhật. </summary>
    public string? HealthStatus { get; set; }

    public bool IsTrainingLocked { get; set; }

    public string? LockReason { get; set; }

    public FitnessVitalsDto? LatestVitals { get; set; }

    public RecentTrainingLoadDto RecentLoad { get; set; } = new();

    /// <summary> Ready / Limited / RecoveryOnly / NotAllowed. </summary>
    public string Readiness { get; set; } = null!;

    public string ReadinessLabel { get; set; } = null!;

    public bool CanTrain { get; set; }

    /// <summary> Mức trần bắt buộc; null nếu không được tập. </summary>
    public string? MaxAllowedIntensity { get; set; }

    /// <summary> Mức khuyến nghị (có tính tải tập gần đây). </summary>
    public string? RecommendedIntensity { get; set; }

    public IReadOnlyList<string> AllowedTrainingTypes { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

    public DateTime AssessedAt { get; set; }
}

public class FitnessVitalsDto
{
    public decimal? HeartRate { get; set; }

    public decimal? RespiratoryRate { get; set; }

    public decimal? BodyTemperature { get; set; }

    public DateTime RecordedAt { get; set; }
}

public class RecentTrainingLoadDto
{
    public int CompletedSessions { get; set; }

    public int HighIntensitySessions { get; set; }

    public decimal DistanceKm { get; set; }
}
