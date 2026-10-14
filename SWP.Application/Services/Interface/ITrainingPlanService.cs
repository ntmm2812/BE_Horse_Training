using SWP.Application.Common;
using SWP.Application.DTOs.Training;

namespace SWP.Application.Services.Interface;

public interface ITrainingPlanService
{
    TrainingOptionsDto GetOptions();

    Task<ServiceResult<IReadOnlyList<TrainingPlanDto>>> SearchAsync(
        int? horseId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> GetDetailAsync(int planId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanSuggestionDto>> GetSuggestionAsync(
        int horseId,
        DateOnly? startDate,
        int? weeks,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> CreateAsync(
        CreateTrainingPlanRequestDto request,
        int createdByUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> UpdateAsync(
        int planId,
        UpdateTrainingPlanRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(int planId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> ActivateAsync(int planId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> CompleteAsync(int planId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingPlanDetailDto>> CancelAsync(int planId, CancellationToken cancellationToken = default);

    Task<ServiceResult<WeeklyScheduleGenerationResultDto>> GenerateWeeklyScheduleAsync(
        int planId,
        GenerateWeeklyScheduleRequestDto request,
        CancellationToken cancellationToken = default);
}
