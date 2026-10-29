namespace SWP.Application.DTOs.Veterinary;

public class MedicalRecordDto
{
    public int RecordID { get; set; }

    public int HorseID { get; set; }

    public int VetID { get; set; }

    public string Ngay { get; set; } = null!;

    public string ChanDoan { get; set; } = null!;

    public string PhacDo { get; set; } = null!;

    public string ViTriChanThuong { get; set; } = null!;
}

public class SaveMedicalRecordRequestDto
{
    public int HorseID { get; set; }

    public string? Ngay { get; set; }

    public string? ChanDoan { get; set; }

    public string? PhacDo { get; set; }

    public string? ViTriChanThuong { get; set; }

    public string? TrangThai { get; set; }
}

public class ExamSaveResultDto
{
    public MedicalRecordDto Record { get; set; } = null!;

    public bool HealthUpdated { get; set; }

    public TrainingLockDto? Lock { get; set; }
}

public class LatestExamDto
{
    public int HorseID { get; set; }

    public string Ngay { get; set; } = null!;
}

public class IsLatestExamDto
{
    public bool IsLatest { get; set; }
}

public class TrainingLockDto
{
    public int LockID { get; set; }

    public int HorseID { get; set; }

    public int VetID { get; set; }

    public string LyDo { get; set; } = null!;

    public string NgayDat { get; set; } = null!;

    public string? NgayGo { get; set; }
}

public class CreateTrainingLockRequestDto
{
    public int HorseID { get; set; }

    public string? LyDo { get; set; }

    public string? NgayDat { get; set; }
}

public class ReleaseTrainingLockRequestDto
{
    public string? TrangThai { get; set; }
}

public class VaccinationDto
{
    public int ScheduleID { get; set; }

    public int HorseID { get; set; }

    public string Loai { get; set; } = null!;

    public string NgayHen { get; set; } = null!;

    public string TrangThai { get; set; } = null!;
}

public class SaveVaccinationRequestDto
{
    public int HorseID { get; set; }

    public string? Loai { get; set; }

    public string? NgayHen { get; set; }
}

public class MarkVaccinatedRequestDto
{
    public string? NextDate { get; set; }
}

public class MarkVaccinatedResultDto
{
    public VaccinationDto Item { get; set; } = null!;

    public VaccinationDto? Next { get; set; }
}

public class VetUserDto
{
    public int UserID { get; set; }

    public string HoTen { get; set; } = null!;
}

public class SessionVitalDto
{
    public int HorseID { get; set; }

    public string Ngay { get; set; } = null!;

    public decimal NhipTim { get; set; }
}

public class RelatedHorseIdsDto
{
    public int[] RecordHorseIds { get; set; } = [];

    public int[] LockHorseIds { get; set; } = [];

    public int[] VaccinationHorseIds { get; set; } = [];
}
