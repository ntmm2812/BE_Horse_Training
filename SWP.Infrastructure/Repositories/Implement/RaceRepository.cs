using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class RaceRepository : IRaceRepository
{
    private readonly ApplicationDbContext _db;

    public RaceRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Race>> ListRacesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Races
            .AsNoTracking()
            .OrderBy(r => r.RaceDate)
            .ThenBy(r => r.RaceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceRegistration>> ListRegistrationsAsync(
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var query = ActiveRegistrations();
        if (ownerUserId is int ownerId)
        {
            query = query.Where(r => r.Horse.UserId == ownerId);
        }

        return await query
            .OrderBy(r => r.Race.RaceDate)
            .ThenBy(r => r.HorseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceRegistration>> ListPrizeReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.RaceRegistrations
            .AsNoTracking()
            .Where(r => r.ResultStatus == RaceCatalog.PrizeReview)
            .OrderByDescending(r => r.UpdatedAt)
            .ThenByDescending(r => r.RegistrationId)
            .ToListAsync(cancellationToken);
    }

    public Task<Race?> GetRaceTrackedAsync(int raceId, CancellationToken cancellationToken = default) =>
        _db.Races.FirstOrDefaultAsync(r => r.RaceId == raceId, cancellationToken);

    public Task<RaceRegistration?> GetRegistrationTrackedAsync(
        int raceId,
        int horseId,
        CancellationToken cancellationToken = default) =>
        _db.RaceRegistrations.FirstOrDefaultAsync(
            r => r.RaceId == raceId && r.HorseId == horseId,
            cancellationToken);

    public Task<RaceRegistration?> GetRegistrationTrackedByIdAsync(
        int registrationId,
        CancellationToken cancellationToken = default) =>
        _db.RaceRegistrations.FirstOrDefaultAsync(r => r.RegistrationId == registrationId, cancellationToken);

    public Task<int> CountActiveRegistrationsAsync(int raceId, CancellationToken cancellationToken = default) =>
        ActiveRegistrations().CountAsync(r => r.RaceId == raceId, cancellationToken);

    public Task<bool> RankTakenAsync(
        int raceId,
        int rank,
        int exceptRegistrationId,
        CancellationToken cancellationToken = default) =>
        ActiveRegistrations().AnyAsync(
            r => r.RaceId == raceId && r.RegistrationId != exceptRegistrationId && r.FinishPosition == rank,
            cancellationToken);

    public Task<bool> HorseExistsAsync(int horseId, CancellationToken cancellationToken = default) =>
        _db.Horses.AnyAsync(h => h.HorseId == horseId, cancellationToken);

    public async Task<string?> GetHealthStatusAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _db.HorseHealthStatuses
            .AsNoTracking()
            .Where(s => s.HorseId == horseId)
            .Select(s => s.TrangThai)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasActiveLockAsync(int horseId, CancellationToken cancellationToken = default)
    {
        if (await _db.TrainingLocks.AnyAsync(l => l.HorseId == horseId && l.ReleasedOn == null, cancellationToken))
        {
            return true;
        }

        return await _db.Horses.AnyAsync(h => h.HorseId == horseId && h.LockedAt != null, cancellationToken);
    }

    public void AddRegistration(RaceRegistration registration) => _db.RaceRegistrations.Add(registration);

    public void RemoveRegistration(RaceRegistration registration) => _db.RaceRegistrations.Remove(registration);

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private IQueryable<RaceRegistration> ActiveRegistrations() =>
        _db.RaceRegistrations.Where(r =>
            r.RegistrationStatus != "Cancelled"
            && r.RegistrationStatus != "Canceled"
            && r.RegistrationStatus != "Withdrawn"
            && r.RegistrationStatus != "Rejected");
}
