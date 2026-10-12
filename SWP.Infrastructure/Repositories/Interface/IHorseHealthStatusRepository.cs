using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IHorseHealthStatusRepository
{
    Task<IReadOnlyList<HorseHealthStatus>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseHealthStatus>> GetByOwnerUserIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);

    Task<HorseHealthStatus?> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default);

    Task UpsertAsync(int horseId, string trangThai, CancellationToken cancellationToken = default);
}
