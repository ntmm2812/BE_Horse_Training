using SWP.Domain.Models.Entity;
using Microsoft.EntityFrameworkCore.Storage;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IMedicalRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<TrainingLock?> GetActiveLockForHorseAsync(int horseId, CancellationToken cancellationToken = default);
    Task TrackCaseLockReleaseAsync(TrainingLock trainingLock, int userId, string reason, CancellationToken cancellationToken = default);

    // Medical Record
    Task<IReadOnlyList<MedicalRecord>> GetRecordsAsync(int? horseId, CancellationToken cancellationToken = default);
    Task<MedicalRecord?> GetRecordByIdAsync(int recordId, CancellationToken cancellationToken = default);
    Task<MedicalRecord?> GetRecordByIdForUpdateAsync(int recordId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLatestRecordDateAsync(int horseId, CancellationToken cancellationToken = default);
    Task<bool> IsLatestRecordAsync(int horseId, DateTime recordDate, int recordId, CancellationToken cancellationToken = default);
    Task<MedicalRecord> AddRecordAsync(MedicalRecord record, CancellationToken cancellationToken = default);

    // Training Lock
    Task<IReadOnlyList<TrainingLock>> GetLocksAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingLock>> GetActiveLocksAsync(CancellationToken cancellationToken = default);
    Task<TrainingLock?> GetLockByIdForUpdateAsync(int lockId, CancellationToken cancellationToken = default);
    Task<bool> IsHorseLockedAsync(int horseId, CancellationToken cancellationToken = default);
    Task<TrainingLock> AddLockAsync(TrainingLock lockEntity, CancellationToken cancellationToken = default);

    // Vaccination
    Task<IReadOnlyList<VaccinationSchedule>> GetVaccinationsAsync(int? horseId, CancellationToken cancellationToken = default);
    Task<VaccinationSchedule?> GetVaccinationByIdAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<VaccinationSchedule?> GetVaccinationByIdForUpdateAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<bool> HasDuplicatePendingVaccineAsync(int horseId, string loai, int? exceptScheduleId = null, CancellationToken cancellationToken = default);
    Task<VaccinationSchedule> AddVaccinationAsync(VaccinationSchedule item, CancellationToken cancellationToken = default);
    Task DeleteVaccinationAsync(VaccinationSchedule item, CancellationToken cancellationToken = default);

    // Vets
    Task<IReadOnlyList<User>> GetVetsAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
