using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class CareRepository : ICareRepository
{
    private readonly ApplicationDbContext _db;

    public CareRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CareTask>> ListTasksAsync(int groomId, CancellationToken cancellationToken = default)
    {
        return await _db.CareTasks
            .AsNoTracking()
            .Where(t => t.UserId == groomId)
            .OrderByDescending(t => t.ScheduledAt)
            .ThenBy(t => t.CareTaskId)
            .ToListAsync(cancellationToken);
    }

    public Task<CareTask?> GetTaskTrackedAsync(int taskId, CancellationToken cancellationToken = default) =>
        _db.CareTasks.FirstOrDefaultAsync(t => t.CareTaskId == taskId, cancellationToken);

    public void AddTask(CareTask task) => _db.CareTasks.Add(task);

    public Task<bool> HorseExistsAsync(int horseId, CancellationToken cancellationToken = default) =>
        _db.Horses.AsNoTracking().AnyAsync(h => h.HorseId == horseId, cancellationToken);

    public async Task<IReadOnlyList<StableAssignment>> ListOpenAssignmentsAsync(
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        return await _db.StableAssignments
            .AsNoTracking()
            .Include(a => a.Stable)
            .Where(a => a.EndDate == null || a.EndDate >= today)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CareTask>> ListScheduledBetweenAsync(
        DateTime fromInclusive,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
    {
        return await _db.CareTasks
            .AsNoTracking()
            .Where(t => t.ScheduledAt != null && t.ScheduledAt >= fromInclusive && t.ScheduledAt < toExclusive)
            .OrderBy(t => t.ScheduledAt)
            .ThenBy(t => t.CareTaskId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedingPlan>> ListPlansInRangeAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        return await _db.FeedingPlans
            .AsNoTracking()
            .Include(p => p.FeedingPlanDetails)
            .Where(p => p.StartDate <= today && (p.EndDate == null || p.EndDate >= today))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IncidentReport>> ListIncidentsAsync(int groomId, CancellationToken cancellationToken = default)
    {
        return await _db.IncidentReports
            .AsNoTracking()
            .Where(r => r.GroomId == groomId)
            .OrderByDescending(r => r.ReportedOn)
            .ThenByDescending(r => r.ReportId)
            .ToListAsync(cancellationToken);
    }

    public void AddIncident(IncidentReport report) => _db.IncidentReports.Add(report);

    public async Task<IReadOnlyList<Supply>> ListSuppliesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Supplies
            .AsNoTracking()
            .OrderBy(s => s.SupplyName)
            .ToListAsync(cancellationToken);
    }

    public Task<Supply?> GetSupplyTrackedAsync(int supplyId, CancellationToken cancellationToken = default) =>
        _db.Supplies.FirstOrDefaultAsync(s => s.SupplyId == supplyId, cancellationToken);

    public void AddSupply(Supply supply) => _db.Supplies.Add(supply);

    public void RemoveSupply(Supply supply) => _db.Supplies.Remove(supply);

    public Task<User?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> ListUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
