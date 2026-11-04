namespace SWP.Domain.Models.Entity;

/// <summary> Lịch tiêm phòng / tẩy giun. Trạng thái lưu chỉ là chờ tiêm hoặc đã tiêm. </summary>
public class VaccinationSchedule
{
    public int ScheduleId { get; set; }

    public int HorseId { get; set; }

    public string VaccineType { get; set; } = null!;

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Horse Horse { get; set; } = null!;
}
