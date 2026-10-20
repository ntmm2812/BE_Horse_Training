using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class MedicalRepository : IMedicalRepository
{
    private readonly ApplicationDbContext _context;

    public MedicalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

    public Task<bool> IsExamLinkedToCaseAsync(int caseId, int recordId, CancellationToken cancellationToken = default) =>
        _context.InjuryCaseEvents.AnyAsync(e => e.InjuryCaseId == caseId && e.MedicalRecordId == recordId, cancellationToken);

    public async Task TrackCaseLockReleaseAsync(TrainingLock trainingLock, int userId, CancellationToken cancellationToken = default)
    {
        // Audit every release, including locks outside the injury workflow.
        _context.AuditLogs.Add(new AuditLog
        {
            Action = "ReleaseTrainingLock", TableName = "TrainingLock", RecordId = trainingLock.LockId,
            UserId = userId, CreatedAt = DateTime.UtcNow,
            NewValue = System.Text.Json.JsonSerializer.Serialize(new
            { trainingLock.NgayGo, trainingLock.ReleaseReason, trainingLock.ReexamRecordId })
        });
        if (_context is OriginalCareDbContext || trainingLock.InjuryCaseId is not int id) return;
        var injury = await _context.InjuryCases.SingleAsync(c => c.InjuryCaseId == id, cancellationToken);
        injury.Version++;
        injury.UpdatedAt = DateTime.UtcNow;
        _context.InjuryCaseEvents.Add(new InjuryCaseEvent
        {
            InjuryCaseId = id, ActorUserId = userId, Kind = "TrainingLockReleased", CreatedAt = injury.UpdatedAt,
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            { trainingLock.LockId, trainingLock.ReleaseReason, trainingLock.ReexamRecordId })
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<TrainingLock?> GetActiveLockForHorseAsync(int horseId, CancellationToken cancellationToken = default) =>
        _context.TrainingLocks.AsNoTracking()
            .Where(l => l.HorseId == horseId && l.NgayGo == null)
            .OrderByDescending(l => l.LockId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicalRecord>> GetRecordsAsync(int? horseId, CancellationToken cancellationToken = default)
    {
        var query = _context.MedicalRecords.AsNoTracking();
        if (horseId.HasValue && horseId.Value > 0)
        {
            query = query.Where(m => m.HorseId == horseId.Value);
        }

        return await query
            .OrderByDescending(m => m.RecordDate)
            .ThenByDescending(m => m.MedicalRecordId)
            .ToListAsync(cancellationToken);
    }

    public async Task<MedicalRecord?> GetRecordByIdAsync(int recordId, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MedicalRecordId == recordId, cancellationToken);
    }

    public async Task<MedicalRecord?> GetRecordByIdForUpdateAsync(int recordId, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalRecords
            .FirstOrDefaultAsync(m => m.MedicalRecordId == recordId, cancellationToken);
    }

    public async Task<DateTime?> GetLatestRecordDateAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.MedicalRecords
            .AsNoTracking()
            .Where(m => m.HorseId == horseId)
            .OrderByDescending(m => m.RecordDate)
            .Select(m => (DateTime?)m.RecordDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsLatestRecordAsync(int horseId, DateTime recordDate, int recordId, CancellationToken cancellationToken = default)
    {
        return !await _context.MedicalRecords
            .AsNoTracking()
            .AnyAsync(m => m.HorseId == horseId &&
                (m.RecordDate > recordDate || (m.RecordDate == recordDate && m.MedicalRecordId > recordId)),
                cancellationToken);
    }

    public async Task<MedicalRecord> AddRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default)
    {
        _context.MedicalRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<IReadOnlyList<TrainingLock>> GetLocksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrainingLocks
            .AsNoTracking()
            .OrderByDescending(l => l.NgayDat)
            .ThenByDescending(l => l.LockId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingLock>> GetActiveLocksAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrainingLocks
            .AsNoTracking()
            .Where(l => l.NgayGo == null)
            .OrderByDescending(l => l.NgayDat)
            .ThenByDescending(l => l.LockId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrainingLock?> GetLockByIdForUpdateAsync(int lockId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingLocks
            .FirstOrDefaultAsync(l => l.LockId == lockId, cancellationToken);
    }

    public async Task<bool> IsHorseLockedAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingLocks
            .AsNoTracking()
            .AnyAsync(l => l.HorseId == horseId && l.NgayGo == null, cancellationToken);
    }

    public async Task<TrainingLock> AddLockAsync(TrainingLock lockEntity, CancellationToken cancellationToken = default)
    {
        _context.TrainingLocks.Add(lockEntity);
        await _context.SaveChangesAsync(cancellationToken);
        return lockEntity;
    }

    public async Task<IReadOnlyList<VaccinationSchedule>> GetVaccinationsAsync(int? horseId, CancellationToken cancellationToken = default)
    {
        var query = _context.VaccinationSchedules.AsNoTracking();
        if (horseId.HasValue && horseId.Value > 0)
        {
            query = query.Where(v => v.HorseId == horseId.Value);
        }

        return await query
            .OrderBy(v => v.NgayHen)
            .ThenBy(v => v.ScheduleId)
            .ToListAsync(cancellationToken);
    }

    public async Task<VaccinationSchedule?> GetVaccinationByIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.VaccinationSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ScheduleId == scheduleId, cancellationToken);
    }

    public async Task<VaccinationSchedule?> GetVaccinationByIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        return await _context.VaccinationSchedules
            .FirstOrDefaultAsync(v => v.ScheduleId == scheduleId, cancellationToken);
    }

    public async Task<bool> HasDuplicatePendingVaccineAsync(int horseId, string loai, int? exceptScheduleId = null, CancellationToken cancellationToken = default)
    {
        var normalized = loai.Trim().ToLower();
        return await _context.VaccinationSchedules
            .AsNoTracking()
            .AnyAsync(v =>
                v.HorseId == horseId &&
                v.TrangThai == "Chờ tiêm" &&
                (!exceptScheduleId.HasValue || v.ScheduleId != exceptScheduleId.Value) &&
                v.Loai.ToLower() == normalized,
                cancellationToken);
    }

    public async Task<VaccinationSchedule> AddVaccinationAsync(VaccinationSchedule item, CancellationToken cancellationToken = default)
    {
        _context.VaccinationSchedules.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task DeleteVaccinationAsync(VaccinationSchedule item, CancellationToken cancellationToken = default)
    {
        _context.VaccinationSchedules.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetVetsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Role.RoleName.ToLower() == "veterinarian" || u.Role.RoleName.ToLower() == "bác sĩ thú y")
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
