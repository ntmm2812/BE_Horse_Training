namespace SWP.Application.DTOs.Care;

public class CareTaskDto
{
    public int TaskID { get; set; }

    public int HorseID { get; set; }

    public int GroomID { get; set; }

    public string LoaiViec { get; set; } = string.Empty;

    public bool TrangThai { get; set; }

    public string Ngay { get; set; } = string.Empty;

    public string Gio { get; set; } = string.Empty;
}

public class StableAssignmentDto
{
    public int StableID { get; set; }

    public int HorseID { get; set; }

    public string SoChuong { get; set; } = string.Empty;

    public string LichSinhHoat { get; set; } = string.Empty;
}

public class FeedingMealDto
{
    public int FeedID { get; set; }

    public int HorseID { get; set; }

    public decimal NguCoc { get; set; }

    public decimal Co { get; set; }

    public decimal Vitamin { get; set; }

    public string Bua { get; set; } = string.Empty;
}

public class IncidentReportDto
{
    public int ReportID { get; set; }

    public int HorseID { get; set; }

    public int GroomID { get; set; }

    public string LoaiSuCo { get; set; } = string.Empty;

    public string MoTa { get; set; } = string.Empty;

    public string HinhAnh { get; set; } = string.Empty;

    public string Ngay { get; set; } = string.Empty;
}

public class CreateIncidentReportRequestDto
{
    public int HorseID { get; set; }

    public string? LoaiSuCo { get; set; }

    public string? MoTa { get; set; }

    public string? HinhAnh { get; set; }

    public DateOnly Ngay { get; set; }
}

public class SupplyDto
{
    public int SupplyID { get; set; }

    public string TenVatTu { get; set; } = string.Empty;

    public string Loai { get; set; } = string.Empty;

    public decimal SoLuongTon { get; set; }

    public int ManagedBy { get; set; }

    public decimal DonGia { get; set; }

    public string DonVi { get; set; } = string.Empty;
}

public class SaveSupplyRequestDto
{
    public string? TenVatTu { get; set; }

    public string? Loai { get; set; }

    public decimal SoLuongTon { get; set; }

    public decimal DonGia { get; set; }

    public int ManagedBy { get; set; }
}

public class AdjustStockRequestDto
{
    public decimal Delta { get; set; }
}

public class StaffMemberDto
{
    public int UserID { get; set; }

    public string HoTen { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
