namespace SWP.Domain.Models.Entity;

// Đánh giá của HLV cho một buổi tập (mỗi buổi tối đa một đánh giá). SentToOwnerAt = null: chủ ngựa chưa thấy.
public class TrainingSessionReview
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public TrainingSchedule Schedule { get; set; } = null!;
    public int Score { get; set; }
    public string Assessment { get; set; } = null!;
    public string Comment { get; set; } = null!;
    public int ReviewedByUserId { get; set; }
    public User ReviewedByUser { get; set; } = null!;
    public DateTime ReviewedAt { get; set; }
    public DateTime? SentToOwnerAt { get; set; }
}
