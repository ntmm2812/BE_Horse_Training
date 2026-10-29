using SWP.Application.Common;
using SWP.Application.DTOs.Care;

namespace SWP.Application.Services.Interface;

public interface ICareService
{
    Task<IReadOnlyList<CareTaskDto>> GetTasksAsync(int groomId, CancellationToken cancellationToken = default);

    Task<ServiceResult<CareTaskDto>> CompleteTaskAsync(int taskId, int groomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StableAssignmentDto>> GetStablesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedingMealDto>> GetFeedingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentReportDto>> GetIncidentsAsync(int groomId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IncidentReportDto>> CreateIncidentAsync(
        CreateIncidentReportRequestDto request,
        int groomId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SupplyDto>> GetSuppliesAsync(CancellationToken cancellationToken = default);

    Task<SupplyDto?> GetSupplyAsync(int supplyId, CancellationToken cancellationToken = default);

    Task<ServiceResult<SupplyDto>> CreateSupplyAsync(SaveSupplyRequestDto request, CancellationToken cancellationToken = default);

    Task<ServiceResult<SupplyDto>> UpdateSupplyAsync(int supplyId, SaveSupplyRequestDto request, CancellationToken cancellationToken = default);

    Task<ServiceResult<SupplyDto>> AdjustStockAsync(int supplyId, decimal delta, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteSupplyAsync(int supplyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffMemberDto>> GetStaffAsync(CancellationToken cancellationToken = default);
}
