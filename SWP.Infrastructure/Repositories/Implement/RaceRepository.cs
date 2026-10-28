using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class RaceRepository : IRaceRepository
{
    private readonly ApplicationDbContext _context;

    public RaceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Race>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Races.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.RaceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Race>> GetOpenForRegistrationAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Races
            .AsNoTracking()
            .Where(r => (r.Status == RaceStatuses.OpenForRegistration || r.Status == RaceStatuses.Scheduled)
                        && (r.RegistrationDeadline == null || r.RegistrationDeadline >= now))
            .OrderBy(r => r.RaceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Race?> GetByIdAsync(int raceId, CancellationToken cancellationToken = default)
    {
        return await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken);
    }

    public async Task<Race?> GetByIdWithRegistrationsAsync(int raceId, CancellationToken cancellationToken = default)
    {
        return await _context.Races
            .Include(r => r.RaceRegistrations)
                .ThenInclude(reg => reg.Horse)
            .Include(r => r.RaceRegistrations)
                .ThenInclude(reg => reg.User)
            .FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken);
    }

    public async Task<Race> AddAsync(Race race, CancellationToken cancellationToken = default)
    {
        _context.Races.Add(race);
        await _context.SaveChangesAsync(cancellationToken);
        return race;
    }

    public async Task UpdateAsync(Race race, CancellationToken cancellationToken = default)
    {
        _context.Races.Update(race);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int raceId, CancellationToken cancellationToken = default)
    {
        var race = await _context.Races.FindAsync([raceId], cancellationToken);
        if (race != null)
        {
            _context.Races.Remove(race);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByCodeAsync(string raceCode, int? excludeRaceId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Races.AsNoTracking().Where(r => r.RaceCode == raceCode);
        if (excludeRaceId.HasValue)
        {
            query = query.Where(r => r.RaceId != excludeRaceId.Value);
        }
        return await query.AnyAsync(cancellationToken);
    }
}
