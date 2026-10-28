using SWP.Application.Common;
using SWP.Application.DTOs.Races;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class RaceService : IRaceService
{
    private readonly IRaceRepository _raceRepository;
    private readonly IRaceRegistrationRepository _registrationRepository;
    private readonly IHorseRepository _horseRepository;
    private readonly IHorseHealthStatusRepository _healthStatusRepository;
    private readonly TimeProvider _timeProvider;

    public RaceService(
        IRaceRepository raceRepository,
        IRaceRegistrationRepository registrationRepository,
        IHorseRepository horseRepository,
        IHorseHealthStatusRepository healthStatusRepository,
        TimeProvider timeProvider)
    {
        _raceRepository = raceRepository;
        _registrationRepository = registrationRepository;
        _horseRepository = horseRepository;
        _healthStatusRepository = healthStatusRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<IReadOnlyList<RaceDto>>> GetAllRacesAsync(string? status, CancellationToken cancellationToken = default)
    {
        var races = await _raceRepository.GetAllAsync(status, cancellationToken);
        var dtos = races.Select(RaceMapper.ToDto).ToList();
        return ServiceResult<IReadOnlyList<RaceDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<RaceDto>> GetRaceByIdAsync(int raceId, CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdWithRegistrationsAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<RaceDto>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        return ServiceResult<RaceDto>.Ok(RaceMapper.ToDto(race));
    }

    public async Task<ServiceResult<RaceDto>> CreateRaceAsync(CreateRaceDto dto, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (dto.RaceDate < now)
        {
            return ServiceResult<RaceDto>.Invalid("Ngày diễn ra giải đua phải lớn hơn thời điểm hiện tại.");
        }

        if (dto.RegistrationDeadline.HasValue && dto.RegistrationDeadline.Value > dto.RaceDate)
        {
            return ServiceResult<RaceDto>.Invalid("Hạn chót đăng ký không thể diễn ra sau ngày bắt đầu giải đua.");
        }

        if (!string.IsNullOrWhiteSpace(dto.RaceCode))
        {
            var exists = await _raceRepository.ExistsByCodeAsync(dto.RaceCode.Trim(), null, cancellationToken);
            if (exists)
            {
                return ServiceResult<RaceDto>.Fail(ServiceErrorCodes.Conflict, $"Mã giải đua '{dto.RaceCode}' đã tồn tại.");
            }
        }

        var race = RaceMapper.ToEntity(dto, createdByUserId);
        await _raceRepository.AddAsync(race, cancellationToken);

        return ServiceResult<RaceDto>.Ok(RaceMapper.ToDto(race));
    }

    public async Task<ServiceResult<RaceDto>> UpdateRaceAsync(int raceId, UpdateRaceDto dto, CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<RaceDto>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        if (race.Status == RaceStatuses.Completed)
        {
            return ServiceResult<RaceDto>.Fail(ServiceErrorCodes.InvalidStatus, "Không thể cập nhật thông tin giải đua đã hoàn thành.");
        }

        race.RaceName = dto.RaceName.Trim();
        race.RaceType = dto.RaceType?.Trim();
        race.Location = dto.Location?.Trim();
        if (dto.RaceDate.HasValue) race.RaceDate = dto.RaceDate.Value;
        if (dto.RegistrationDeadline.HasValue) race.RegistrationDeadline = dto.RegistrationDeadline.Value;
        race.DistanceMeters = dto.DistanceMeters;
        race.MaxParticipants = dto.MaxParticipants;
        race.EntryFee = dto.EntryFee;
        race.PrizePool = dto.PrizePool;
        race.Description = dto.Description?.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Status) && RaceStatuses.IsValid(dto.Status))
        {
            race.Status = dto.Status.Trim();
        }

        race.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _raceRepository.UpdateAsync(race, cancellationToken);

        return ServiceResult<RaceDto>.Ok(RaceMapper.ToDto(race));
    }

    public async Task<ServiceResult<bool>> DeleteRaceAsync(int raceId, CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<bool>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        var registeredCount = await _registrationRepository.GetCountByRaceIdAsync(raceId, cancellationToken);
        if (registeredCount > 0 && race.Status != RaceStatuses.Cancelled)
        {
            return ServiceResult<bool>.Fail(ServiceErrorCodes.Conflict, "Không thể xóa giải đua đã có ngựa đăng ký tham gia. Vui lòng chuyển trạng thái sang Đã hủy.");
        }

        await _raceRepository.DeleteAsync(raceId, cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<RaceRegistrationDto>> UpdateRegistrationStatusAsync(
        int registrationId,
        UpdateRaceRegistrationStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!RaceRegistrationStatuses.IsValid(dto.Status))
        {
            return ServiceResult<RaceRegistrationDto>.Invalid($"Trạng thái đăng ký '{dto.Status}' không hợp lệ.");
        }

        var reg = await _registrationRepository.GetByIdWithDetailsAsync(registrationId, cancellationToken);
        if (reg == null)
        {
            return ServiceResult<RaceRegistrationDto>.NotFound($"Không tìm thấy đơn đăng ký với ID {registrationId}.");
        }

        reg.RegistrationStatus = dto.Status.Trim();
        if (dto.HorseNumber.HasValue)
        {
            reg.HorseNumber = dto.HorseNumber.Value;
        }
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            reg.Notes = dto.Notes.Trim();
        }

        reg.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _registrationRepository.UpdateAsync(reg, cancellationToken);

        return ServiceResult<RaceRegistrationDto>.Ok(RaceMapper.ToDto(reg));
    }

    public async Task<ServiceResult<IReadOnlyList<RaceRegistrationDto>>> RecordRaceResultsAsync(
        int raceId,
        BatchRecordRaceResultsDto dto,
        CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdWithRegistrationsAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<IReadOnlyList<RaceRegistrationDto>>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        if (race.Status == RaceStatuses.Cancelled)
        {
            return ServiceResult<IReadOnlyList<RaceRegistrationDto>>.Fail(ServiceErrorCodes.InvalidStatus, "Không thể nhập kết quả cho giải đua đã bị hủy.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var existingRegs = race.RaceRegistrations.ToDictionary(r => r.RegistrationId);

        foreach (var item in dto.Results)
        {
            if (existingRegs.TryGetValue(item.RegistrationId, out var reg))
            {
                reg.FinishPosition = item.FinishPosition;
                reg.FinishTimeSeconds = item.FinishTimeSeconds;
                reg.PrizeMoney = item.PrizeMoney ?? 0;
                reg.ResultStatus = !string.IsNullOrWhiteSpace(item.ResultStatus) ? item.ResultStatus.Trim() : RaceResultStatuses.Finished;
                if (!string.IsNullOrWhiteSpace(item.Notes))
                {
                    reg.Notes = item.Notes.Trim();
                }
                reg.UpdatedAt = now;
                await _registrationRepository.UpdateAsync(reg, cancellationToken);
            }
        }

        // Cập nhật trạng thái giải đua thành Đã hoàn thành (Completed)
        race.Status = RaceStatuses.Completed;
        race.UpdatedAt = now;
        await _raceRepository.UpdateAsync(race, cancellationToken);

        var updatedRegs = await _registrationRepository.GetByRaceIdAsync(raceId, cancellationToken);
        return ServiceResult<IReadOnlyList<RaceRegistrationDto>>.Ok(updatedRegs.Select(RaceMapper.ToDto).ToList());
    }

    public async Task<ServiceResult<ClubRaceSummaryReportDto>> GetClubRaceSummaryReportAsync(CancellationToken cancellationToken = default)
    {
        var races = await _raceRepository.GetAllAsync(null, cancellationToken);
        var completedRegs = await _registrationRepository.GetAllCompletedAsync(null, null, cancellationToken);

        var report = new ClubRaceSummaryReportDto
        {
            TotalRacesOrganized = races.Count,
            CompletedRaces = races.Count(r => r.Status == RaceStatuses.Completed),
            UpcomingRaces = races.Count(r => r.Status == RaceStatuses.OpenForRegistration || r.Status == RaceStatuses.Scheduled),
            TotalRegistrations = completedRegs.Count,
            TotalPrizePoolDistributed = completedRegs.Sum(r => r.PrizeMoney ?? 0),
            TotalEntryFeesCollected = races.Sum(r => (r.EntryFee ?? 0) * (r.RaceRegistrations?.Count(reg => reg.RegistrationStatus != RaceRegistrationStatuses.Cancelled) ?? 0)),
            RecentRaces = races.Take(5).Select(RaceMapper.ToDto).ToList()
        };

        return ServiceResult<ClubRaceSummaryReportDto>.Ok(report);
    }

    public async Task<ServiceResult<IReadOnlyList<RaceDto>>> GetOpenRacesAsync(CancellationToken cancellationToken = default)
    {
        var races = await _raceRepository.GetOpenForRegistrationAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<RaceDto>>.Ok(races.Select(RaceMapper.ToDto).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<RaceEligibleHorseDto>>> GetEligibleHorsesForRaceAsync(int raceId, CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<IReadOnlyList<RaceEligibleHorseDto>>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        var allHorses = await _horseRepository.GetAllAsync(cancellationToken);
        var healthStatuses = (await _healthStatusRepository.GetAllAsync(cancellationToken))
            .ToDictionary(h => h.HorseId);

        var result = new List<RaceEligibleHorseDto>();

        foreach (var horse in allHorses)
        {
            var isLocked = horse.LockedAt.HasValue || !string.IsNullOrWhiteSpace(horse.LockReason);
            healthStatuses.TryGetValue(horse.HorseId, out var healthStatusRecord);
            var currentStatus = healthStatusRecord?.TrangThai ?? horse.HealthStatusRecord?.TrangThai ?? HorseHealthStatuses.DuDieuKien;

            var isRegistered = await _registrationRepository.IsHorseRegisteredAsync(raceId, horse.HorseId, cancellationToken);

            bool isEligible = true;
            string? reason = null;

            if (isRegistered)
            {
                isEligible = false;
                reason = "Ngựa đã được đăng ký trong giải đấu này.";
            }
            else if (isLocked)
            {
                isEligible = false;
                reason = $"Ngựa đang bị khóa huấn luyện/thi đấu: {horse.LockReason ?? "Lệnh khóa từ bác sĩ thú y"}.";
            }
            else if (!string.Equals(currentStatus, HorseHealthStatuses.DuDieuKien, StringComparison.OrdinalIgnoreCase))
            {
                isEligible = false;
                reason = $"Trạng thái sức khỏe không đạt chuẩn: {currentStatus}. Yêu cầu trạng thái '{HorseHealthStatuses.DuDieuKien}'.";
            }
            else if (horse.HorseStatus != "Active")
            {
                isEligible = false;
                reason = $"Trạng thái hồ sơ ngựa không hoạt động ({horse.HorseStatus}).";
            }

            result.Add(new RaceEligibleHorseDto
            {
                HorseId = horse.HorseId,
                HorseName = horse.HorseName,
                Breed = horse.Breed,
                AgeYears = CalculateAge(horse.DateOfBirth),
                CurrentHealthStatus = currentStatus,
                IsEligible = isEligible,
                IneligibilityReason = reason
            });
        }

        return ServiceResult<IReadOnlyList<RaceEligibleHorseDto>>.Ok(result.OrderByDescending(h => h.IsEligible).ThenBy(h => h.HorseName).ToList());
    }

    public async Task<ServiceResult<RaceRegistrationDto>> RegisterHorseForRaceAsync(
        int raceId,
        RegisterHorseForRaceDto dto,
        int registeredByUserId,
        CancellationToken cancellationToken = default)
    {
        var race = await _raceRepository.GetByIdAsync(raceId, cancellationToken);
        if (race == null)
        {
            return ServiceResult<RaceRegistrationDto>.NotFound($"Không tìm thấy giải đua với ID {raceId}.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (race.Status != RaceStatuses.OpenForRegistration && race.Status != RaceStatuses.Scheduled)
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.InvalidStatus, $"Giải đua hiện không mở nhận đăng ký (Trạng thái: {race.Status}).");
        }

        if (race.RegistrationDeadline.HasValue && race.RegistrationDeadline.Value < now)
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.InvalidStatus, "Đã quá hạn chót đăng ký tham gia giải đua này.");
        }

        if (race.MaxParticipants.HasValue)
        {
            var currentCount = await _registrationRepository.GetCountByRaceIdAsync(raceId, cancellationToken);
            if (currentCount >= race.MaxParticipants.Value)
            {
                return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, $"Giải đua đã đủ số lượng tham gia tối đa ({race.MaxParticipants.Value} ngựa).");
            }
        }

        var horse = await _horseRepository.GetByIdAsync(dto.HorseId, cancellationToken);
        if (horse == null)
        {
            return ServiceResult<RaceRegistrationDto>.NotFound($"Không tìm thấy ngựa với ID {dto.HorseId}.");
        }

        // Kiểm tra lệnh khóa
        if (horse.LockedAt.HasValue || !string.IsNullOrWhiteSpace(horse.LockReason))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.HorseNotFit, $"Chiến mã {horse.HorseName} đang bị khóa huấn luyện/thi đấu: {horse.LockReason}.");
        }

        // Kiểm tra sức khỏe
        var healthRecord = await _healthStatusRepository.GetByHorseIdAsync(dto.HorseId, cancellationToken);
        var healthStatus = healthRecord?.TrangThai ?? horse.HealthStatusRecord?.TrangThai ?? HorseHealthStatuses.DuDieuKien;
        if (!string.Equals(healthStatus, HorseHealthStatuses.DuDieuKien, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.HorseNotFit, $"Chiến mã {horse.HorseName} chưa đủ điều kiện sức khỏe để thi đấu (Hiện tại: {healthStatus}).");
        }

        // Kiểm tra đã đăng ký trước đó chưa
        var isAlreadyRegistered = await _registrationRepository.IsHorseRegisteredAsync(raceId, dto.HorseId, cancellationToken);
        if (isAlreadyRegistered)
        {
            return ServiceResult<RaceRegistrationDto>.Fail(ServiceErrorCodes.Conflict, $"Chiến mã {horse.HorseName} đã được đăng ký tham gia giải đấu này.");
        }

        var nextNumber = (await _registrationRepository.GetCountByRaceIdAsync(raceId, cancellationToken)) + 1;

        var registration = new RaceRegistration
        {
            RaceId = raceId,
            HorseId = dto.HorseId,
            UserId = registeredByUserId,
            RegistrationDate = now,
            RegistrationStatus = RaceRegistrationStatuses.Confirmed,
            HorseNumber = nextNumber,
            Notes = dto.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _registrationRepository.AddAsync(registration, cancellationToken);

        var detailedReg = await _registrationRepository.GetByIdWithDetailsAsync(registration.RegistrationId, cancellationToken);
        return ServiceResult<RaceRegistrationDto>.Ok(RaceMapper.ToDto(detailedReg ?? registration));
    }

    public async Task<ServiceResult<bool>> CancelRegistrationAsync(
        int registrationId,
        int userId,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        var reg = await _registrationRepository.GetByIdWithDetailsAsync(registrationId, cancellationToken);
        if (reg == null)
        {
            return ServiceResult<bool>.NotFound($"Không tìm thấy đơn đăng ký với ID {registrationId}.");
        }

        if (reg.Race.Status == RaceStatuses.Completed || reg.Race.Status == RaceStatuses.InProgress)
        {
            return ServiceResult<bool>.Fail(ServiceErrorCodes.InvalidStatus, "Không thể hủy đăng ký khi giải đấu đang diễn ra hoặc đã kết thúc.");
        }

        if (!isManager && reg.UserId != userId && reg.Horse?.UserId != userId)
        {
            return ServiceResult<bool>.Fail(ServiceErrorCodes.Validation, "Bạn không có quyền hủy đơn đăng ký của người khác.");
        }

        reg.RegistrationStatus = RaceRegistrationStatuses.Cancelled;
        reg.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _registrationRepository.UpdateAsync(reg, cancellationToken);

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<IReadOnlyList<RaceRegistrationDto>>> GetOwnerRacesAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var registrations = await _registrationRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        return ServiceResult<IReadOnlyList<RaceRegistrationDto>>.Ok(registrations.Select(RaceMapper.ToDto).ToList());
    }

    public async Task<ServiceResult<HorsePerformanceReportDto>> GetHorsePerformanceReportAsync(int horseId, int? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var horse = await _horseRepository.GetByIdAsync(horseId, cancellationToken);
        if (horse == null)
        {
            return ServiceResult<HorsePerformanceReportDto>.NotFound($"Không tìm thấy ngựa với ID {horseId}.");
        }

        if (ownerUserId.HasValue && horse.UserId != ownerUserId.Value)
        {
            return ServiceResult<HorsePerformanceReportDto>.NotFound("Ngựa không thuộc quyền sở hữu của bạn.");
        }

        var regs = await _registrationRepository.GetByHorseIdAsync(horseId, cancellationToken);
        var completedRegs = regs.Where(r => r.FinishPosition.HasValue || r.ResultStatus == RaceResultStatuses.Finished).ToList();

        var totalRaces = completedRegs.Count;
        var wins = completedRegs.Count(r => r.FinishPosition == 1);
        var podiums = completedRegs.Count(r => r.FinishPosition.HasValue && r.FinishPosition.Value <= 3);
        var totalPrize = completedRegs.Sum(r => r.PrizeMoney ?? 0);
        var bestTime = completedRegs.Where(r => r.FinishTimeSeconds.HasValue).MinBy(r => r.FinishTimeSeconds)?.FinishTimeSeconds;

        var report = new HorsePerformanceReportDto
        {
            HorseId = horse.HorseId,
            HorseName = horse.HorseName,
            TotalRaces = totalRaces,
            TotalWins = wins,
            TotalPodiums = podiums,
            WinRatePercentage = totalRaces > 0 ? Math.Round((decimal)wins / totalRaces * 100, 2) : 0,
            PodiumRatePercentage = totalRaces > 0 ? Math.Round((decimal)podiums / totalRaces * 100, 2) : 0,
            TotalPrizeMoney = totalPrize,
            BestFinishTimeSeconds = bestTime,
            RaceHistory = regs.Select(RaceMapper.ToDto).ToList()
        };

        return ServiceResult<HorsePerformanceReportDto>.Ok(report);
    }

    public async Task<ServiceResult<OwnerRaceSummaryDto>> GetOwnerPerformanceSummaryAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var myHorses = await _horseRepository.GetByOwnerUserIdAsync(ownerUserId, cancellationToken);
        var horseReports = new List<HorsePerformanceReportDto>();

        foreach (var horse in myHorses)
        {
            var res = await GetHorsePerformanceReportAsync(horse.HorseId, ownerUserId, cancellationToken);
            if (res.Success && res.Data != null)
            {
                horseReports.Add(res.Data);
            }
        }

        var summary = new OwnerRaceSummaryDto
        {
            OwnerUserId = ownerUserId,
            TotalHorsesParticipated = horseReports.Count(h => h.TotalRaces > 0),
            TotalRacesEntered = horseReports.Sum(h => h.TotalRaces),
            TotalWins = horseReports.Sum(h => h.TotalWins),
            TotalPodiums = horseReports.Sum(h => h.TotalPodiums),
            TotalPrizeMoneyEarned = horseReports.Sum(h => h.TotalPrizeMoney),
            HorsesPerformance = horseReports
        };

        return ServiceResult<OwnerRaceSummaryDto>.Ok(summary);
    }

    private static int? CalculateAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value > today.AddYears(-age)) age--;
        return Math.Max(0, age);
    }
}
