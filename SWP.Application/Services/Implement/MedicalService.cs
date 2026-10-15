using SWP.Application.Common;
using SWP.Application.DTOs.Medical;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class MedicalService : IMedicalService
{
    private readonly IMedicalRepository _medicalRepository;
    private readonly IHorseRepository _horseRepository;
    private readonly IHorseHealthStatusRepository _healthRepository;

    public MedicalService(
        IMedicalRepository medicalRepository,
        IHorseRepository horseRepository,
        IHorseHealthStatusRepository healthRepository)
    {
        _medicalRepository = medicalRepository;
        _horseRepository = horseRepository;
        _healthRepository = healthRepository;
    }

    public async Task<List<MedicalRecordDto>> GetMedicalRecordsAsync(int? horseId, CancellationToken cancellationToken = default)
    {
        var list = await _medicalRepository.GetRecordsAsync(horseId, cancellationToken);
        return list.Select(MedicalMapper.ToDto).ToList();
    }

    public async Task<MedicalRecordDto?> GetMedicalRecordByIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        var record = await _medicalRepository.GetRecordByIdAsync(recordId, cancellationToken);
        return record is null ? null : MedicalMapper.ToDto(record);
    }

    public async Task<LatestExamDateDto?> GetLatestExamDateAsync(int horseId, CancellationToken cancellationToken = default)
    {
        var latest = await _medicalRepository.GetLatestRecordDateAsync(horseId, cancellationToken);
        if (!latest.HasValue) return null;

        return new LatestExamDateDto
        {
            HorseID = horseId,
            Ngay = latest.Value.ToString("yyyy-MM-dd")
        };
    }

    public async Task<MedicalRecordSaveResultDto> CreateMedicalRecordAsync(
        SaveMedicalRecordRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        ValidateExamForm(request);
        ValidateVetId(vetId);
        await using var transaction = await _medicalRepository.BeginTransactionAsync(cancellationToken);

        var horse = await _horseRepository.GetByIdAsync(request.HorseID, cancellationToken);
        if (horse is null) throw new ArgumentException("Không tìm thấy ngựa.");

        var record = MedicalMapper.ToEntity(request, vetId);
        await _medicalRepository.AddRecordAsync(record, cancellationToken);

        var healthUpdated = false;
        if (!string.IsNullOrWhiteSpace(request.TrangThai))
        {
            healthUpdated = await UpdateHorseHealthInternalAsync(request.HorseID, request.TrangThai.Trim(), record.RecordDate, cancellationToken);
        }

        TrainingLockDto? createdLock = null;
        var isLocked = await _medicalRepository.IsHorseLockedAsync(request.HorseID, cancellationToken);
        if (healthUpdated && ShouldAutoLock(request.Injuries, request.TrangThai, isLocked))
        {
            var reason = $"Chấn thương: {record.ViTriChanThuong} - {request.ChanDoan.Trim()}";
            if (reason.Length > 200) reason = reason[..199] + "…";

            var lockEntity = MedicalMapper.ToEntity(new CreateTrainingLockRequestDto
            {
                HorseID = request.HorseID,
                LyDo = reason,
                NgayDat = request.Ngay
            }, vetId);

            await _medicalRepository.AddLockAsync(lockEntity, cancellationToken);
            createdLock = MedicalMapper.ToDto(lockEntity);
        }

        await transaction.CommitAsync(cancellationToken);
        return new MedicalRecordSaveResultDto
        {
            Record = MedicalMapper.ToDto(record),
            HealthUpdated = healthUpdated,
            Lock = createdLock
        };
    }

    public async Task<MedicalRecordSaveResultDto> UpdateMedicalRecordAsync(
        int recordId,
        SaveMedicalRecordRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        ValidateExamForm(request);
        ValidateVetId(vetId);
        await using var transaction = await _medicalRepository.BeginTransactionAsync(cancellationToken);

        var record = await _medicalRepository.GetRecordByIdForUpdateAsync(recordId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy bệnh án");

        if (record.CreateBy != vetId)
        {
            throw new UnauthorizedAccessException("Bạn chỉ được sửa bệnh án do chính mình ghi");
        }

        if (record.HorseId != request.HorseID)
            throw new ArgumentException("Không được chuyển bệnh án sang ngựa khác.");

        MedicalMapper.ApplyUpdate(record, request);

        var isLatest = await _medicalRepository.IsLatestRecordAsync(record.HorseId, record.RecordDate, record.MedicalRecordId, cancellationToken);

        var healthUpdated = false;
        TrainingLockDto? createdLock = null;

        if (isLatest && !string.IsNullOrWhiteSpace(request.TrangThai))
        {
            healthUpdated = await UpdateHorseHealthInternalAsync(record.HorseId, request.TrangThai.Trim(), record.RecordDate, cancellationToken);
            var isLocked = await _medicalRepository.IsHorseLockedAsync(record.HorseId, cancellationToken);
            if (healthUpdated && ShouldAutoLock(request.Injuries, request.TrangThai, isLocked))
            {
                var reason = $"Chấn thương: {record.ViTriChanThuong} - {request.ChanDoan.Trim()}";
                if (reason.Length > 200) reason = reason[..199] + "…";

                var lockEntity = MedicalMapper.ToEntity(new CreateTrainingLockRequestDto
                {
                    HorseID = record.HorseId,
                    LyDo = reason,
                    NgayDat = request.Ngay
                }, vetId);

                await _medicalRepository.AddLockAsync(lockEntity, cancellationToken);
                createdLock = MedicalMapper.ToDto(lockEntity);
            }
        }

        await _medicalRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new MedicalRecordSaveResultDto
        {
            Record = MedicalMapper.ToDto(record),
            HealthUpdated = healthUpdated,
            Lock = createdLock
        };
    }

    public async Task<List<TrainingLockDto>> GetTrainingLocksAsync(CancellationToken cancellationToken = default)
    {
        var list = await _medicalRepository.GetLocksAsync(cancellationToken);
        return list.Select(MedicalMapper.ToDto).ToList();
    }

    public async Task<List<TrainingLockDto>> GetActiveTrainingLocksAsync(CancellationToken cancellationToken = default)
    {
        var list = await _medicalRepository.GetActiveLocksAsync(cancellationToken);
        return list.Select(MedicalMapper.ToDto).ToList();
    }

    public async Task<TrainingLockDto> CreateTrainingLockAsync(
        CreateTrainingLockRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        ValidateVetId(vetId);
        await using var transaction = await _medicalRepository.BeginTransactionAsync(cancellationToken);
        if (await _horseRepository.GetByIdAsync(request.HorseID, cancellationToken) is null)
            throw new ArgumentException("Không tìm thấy ngựa.");

        if (await _medicalRepository.IsHorseLockedAsync(request.HorseID, cancellationToken))
        {
            throw new InvalidOperationException("Ngựa này đang có lệnh khóa huấn luyện hiệu lực");
        }

        if (!DateOnly.TryParse(request.NgayDat, out var ngayDat) || ngayDat > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Ngày đặt không được ở tương lai");
        }

        var reason = request.LyDo?.Trim() ?? "";
        if (reason.Length < 5 || reason.Length > 200)
        {
            throw new ArgumentException("Lý do không hợp lệ");
        }

        var lockEntity = MedicalMapper.ToEntity(request, vetId);
        await _medicalRepository.AddLockAsync(lockEntity, cancellationToken);

        // Khóa tập không được làm mất trạng thái cách ly đang có.
        var health = await _healthRepository.GetByHorseIdAsync(request.HorseID, cancellationToken);
        if (health?.TrangThai != HorseHealthStatuses.CachLy)
            await UpdateHorseHealthInternalAsync(request.HorseID, HorseHealthStatuses.ChanThuong, DateTime.UtcNow, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return MedicalMapper.ToDto(lockEntity);
    }

    public async Task<TrainingLockDto> ReleaseTrainingLockAsync(
        int lockId,
        ReleaseTrainingLockRequestDto request,
        int vetId,
        CancellationToken cancellationToken = default)
    {
        ValidateVetId(vetId);
        var newStatus = NormalizeHealthStatus(request.NewStatus);
        var reason = request.ReleaseReason?.Trim() ?? "";
        if (reason.Length < 5 || reason.Length > 500)
            throw new ArgumentException("Lý do mở khóa phải có từ 5 đến 500 ký tự.");
        await using var transaction = await _medicalRepository.BeginTransactionAsync(cancellationToken);
        var lockEntity = await _medicalRepository.GetLockByIdForUpdateAsync(lockId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy lệnh khóa");

        if (lockEntity.NgayGo.HasValue)
        {
            throw new InvalidOperationException("Lệnh khóa này đã được gỡ");
        }

        var validStatuses = new[] { HorseHealthStatuses.CanTheoDoi, HorseHealthStatuses.DuDieuKien };
        if (string.IsNullOrWhiteSpace(newStatus) || !validStatuses.Contains(newStatus.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Vui lòng chọn tình trạng sức khỏe mới hợp lệ");
        }

        var reexam = await _medicalRepository.GetRecordByIdAsync(request.ReexamRecordId, cancellationToken);
        if (reexam is null || reexam.HorseId != lockEntity.HorseId
            || DateOnly.FromDateTime(reexam.RecordDate) < lockEntity.NgayDat
            || reexam.RecordDate.Date > DateTime.Today
            || reexam.Status != "Completed"
            || !await _medicalRepository.IsLatestRecordAsync(reexam.HorseId, reexam.RecordDate, reexam.MedicalRecordId, cancellationToken))
            throw new ArgumentException("Cần bệnh án tái khám hoàn tất mới nhất của đúng ngựa, từ ngày khóa đến hôm nay.");

        if (lockEntity.InjuryCaseId is int caseId &&
            !await _medicalRepository.IsExamLinkedToCaseAsync(caseId, reexam.MedicalRecordId, cancellationToken))
            throw new ArgumentException("Bệnh án tái khám phải được gắn vào đúng hồ sơ chấn thương trước khi mở khóa.");

        lockEntity.NgayGo = DateOnly.FromDateTime(DateTime.Today);
        lockEntity.ReleasedByUserId = vetId;
        lockEntity.ReleaseReason = reason;
        lockEntity.ReexamRecordId = reexam.MedicalRecordId;
        await _medicalRepository.TrackCaseLockReleaseAsync(lockEntity, vetId, cancellationToken);

        // Lưu trong transaction trước khi truy vấn lại các khóa còn hiệu lực.
        await _medicalRepository.SaveChangesAsync(cancellationToken);
        var horse = await _horseRepository.GetByIdAsync(lockEntity.HorseId, cancellationToken);
        var health = await _healthRepository.GetByHorseIdAsync(lockEntity.HorseId, cancellationToken);
        if (!await _medicalRepository.IsHorseLockedAsync(lockEntity.HorseId, cancellationToken)
            && horse?.LockedAt is null && health?.TrangThai != HorseHealthStatuses.CachLy)
            await UpdateHorseHealthInternalAsync(lockEntity.HorseId, newStatus, DateTime.UtcNow, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return MedicalMapper.ToDto(lockEntity);
    }

    public async Task<List<VaccinationScheduleDto>> GetVaccinationsAsync(int? horseId, CancellationToken cancellationToken = default)
    {
        var list = await _medicalRepository.GetVaccinationsAsync(horseId, cancellationToken);
        return list.Select(MedicalMapper.ToDto).ToList();
    }

    public async Task<VaccinationScheduleDto?> GetVaccinationByIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var item = await _medicalRepository.GetVaccinationByIdAsync(scheduleId, cancellationToken);
        return item is null ? null : MedicalMapper.ToDto(item);
    }

    public async Task<VaccinationScheduleDto> CreateVaccinationAsync(
        SaveVaccinationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParse(request.NgayHen, out var ngayHen) || ngayHen < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Ngày hẹn không được ở quá khứ");
        }

        var loai = MedicalMapper.NormalizeVaccineType(request.Loai);
        if (loai.Length < 2 || loai.Length > 50)
        {
            throw new ArgumentException("Loại không hợp lệ");
        }

        var duplicate = await _medicalRepository.HasDuplicatePendingVaccineAsync(request.HorseID, loai, null, cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Ngựa này đã có lịch tiêm đang chờ cho loại vắc-xin/thuốc này");
        }

        var item = MedicalMapper.ToEntity(request);
        await _medicalRepository.AddVaccinationAsync(item, cancellationToken);

        return MedicalMapper.ToDto(item);
    }

    public async Task<VaccinationScheduleDto> UpdateVaccinationAsync(
        int scheduleId,
        SaveVaccinationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var item = await _medicalRepository.GetVaccinationByIdForUpdateAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch tiêm");

        if (item.TrangThai != "Chờ tiêm")
        {
            throw new InvalidOperationException("Lịch đã tiêm không thể sửa hoặc xóa");
        }

        if (!DateOnly.TryParse(request.NgayHen, out var ngayHen))
        {
            throw new ArgumentException("Ngày hẹn không hợp lệ");
        }

        if (ngayHen != item.NgayHen && ngayHen < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Ngày hẹn không được ở quá khứ");
        }

        var loai = MedicalMapper.NormalizeVaccineType(request.Loai);
        if (loai.Length < 2 || loai.Length > 50)
        {
            throw new ArgumentException("Loại không hợp lệ");
        }

        var duplicate = await _medicalRepository.HasDuplicatePendingVaccineAsync(item.HorseId, loai, scheduleId, cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Ngựa này đã có lịch tiêm đang chờ cho loại vắc-xin/thuốc này");
        }

        MedicalMapper.ApplyUpdate(item, request);
        await _medicalRepository.SaveChangesAsync(cancellationToken);

        return MedicalMapper.ToDto(item);
    }

    public async Task<bool> DeleteVaccinationAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var item = await _medicalRepository.GetVaccinationByIdForUpdateAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch tiêm");

        if (item.TrangThai != "Chờ tiêm")
        {
            throw new InvalidOperationException("Lịch đã tiêm không thể sửa hoặc xóa");
        }

        await _medicalRepository.DeleteVaccinationAsync(item, cancellationToken);
        return true;
    }

    public async Task<MarkVaccinatedResultDto> MarkVaccinatedAsync(
        int scheduleId,
        string? nextDate,
        CancellationToken cancellationToken = default)
    {
        var item = await _medicalRepository.GetVaccinationByIdForUpdateAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch tiêm");

        if (item.TrangThai != "Chờ tiêm")
        {
            throw new InvalidOperationException("Lịch đã tiêm không thể thao tác lại");
        }

        DateOnly? parsedNext = null;
        if (!string.IsNullOrWhiteSpace(nextDate))
        {
            if (!DateOnly.TryParse(nextDate, out var d) || d <= DateOnly.FromDateTime(DateTime.Today))
            {
                throw new ArgumentException("Ngày hẹn lần tiếp theo phải sau hôm nay");
            }
            parsedNext = d;
        }

        item.TrangThai = "Đã tiêm";

        VaccinationSchedule? nextItem = null;
        if (parsedNext.HasValue)
        {
            nextItem = new VaccinationSchedule
            {
                HorseId = item.HorseId,
                Loai = item.Loai,
                NgayHen = parsedNext.Value,
                TrangThai = "Chờ tiêm"
            };
            await _medicalRepository.AddVaccinationAsync(nextItem, cancellationToken);
        }

        await _medicalRepository.SaveChangesAsync(cancellationToken);

        return new MarkVaccinatedResultDto
        {
            Item = MedicalMapper.ToDto(item),
            Next = nextItem is null ? null : MedicalMapper.ToDto(nextItem)
        };
    }

    public async Task<List<VetUserDto>> GetVetsAsync(CancellationToken cancellationToken = default)
    {
        var vets = await _medicalRepository.GetVetsAsync(cancellationToken);
        return vets.Select(MedicalMapper.ToVetDto).ToList();
    }

    private async Task<bool> UpdateHorseHealthInternalAsync(
        int horseId,
        string trangThai,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var health = await _healthRepository.GetByHorseIdAsync(horseId, cancellationToken);
        if (health is null || date.Date >= health.NgayCapNhat.Date || date >= health.NgayCapNhat)
        {
            await _healthRepository.UpsertAsync(horseId, NormalizeHealthStatus(trangThai), cancellationToken);
            return true;
        }

        return false;
    }

    private static bool ShouldAutoLock(List<string> injuries, string? trangThai, bool horseLocked)
    {
        var hasInjury = injuries.Count > 0 && !injuries.Contains("Không có chấn thương", StringComparer.OrdinalIgnoreCase);
        return hasInjury && NormalizeHealthStatus(trangThai) == HorseHealthStatuses.ChanThuong && !horseLocked;
    }

    private static void ValidateVetId(int vetId)
    {
        if (vetId <= 0) throw new UnauthorizedAccessException("Cần đăng nhập bằng tài khoản bác sĩ thú y.");
    }

    // Nhận nhãn cũ để tương thích client, nhưng ghi trạng thái thống nhất vào DB.
    private static string NormalizeHealthStatus(string? status) => status?.Trim() switch
    {
        HorseHealthStatuses.Tot => HorseHealthStatuses.DuDieuKien,
        HorseHealthStatuses.DangDieuTri => HorseHealthStatuses.ChanThuong,
        var value => value ?? ""
    };

    private static void ValidateExamForm(SaveMedicalRecordRequestDto request)
    {
        if (!string.IsNullOrWhiteSpace(request.TrangThai) && !HorseHealthStatuses.IsValid(request.TrangThai))
            throw new ArgumentException("Trạng thái sức khỏe không hợp lệ.");
        if (request.HorseID <= 0) throw new ArgumentException("Vui lòng chọn ngựa.");
        if (string.IsNullOrWhiteSpace(request.Ngay)) throw new ArgumentException("Ngày khám không được để trống.");
        if (!DateTime.TryParse(request.Ngay, out var d) || d.Date > DateTime.Today)
        {
            throw new ArgumentException("Ngày khám không được ở tương lai");
        }

        var diagnosis = request.ChanDoan?.Trim() ?? "";
        if (diagnosis.Length < 5 || diagnosis.Length > 500)
        {
            throw new ArgumentException("Chẩn đoán không hợp lệ");
        }

        var treatment = request.PhacDo?.Trim() ?? "";
        if (treatment.Length < 5 || treatment.Length > 1000)
        {
            throw new ArgumentException("Phác đồ điều trị không hợp lệ");
        }

        if (request.Injuries == null || request.Injuries.Count == 0
            || (request.Injuries.Contains("Không có chấn thương", StringComparer.OrdinalIgnoreCase) && request.Injuries.Count > 1))
        {
            throw new ArgumentException("Vị trí chấn thương không hợp lệ");
        }
        if (MedicalMapper.FormatInjuries(request.Injuries).Length > 255)
            throw new ArgumentException("Danh sách vị trí chấn thương không được vượt quá 255 ký tự.");
    }
}
