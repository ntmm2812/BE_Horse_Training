using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IRaceRepository
{
    Task<IReadOnlyList<Race>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Race>> GetOpenForRegistrationAsync(CancellationToken cancellationToken = default);

    Task<Race?> GetByIdAsync(int raceId, CancellationToken cancellationToken = default);

    Task<Race?> GetByIdWithRegistrationsAsync(int raceId, CancellationToken cancellationToken = default);

    Task<Race> AddAsync(Race race, CancellationToken cancellationToken = default);

    Task UpdateAsync(Race race, CancellationToken cancellationToken = default);

    Task DeleteAsync(int raceId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string raceCode, int? excludeRaceId = null, CancellationToken cancellationToken = default);
}
