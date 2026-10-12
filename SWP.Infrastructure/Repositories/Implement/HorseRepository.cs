using Microsoft.EntityFrameworkCore;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class HorseRepository : IHorseRepository
{
    private readonly ApplicationDbContext _context;

    public HorseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Horse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Horses
            .AsNoTracking()
            .OrderBy(h => h.HorseName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Horse>> GetByOwnerUserIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Horses
            .AsNoTracking()
            .Where(h => h.UserId == ownerUserId)
            .OrderBy(h => h.HorseName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Horse?> GetByIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.Horses
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HorseId == horseId, cancellationToken);
    }

    public async Task<Horse?> GetByIdForOwnerAsync(
        int horseId,
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Horses
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HorseId == horseId && h.UserId == ownerUserId, cancellationToken);
    }

    public async Task<Horse?> GetByIdForUpdateAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.Horses
            .FirstOrDefaultAsync(h => h.HorseId == horseId, cancellationToken);
    }

    public async Task<Horse> AddAsync(Horse horse, CancellationToken cancellationToken = default)
    {
        _context.Horses.Add(horse);
        await _context.SaveChangesAsync(cancellationToken);
        return horse;
    }

    public async Task UpdateAsync(Horse horse, CancellationToken cancellationToken = default)
    {
        _context.Horses.Update(horse);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int horseId, CancellationToken cancellationToken = default)
    {
        var horse = await _context.Horses.FirstOrDefaultAsync(h => h.HorseId == horseId, cancellationToken);
        if (horse is null)
        {
            return;
        }

        _context.Horses.Remove(horse);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> OwnerExistsAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == ownerUserId, cancellationToken);
    }

    public async Task<string?> GetActiveStableLabelAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.StableAssignments
            .AsNoTracking()
            .Where(a => a.HorseId == horseId && a.EndDate == null)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.Stable.StableName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CareTask>> GetDailyCareTasksAsync(
        int horseId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CareTasks
            .AsNoTracking()
            .Where(t => t.HorseId == horseId && t.ScheduledAt != null)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedingPlanDetail>> GetActiveFeedingDetailsAsync(
        int horseId,
        CancellationToken cancellationToken = default)
    {
        var planId = await _context.FeedingPlans
            .AsNoTracking()
            .Where(p => p.HorseId == horseId && p.Status.ToLower() == "active")
            .OrderByDescending(p => p.StartDate)
            .Select(p => (int?)p.FeedingPlanId)
            .FirstOrDefaultAsync(cancellationToken);

        if (planId is null)
        {
            return Array.Empty<FeedingPlanDetail>();
        }

        return await _context.FeedingPlanDetails
            .AsNoTracking()
            .Where(d => d.FeedingPlanId == planId)
            .OrderBy(d => d.FeedingTime)
            .ThenBy(d => d.MealName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(RaceRegistration Registration, Race Race)>> GetRaceAchievementsAsync(
        int horseId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.RaceRegistrations
            .AsNoTracking()
            .Where(r => r.HorseId == horseId)
            .Join(
                _context.Races.AsNoTracking(),
                reg => reg.RaceId,
                race => race.RaceId,
                (reg, race) => new { Registration = reg, Race = race })
            .OrderByDescending(x => x.Race.RaceDate)
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Registration, x.Race)).ToList();
    }
}
