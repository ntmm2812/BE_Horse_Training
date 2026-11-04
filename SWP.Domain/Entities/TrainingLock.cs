namespace SWP.Domain.Models.Entity;

/// <summary> Lệnh khóa huấn luyện của bác sĩ thú y. NgayGo null = đang hiệu lực. </summary>
public class TrainingLock
{
    public int LockId { get; set; }

    public int HorseId { get; set; }

    public int VetId { get; set; }

    public string Reason { get; set; } = null!;

    public DateOnly PlacedOn { get; set; }

    public DateOnly? ReleasedOn { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User Vet { get; set; } = null!;
}
