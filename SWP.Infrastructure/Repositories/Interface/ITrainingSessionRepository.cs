using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

/// <summary> Buổi tập (bảng TrainingSchedule) và dữ liệu thể trạng phục vụ xếp lịch. </summary>
public interface ITrainingSessionRepository
{
    /// <summary> Buổi tập trong khoảng ngày [from, to] – không tracking, kèm Plan.Horse, Phase, AssignedToUser. </summary>
    Task<IReadOnlyList<TrainingSchedule>> SearchAsync(
        DateOnly from,
        DateOnly to,
        int? horseId = null,
        int? planId = null,
        int? assignedToUserId = null,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<TrainingSchedule?> GetByIdAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary> Buổi tập được tracking, kèm Plan (Horse, Phases). </summary>
    Task<TrainingSchedule?> GetForUpdateAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary> Các buổi Scheduled/InProgress của giáo án – tracking. </summary>
    Task<IReadOnlyList<TrainingSchedule>> GetOpenSessionsForPlanAsync(
        int planId,
        CancellationToken cancellationToken = default);

    /// <summary> Các buổi Scheduled của giáo án trong khoảng ngày – tracking. </summary>
    Task<IReadOnlyList<TrainingSchedule>> GetScheduledForPlanInRangeAsync(
        int planId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary> Số buổi chưa hủy của ngựa trong một ngày. </summary>
    Task<int> CountActiveSessionsOnDateAsync(
        int horseId,
        DateOnly date,
        int? excludeSessionId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrainingSchedule>> GetCompletedSinceAsync(
        int horseId,
        DateOnly since,
        CancellationToken cancellationToken = default);

    /// <summary> Chỉ số sinh hiệu lúc nghỉ (không có vận tốc) gần nhất kể từ <paramref name="sinceUtc"/>. </summary>
    Task<HealthMetric?> GetLatestRestingMetricAsync(
        int horseId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(TrainingSchedule session, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TrainingSchedule> sessions, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<TrainingSchedule> sessions);

    Task DeleteAsync(TrainingSchedule session, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
