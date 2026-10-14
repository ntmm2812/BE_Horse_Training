namespace SWP.Application.DTOs.Training;

public class TrainingPlanDto
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public int HorseId { get; set; }

    public string? HorseName { get; set; }

    public string? Objective { get; set; }

    public string? TrainingType { get; set; }

    public string? IntensityLevel { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int PhaseCount { get; set; }

    public TrainingPlanProgressDto Progress { get; set; } = new();
}

public class TrainingPlanDetailDto : TrainingPlanDto
{
    public IReadOnlyList<TrainingPhaseDto> Phases { get; set; } = Array.Empty<TrainingPhaseDto>();

    public IReadOnlyList<TrainingSessionDto> Sessions { get; set; } = Array.Empty<TrainingSessionDto>();
}

public class TrainingPlanProgressDto
{
    public int TotalSessions { get; set; }

    public int CompletedSessions { get; set; }

    public int CancelledSessions { get; set; }

    public int UpcomingSessions { get; set; }

    /// <summary> % buổi hoàn thành trên tổng buổi không bị hủy. </summary>
    public decimal CompletionRate { get; set; }

    public decimal? AveragePerformanceScore { get; set; }

    public decimal TotalActualDistanceKm { get; set; }
}

public class TrainingPhaseDto
{
    public int PhaseId { get; set; }

    public int PhaseOrder { get; set; }

    public string PhaseName { get; set; } = null!;

    public string? Goal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string IntensityLevel { get; set; } = null!;

    public int SessionsPerWeek { get; set; }

    public decimal TargetDistanceKm { get; set; }

    public int SessionDurationMinutes { get; set; }

    public string? Surface { get; set; }
}

public class TrainingPlanSuggestionDto
{
    public int HorseId { get; set; }

    public string HorseName { get; set; } = null!;

    public bool CanCreatePlan { get; set; }

    public string? SuggestedPlanName { get; set; }

    public string? SuggestedTrainingType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    /// <summary> Cùng định dạng body tạo giáo án – FE có thể gửi lại nguyên văn. </summary>
    public IReadOnlyList<TrainingPhaseRequestDto> Phases { get; set; } = Array.Empty<TrainingPhaseRequestDto>();

    public IReadOnlyList<string> Rationale { get; set; } = Array.Empty<string>();

    public HorseFitnessAssessmentDto Fitness { get; set; } = null!;
}

public class WeeklyScheduleGenerationResultDto
{
    public int PlanId { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public bool PreviewOnly { get; set; }

    public int CreatedCount { get; set; }

    public int RemovedCount { get; set; }

    public IReadOnlyList<TrainingSessionDto> Sessions { get; set; } = Array.Empty<TrainingSessionDto>();

    public IReadOnlyList<SkippedDayDto> SkippedDays { get; set; } = Array.Empty<SkippedDayDto>();

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public class SkippedDayDto
{
    public DateOnly Date { get; set; }

    public string Reason { get; set; } = null!;
}

public class TrainingOptionDto
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    /// <summary> Chỉ có với loại bài tập. </summary>
    public string? IntensityLevel { get; set; }

    /// <summary> Chỉ có với loại bài tập. </summary>
    public string? DefaultSurface { get; set; }
}

public class TrainingOptionsDto
{
    public IReadOnlyList<TrainingOptionDto> IntensityLevels { get; set; } = Array.Empty<TrainingOptionDto>();

    public IReadOnlyList<TrainingOptionDto> TrainingTypes { get; set; } = Array.Empty<TrainingOptionDto>();

    public IReadOnlyList<TrainingOptionDto> Surfaces { get; set; } = Array.Empty<TrainingOptionDto>();

    public IReadOnlyList<TrainingOptionDto> PlanTypes { get; set; } = Array.Empty<TrainingOptionDto>();

    public IReadOnlyList<TrainingOptionDto> PlanStatuses { get; set; } = Array.Empty<TrainingOptionDto>();

    public IReadOnlyList<TrainingOptionDto> SessionStatuses { get; set; } = Array.Empty<TrainingOptionDto>();
}
