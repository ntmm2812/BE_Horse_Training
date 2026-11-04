using SWP.Application.Common;
using SWP.Application.DTOs.Race;

namespace SWP.Application.Services.Interface;

public interface IRaceService
{
    Task<IReadOnlyList<RaceDto>> GetRacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistrationDto>> GetRegistrationsAsync(int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceRegistrationDto>> RegisterAsync(
        int raceId,
        int horseId,
        int trainerId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> CancelAsync(int raceId, int horseId, CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceResultUpdateDto>> UpdateResultAsync(
        int raceId,
        int horseId,
        int? rank,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<RaceRegistrationDto>> SetPrizeAsync(
        int raceId,
        int horseId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrizeAlertDto>> GetPrizeAlertsAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DismissPrizeAlertAsync(int alertId, CancellationToken cancellationToken = default);
}
