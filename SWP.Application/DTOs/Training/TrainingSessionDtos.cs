namespace SWP.Application.DTOs.Training;

public class TrainingSessionDto
{
    /// <summary> 0 với buổi ở chế độ xem trước (chưa lưu). </summary>
    public int SessionId { get; set; }

    public int PlanId { get; set; }

    public string? PlanName { get; set; }

    public int? PhaseId { get; set; }

    public string? PhaseName { get; set; }

    public int HorseId { get; set; }

    public string? HorseName { get; set; }

    public string? SessionTitle { get; set; }

    public DateOnly TrainingDate { get; set; }

    /// <summary> "HH:mm" – giờ dự kiến, được ghi đè bằng giờ thực tế khi bắt đầu buổi tập. </summary>
    public string? StartTime { get; set; }

    /// <summary> "HH:mm" – giờ kết thúc thực tế. </summary>
    public string? EndTime { get; set; }

    public string? TrainingType { get; set; }

    public string? IntensityLevel { get; set; }

    public string? Surface { get; set; }

    public decimal? DistanceKm { get; set; }

    public int? DurationMinutes { get; set; }

    public string Status { get; set; } = null!;

    public int? AssignedToUserId { get; set; }

    public string? AssignedToName { get; set; }

    public string? Notes { get; set; }

    public decimal? ActualDistanceKm { get; set; }

    public int? ActualDurationMinutes { get; set; }

    public decimal? AvgHeartRate { get; set; }

    public decimal? MaxHeartRate { get; set; }

    public decimal? AvgSpeedKmh { get; set; }

    /// <summary> Điểm phong độ thang 10. </summary>
    public decimal? PerformanceScore { get; set; }

    public string? Result { get; set; }

    public string? TrainerComment { get; set; }

    public string? TrialVideoUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class TrainingSessionQueryDto
{
    /// <summary> Mặc định hôm nay. </summary>
    public DateOnly? From { get; set; }

    /// <summary> Mặc định From + 6 ngày. </summary>
    public DateOnly? To { get; set; }

    public int? HorseId { get; set; }

    public int? PlanId { get; set; }

    public int? AssignedToUserId { get; set; }

    public string? Status { get; set; }
}

public class SaveTrainingSessionRequestDto
{
    public DateOnly TrainingDate { get; set; }

    /// <summary> "HH:mm"; mặc định 06:00. </summary>
    public string? StartTime { get; set; }

    public string TrainingType { get; set; } = null!;

    public decimal DistanceKm { get; set; }

    public int DurationMinutes { get; set; }

    public string? Surface { get; set; }

    public string? SessionTitle { get; set; }

    public string? Notes { get; set; }

    public int? AssignedToUserId { get; set; }
}

public class AssignTrainingSessionRequestDto
{
    /// <summary> null = bỏ phân công. </summary>
    public int? AssignedToUserId { get; set; }
}

public class CompleteTrainingSessionRequestDto
{
    public decimal ActualDistanceKm { get; set; }

    public int ActualDurationMinutes { get; set; }

    /// <summary> Điểm phong độ 0–10. </summary>
    public decimal PerformanceScore { get; set; }

    public decimal? AvgHeartRate { get; set; }

    public decimal? MaxHeartRate { get; set; }

    public decimal? AvgSpeedKmh { get; set; }

    public string? Result { get; set; }

    /// <summary> Nhận xét chuyên môn của HLV (bắt buộc). </summary>
    public string TrainerComment { get; set; } = null!;

    public string? TrialVideoUrl { get; set; }
}

public class CancelTrainingSessionRequestDto
{
    public string Reason { get; set; } = null!;
}

public class TrainingStaffDto
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleId { get; set; } = null!;
}

public class HorseWeeklyScheduleDto
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public IReadOnlyList<WeeklyScheduleDayDto> Days { get; set; } = Array.Empty<WeeklyScheduleDayDto>();

    public WeeklyScheduleSummaryDto Summary { get; set; } = new();
}

public class WeeklyScheduleDayDto
{
    public DateOnly Date { get; set; }

    public string DayOfWeek { get; set; } = null!;

    /// <summary> true nếu ngày không có buổi tập nào (chưa hủy). </summary>
    public bool IsRestDay { get; set; }

    public IReadOnlyList<TrainingSessionDto> Sessions { get; set; } = Array.Empty<TrainingSessionDto>();
}

public class WeeklyScheduleSummaryDto
{
    public int TotalSessions { get; set; }

    public int CompletedSessions { get; set; }

    public int CancelledSessions { get; set; }

    public decimal PlannedDistanceKm { get; set; }

    public decimal ActualDistanceKm { get; set; }

    public int PlannedDurationMinutes { get; set; }

    public decimal? AveragePerformanceScore { get; set; }
}
