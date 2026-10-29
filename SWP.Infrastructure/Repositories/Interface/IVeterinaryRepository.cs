using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public sealed record SessionHeartRateRow(int HorseId, DateOnly Date, decimal HeartRate);

public interface IVeterinaryRepository
{
    Task<bool> HorseExistsAsync(int horseId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<Horse?> GetHorseTrackedAsync(int horseId, CancellationToken cancellationToken = default);

    Task<HorseHealthStatus?> GetHealthTrackedAsync(int horseId, CancellationToken cancellationToken = default);

    void AddHealth(HorseHealthStatus status);

    void AddRecord(MedicalRecord record);

    Task<MedicalRecord?> GetRecordTrackedAsync(int recordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicalRecord>> ListRecordsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default);

    Task<MedicalRecord?> GetLatestRecordAsync(int horseId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLockAsync(int horseId, CancellationToken cancellationToken = default);

    void AddLock(TrainingLock trainingLock);

    Task<TrainingLock?> GetLockTrackedAsync(int lockId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingLock>> ListLocksAsync(
        int? ownerUserId,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    void AddVaccination(VaccinationSchedule item);

    void RemoveVaccination(VaccinationSchedule item);

    Task<VaccinationSchedule?> GetVaccinationTrackedAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VaccinationSchedule>> ListVaccinationsAsync(
        int? horseId,
        int? ownerUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingVaccineAsync(
        int horseId,
        string vaccineType,
        int? exceptScheduleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionHeartRateRow>> ListRecentHeartRatesAsync(DateOnly from, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> HorseIdsWithRecordsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> HorseIdsWithLocksAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> HorseIdsWithVaccinationsAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
