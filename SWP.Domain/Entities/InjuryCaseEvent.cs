namespace SWP.Domain.Models.Entity;

/// <summary>Lịch sử chỉ ghi thêm; chỉ bác sĩ đọc nội dung lâm sàng và bản chụp bệnh án.</summary>
public class InjuryCaseEvent
{
    public int InjuryCaseEventId { get; set; }
    public int InjuryCaseId { get; set; }
    public int ActorUserId { get; set; }
    public string Kind { get; set; } = null!;
    public string DetailsJson { get; set; } = null!;
    public int? MedicalRecordId { get; set; }
    public DateTime CreatedAt { get; set; }
}
