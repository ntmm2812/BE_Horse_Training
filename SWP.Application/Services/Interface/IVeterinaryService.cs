using SWP.Application.Common;
using SWP.Application.DTOs.Veterinary;

namespace SWP.Application.Services.Interface;

public interface IVeterinaryService
{
    Task<IReadOnlyList<MedicalRecordDto>> GetRecordsAsync(int? horseId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<MedicalRecordDto>> GetRecordAsync(int recordId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IsLatestExamDto>> IsLatestRecordAsync(int recordId, CancellationToken cancellationToken = default);

    Task<ServiceResult<LatestExamDto>> GetLatestExamAsync(int horseId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<ExamSaveResultDto>> CreateRecordAsync(SaveMedicalRecordRequestDto request, int vetId, CancellationToken cancellationToken = default);

    Task<ServiceResult<ExamSaveResultDto>> UpdateRecordAsync(int recordId, SaveMedicalRecordRequestDto request, int vetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingLockDto>> GetLocksAsync(int? ownerUserId, bool activeOnly, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingLockDto>> CreateLockAsync(CreateTrainingLockRequestDto request, int vetId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingLockDto>> ReleaseLockAsync(int lockId, string? newStatus, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VaccinationDto>> GetVaccinationsAsync(int? horseId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<VaccinationDto>> GetVaccinationAsync(int scheduleId, int? ownerUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<VaccinationDto>> CreateVaccinationAsync(SaveVaccinationRequestDto request, CancellationToken cancellationToken = default);

    Task<ServiceResult<VaccinationDto>> UpdateVaccinationAsync(int scheduleId, SaveVaccinationRequestDto request, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteVaccinationAsync(int scheduleId, CancellationToken cancellationToken = default);

    Task<ServiceResult<MarkVaccinatedResultDto>> MarkVaccinatedAsync(int scheduleId, string? nextDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VetUserDto>> GetVetsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SessionVitalDto>> GetRecentSessionVitalsAsync(CancellationToken cancellationToken = default);

    Task<RelatedHorseIdsDto> GetRelatedHorseIdsAsync(CancellationToken cancellationToken = default);
}
