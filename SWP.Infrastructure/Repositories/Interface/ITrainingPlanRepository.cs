using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface ITrainingPlanRepository
{
    /// <summary> Danh sách giáo án (kèm Horse, Phases, TrainingSchedules) – không tracking. </summary>
    Task<IReadOnlyList<TrainingPlan>> SearchAsync(
        int? horseId,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary> Chi tiết giáo án kèm giai đoạn và các buổi tập – không tracking. </summary>
    Task<TrainingPlan?> GetDetailAsync(int planId, CancellationToken cancellationToken = default);

    /// <summary> Giáo án được tracking (kèm Horse, Phases) để cập nhật. </summary>
    Task<TrainingPlan?> GetForUpdateAsync(int planId, CancellationToken cancellationToken = default);

    Task<bool> HasOtherActivePlanAsync(
        int horseId,
        int excludePlanId,
        CancellationToken cancellationToken = default);

    Task<TrainingPlan> AddAsync(TrainingPlan plan, CancellationToken cancellationToken = default);

    Task DeleteAsync(TrainingPlan plan, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
