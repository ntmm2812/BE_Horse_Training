using Microsoft.EntityFrameworkCore;
using SWP.Application.Common;
using SWP.Application.DTOs.Audit;
using SWP.Application.Services.Interface;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;

namespace SWP.Application.Services.Implement;

public class AuditLogService(ApplicationDbContext db, TimeProvider clock) : IAuditLogService
{
    public const int MaxActionLength = 300;
    public const int MaxLabelLength = 200;
    public const int MaxPageSize = 100;

    /// <summary> Các bảng bị tác động mà FE được phép ghi nhật ký. </summary>
    public static readonly string[] AllowedTables =
    [
        "USER", "HORSE", "HEALTHSTATUS", "TRAININGPLAN", "TRAININGSESSION", "TRAININGLOCK",
        "VACCINATIONSCHEDULE", "CARETASK", "INCIDENTREPORT", "RACEREGISTRATION", "EXPENSE", "SUPPLY",
    ];

    public async Task<ServiceResult<bool>> AddAsync(int userId, CreateAuditLogRequestDto request, CancellationToken cancellationToken = default)
    {
        var action = request.Action?.Trim();
        if (string.IsNullOrEmpty(action) || action.Length > MaxActionLength)
        {
            return ServiceResult<bool>.Invalid($"Hành động là bắt buộc và tối đa {MaxActionLength} ký tự");
        }

        var table = request.TargetTable?.Trim().ToUpperInvariant();
        if (table is null || !AllowedTables.Contains(table))
        {
            return ServiceResult<bool>.Invalid("Đối tượng tác động không hợp lệ");
        }

        var label = request.TargetLabel?.Trim();
        if (label is { Length: > MaxLabelLength })
        {
            return ServiceResult<bool>.Invalid($"Tên đối tượng tối đa {MaxLabelLength} ký tự");
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            TableName = table,
            RecordId = request.TargetId,
            TargetLabel = string.IsNullOrEmpty(label) ? null : label,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
        });
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<AuditLogPageDto>> ListAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default)
    {
        if (query.From is not null && query.To is not null && query.From > query.To)
        {
            return ServiceResult<AuditLogPageDto>.Invalid("Khoảng thời gian không hợp lệ");
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var logs = db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.UserId is int userId)
        {
            logs = logs.Where(l => l.UserId == userId);
        }

        if (query.From is DateOnly from)
        {
            var start = ToUtc(from.ToDateTime(TimeOnly.MinValue));
            logs = logs.Where(l => l.CreatedAt >= start);
        }

        if (query.To is DateOnly to)
        {
            var end = ToUtc(to.AddDays(1).ToDateTime(TimeOnly.MinValue));
            logs = logs.Where(l => l.CreatedAt < end);
        }

        var keyword = query.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            logs = logs.Where(l => l.Action.Contains(keyword)
                || (l.TargetLabel != null && l.TargetLabel.Contains(keyword))
                || (l.User != null && l.User.FullName.Contains(keyword)));
        }

        var total = await logs.CountAsync(cancellationToken);
        var rows = await logs
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.LogId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new
            {
                l.LogId,
                l.UserId,
                ActorName = l.User != null ? l.User.FullName : null,
                ActorRole = l.User != null ? l.User.Role.RoleName : null,
                l.Action,
                l.TableName,
                l.RecordId,
                l.TargetLabel,
                l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return ServiceResult<AuditLogPageDto>.Ok(new AuditLogPageDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = rows.Select(r => new AuditLogItemDto
            {
                LogId = r.LogId,
                UserId = r.UserId,
                ActorName = r.ActorName ?? (r.UserId is int id ? $"Người dùng #{id}" : "Hệ thống"),
                ActorRole = r.ActorRole,
                Action = r.Action,
                TargetTable = r.TableName,
                TargetId = r.RecordId,
                TargetLabel = r.TargetLabel,
                CreatedAt = ToLocal(r.CreatedAt).ToString("yyyy-MM-dd'T'HH:mm:ss"),
            }).ToList(),
        });
    }

    private DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), clock.LocalTimeZone);

    private DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), clock.LocalTimeZone);
}
