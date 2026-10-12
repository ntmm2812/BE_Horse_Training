using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IUserRepository
{
    Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<int?> GetRoleIdByRoleNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<int?> GetRoleIdByCanonicalRoleIdAsync(string canonicalRoleId, CancellationToken cancellationToken = default);

    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRoleAsync(int userId, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
