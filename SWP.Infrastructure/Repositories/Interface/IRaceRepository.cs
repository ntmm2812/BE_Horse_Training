using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IRaceRepository
{
    Task<IReadOnlyList<Race>> ListRacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> ListRegistrationsAsync(int? ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> ListPrizeReviewsAsync(CancellationToken cancellationToken = default);

    Task<Race?> GetRaceTrackedAsync(int raceId, CancellationToken cancellationToken = default);

    Task<RaceRegistration?> GetRegistrationTrackedAsync(int raceId, int horseId, CancellationToken cancellationToken = default);

    Task<RaceRegistration?> GetRegistrationTrackedByIdAsync(int registrationId, CancellationToken cancellationToken = default);

    Task<int> CountActiveRegistrationsAsync(int raceId, CancellationToken cancellationToken = default);

    Task<bool> RankTakenAsync(int raceId, int rank, int exceptRegistrationId, CancellationToken cancellationToken = default);

    Task<bool> HorseExistsAsync(int horseId, CancellationToken cancellationToken = default);

    Task<string?> GetHealthStatusAsync(int horseId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLockAsync(int horseId, CancellationToken cancellationToken = default);

    void AddRegistration(RaceRegistration registration);

    void RemoveRegistration(RaceRegistration registration);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
