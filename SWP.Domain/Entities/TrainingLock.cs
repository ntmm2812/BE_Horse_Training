using System;

namespace SWP.Domain.Models.Entity;

public class TrainingLock
{
    public int LockId { get; set; }

    public int HorseId { get; set; }

    public int VetId { get; set; }

    public string LyDo { get; set; } = null!;

    public DateOnly NgayDat { get; set; }

    public DateOnly? NgayGo { get; set; }

    public virtual Horse Horse { get; set; } = null!;

    public virtual User Vet { get; set; } = null!;
}
