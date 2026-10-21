using System;

namespace SWP.Domain.Models.Entity;

public class VaccinationSchedule
{
    public int ScheduleId { get; set; }

    public int HorseId { get; set; }

    public string Loai { get; set; } = null!;

    public DateOnly NgayHen { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public virtual Horse Horse { get; set; } = null!;
}
