using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IUserRepository
{
    Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default);
}
