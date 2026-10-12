namespace SWP.Application.DTOs.Horses;

/// <summary> Khớp field mock FE (bảng HORSE / horses.js). </summary>
public class HorseDto
{
    public int HorseID { get; set; }

    public string Ten { get; set; } = null!;

    public string Giong { get; set; } = null!;

    public int Tuoi { get; set; }

    public decimal CanNang { get; set; }

    public string DongDoi { get; set; } = null!;

    public int OwnerID { get; set; }

    public string? PhotoUrl { get; set; }
}
