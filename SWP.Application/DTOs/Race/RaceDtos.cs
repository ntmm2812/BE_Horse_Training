namespace SWP.Application.DTOs.Race;

public class RaceDto
{
    public string? RegistrationDeadline { get; set; }
    public decimal? EntryFee { get; set; }
    public int? MaxParticipants { get; set; }
    public string? Status { get; set; }
    public int RaceID { get; set; }

    public string TenGiai { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;

    public string DiaDiem { get; set; } = string.Empty;

    public int? DistanceMeters { get; set; }

    public decimal? PrizePool { get; set; }

    public decimal? PrizeFirst { get; set; }

    public decimal? PrizeSecond { get; set; }

    public decimal? PrizeThird { get; set; }

    public string? Description { get; set; }
}

// Quản lý CLB tạo giải. Ngày dùng DateOnly ("yyyy-MM-dd"); giải thưởng lưu ở Race.PrizePool.
public class CreateRaceRequestDto
{
    public string? TenGiai { get; set; }

    public string? DiaDiem { get; set; }

    public DateOnly? Ngay { get; set; }

    public DateOnly? RegistrationDeadline { get; set; }

    public int? DistanceMeters { get; set; }

    public int? MaxParticipants { get; set; }

    // Tổng giải thưởng nhập tay; bị bỏ qua nếu có nhập ít nhất một hạng (BE tự tính = Nhất + Nhì + Ba).
    public decimal? PrizePool { get; set; }

    public decimal? PrizeFirst { get; set; }

    public decimal? PrizeSecond { get; set; }

    public decimal? PrizeThird { get; set; }

    // Lệ phí đăng ký (VNĐ), để trống = 0.
    public decimal? EntryFee { get; set; }

    public string? Description { get; set; }
}

// Quản lý CLB hủy giải: lý do bắt buộc, tối đa 500 ký tự.
public class CancelRaceRequestDto
{
    public string? Reason { get; set; }
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
