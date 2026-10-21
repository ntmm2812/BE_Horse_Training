using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface ICareRepository
{
    Task<List<CareTask>> GetGroomDailyTasksAsync(int groomId, CancellationToken ct = default);
    Task<CareTask?> GetCareTaskByIdAsync(int taskId, CancellationToken ct = default);
    Task<List<StableAssignment>> GetActiveStableAssignmentsAsync(DateTime now, CancellationToken ct = default);
    Task<List<CareTask>> GetDailyCareTasksForDateAsync(DateTime startOfDay, DateTime endOfDay, CancellationToken ct = default);
    Task<List<FeedingPlan>> GetActiveFeedingPlansAsync(DateOnly today, CancellationToken ct = default);
    Task<List<Supply>> GetActiveSuppliesAsync(CancellationToken ct = default);
    Task<List<AuditLog>> GetIncidentAuditLogsAsync(int? groomId = null, int? horseId = null, CancellationToken ct = default);
    Task<AuditLog> AddIncidentAuditLogAsync(AuditLog log, CancellationToken ct = default);
    Task<bool> HorseExistsAsync(int horseId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
