using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class TrainingPlanRepository : ITrainingPlanRepository
{
    private readonly ApplicationDbContext _context;

    public TrainingPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrainingPlan>> SearchAsync(
        int? horseId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TrainingPlans
            .AsNoTracking()
            .Include(p => p.Horse)
            .Include(p => p.Phases)
            .Include(p => p.TrainingSchedules)
            .AsQueryable();

        if (horseId is not null)
        {
            query = query.Where(p => p.HorseId == horseId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.PlanStatus == status);
        }

        return await query
            .OrderByDescending(p => p.StartDate)
            .ThenByDescending(p => p.PlanId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingPlan?> GetDetailAsync(int planId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingPlans
            .AsNoTracking()
            .Include(p => p.Horse)
            .Include(p => p.Phases)
            .Include(p => p.TrainingSchedules).ThenInclude(s => s.AssignedToUser)
            .FirstOrDefaultAsync(p => p.PlanId == planId, cancellationToken);
    }

    public async Task<TrainingPlan?> GetForUpdateAsync(int planId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingPlans
            .Include(p => p.Horse)
            .Include(p => p.Phases)
            .FirstOrDefaultAsync(p => p.PlanId == planId, cancellationToken);
    }

    public async Task<bool> HasOtherActivePlanAsync(
        int horseId,
        int excludePlanId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TrainingPlans
            .AsNoTracking()
            .AnyAsync(
                p => p.HorseId == horseId
                     && p.PlanId != excludePlanId
                     && p.PlanStatus == TrainingPlanStatuses.Active,
                cancellationToken);
    }

    public async Task<TrainingPlan> AddAsync(TrainingPlan plan, CancellationToken cancellationToken = default)
    {
        _context.TrainingPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task DeleteAsync(TrainingPlan plan, CancellationToken cancellationToken = default)
    {
        _context.TrainingPlans.Remove(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
