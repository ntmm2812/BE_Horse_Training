namespace SWP.Application.DTOs.Medical;

public class MedicalRecordDto
{
    public int RecordID { get; set; }
    public int HorseID { get; set; }
    public int? VetID { get; set; }
    public string Ngay { get; set; } = string.Empty;
    public string ChanDoan { get; set; } = string.Empty;
    public string PhacDo { get; set; } = string.Empty;
    public string ViTriChanThuong { get; set; } = string.Empty;
}

public class SaveMedicalRecordRequestDto
{
    public int HorseID { get; set; }
    public string Ngay { get; set; } = string.Empty;
    public string ChanDoan { get; set; } = string.Empty;
    public string PhacDo { get; set; } = string.Empty;
    public List<string> Injuries { get; set; } = [];
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
    public int? InjuryCaseId { get; set; }
    public int LockID { get; set; }
    public int HorseID { get; set; }
    public int VetID { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string NgayDat { get; set; } = string.Empty;
    public string? NgayGo { get; set; }
    public int? ReleasedByUserId { get; set; }
    public string? ReleaseReason { get; set; }
    public int? ReexamRecordId { get; set; }
}

public class CreateTrainingLockRequestDto
{
    public int HorseID { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string NgayDat { get; set; } = string.Empty;
}

public class ReleaseTrainingLockRequestDto
{
    public string NewStatus { get; set; } = string.Empty;
    public string ReleaseReason { get; set; } = string.Empty;
    public int ReexamRecordId { get; set; }
}

public class VaccinationScheduleDto
{
    public int ScheduleID { get; set; }
    public int HorseID { get; set; }
    public string Loai { get; set; } = string.Empty;
    public string NgayHen { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class SaveVaccinationRequestDto
{
    public int HorseID { get; set; }
    public string Loai { get; set; } = string.Empty;
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
    public int UserID { get; set; }
    public string HoTen { get; set; } = string.Empty;
}

public class LatestExamDateDto
{
    public int HorseID { get; set; }
    public string Ngay { get; set; } = string.Empty;
}
