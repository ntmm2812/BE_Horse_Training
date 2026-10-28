using SWP.Application.Common;
using SWP.Application.DTOs.Races;

namespace SWP.Application.Services.Interface;

public interface IRaceService
{
    // --- 1. Quản lý giải đua (Club Manager) ---
    Task<ServiceResult<IReadOnlyList<RaceDto>>> GetAllRacesAsync(string? status, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceDto>> GetRaceByIdAsync(int raceId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceDto>> CreateRaceAsync(CreateRaceDto dto, int createdByUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceDto>> UpdateRaceAsync(int raceId, UpdateRaceDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteRaceAsync(int raceId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceRegistrationDto>> UpdateRegistrationStatusAsync(int registrationId, UpdateRaceRegistrationStatusDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<RaceRegistrationDto>>> RecordRaceResultsAsync(int raceId, BatchRecordRaceResultsDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<ClubRaceSummaryReportDto>> GetClubRaceSummaryReportAsync(CancellationToken cancellationToken = default);

    // --- 2. Huấn luyện viên (Head Trainer) ---
    Task<ServiceResult<IReadOnlyList<RaceDto>>> GetOpenRacesAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<RaceEligibleHorseDto>>> GetEligibleHorsesForRaceAsync(int raceId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceRegistrationDto>> RegisterHorseForRaceAsync(int raceId, RegisterHorseForRaceDto dto, int registeredByUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> CancelRegistrationAsync(int registrationId, int userId, bool isManager, CancellationToken cancellationToken = default);

    // --- 3. Chủ sở hữu ngựa (Horse Owner) ---
    Task<ServiceResult<IReadOnlyList<RaceRegistrationDto>>> GetOwnerRacesAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<HorsePerformanceReportDto>> GetHorsePerformanceReportAsync(int horseId, int? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<ServiceResult<OwnerRaceSummaryDto>> GetOwnerPerformanceSummaryAsync(int ownerUserId, CancellationToken cancellationToken = default);
}
