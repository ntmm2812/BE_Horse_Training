using System.Text.Json.Serialization;

namespace SWP.Application.DTOs.Care;

public sealed record CareTaskDto(
    [property: JsonPropertyName("TaskID")] int TaskID,
    [property: JsonPropertyName("HorseID")] int HorseID,
    [property: JsonPropertyName("GroomID")] int? GroomID,
    [property: JsonPropertyName("LoaiViec")] string LoaiViec,
    [property: JsonPropertyName("TrangThai")] bool TrangThai,
    [property: JsonPropertyName("Ngay")] string Ngay);

public sealed record StableAssignmentDto(
    [property: JsonPropertyName("StableID")] int StableID,
    [property: JsonPropertyName("HorseID")] int HorseID,
    [property: JsonPropertyName("SoChuong")] string SoChuong,
    [property: JsonPropertyName("LichSinhHoat")] string LichSinhHoat);

public sealed record FeedDetailDto(string? FeedName, decimal Quantity, string? Unit, string? Instructions);

public sealed record FeedingPlanDto(
    [property: JsonPropertyName("FeedID")] int FeedID,
    [property: JsonPropertyName("HorseID")] int HorseID,
    [property: JsonPropertyName("NguCoc")] decimal NguCoc,
    [property: JsonPropertyName("Co")] decimal Co,
    [property: JsonPropertyName("Vitamin")] decimal Vitamin,
    [property: JsonPropertyName("Bua")] string Bua,
    [property: JsonPropertyName("ChiTiet")] IReadOnlyList<FeedDetailDto> ChiTiet);

public sealed record CareSupplyDto(
    [property: JsonPropertyName("SupplyID")] int SupplyID,
    [property: JsonPropertyName("TenVatTu")] string TenVatTu,
    [property: JsonPropertyName("Loai")] string? Loai,
    [property: JsonPropertyName("SoLuongTon")] decimal SoLuongTon,
    [property: JsonPropertyName("ManagedBy")] int? ManagedBy,
    [property: JsonPropertyName("DonGia")] decimal? DonGia,
    [property: JsonPropertyName("DonVi")] string? DonVi);

public sealed record IncidentReportDto(
    [property: JsonPropertyName("ReportID")] long ReportID,
    [property: JsonPropertyName("HorseID")] int HorseID,
    [property: JsonPropertyName("GroomID")] int GroomID,
    [property: JsonPropertyName("LoaiSuCo")] string LoaiSuCo,
    [property: JsonPropertyName("MoTa")] string MoTa,
    [property: JsonPropertyName("HinhAnh")] string HinhAnh,
    [property: JsonPropertyName("Ngay")] string Ngay);

public sealed class CreateIncidentReportRequest
{
    public int HorseID { get; set; }
    public string LoaiSuCo { get; set; } = "";
    public string? MoTa { get; set; }
    public string? HinhAnh { get; set; }
    public string Ngay { get; set; } = "";
}
