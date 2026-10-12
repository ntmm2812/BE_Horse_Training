using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IHorseRepository
{
    Task<IReadOnlyList<Horse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Horse>> GetByOwnerUserIdAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<Horse?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default);

    Task<Horse?> GetByIdForOwnerAsync(int horseId, int ownerUserId, CancellationToken cancellationToken = default);

    Task<Horse?> GetByIdForUpdateAsync(int horseId, CancellationToken cancellationToken = default);

    Task<Horse> AddAsync(Horse horse, CancellationToken cancellationToken = default);

    Task UpdateAsync(Horse horse, CancellationToken cancellationToken = default);

    Task DeleteAsync(int horseId, CancellationToken cancellationToken = default);

    Task<bool> OwnerExistsAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<string?> GetActiveStableLabelAsync(int horseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CareTask>> GetDailyCareTasksAsync(int horseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedingPlanDetail>> GetActiveFeedingDetailsAsync(
        int horseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(RaceRegistration Registration, Race Race)>> GetRaceAchievementsAsync(
        int horseId,
        CancellationToken cancellationToken = default);
}
