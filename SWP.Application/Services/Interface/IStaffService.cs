using SWP.Application.Common;
using SWP.Application.DTOs.Staff;

namespace SWP.Application.Services.Interface;

public interface IStaffService
{
    Task<IReadOnlyList<StaffAccountDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<StaffAccountDto>> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<StaffAccountDto>> CreateAsync(
        SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<StaffAccountDto>> UpdateAsync(
        int userId,
        SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<StaffAccountDto>> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
