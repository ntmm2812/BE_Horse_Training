using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class RaceRegistrationRepository : IRaceRegistrationRepository
{
    private readonly ApplicationDbContext _context;

    public RaceRegistrationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RaceRegistration?> GetByIdAsync(int registrationId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId, cancellationToken);
    }

    public async Task<RaceRegistration?> GetByIdWithDetailsAsync(int registrationId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .Include(r => r.Race)
            .Include(r => r.Horse)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.RegistrationId == registrationId, cancellationToken);
    }

    public async Task<IReadOnlyList<RaceRegistration>> GetByRaceIdAsync(int raceId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .AsNoTracking()
            .Where(r => r.RaceId == raceId)
            .Include(r => r.Horse)
            .Include(r => r.User)
            .OrderBy(r => r.HorseNumber)
            .ThenBy(r => r.FinishPosition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceRegistration>> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .AsNoTracking()
            .Where(r => r.HorseId == horseId)
            .Include(r => r.Race)
            .OrderByDescending(r => r.Race.RaceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceRegistration>> GetByOwnerUserIdAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .AsNoTracking()
            .Where(r => r.Horse.UserId == ownerUserId)
            .Include(r => r.Race)
            .Include(r => r.Horse)
            .OrderByDescending(r => r.Race.RaceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsHorseRegisteredAsync(int raceId, int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .AsNoTracking()
            .AnyAsync(r => r.RaceId == raceId && r.HorseId == horseId && r.RegistrationStatus != RaceRegistrationStatuses.Cancelled, cancellationToken);
    }

    public async Task<int> GetCountByRaceIdAsync(int raceId, CancellationToken cancellationToken = default)
    {
        return await _context.RaceRegistrations
            .AsNoTracking()
            .CountAsync(r => r.RaceId == raceId && r.RegistrationStatus != RaceRegistrationStatuses.Cancelled, cancellationToken);
    }

    public async Task<RaceRegistration> AddAsync(RaceRegistration registration, CancellationToken cancellationToken = default)
    {
        _context.RaceRegistrations.Add(registration);
        await _context.SaveChangesAsync(cancellationToken);
        return registration;
    }

    public async Task UpdateAsync(RaceRegistration registration, CancellationToken cancellationToken = default)
    {
        _context.RaceRegistrations.Update(registration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int registrationId, CancellationToken cancellationToken = default)
    {
        var reg = await _context.RaceRegistrations.FindAsync([registrationId], cancellationToken);
        if (reg != null)
        {
            _context.RaceRegistrations.Remove(reg);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<RaceRegistration>> GetAllCompletedAsync(int? horseId = null, int? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.RaceRegistrations
            .AsNoTracking()
            .Include(r => r.Race)
            .Include(r => r.Horse)
            .Where(r => r.FinishPosition != null || r.ResultStatus != null);

        if (horseId.HasValue)
        {
            query = query.Where(r => r.HorseId == horseId.Value);
        }

        if (ownerUserId.HasValue)
        {
            query = query.Where(r => r.Horse.UserId == ownerUserId.Value);
        }

        return await query
            .OrderByDescending(r => r.Race.RaceDate)
            .ToListAsync(cancellationToken);
    }
}
