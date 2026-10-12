using SWP.Application.Common;
using SWP.Application.DTOs.Users;

namespace SWP.Application.Services.Interface;

public interface IAccountApprovalService
{
    Task<IReadOnlyList<PendingUserDto>> GetPendingUsersAsync(CancellationToken cancellationToken = default);

    Task<ApprovalResult> ApproveUserAsync(int userId, CancellationToken cancellationToken = default);
}
