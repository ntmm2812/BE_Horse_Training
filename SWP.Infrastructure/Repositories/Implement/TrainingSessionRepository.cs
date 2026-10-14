using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class TrainingSessionRepository : ITrainingSessionRepository
{
    private const int RecentMetricsToScan = 50;

    private readonly ApplicationDbContext _context;

    public TrainingSessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrainingSchedule>> SearchAsync(
        DateOnly from,
        DateOnly to,
        int? horseId = null,
        int? planId = null,
        int? assignedToUserId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = ToRange(from, to);
        var query = _context.TrainingSchedules
            .AsNoTracking()
            .Include(s => s.Plan).ThenInclude(p => p.Horse)
            .Include(s => s.Phase)
            .Include(s => s.AssignedToUser)
            .Where(s => s.TrainingDate >= start && s.TrainingDate < endExclusive);

        if (horseId is not null)
        {
            query = query.Where(s => s.Plan.HorseId == horseId);
        }

        if (planId is not null)
        {
            query = query.Where(s => s.PlanId == planId);
        }

        if (assignedToUserId is not null)
        {
            query = query.Where(s => s.AssignedToUserId == assignedToUserId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        return await query
            .OrderBy(s => s.TrainingDate)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.ScheduleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingSchedule?> GetByIdAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSchedules
            .AsNoTracking()
            .Include(s => s.Plan).ThenInclude(p => p.Horse)
            .Include(s => s.Phase)
            .Include(s => s.AssignedToUser)
            .FirstOrDefaultAsync(s => s.ScheduleId == sessionId, cancellationToken);
    }

    public async Task<TrainingSchedule?> GetForUpdateAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSchedules
            .Include(s => s.Plan).ThenInclude(p => p.Horse)
            .Include(s => s.Plan).ThenInclude(p => p.Phases)
            .Include(s => s.AssignedToUser)
            .FirstOrDefaultAsync(s => s.ScheduleId == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSchedule>> GetOpenSessionsForPlanAsync(
        int planId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSchedules
            .Where(s => s.PlanId == planId
                        && (s.Status == TrainingSessionStatuses.Scheduled
                            || s.Status == TrainingSessionStatuses.InProgress))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSchedule>> GetScheduledForPlanInRangeAsync(
        int planId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = ToRange(from, to);
        return await _context.TrainingSchedules
            .Where(s => s.PlanId == planId
                        && s.Status == TrainingSessionStatuses.Scheduled
                        && s.TrainingDate >= start
                        && s.TrainingDate < endExclusive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveSessionsOnDateAsync(
        int horseId,
        DateOnly date,
        int? excludeSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var (start, endExclusive) = ToRange(date, date);
        return await _context.TrainingSchedules
            .AsNoTracking()
            .Where(s => s.Plan.HorseId == horseId
                        && s.TrainingDate >= start
                        && s.TrainingDate < endExclusive
                        && s.Status != TrainingSessionStatuses.Cancelled
                        && (excludeSessionId == null || s.ScheduleId != excludeSessionId))
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingSchedule>> GetCompletedSinceAsync(
        int horseId,
        DateOnly since,
        CancellationToken cancellationToken = default)
    {
        var start = since.ToDateTime(TimeOnly.MinValue);
        return await _context.TrainingSchedules
            .AsNoTracking()
            .Where(s => s.Plan.HorseId == horseId
                        && s.TrainingDate >= start
                        && s.Status == TrainingSessionStatuses.Completed)
            .OrderBy(s => s.TrainingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<HealthMetric?> GetLatestRestingMetricAsync(
        int horseId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        // Lọc "lúc nghỉ" phía client: so sánh decimal không dịch được trên mọi provider.
        var recent = await _context.HealthMetrics
            .AsNoTracking()
            .Where(m => m.HorseId == horseId && m.RecordedAt >= sinceUtc)
            .OrderByDescending(m => m.RecordedAt)
            .Take(RecentMetricsToScan)
            .ToListAsync(cancellationToken);

        return recent.FirstOrDefault(m => m.SpeedKmh is null || m.SpeedKmh == 0);
    }

    public async Task AddAsync(TrainingSchedule session, CancellationToken cancellationToken = default)
    {
        _context.TrainingSchedules.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TrainingSchedule> sessions, CancellationToken cancellationToken = default)
    {
        _context.TrainingSchedules.AddRange(sessions);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<TrainingSchedule> sessions)
    {
        _context.TrainingSchedules.RemoveRange(sessions);
    }

    public async Task DeleteAsync(TrainingSchedule session, CancellationToken cancellationToken = default)
    {
        _context.TrainingSchedules.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static (DateTime Start, DateTime EndExclusive) ToRange(DateOnly from, DateOnly to) =>
        (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
}
