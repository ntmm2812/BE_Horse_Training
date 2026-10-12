using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == email, cancellationToken);
    }

    public async Task<int?> GetRoleIdByRoleNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (RoleMapper.TryMapToCanonicalRoleId(roleName, out var canonical))
        {
            return await GetRoleIdByCanonicalRoleIdAsync(canonical, cancellationToken);
        }

        var roles = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);
        var match = roles.FirstOrDefault(r =>
            string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

        return match?.RoleId;
    }

    public async Task<int?> GetRoleIdByCanonicalRoleIdAsync(
        string canonicalRoleId,
        CancellationToken cancellationToken = default)
    {
        if (!RoleMapper.IsCanonicalRoleId(canonicalRoleId))
        {
            return null;
        }

        var target = canonicalRoleId.Trim().ToLowerInvariant();
        var roles = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (!RoleMapper.TryMapToCanonicalRoleId(role.RoleName, out var mapped))
            {
                continue;
            }

            if (string.Equals(mapped, target, StringComparison.OrdinalIgnoreCase))
            {
                return role.RoleId;
            }
        }

        return null;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<IReadOnlyList<User>> GetPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.Status.ToLower() == UserStatuses.Pending.ToLower())
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdWithRoleAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
