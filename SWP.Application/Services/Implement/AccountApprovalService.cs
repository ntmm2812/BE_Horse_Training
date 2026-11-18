using SWP.Application.Common;
using SWP.Application.DTOs.Users;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Application.Services.Implement;

public class AccountApprovalService : IAccountApprovalService
{
    private readonly IUserRepository _userRepository;

    public AccountApprovalService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<PendingUserDto>> GetPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetPendingUsersAsync(cancellationToken);
        return users
            .Where(IsHorseOwner)
            .Select(MapToDto)
            .OrderBy(u => u.CreatedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<PendingUserDto>> GetOwnerUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetHorseOwnerUsersAsync(cancellationToken);
        return users.Select(MapToDto).ToList();
    }

    public async Task<ApprovalResult> ApproveUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(userId, cancellationToken);
        if (user is null || !IsHorseOwner(user))
        {
            return ApprovalResult.Fail("Không tìm thấy tài khoản chủ ngựa.", "NOT_FOUND");
        }

        if (!string.Equals(user.Status, UserStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return ApprovalResult.Fail("Tài khoản này không ở trạng thái chờ duyệt.", "NOT_PENDING");
        }

        user.Status = UserStatuses.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var fresh = await _userRepository.GetByIdWithRoleReadAsync(userId, cancellationToken);
        return ApprovalResult.Ok(MapToDto(fresh!));
    }

    public async Task<ApprovalResult> RejectUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(userId, cancellationToken);
        if (user is null || !IsHorseOwner(user))
        {
            return ApprovalResult.Fail("Không tìm thấy tài khoản chủ ngựa.", "NOT_FOUND");
        }

        if (!string.Equals(user.Status, UserStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return ApprovalResult.Fail("Tài khoản này không ở trạng thái chờ duyệt.", "NOT_PENDING");
        }

        user.Status = UserStatuses.Rejected;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var fresh = await _userRepository.GetByIdWithRoleReadAsync(userId, cancellationToken);
        return ApprovalResult.Ok(MapToDto(fresh!));
    }

    private static bool IsHorseOwner(Domain.Models.Entity.User user) =>
        user.Role is not null
        && RoleMapper.TryMapToCanonicalRoleId(user.Role.RoleName, out var mapped)
        && string.Equals(mapped, RoleNames.HorseOwner, StringComparison.OrdinalIgnoreCase);

    private static PendingUserDto MapToDto(Domain.Models.Entity.User user) =>
        new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = RoleMapper.ToRoleId(user.Role.RoleName),
            Status = user.Status,
            CreatedAt = user.CreatedAt,
        };
}
