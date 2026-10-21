using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class CareRepository : ICareRepository
{
    private const string IncidentAction = "ReportIncident";
    private readonly ApplicationDbContext _context;

    public CareRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<CareTask> DailyTasksQuery => _context.CareTasks
        .Where(t => t.TaskType == null || t.TaskType.Trim().ToLower() != "medical");

    public Task<List<CareTask>> GetGroomDailyTasksAsync(int groomId, CancellationToken ct = default) =>
        DailyTasksQuery
            .AsNoTracking()
            .Where(t => t.UserId == groomId && t.Status != "Cancelled")
            .OrderByDescending(t => (t.ScheduledAt ?? t.CreatedAt).Date)
            .ThenBy(t => t.CareTaskId)
            .ToListAsync(ct);

    public Task<CareTask?> GetCareTaskByIdAsync(int taskId, CancellationToken ct = default) =>
        _context.CareTasks.SingleOrDefaultAsync(t => t.CareTaskId == taskId, ct);

    public Task<List<StableAssignment>> GetActiveStableAssignmentsAsync(DateTime now, CancellationToken ct = default) =>
        _context.StableAssignments
            .AsNoTracking()
            .Include(a => a.Stable)
            .Where(a => a.StartDate <= now && (a.EndDate == null || a.EndDate > now) && a.Status == "Active")
            .OrderByDescending(a => a.StartDate)
            .ThenByDescending(a => a.AssignmentId)
            .ToListAsync(ct);

    public Task<List<CareTask>> GetDailyCareTasksForDateAsync(DateTime startOfDay, DateTime endOfDay, CancellationToken ct = default) =>
        DailyTasksQuery
            .AsNoTracking()
            .Where(t => t.ScheduledAt >= startOfDay && t.ScheduledAt < endOfDay && t.Status != "Cancelled")
            .OrderBy(t => t.ScheduledAt)
            .ThenBy(t => t.CareTaskId)
            .ToListAsync(ct);

    public Task<List<FeedingPlan>> GetActiveFeedingPlansAsync(DateOnly today, CancellationToken ct = default) =>
        _context.FeedingPlans
            .AsNoTracking()
            .Include(p => p.FeedingPlanDetails)
            .Where(p => p.Status == "Active" && p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.FeedingPlanId)
            .ToListAsync(ct);

    public Task<List<Supply>> GetActiveSuppliesAsync(CancellationToken ct = default) =>
        _context.Supplies
            .AsNoTracking()
            .OrderBy(s => s.SupplyName)
            .ToListAsync(ct);

    public Task<List<AuditLog>> GetIncidentAuditLogsAsync(int? groomId = null, int? horseId = null, CancellationToken ct = default)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.Action == IncidentAction && a.TableName == "Horse" && a.UserId != null);

        if (groomId.HasValue)
        {
            query = query.Where(a => a.UserId == groomId.Value);
        }

        if (horseId.HasValue)
        {
            query = query.Where(a => a.RecordId == horseId.Value);
        }

        return query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.LogId)
            .ToListAsync(ct);
    }

    public async Task<AuditLog> AddIncidentAuditLogAsync(AuditLog log, CancellationToken ct = default)
    {
        await _context.AuditLogs.AddAsync(log, ct);
        return log;
    }

    public Task<bool> HorseExistsAsync(int horseId, CancellationToken ct = default) =>
        _context.Horses.AnyAsync(h => h.HorseId == horseId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
