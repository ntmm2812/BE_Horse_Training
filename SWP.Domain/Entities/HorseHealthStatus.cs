namespace SWP.Domain.Models.Entity;

public class HorseHealthStatus
{
    public int HorseId { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime NgayCapNhat { get; set; }

    public virtual Horse Horse { get; set; } = null!;
}
