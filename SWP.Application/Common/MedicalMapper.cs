using SWP.Application.DTOs.Medical;
using SWP.Domain.Models.Entity;

namespace SWP.Application.Common;

public static class MedicalMapper
{
    public static MedicalRecordDto ToDto(MedicalRecord m)
    {
        return new MedicalRecordDto
        {
            RecordID = m.MedicalRecordId,
            HorseID = m.HorseId,
            VetID = m.CreateBy ?? 2,
            Ngay = m.RecordDate.ToString("yyyy-MM-dd"),
            ChanDoan = m.Diagnosis ?? string.Empty,
            PhacDo = m.Treatment ?? string.Empty,
            ViTriChanThuong = m.ViTriChanThuong ?? "Không có chấn thương"
        };
    }

    public static TrainingLockDto ToDto(TrainingLock l)
    {
        return new TrainingLockDto
        {
            LockID = l.LockId,
            HorseID = l.HorseId,
            VetID = l.VetId,
            LyDo = l.LyDo,
            NgayDat = l.NgayDat.ToString("yyyy-MM-dd"),
            NgayGo = l.NgayGo?.ToString("yyyy-MM-dd")
        };
    }

    public static VaccinationScheduleDto ToDto(VaccinationSchedule v)
    {
        return new VaccinationScheduleDto
        {
            ScheduleID = v.ScheduleId,
            HorseID = v.HorseId,
            Loai = v.Loai,
            NgayHen = v.NgayHen.ToString("yyyy-MM-dd"),
            TrangThai = v.TrangThai,
            GhiChu = v.GhiChu
        };
    }

    public static VetUserDto ToVetDto(User u)
    {
        return new VetUserDto
        {
            UserID = u.UserId,
            HoTen = u.FullName
        };
    }

    public static MedicalRecord ToEntity(SaveMedicalRecordRequestDto request, int vetId)
    {
        return new MedicalRecord
        {
            HorseId = request.HorseID,
            CreateBy = vetId > 0 ? vetId : null,
            RecordDate = DateTime.Parse(request.Ngay),
            Diagnosis = request.ChanDoan.Trim(),
            Treatment = request.PhacDo.Trim(),
            ViTriChanThuong = FormatInjuries(request.Injuries),
            Status = "Completed",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TrainingLock ToEntity(CreateTrainingLockRequestDto request, int vetId)
    {
        return new TrainingLock
        {
            HorseId = request.HorseID,
            VetId = vetId > 0 ? vetId : 2,
            LyDo = request.LyDo.Trim(),
            NgayDat = DateOnly.Parse(request.NgayDat),
            NgayGo = null
        };
    }

    public static VaccinationSchedule ToEntity(SaveVaccinationRequestDto request)
    {
        return new VaccinationSchedule
        {
            HorseId = request.HorseID,
            Loai = NormalizeVaccineType(request.Loai),
            NgayHen = DateOnly.Parse(request.NgayHen),
            TrangThai = "Chờ tiêm"
        };
    }

    public static void ApplyUpdate(MedicalRecord record, SaveMedicalRecordRequestDto request)
    {
        record.RecordDate = DateTime.Parse(request.Ngay);
        record.Diagnosis = request.ChanDoan.Trim();
        record.Treatment = request.PhacDo.Trim();
        record.ViTriChanThuong = FormatInjuries(request.Injuries);
    }

    public static void ApplyUpdate(VaccinationSchedule item, SaveVaccinationRequestDto request)
    {
        item.Loai = NormalizeVaccineType(request.Loai);
        item.NgayHen = DateOnly.Parse(request.NgayHen);
    }

    public static string FormatInjuries(List<string> injuries)
    {
        if (injuries == null || injuries.Count == 0 || injuries.Contains("Không có chấn thương", StringComparer.OrdinalIgnoreCase))
        {
            return "Không có chấn thương";
        }
        return string.Join(", ", injuries.Distinct());
    }

    public static string NormalizeVaccineType(string type)
    {
        var text = type.Trim();
        var known = new[] { "Cúm ngựa", "Uốn ván", "Viêm não Nhật Bản", "Dại", "Tẩy giun", "Herpesvirus (EHV-1/4)", "Viêm phế quản truyền nhiễm" };
        var found = known.FirstOrDefault(k => string.Equals(k, text, StringComparison.OrdinalIgnoreCase));
        if (found is not null) return found;

        if (text.Length > 0)
        {
            return char.ToUpper(text[0]) + text[1..];
        }
        return text;
    }
}
