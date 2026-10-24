using System.Text.Json.Serialization;

namespace SWP.Application.DTOs.Medical;

public class MedicalRecordDto
{
    [JsonPropertyName("RecordID")]
    public int RecordID { get; set; }
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("VetID")]
    public int? VetID { get; set; }
    [JsonPropertyName("Ngay")]
    public string Ngay { get; set; } = string.Empty;
    [JsonPropertyName("ChanDoan")]
    public string ChanDoan { get; set; } = string.Empty;
    [JsonPropertyName("PhacDo")]
    public string PhacDo { get; set; } = string.Empty;
    [JsonPropertyName("ViTriChanThuong")]
    public string ViTriChanThuong { get; set; } = string.Empty;
}

public class SaveMedicalRecordRequestDto
{
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("Ngay")]
    public string Ngay { get; set; } = string.Empty;
    [JsonPropertyName("ChanDoan")]
    public string ChanDoan { get; set; } = string.Empty;
    [JsonPropertyName("PhacDo")]
    public string PhacDo { get; set; } = string.Empty;
    public List<string> Injuries { get; set; } = [];
    [JsonPropertyName("TrangThai")]
    public string? TrangThai { get; set; }
}

public class MedicalRecordSaveResultDto
{
    public MedicalRecordDto Record { get; set; } = null!;
    public bool HealthUpdated { get; set; }
    public TrainingLockDto? Lock { get; set; }
}

public class TrainingLockDto
{
    [JsonPropertyName("LockID")]
    public int LockID { get; set; }
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("VetID")]
    public int VetID { get; set; }
    [JsonPropertyName("LyDo")]
    public string LyDo { get; set; } = string.Empty;
    [JsonPropertyName("NgayDat")]
    public string NgayDat { get; set; } = string.Empty;
    [JsonPropertyName("NgayGo")]
    public string? NgayGo { get; set; }
}

public class CreateTrainingLockRequestDto
{
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("LyDo")]
    public string LyDo { get; set; } = string.Empty;
    [JsonPropertyName("NgayDat")]
    public string NgayDat { get; set; } = string.Empty;
}

public class ReleaseTrainingLockRequestDto
{
    public string NewStatus { get; set; } = string.Empty;
    public string? ReleaseReason { get; set; }
    public int? ReexamRecordId { get; set; }
}

public class VaccinationScheduleDto
{
    [JsonPropertyName("ScheduleID")]
    public int ScheduleID { get; set; }
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("Loai")]
    public string Loai { get; set; } = string.Empty;
    [JsonPropertyName("NgayHen")]
    public string NgayHen { get; set; } = string.Empty;
    [JsonPropertyName("TrangThai")]
    public string TrangThai { get; set; } = string.Empty;
    [JsonPropertyName("GhiChu")]
    public string? GhiChu { get; set; }
}

public class SaveVaccinationRequestDto
{
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("Loai")]
    public string Loai { get; set; } = string.Empty;
    [JsonPropertyName("NgayHen")]
    public string NgayHen { get; set; } = string.Empty;
}

public class MarkVaccinatedRequestDto
{
    public string? NextDate { get; set; }
}

public class MarkVaccinatedResultDto
{
    public VaccinationScheduleDto Item { get; set; } = null!;
    public VaccinationScheduleDto? Next { get; set; }
}

public class VetUserDto
{
    [JsonPropertyName("UserID")]
    public int UserID { get; set; }
    [JsonPropertyName("HoTen")]
    public string HoTen { get; set; } = string.Empty;
}

public class LatestExamDateDto
{
    [JsonPropertyName("HorseID")]
    public int HorseID { get; set; }
    [JsonPropertyName("Ngay")]
    public string Ngay { get; set; } = string.Empty;
}

public class UpdateHealthStatusRequestDto
{
    public string TrangThai { get; set; } = string.Empty;
    public string Ngay { get; set; } = string.Empty;
}

public sealed record IncidentReportDto(
    [property: JsonPropertyName("ReportID")] long ReportID,
    [property: JsonPropertyName("HorseID")] int HorseID,
    [property: JsonPropertyName("GroomID")] int GroomID,
    [property: JsonPropertyName("LoaiSuCo")] string LoaiSuCo,
    [property: JsonPropertyName("MoTa")] string MoTa,
    [property: JsonPropertyName("HinhAnh")] string HinhAnh,
    [property: JsonPropertyName("Ngay")] string Ngay);
