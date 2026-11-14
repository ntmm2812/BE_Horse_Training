using SWP.Application.Common;
using SWP.Application.DTOs.Training;

namespace SWP.Application.Services.Interface;

public interface ITrainingSessionService
{
    Task<ServiceResult<IReadOnlyList<TrainingSessionDto>>> SearchAsync(
        TrainingSessionQueryDto query,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> GetByIdAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> CreateAsync(
        int planId,
        SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> UpdateAsync(
        int sessionId,
        SaveTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> AssignAsync(
        int sessionId,
        AssignTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> StartAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> CompleteAsync(
        int sessionId,
        CompleteTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SessionReportDto>> SubmitReportAsync(
        int sessionId,
        int groomId,
        SubmitSessionReportRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SessionReviewDto>> SubmitReviewAsync(
        int sessionId,
        int trainerId,
        SubmitSessionReviewRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SessionReviewDto>> UpdateReviewAsync(
        int sessionId,
        int trainerId,
        SubmitSessionReviewRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<SessionReviewDto>> SendReviewToOwnerAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<OwnerSessionReviewDto>>> GetOwnerReviewsAsync(
        int ownerUserId,
        int? horseId,
        DateOnly? from,
        DateOnly? to,
        int? limit,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<TrainingSessionDto>> CancelAsync(
        int sessionId,
        CancelTrainingSessionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingStaffDto>> GetAssignableStaffAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<TrainingSessionDto>>> GetMyAssignmentsAsync(
        int userId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<HorseWeeklyScheduleDto>> GetHorseWeeklyScheduleAsync(
        int horseId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default);
}
