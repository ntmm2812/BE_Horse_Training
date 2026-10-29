using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface ICareRepository
{
    Task<IReadOnlyList<CareTask>> ListTasksAsync(int groomId, CancellationToken cancellationToken = default);

    Task<CareTask?> GetTaskTrackedAsync(int taskId, CancellationToken cancellationToken = default);

    void AddTask(CareTask task);

    Task<bool> HorseExistsAsync(int horseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StableAssignment>> ListOpenAssignmentsAsync(DateTime today, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CareTask>> ListScheduledBetweenAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedingPlan>> ListPlansInRangeAsync(DateOnly today, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentReport>> ListIncidentsAsync(int groomId, CancellationToken cancellationToken = default);

    void AddIncident(IncidentReport report);

    Task<IReadOnlyList<Supply>> ListSuppliesAsync(CancellationToken cancellationToken = default);

    Task<Supply?> GetSupplyTrackedAsync(int supplyId, CancellationToken cancellationToken = default);

    void AddSupply(Supply supply);

    void RemoveSupply(Supply supply);

    Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
