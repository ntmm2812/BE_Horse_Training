using System.Globalization;
using SWP.Application.Common;
using SWP.Application.DTOs.Staff;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Application.Services.Implement;

public class StaffService : IStaffService
{
    public const string DefaultStaffPassword = "123456";

    private static readonly HashSet<string> StaffRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleNames.HeadTrainer,
        RoleNames.Veterinarian,
        RoleNames.Groom,
    };

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public StaffService(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<StaffAccountDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetStaffUsersAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<ServiceResult<StaffAccountDto>> GetByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdWithRoleAsync(userId, cancellationToken);
        if (user is null || !TryGetStaffRoleId(user, out _))
        {
            return ServiceResult<StaffAccountDto>.NotFound("Không tìm thấy nhân sự.");
        }

        return ServiceResult<StaffAccountDto>.Ok(Map(user));
    }

    public async Task<ServiceResult<StaffAccountDto>> CreateAsync(
        SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var parsed = await ParseRequestAsync(request, exceptUserId: null, cancellationToken);
        if (!parsed.Success)
        {
            return parsed.AsFailure<StaffAccountDto>();
        }

        var (fullName, email, roleId) = parsed.Data!;
        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = _passwordHasher.Hash(DefaultStaffPassword),
            Status = UserStatuses.Active,
            MustChangePassword = true,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
        };

        await _users.AddAsync(user, cancellationToken);
        var created = await _users.GetByIdWithRoleReadAsync(user.UserId, cancellationToken);
        return ServiceResult<StaffAccountDto>.Ok(Map(created!));
    }

    public async Task<ServiceResult<StaffAccountDto>> UpdateAsync(
        int userId,
        SaveStaffAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdWithRoleAsync(userId, cancellationToken);
        if (user is null || !TryGetStaffRoleId(user, out _))
        {
            return ServiceResult<StaffAccountDto>.NotFound("Không tìm thấy nhân sự.");
        }

        var parsed = await ParseRequestAsync(request, exceptUserId: userId, cancellationToken);
        if (!parsed.Success)
        {
            return parsed.AsFailure<StaffAccountDto>();
        }

        var (fullName, email, roleId) = parsed.Data!;
        user.FullName = fullName;
        user.Email = email;
        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        var updated = await _users.GetByIdWithRoleReadAsync(userId, cancellationToken);
        return ServiceResult<StaffAccountDto>.Ok(Map(updated!));
    }

    public async Task<ServiceResult<StaffAccountDto>> SetActiveAsync(
        int userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdWithRoleAsync(userId, cancellationToken);
        if (user is null || !TryGetStaffRoleId(user, out _))
        {
            return ServiceResult<StaffAccountDto>.NotFound("Không tìm thấy nhân sự.");
        }

        user.Status = isActive ? UserStatuses.Active : UserStatuses.Inactive;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        var updated = await _users.GetByIdWithRoleReadAsync(userId, cancellationToken);
        return ServiceResult<StaffAccountDto>.Ok(Map(updated!));
    }

    private async Task<ServiceResult<(string FullName, string Email, int RoleId)>> ParseRequestAsync(
        SaveStaffAccountRequestDto request,
        int? exceptUserId,
        CancellationToken cancellationToken)
    {
        var fullName = (request.FullName ?? string.Empty).Trim();
        if (fullName.Length is < 1 or > 100)
        {
            return ServiceResult<(string, string, int)>.Invalid("Vui lòng nhập họ và tên.");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 255)
        {
            return ServiceResult<(string, string, int)>.Invalid("Email không hợp lệ.");
        }

        var roleIdRaw = (request.RoleId ?? string.Empty).Trim().ToLowerInvariant();
        if (!StaffRoles.Contains(roleIdRaw))
        {
            return ServiceResult<(string, string, int)>.Invalid("Vai trò nhân sự không hợp lệ.");
        }

        var emailTaken = exceptUserId is null
            ? await _users.EmailExistsAsync(email, cancellationToken)
            : await _users.EmailExistsExceptAsync(email, exceptUserId.Value, cancellationToken);
        if (emailTaken)
        {
            return ServiceResult<(string, string, int)>.Fail(ServiceErrorCodes.Conflict, "Email này đã có tài khoản.");
        }

        var roleId = await _users.GetRoleIdByCanonicalRoleIdAsync(roleIdRaw, cancellationToken);
        if (roleId is null)
        {
            return ServiceResult<(string, string, int)>.Invalid("Không tìm thấy vai trò trong hệ thống.");
        }

        return ServiceResult<(string, string, int)>.Ok((fullName, email, roleId.Value));
    }

    private static bool TryGetStaffRoleId(User user, out string roleId)
    {
        roleId = string.Empty;
        if (user.Role is null || !RoleMapper.TryMapToCanonicalRoleId(user.Role.RoleName, out var mapped))
        {
            return false;
        }

        if (!StaffRoles.Contains(mapped))
        {
            return false;
        }

        roleId = mapped;
        return true;
    }

    private static StaffAccountDto Map(User user)
    {
        RoleMapper.TryMapToCanonicalRoleId(user.Role.RoleName, out var roleId);
        return new StaffAccountDto
        {
            UserID = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = roleId ?? string.Empty,
            IsActive = string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase),
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
    }
}
