using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class VeterinaryRepository : IVeterinaryRepository
{
    private readonly ApplicationDbContext _db;

    public VeterinaryRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> HorseExistsAsync(int horseId, int? ownerUserId, CancellationToken cancellationToken = default) =>
        _db.Horses.AsNoTracking().AnyAsync(
            h => h.HorseId == horseId && (ownerUserId == null || h.UserId == ownerUserId),
            cancellationToken);

    public Task<Horse?> GetHorseTrackedAsync(int horseId, CancellationToken cancellationToken = default) =>
        _db.Horses.FirstOrDefaultAsync(h => h.HorseId == horseId, cancellationToken);

    public Task<HorseHealthStatus?> GetHealthTrackedAsync(int horseId, CancellationToken cancellationToken = default) =>
        _db.HorseHealthStatuses.FirstOrDefaultAsync(s => s.HorseId == horseId, cancellationToken);

    public void AddHealth(HorseHealthStatus status) => _db.HorseHealthStatuses.Add(status);

    public void AddRecord(MedicalRecord record) => _db.MedicalRecords.Add(record);

    public Task<MedicalRecord?> GetRecordTrackedAsync(int recordId, CancellationToken cancellationToken = default) =>
        _db.MedicalRecords.FirstOrDefaultAsync(r => r.MedicalRecordId == recordId, cancellationToken);

    public async Task<IReadOnlyList<MedicalRecord>> ListRecordsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.MedicalRecords.AsNoTracking().AsQueryable();
        if (horseId is int id)
        {
            query = query.Where(r => r.HorseId == id);
        }

        if (ownerUserId is int owner)
        {
            query = query.Where(r => _db.Horses.Any(h => h.HorseId == r.HorseId && h.UserId == owner));
        }

        return await query
            .OrderByDescending(r => r.RecordDate)
            .ThenByDescending(r => r.MedicalRecordId)
            .ToListAsync(cancellationToken);
    }

    public Task<MedicalRecord?> GetLatestRecordAsync(int horseId, CancellationToken cancellationToken = default) =>
        _db.MedicalRecords.AsNoTracking()
            .Where(r => r.HorseId == horseId)
            .OrderByDescending(r => r.RecordDate)
            .ThenByDescending(r => r.MedicalRecordId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasActiveLockAsync(int horseId, CancellationToken cancellationToken = default)
    {
        if (await _db.TrainingLocks.AnyAsync(l => l.HorseId == horseId && l.ReleasedOn == null, cancellationToken))
        {
            return true;
        }

        return await _db.Horses.AnyAsync(h => h.HorseId == horseId && h.LockedAt != null, cancellationToken);
    }

    public void AddLock(TrainingLock trainingLock) => _db.TrainingLocks.Add(trainingLock);

    public Task<TrainingLock?> GetLockTrackedAsync(int lockId, CancellationToken cancellationToken = default) =>
        _db.TrainingLocks.FirstOrDefaultAsync(l => l.LockId == lockId, cancellationToken);

    public async Task<IReadOnlyList<TrainingLock>> ListLocksAsync(
        int? ownerUserId,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TrainingLocks.AsNoTracking().AsQueryable();
        if (activeOnly)
        {
            query = query.Where(l => l.ReleasedOn == null);
        }

        if (ownerUserId is int owner)
        {
            query = query.Where(l => _db.Horses.Any(h => h.HorseId == l.HorseId && h.UserId == owner));
        }

        return await query
            .OrderByDescending(l => l.PlacedOn)
            .ThenByDescending(l => l.LockId)
            .ToListAsync(cancellationToken);
    }

    public void AddVaccination(VaccinationSchedule item) => _db.VaccinationSchedules.Add(item);

    public void RemoveVaccination(VaccinationSchedule item) => _db.VaccinationSchedules.Remove(item);

    public Task<VaccinationSchedule?> GetVaccinationTrackedAsync(int scheduleId, CancellationToken cancellationToken = default) =>
        _db.VaccinationSchedules.FirstOrDefaultAsync(v => v.ScheduleId == scheduleId, cancellationToken);

    public async Task<IReadOnlyList<VaccinationSchedule>> ListVaccinationsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.VaccinationSchedules.AsNoTracking().AsQueryable();
        if (horseId is int id)
        {
            query = query.Where(v => v.HorseId == id);
        }

        if (ownerUserId is int owner)
        {
            query = query.Where(v => _db.Horses.Any(h => h.HorseId == v.HorseId && h.UserId == owner));
        }

        return await query.OrderBy(v => v.DueDate).ThenBy(v => v.ScheduleId).ToListAsync(cancellationToken);
    }

    public Task<bool> HasPendingVaccineAsync(
        int horseId,
        string vaccineType,
        int? exceptScheduleId,
        CancellationToken cancellationToken = default) =>
        _db.VaccinationSchedules.AnyAsync(
            v => v.HorseId == horseId
                && v.Status == VaccineStatuses.Pending
                && v.VaccineType == vaccineType
                && (exceptScheduleId == null || v.ScheduleId != exceptScheduleId),
            cancellationToken);

    public async Task<IReadOnlyList<SessionHeartRateRow>> ListRecentHeartRatesAsync(
        DateOnly from,
        CancellationToken cancellationToken = default)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var rows = await (
            from session in _db.TrainingSchedules.AsNoTracking()
            join plan in _db.TrainingPlans.AsNoTracking() on session.PlanId equals plan.PlanId
            where session.TrainingDate >= fromDate && (session.MaxHeartRate != null || session.AvgHeartRate != null)
            select new { plan.HorseId, session.TrainingDate, Heart = session.MaxHeartRate ?? session.AvgHeartRate }
        ).ToListAsync(cancellationToken);

        return rows
            .Where(row => row.Heart is not null)
            .Select(row => new SessionHeartRateRow(row.HorseId, DateOnly.FromDateTime(row.TrainingDate), row.Heart!.Value))
            .ToList();
    }

    public Task<IReadOnlyList<int>> HorseIdsWithRecordsAsync(CancellationToken cancellationToken = default) =>
        Ids(_db.MedicalRecords.Select(r => r.HorseId), cancellationToken);

    public Task<IReadOnlyList<int>> HorseIdsWithLocksAsync(CancellationToken cancellationToken = default) =>
        Ids(_db.TrainingLocks.Select(l => l.HorseId), cancellationToken);

    public Task<IReadOnlyList<int>> HorseIdsWithVaccinationsAsync(CancellationToken cancellationToken = default) =>
        Ids(_db.VaccinationSchedules.Select(v => v.HorseId), cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private static async Task<IReadOnlyList<int>> Ids(IQueryable<int> query, CancellationToken cancellationToken) =>
        await query.Distinct().ToListAsync(cancellationToken);
}
