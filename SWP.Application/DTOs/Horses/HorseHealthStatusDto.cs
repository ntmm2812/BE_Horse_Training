namespace SWP.Application.DTOs.Horses;

public class HorseHealthStatusDto
{
    public int HorseID { get; set; }

    public string TrangThai { get; set; } = null!;

    /// <summary>Ngày cập nhật yyyy-MM-dd.</summary>
    public string NgayCapNhat { get; set; } = null!;
}
