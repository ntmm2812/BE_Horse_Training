using System;

namespace SWP.Domain.Models.Entity;

public class TrainingLock
{
    public int LockId { get; set; }
    public int? InjuryCaseId { get; set; }

    public int HorseId { get; set; }

    public int VetId { get; set; }

    public string LyDo { get; set; } = null!;

    public DateOnly NgayDat { get; set; }

    public DateOnly? NgayGo { get; set; }

    // Nullable để giữ nguyên lịch sử khóa cũ chưa có thông tin mở khóa.
    public int? ReleasedByUserId { get; set; }
    public string? ReleaseReason { get; set; }
    public int? ReexamRecordId { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User Vet { get; set; } = null!;
}
