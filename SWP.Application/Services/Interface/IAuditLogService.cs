using SWP.Application.Common;
using SWP.Application.DTOs.Audit;

namespace SWP.Application.Services.Interface;

public interface IAuditLogService
{
    Task<ServiceResult<bool>> AddAsync(int userId, CreateAuditLogRequestDto request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AuditLogPageDto>> ListAsync(AuditLogQueryDto query, CancellationToken cancellationToken = default);
}
