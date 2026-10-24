using SWP.Application.DTOs.Medical;

namespace SWP.Application.Services.Interface;

public interface IMedicalService
{
    // Medical Records
    Task<List<MedicalRecordDto>> GetMedicalRecordsAsync(int? horseId, CancellationToken cancellationToken = default);
    Task<MedicalRecordDto?> GetMedicalRecordByIdAsync(int recordId, CancellationToken cancellationToken = default);
    Task<LatestExamDateDto?> GetLatestExamDateAsync(int horseId, CancellationToken cancellationToken = default);
    Task<bool> IsLatestMedicalRecordAsync(int recordId, CancellationToken cancellationToken = default);
    Task<bool> UpdateHealthStatusAsync(int horseId, UpdateHealthStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<MedicalRecordSaveResultDto> CreateMedicalRecordAsync(SaveMedicalRecordRequestDto request, int vetId, CancellationToken cancellationToken = default);
    Task<MedicalRecordSaveResultDto> UpdateMedicalRecordAsync(int recordId, SaveMedicalRecordRequestDto request, int vetId, CancellationToken cancellationToken = default);

    // Training Locks
    Task<List<TrainingLockDto>> GetTrainingLocksAsync(CancellationToken cancellationToken = default);
    Task<List<TrainingLockDto>> GetActiveTrainingLocksAsync(CancellationToken cancellationToken = default);
    Task<TrainingLockDto> CreateTrainingLockAsync(CreateTrainingLockRequestDto request, int vetId, CancellationToken cancellationToken = default);
    Task<TrainingLockDto> ReleaseTrainingLockAsync(int lockId, ReleaseTrainingLockRequestDto request, int vetId, CancellationToken cancellationToken = default);

    // Vaccinations
    Task<List<VaccinationScheduleDto>> GetVaccinationsAsync(int? horseId, CancellationToken cancellationToken = default);
    Task<VaccinationScheduleDto?> GetVaccinationByIdAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<VaccinationScheduleDto> CreateVaccinationAsync(SaveVaccinationRequestDto request, CancellationToken cancellationToken = default);
    Task<VaccinationScheduleDto> UpdateVaccinationAsync(int scheduleId, SaveVaccinationRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteVaccinationAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<MarkVaccinatedResultDto> MarkVaccinatedAsync(int scheduleId, string? nextDate, CancellationToken cancellationToken = default);

    // Vets
    Task<List<VetUserDto>> GetVetsAsync(CancellationToken cancellationToken = default);

}
