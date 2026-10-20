using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SWP.Application.DTOs.Care;
using SWP.Application.Services.Implement;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

public sealed class CareExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception switch
        {
            CareException ex => (ex.StatusCode, ex.Message),
            DbUpdateConcurrencyException => (409, "Công việc đã thay đổi. Vui lòng tải lại."),
            _ => (0, "")
        };
        if (error.Item1 == 0) return;
        context.Result = new ObjectResult(new { message = error.Item2 }) { StatusCode = error.Item1 };
        context.ExceptionHandled = true;
    }
}

[ApiController]
[Route("api/care")]
[Authorize(Roles = RoleNames.Groom)]
[TypeFilter(typeof(CareExceptionFilter))]
public sealed class CareController(CareService service) : ControllerBase
{
    private int GroomId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
        ? id : throw new CareException(401, "Không xác định được tài khoản đăng nhập.");

    private int OwnId(int? groomId)
    {
        var id = GroomId;
        if (groomId.HasValue && groomId != id)
            throw new CareException(403, "Bạn chỉ được xem dữ liệu chăm sóc của chính mình.");
        return id;
    }

    [HttpGet("tasks")]
    public Task<List<CareTaskDto>> Tasks(CancellationToken ct, [FromQuery] int? groomId = null) => service.GetTasks(OwnId(groomId), ct);

    [HttpPost("tasks/{taskId:int}/complete")]
    public Task<CareTaskDto> Complete(int taskId, CancellationToken ct) => service.CompleteTask(taskId, GroomId, ct);

    [HttpGet("stable-assignments")]
    public Task<List<StableAssignmentDto>> Stables(CancellationToken ct) => service.GetStableAssignments(ct);

    [HttpGet("stable-numbers")]
    public Task<Dictionary<int, string>> StableNumbers(CancellationToken ct) => service.GetStableNumbers(ct);

    [HttpGet("feeding-plans")]
    public Task<List<FeedingPlanDto>> Feeding(CancellationToken ct) => service.GetFeedingPlans(ct);

    [HttpGet("supplies")]
    public Task<List<CareSupplyDto>> Supplies(CancellationToken ct) => service.GetSupplies(ct);

    [HttpGet("incident-reports")]
    public Task<List<IncidentReportDto>> Incidents(CancellationToken ct, [FromQuery] int? groomId = null) => service.GetIncidents(OwnId(groomId), ct);

    [HttpPost("incident-reports")]
    [RequestSizeLimit(8 * 1024 * 1024)]
    public async Task<IActionResult> Report(CreateIncidentReportRequest request, CancellationToken ct) =>
        StatusCode(201, await service.CreateIncident(request, GroomId, ct));
}
