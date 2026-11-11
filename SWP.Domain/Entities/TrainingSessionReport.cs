namespace SWP.Domain.Models.Entity;

// Báo cáo của nhân viên chăm sóc cho một buổi tập (mỗi buổi tối đa một báo cáo).
public class TrainingSessionReport
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public TrainingSchedule Schedule { get; set; } = null!;
    public string Result { get; set; } = null!;
    public decimal? ActualDistanceKm { get; set; }
    public string HorseReaction { get; set; } = null!;
    public string Note { get; set; } = "";
    public int ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;
    public DateTime ReportedAt { get; set; }
}
