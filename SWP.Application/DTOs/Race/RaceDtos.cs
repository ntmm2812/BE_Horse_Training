namespace SWP.Application.DTOs.Race;

public class RaceDto
{
    public int RaceID { get; set; }

    public string TenGiai { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;

    public string DiaDiem { get; set; } = string.Empty;
}

public class RaceRegistrationDto
{
    public int HorseID { get; set; }

    public int RaceID { get; set; }

    public string NgayDangKy { get; set; } = string.Empty;

    public int? ThuHang { get; set; }

    public decimal? TienThuong { get; set; }
}

public class RegisterRaceRequestDto
{
    public int HorseID { get; set; }
}

public class UpdateRaceResultRequestDto
{
    public int? ThuHang { get; set; }
}

public class RaceResultUpdateDto
{
    public RaceRegistrationDto Registration { get; set; } = new();

    public bool PrizeCleared { get; set; }
}

public class SetRacePrizeRequestDto
{
    public decimal Amount { get; set; }
}

public class PrizeAlertDto
{
    public int AlertID { get; set; }

    public int HorseID { get; set; }

    public int RaceID { get; set; }

    public string Ngay { get; set; } = string.Empty;
}
