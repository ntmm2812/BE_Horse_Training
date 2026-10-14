namespace SWP.Application.DTOs.Training;

public class CreateTrainingPlanRequestDto
{
    public int HorseId { get; set; }

    public string PlanName { get; set; } = null!;

    public string? Objective { get; set; }

    /// <summary> Conditioning / Endurance / Speed / RacePreparation / Rehabilitation. </summary>
    public string? TrainingType { get; set; }

    public string? Notes { get; set; }

    public List<TrainingPhaseRequestDto> Phases { get; set; } = new();
}

public class UpdateTrainingPlanRequestDto
{
    public string PlanName { get; set; } = null!;

    public string? Objective { get; set; }

    public string? TrainingType { get; set; }

    public string? Notes { get; set; }

    /// <summary> null = giữ nguyên giai đoạn; chỉ được thay khi giáo án đang Nháp. </summary>
    public List<TrainingPhaseRequestDto>? Phases { get; set; }
}

public class TrainingPhaseRequestDto
{
    public string PhaseName { get; set; } = null!;

    public string? Goal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary> Cường độ tối đa của giai đoạn: Recovery / Low / Medium / High. </summary>
    public string IntensityLevel { get; set; } = null!;

    public int SessionsPerWeek { get; set; }

    /// <summary> Cự ly mục tiêu mỗi buổi (km). </summary>
    public decimal TargetDistanceKm { get; set; }

    /// <summary> Khối lượng thời gian mỗi buổi (phút). </summary>
    public int SessionDurationMinutes { get; set; }

    /// <summary> Turf / Dirt / Sand / Synthetic / Pool; null = theo loại bài tập. </summary>
    public string? Surface { get; set; }
}

public class GenerateWeeklyScheduleRequestDto
{
    /// <summary> Ngày bất kỳ trong tuần cần xếp lịch (chuẩn hóa về Thứ Hai). </summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary> Giờ bắt đầu mặc định "HH:mm"; mặc định 06:00. </summary>
    public string? DefaultStartTime { get; set; }

    public int? AssignedToUserId { get; set; }

    /// <summary> Xóa các buổi Scheduled của giáo án trong tuần rồi sinh lại. </summary>
    public bool ReplaceExisting { get; set; }

    /// <summary> Chỉ xem trước, không lưu (dùng được cả khi giáo án Nháp). </summary>
    public bool PreviewOnly { get; set; }
}
