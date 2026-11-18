using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IUserRepository
{
    Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsExceptAsync(string email, int exceptUserId, CancellationToken cancellationToken = default);

    Task<int?> GetRoleIdByRoleNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<int?> GetRoleIdByCanonicalRoleIdAsync(string canonicalRoleId, CancellationToken cancellationToken = default);

    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken cancellationToken = default);

    /// <summary> Chủ ngựa (mọi trạng thái Pending/Active/Rejected/…). </summary>
    Task<IReadOnlyList<User>> GetHorseOwnerUsersAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRoleAsync(int userId, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRoleReadAsync(int userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetActiveUsersByCanonicalRoleAsync(
        string canonicalRoleId,
        CancellationToken cancellationToken = default);

    /// <summary> Nhân sự nội bộ (HLV / vet / groom), mọi trạng thái Active/Inactive. </summary>
    Task<IReadOnlyList<User>> GetStaffUsersAsync(CancellationToken cancellationToken = default);
}
