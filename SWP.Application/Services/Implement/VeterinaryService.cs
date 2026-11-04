using System.Globalization;
using SWP.Application.Common;
using SWP.Application.DTOs.Veterinary;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class VeterinaryService : IVeterinaryService
{
    private const int DiagnosisMin = 5;
    private const int DiagnosisMax = 200;
    private const int TreatmentMin = 5;
    private const int TreatmentMax = 500;
    private const int ReasonMin = 5;
    private const int ReasonMax = 200;

    private static readonly string[] ReleaseStatuses =
    [
        HorseHealthStatuses.CanTheoDoi,
        HorseHealthStatuses.DuDieuKien,
    ];

    private readonly IVeterinaryRepository _repository;
    private readonly IUserRepository _users;
    private readonly TimeProvider _time;

    public VeterinaryService(IVeterinaryRepository repository, IUserRepository users, TimeProvider time)
    {
        _repository = repository;
        _users = users;
        _time = time;
    }

    public async Task<IReadOnlyList<MedicalRecordDto>> GetRecordsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.ListRecordsAsync(horseId, ownerUserId, cancellationToken);
        return records.Select(ToRecordDto).ToList();
    }

    public async Task<ServiceResult<MedicalRecordDto>> GetRecordAsync(
        int recordId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.ListRecordsAsync(null, ownerUserId, cancellationToken);
        var record = records.FirstOrDefault(r => r.MedicalRecordId == recordId);
        return record is null
            ? ServiceResult<MedicalRecordDto>.NotFound("Không tìm thấy bệnh án")
            : ServiceResult<MedicalRecordDto>.Ok(ToRecordDto(record));
    }

    public async Task<ServiceResult<IsLatestExamDto>> IsLatestRecordAsync(
        int recordId,
        CancellationToken cancellationToken = default)
    {
        var records = await _repository.ListRecordsAsync(null, null, cancellationToken);
        var record = records.FirstOrDefault(r => r.MedicalRecordId == recordId);
        if (record is null)
        {
            return ServiceResult<IsLatestExamDto>.NotFound("Không tìm thấy bệnh án");
        }

        var latest = await _repository.GetLatestRecordAsync(record.HorseId, cancellationToken);
        return ServiceResult<IsLatestExamDto>.Ok(new IsLatestExamDto
        {
            IsLatest = latest?.MedicalRecordId == record.MedicalRecordId,
        });
    }

    public async Task<ServiceResult<LatestExamDto>> GetLatestExamAsync(
        int horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.HorseExistsAsync(horseId, ownerUserId, cancellationToken))
        {
            return ServiceResult<LatestExamDto>.NotFound("Không tìm thấy ngựa");
        }

        var latest = await _repository.GetLatestRecordAsync(horseId, cancellationToken);
        if (latest is null)
        {
            return ServiceResult<LatestExamDto>.NotFound("Chưa có lần khám");
        }

        return ServiceResult<LatestExamDto>.Ok(new LatestExamDto
        {
            HorseID = horseId,
            Ngay = DateText(latest.RecordDate),
        });
    }

    public async Task<ServiceResult<ExamSaveResultDto>> CreateRecordAsync(
        SaveMedicalRecordRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.HorseExistsAsync(request.HorseID, null, cancellationToken))
        {
            return ServiceResult<ExamSaveResultDto>.NotFound("Không tìm thấy ngựa");
        }

        var parsed = ParseRecord(request);
        if (!parsed.Success)
        {
            return parsed.AsFailure<ExamSaveResultDto>();
        }

        var (date, diagnosis, treatment, injury) = parsed.Data;
        var now = UtcNow();
        var record = new MedicalRecord
        {
            HorseId = request.HorseID,
            CreateBy = vetId,
            RecordType = "Exam",
            RecordDate = ToDateTime(date),
            Diagnosis = diagnosis,
            Treatment = treatment,
            Symptoms = injury,
            CreatedAt = now,
        };
        _repository.AddRecord(record);

        var healthUpdated = await ApplyHealthAsync(request.HorseID, request.TrangThai, date, cancellationToken);
        if (healthUpdated.Error is not null)
        {
            return ServiceResult<ExamSaveResultDto>.Invalid(healthUpdated.Error);
        }

        TrainingLock? createdLock = null;
        if (ShouldAutoLock(injury, request.TrangThai) && !await _repository.HasActiveLockAsync(request.HorseID, cancellationToken))
        {
            createdLock = await PlaceLockAsync(request.HorseID, vetId, LockReasonFromExam(injury, diagnosis), date, now, cancellationToken);
        }

        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<ExamSaveResultDto>.Ok(new ExamSaveResultDto
        {
            Record = ToRecordDto(record),
            HealthUpdated = healthUpdated.Updated,
            Lock = createdLock is null ? null : ToLockDto(createdLock),
        });
    }

    public async Task<ServiceResult<ExamSaveResultDto>> UpdateRecordAsync(
        int recordId,
        SaveMedicalRecordRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetRecordTrackedAsync(recordId, cancellationToken);
        if (current is null)
        {
            return ServiceResult<ExamSaveResultDto>.NotFound("Không tìm thấy bệnh án");
        }

        if (current.CreateBy != vetId)
        {
            return ServiceResult<ExamSaveResultDto>.Fail(ServiceErrorCodes.Conflict, "Bạn chỉ được sửa bệnh án do chính mình ghi");
        }

        var parsed = ParseRecord(request);
        if (!parsed.Success)
        {
            return parsed.AsFailure<ExamSaveResultDto>();
        }

        var (date, diagnosis, treatment, injury) = parsed.Data;
        current.RecordDate = ToDateTime(date);
        current.Diagnosis = diagnosis;
        current.Treatment = treatment;
        current.Symptoms = injury;

        var latest = await _repository.GetLatestRecordAsync(current.HorseId, cancellationToken);
        var isLatest = latest is null || latest.MedicalRecordId == current.MedicalRecordId;
        var healthUpdated = false;
        TrainingLock? createdLock = null;
        if (isLatest)
        {
            var health = await ApplyHealthAsync(current.HorseId, request.TrangThai, date, cancellationToken);
            if (health.Error is not null)
            {
                return ServiceResult<ExamSaveResultDto>.Invalid(health.Error);
            }

            healthUpdated = health.Updated;
            if (ShouldAutoLock(injury, request.TrangThai) && !await _repository.HasActiveLockAsync(current.HorseId, cancellationToken))
            {
                createdLock = await PlaceLockAsync(
                    current.HorseId,
                    vetId,
                    LockReasonFromExam(injury, diagnosis),
                    date,
                    UtcNow(),
                    cancellationToken);
            }
        }

        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<ExamSaveResultDto>.Ok(new ExamSaveResultDto
        {
            Record = ToRecordDto(current),
            HealthUpdated = healthUpdated,
            Lock = createdLock is null ? null : ToLockDto(createdLock),
        });
    }

    public Task<IReadOnlyList<TrainingLockDto>> GetLocksAsync(
        int? ownerUserId,
        bool activeOnly,
        CancellationToken cancellationToken = default) =>
        MapLocks(_repository.ListLocksAsync(ownerUserId, activeOnly, cancellationToken));

    public async Task<ServiceResult<TrainingLockDto>> CreateLockAsync(
        CreateTrainingLockRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.HorseExistsAsync(request.HorseID, null, cancellationToken))
        {
            return ServiceResult<TrainingLockDto>.NotFound("Không tìm thấy ngựa");
        }

        if (await _repository.HasActiveLockAsync(request.HorseID, cancellationToken))
        {
            return ServiceResult<TrainingLockDto>.Fail(ServiceErrorCodes.Conflict, "Ngựa này đang có lệnh khóa huấn luyện hiệu lực");
        }

        if (!TryParseDate(request.NgayDat, out var placedOn) || placedOn > Today())
        {
            return ServiceResult<TrainingLockDto>.Invalid("Ngày đặt không được ở tương lai");
        }

        var reason = Collapse(request.LyDo);
        if (reason.Length < ReasonMin || reason.Length > ReasonMax)
        {
            return ServiceResult<TrainingLockDto>.Invalid("Lý do không hợp lệ");
        }

        var now = UtcNow();
        var created = await PlaceLockAsync(request.HorseID, vetId, reason, placedOn, now, cancellationToken);
        var health = await ApplyHealthAsync(request.HorseID, HorseHealthStatuses.ChanThuong, Today(), cancellationToken);
        if (health.Error is not null)
        {
            return ServiceResult<TrainingLockDto>.Invalid(health.Error);
        }

        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<TrainingLockDto>.Ok(ToLockDto(created));
    }

    public async Task<ServiceResult<TrainingLockDto>> ReleaseLockAsync(
        int lockId,
        string? newStatus,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var trainingLock = await _repository.GetLockTrackedAsync(lockId, cancellationToken);
        if (trainingLock is null || !await _repository.HorseExistsAsync(trainingLock.HorseId, ownerUserId, cancellationToken))
        {
            return ServiceResult<TrainingLockDto>.NotFound("Không tìm thấy lệnh khóa");
        }

        if (trainingLock.ReleasedOn is not null)
        {
            return ServiceResult<TrainingLockDto>.Fail(ServiceErrorCodes.InvalidStatus, "Lệnh khóa này đã được gỡ");
        }

        var status = newStatus?.Trim() ?? string.Empty;
        if (!ReleaseStatuses.Contains(status, StringComparer.Ordinal))
        {
            return ServiceResult<TrainingLockDto>.Invalid("Vui lòng chọn tình trạng sức khỏe mới");
        }

        var today = Today();
        trainingLock.ReleasedOn = today;
        var horse = await _repository.GetHorseTrackedAsync(trainingLock.HorseId, cancellationToken);
        if (horse is not null)
        {
            horse.LockedAt = null;
            horse.LockReason = null;
            horse.LockedByUserId = null;
        }

        var health = await ApplyHealthAsync(trainingLock.HorseId, status, today, cancellationToken);
        if (health.Error is not null)
        {
            return ServiceResult<TrainingLockDto>.Invalid(health.Error);
        }

        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<TrainingLockDto>.Ok(ToLockDto(trainingLock));
    }

    public async Task<IReadOnlyList<VaccinationDto>> GetVaccinationsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListVaccinationsAsync(horseId, ownerUserId, cancellationToken);
        return items.Select(ToVaccineDto).ToList();
    }

    public async Task<ServiceResult<VaccinationDto>> GetVaccinationAsync(
        int scheduleId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListVaccinationsAsync(null, ownerUserId, cancellationToken);
        var item = items.FirstOrDefault(v => v.ScheduleId == scheduleId);
        return item is null
            ? ServiceResult<VaccinationDto>.NotFound("Không tìm thấy lịch tiêm")
            : ServiceResult<VaccinationDto>.Ok(ToVaccineDto(item));
    }

    public async Task<ServiceResult<VaccinationDto>> CreateVaccinationAsync(
        SaveVaccinationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.HorseExistsAsync(request.HorseID, null, cancellationToken))
        {
            return ServiceResult<VaccinationDto>.NotFound("Không tìm thấy ngựa");
        }

        var parsed = await ParsePendingVaccineAsync(request.HorseID, request.Loai, request.NgayHen, exceptId: null, allowUnchangedPast: false, previousDate: null, cancellationToken);
        if (!parsed.Success)
        {
            return parsed.AsFailure<VaccinationDto>();
        }

        var item = new VaccinationSchedule
        {
            HorseId = request.HorseID,
            VaccineType = parsed.Data.Type,
            DueDate = parsed.Data.Date,
            Status = VaccineStatuses.Pending,
            CreatedAt = UtcNow(),
        };
        _repository.AddVaccination(item);
        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<VaccinationDto>.Ok(ToVaccineDto(item));
    }

    public async Task<ServiceResult<VaccinationDto>> UpdateVaccinationAsync(
        int scheduleId,
        SaveVaccinationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var item = await FindPendingAsync(scheduleId, cancellationToken);
        if (!item.Success)
        {
            return item.AsFailure<VaccinationDto>();
        }

        var parsed = await ParsePendingVaccineAsync(
            item.Data!.HorseId,
            request.Loai,
            request.NgayHen,
            item.Data.ScheduleId,
            allowUnchangedPast: true,
            item.Data.DueDate,
            cancellationToken);
        if (!parsed.Success)
        {
            return parsed.AsFailure<VaccinationDto>();
        }

        item.Data.VaccineType = parsed.Data.Type;
        item.Data.DueDate = parsed.Data.Date;
        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<VaccinationDto>.Ok(ToVaccineDto(item.Data));
    }

    public async Task<ServiceResult<bool>> DeleteVaccinationAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var item = await FindPendingAsync(scheduleId, cancellationToken);
        if (!item.Success)
        {
            return item.AsFailure<bool>();
        }

        _repository.RemoveVaccination(item.Data!);
        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<MarkVaccinatedResultDto>> MarkVaccinatedAsync(
        int scheduleId,
        string? nextDate,
        CancellationToken cancellationToken = default)
    {
        var item = await FindPendingAsync(scheduleId, cancellationToken);
        if (!item.Success)
        {
            return item.AsFailure<MarkVaccinatedResultDto>();
        }

        DateOnly? followUp = null;
        if (!string.IsNullOrWhiteSpace(nextDate))
        {
            if (!TryParseDate(nextDate, out var parsed) || parsed <= Today())
            {
                return ServiceResult<MarkVaccinatedResultDto>.Invalid("Ngày hẹn lần tiếp theo phải sau hôm nay");
            }

            followUp = parsed;
        }

        item.Data!.Status = VaccineStatuses.Done;
        VaccinationSchedule? next = null;
        if (followUp is DateOnly due)
        {
            if (await _repository.HasPendingVaccineAsync(item.Data.HorseId, item.Data.VaccineType, item.Data.ScheduleId, cancellationToken))
            {
                return ServiceResult<MarkVaccinatedResultDto>.Fail(ServiceErrorCodes.Conflict, "Ngựa này đã có lịch chờ tiêm cùng loại.");
            }

            next = new VaccinationSchedule
            {
                HorseId = item.Data.HorseId,
                VaccineType = item.Data.VaccineType,
                DueDate = due,
                Status = VaccineStatuses.Pending,
                CreatedAt = UtcNow(),
            };
            _repository.AddVaccination(next);
        }

        await _repository.SaveAsync(cancellationToken);
        return ServiceResult<MarkVaccinatedResultDto>.Ok(new MarkVaccinatedResultDto
        {
            Item = ToVaccineDto(item.Data),
            Next = next is null ? null : ToVaccineDto(next),
        });
    }

    public async Task<IReadOnlyList<SessionVitalDto>> GetRecentSessionVitalsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _repository.ListRecentHeartRatesAsync(Today().AddDays(-14), cancellationToken);
        return rows.Select(row => new SessionVitalDto
        {
            HorseID = row.HorseId,
            Ngay = row.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            NhipTim = row.HeartRate,
        }).ToList();
    }

    public async Task<IReadOnlyList<VetUserDto>> GetVetsAsync(CancellationToken cancellationToken = default)
    {
        var vets = await _users.GetActiveUsersByCanonicalRoleAsync(RoleNames.Veterinarian, cancellationToken);
        return vets.Select(v => new VetUserDto { UserID = v.UserId, HoTen = v.FullName }).ToList();
    }

    public async Task<RelatedHorseIdsDto> GetRelatedHorseIdsAsync(CancellationToken cancellationToken = default)
    {
        var records = await _repository.HorseIdsWithRecordsAsync(cancellationToken);
        var locks = await _repository.HorseIdsWithLocksAsync(cancellationToken);
        var vaccines = await _repository.HorseIdsWithVaccinationsAsync(cancellationToken);
        return new RelatedHorseIdsDto
        {
            RecordHorseIds = records.ToArray(),
            LockHorseIds = locks.ToArray(),
            VaccinationHorseIds = vaccines.ToArray(),
        };
    }

    private async Task<ServiceResult<VaccinationSchedule>> FindPendingAsync(int scheduleId, CancellationToken cancellationToken)
    {
        var item = await _repository.GetVaccinationTrackedAsync(scheduleId, cancellationToken);
        if (item is null)
        {
            return ServiceResult<VaccinationSchedule>.NotFound("Không tìm thấy lịch tiêm");
        }

        if (item.Status != VaccineStatuses.Pending)
        {
            return ServiceResult<VaccinationSchedule>.Fail(ServiceErrorCodes.InvalidStatus, "Lịch đã tiêm không thể sửa hoặc xóa");
        }

        return ServiceResult<VaccinationSchedule>.Ok(item);
    }

    private async Task<ServiceResult<(string Type, DateOnly Date)>> ParsePendingVaccineAsync(
        int horseId,
        string? rawType,
        string? rawDate,
        int? exceptId,
        bool allowUnchangedPast,
        DateOnly? previousDate,
        CancellationToken cancellationToken)
    {
        var type = VaccineCatalog.Normalize(rawType);
        if (type is null)
        {
            return ServiceResult<(string, DateOnly)>.Invalid("Loại không hợp lệ");
        }

        if (!TryParseDate(rawDate, out var date))
        {
            return ServiceResult<(string, DateOnly)>.Invalid("Ngày hẹn không được ở quá khứ");
        }

        var unchanged = allowUnchangedPast && previousDate == date;
        if (!unchanged && date < Today())
        {
            return ServiceResult<(string, DateOnly)>.Invalid("Ngày hẹn không được ở quá khứ");
        }

        if (await _repository.HasPendingVaccineAsync(horseId, type, exceptId, cancellationToken))
        {
            return ServiceResult<(string, DateOnly)>.Fail(ServiceErrorCodes.Conflict, "Ngựa này đã có lịch chờ tiêm cùng loại.");
        }

        return ServiceResult<(string, DateOnly)>.Ok((type, date));
    }

    private ServiceResult<(DateOnly Date, string Diagnosis, string Treatment, string Injury)> ParseRecord(SaveMedicalRecordRequestDto request)
    {
        if (!TryParseDate(request.Ngay, out var date) || date > Today())
        {
            return ServiceResult<(DateOnly, string, string, string)>.Invalid("Ngày khám không được ở tương lai");
        }

        var diagnosis = Collapse(request.ChanDoan);
        if (diagnosis.Length < DiagnosisMin || diagnosis.Length > DiagnosisMax)
        {
            return ServiceResult<(DateOnly, string, string, string)>.Invalid("Chẩn đoán không hợp lệ");
        }

        var treatment = request.PhacDo?.Trim() ?? string.Empty;
        if (treatment.Length < TreatmentMin || treatment.Length > TreatmentMax)
        {
            return ServiceResult<(DateOnly, string, string, string)>.Invalid("Phác đồ điều trị không hợp lệ");
        }

        if (!InjuryCatalog.TryNormalize(request.ViTriChanThuong, out var injury))
        {
            return ServiceResult<(DateOnly, string, string, string)>.Invalid("Vị trí chấn thương không hợp lệ");
        }

        return ServiceResult<(DateOnly, string, string, string)>.Ok((date, diagnosis, treatment, injury));
    }

    private async Task<(bool Updated, string? Error)> ApplyHealthAsync(
        int horseId,
        string? status,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return (false, null);
        }

        var normalized = status.Trim();
        if (!HorseHealthStatuses.IsValid(normalized))
        {
            return (false, "Tình trạng sức khỏe không hợp lệ");
        }

        var current = await _repository.GetHealthTrackedAsync(horseId, cancellationToken);
        if (current is not null && DateOnly.FromDateTime(current.NgayCapNhat) > asOf)
        {
            return (false, null);
        }

        var stamp = ToDateTime(asOf);
        if (current is null)
        {
            _repository.AddHealth(new HorseHealthStatus
            {
                HorseId = horseId,
                TrangThai = normalized,
                NgayCapNhat = stamp,
            });
        }
        else
        {
            current.TrangThai = normalized;
            current.NgayCapNhat = stamp;
        }

        return (true, null);
    }

    private async Task<TrainingLock> PlaceLockAsync(
        int horseId,
        int vetId,
        string reason,
        DateOnly placedOn,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var trainingLock = new TrainingLock
        {
            HorseId = horseId,
            VetId = vetId,
            Reason = reason,
            PlacedOn = placedOn,
            CreatedAt = createdAt,
        };
        _repository.AddLock(trainingLock);

        var horse = await _repository.GetHorseTrackedAsync(horseId, cancellationToken);
        if (horse is not null)
        {
            horse.LockReason = reason;
            horse.LockedByUserId = vetId;
            horse.LockedAt = ToDateTime(placedOn);
        }

        return trainingLock;
    }

    private static bool ShouldAutoLock(string injury, string? status) =>
        InjuryCatalog.HasInjury(injury)
        && string.Equals(status?.Trim(), HorseHealthStatuses.ChanThuong, StringComparison.Ordinal);

    private static string LockReasonFromExam(string injury, string diagnosis)
    {
        var reason = $"Chấn thương: {injury} - {diagnosis}";
        return reason.Length > ReasonMax ? $"{reason[..(ReasonMax - 1)]}…" : reason;
    }

    private static async Task<IReadOnlyList<TrainingLockDto>> MapLocks(Task<IReadOnlyList<TrainingLock>> query)
    {
        var locks = await query;
        return locks.Select(ToLockDto).ToList();
    }

    private static MedicalRecordDto ToRecordDto(MedicalRecord record) => new()
    {
        RecordID = record.MedicalRecordId,
        HorseID = record.HorseId,
        VetID = record.CreateBy ?? 0,
        Ngay = DateText(record.RecordDate),
        ChanDoan = record.Diagnosis ?? string.Empty,
        PhacDo = record.Treatment ?? string.Empty,
        ViTriChanThuong = record.Symptoms ?? string.Empty,
    };

    private static TrainingLockDto ToLockDto(TrainingLock trainingLock) => new()
    {
        LockID = trainingLock.LockId,
        HorseID = trainingLock.HorseId,
        VetID = trainingLock.VetId,
        LyDo = trainingLock.Reason,
        NgayDat = trainingLock.PlacedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        NgayGo = trainingLock.ReleasedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };

    private static VaccinationDto ToVaccineDto(VaccinationSchedule item) => new()
    {
        ScheduleID = item.ScheduleId,
        HorseID = item.HorseId,
        Loai = item.VaccineType,
        NgayHen = item.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TrangThai = item.Status,
    };

    private DateOnly Today() => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    private DateTime UtcNow() => _time.GetUtcNow().UtcDateTime;

    private static DateTime ToDateTime(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);

    private static string DateText(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Collapse(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool TryParseDate(string? raw, out DateOnly date) =>
        DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
