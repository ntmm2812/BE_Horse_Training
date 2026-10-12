namespace SWP.Application.DTOs.Horses;

public class OwnerHorseDetailDto
{
    public int HorseID { get; set; }

    public string Ten { get; set; } = null!;

    public string Giong { get; set; } = null!;

    public string DongDoi { get; set; } = null!;

    public int Tuoi { get; set; }

    public decimal CanNang { get; set; }

    public string? PhotoUrl { get; set; }

    public string? GioiTinh { get; set; }

    public string? MauLong { get; set; }

    public string? NgaySinh { get; set; }

    public string? SoChuong { get; set; }

    public IReadOnlyList<OwnerDailyScheduleItemDto> LichSinhHoat { get; set; } =
        Array.Empty<OwnerDailyScheduleItemDto>();

    public IReadOnlyList<string> KhauPhan { get; set; } = Array.Empty<string>();

    public IReadOnlyList<OwnerRaceAchievementDto> ThanhTichThiDau { get; set; } =
        Array.Empty<OwnerRaceAchievementDto>();
}

public class OwnerDailyScheduleItemDto
{
    public string Gio { get; set; } = null!;

    public string CongViec { get; set; } = null!;
}

public class OwnerRaceAchievementDto
{
    public string Ngay { get; set; } = null!;

    public string TenCuocDua { get; set; } = null!;

    public string TrangThai { get; set; } = null!;
}
