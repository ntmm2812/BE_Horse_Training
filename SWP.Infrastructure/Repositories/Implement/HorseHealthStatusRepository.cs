using Microsoft.EntityFrameworkCore;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Repositories.Interface;

namespace SWP.Infrastructure.Repositories.Implement;

public class HorseHealthStatusRepository : IHorseHealthStatusRepository
{
    private readonly ApplicationDbContext _context;

    public HorseHealthStatusRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HorseHealthStatus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.HorseHealthStatuses
            .AsNoTracking()
            .OrderBy(s => s.HorseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HorseHealthStatus>> GetByOwnerUserIdAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from status in _context.HorseHealthStatuses.AsNoTracking()
            join horse in _context.Horses.AsNoTracking() on status.HorseId equals horse.HorseId
            where horse.UserId == ownerUserId
            orderby status.HorseId
            select status).ToListAsync(cancellationToken);
    }

    public async Task<HorseHealthStatus?> GetByHorseIdAsync(int horseId, CancellationToken cancellationToken = default)
    {
        return await _context.HorseHealthStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HorseId == horseId, cancellationToken);
    }

    public async Task UpsertAsync(int horseId, string trangThai, CancellationToken cancellationToken = default)
    {
        if (!HorseHealthStatuses.IsValid(trangThai))
        {
            throw new InvalidOperationException("Trạng thái sức khỏe không hợp lệ.");
        }

        var normalized = trangThai.Trim();
        var existing = await _context.HorseHealthStatuses
            .FirstOrDefaultAsync(s => s.HorseId == horseId, cancellationToken);

        if (existing is null)
        {
            _context.HorseHealthStatuses.Add(new HorseHealthStatus
            {
                HorseId = horseId,
                TrangThai = normalized,
                NgayCapNhat = DateTime.UtcNow,
            });
        }
        else
        {
            existing.TrangThai = normalized;
            existing.NgayCapNhat = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
