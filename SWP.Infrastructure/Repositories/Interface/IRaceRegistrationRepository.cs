using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IRaceRegistrationRepository
{
    Task<RaceRegistration?> GetByIdAsync(int registrationId, CancellationToken cancellationToken = default);

    Task<RaceRegistration?> GetByIdWithDetailsAsync(int registrationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> GetByRaceIdAsync(int raceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> GetByOwnerUserIdAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> IsHorseRegisteredAsync(int raceId, int horseId, CancellationToken cancellationToken = default);

    Task<int> GetCountByRaceIdAsync(int raceId, CancellationToken cancellationToken = default);

    Task<RaceRegistration> AddAsync(RaceRegistration registration, CancellationToken cancellationToken = default);

    Task UpdateAsync(RaceRegistration registration, CancellationToken cancellationToken = default);

    Task DeleteAsync(int registrationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> GetAllCompletedAsync(int? horseId = null, int? ownerUserId = null, CancellationToken cancellationToken = default);
}
