using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SWP.Application.DTOs.Medical;
using SWP.Application.Services.Implement;
using SWP.Domain.Common;

namespace SWP.API.Controllers;

public class InjuryWorkflowExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var failure = context.Exception switch
        {
            InjuryWorkflowException ex => (ex.StatusCode, ex.Message),
            DbUpdateConcurrencyException => (409, "Dữ liệu đã thay đổi. Hãy tải lại trước khi thao tác."),
            _ => (0, "")
        };
        if (failure.Item1 == 0) return;
        context.Result = new ObjectResult(new { message = failure.Item2 }) { StatusCode = failure.Item1 };
        context.ExceptionHandled = true;
    }
}

[ApiController]
[Authorize]
[Route("api/injury-cases")]
[TypeFilter(typeof(InjuryWorkflowExceptionFilter))]
public class InjuryWorkflowController(InjuryWorkflowService service) : ControllerBase
{
    private const string CaseReaders = RoleNames.Veterinarian + "," + RoleNames.HeadTrainer + "," + RoleNames.HorseOwner + "," + RoleNames.ClubManager;
    private const string TaskReaders = RoleNames.Veterinarian + "," + RoleNames.Groom + "," + RoleNames.ClubManager;
    private int ActorId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(claim, out var id) && id > 0 ? id : throw new InjuryWorkflowException(401, "Không xác định được tài khoản đăng nhập.");
        }
    }

    [HttpGet]
    [Authorize(Roles = CaseReaders)]
    public Task<PageDto<InjuryCaseDto>> List([FromQuery] int? horseId, [FromQuery] string? status,
        CancellationToken ct, [FromQuery] int skip = 0, [FromQuery] int take = 30) =>
        service.ListCases(ActorId, horseId, status, skip, take, ct);

    [HttpGet("{id:int}")]
    [Authorize(Roles = CaseReaders)]
    public Task<InjuryCaseDto> Get(int id, CancellationToken ct) => service.GetCase(ActorId, id, ct);

    [HttpGet("{id:int}/timeline")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<PageDto<InjuryEventDto>> Timeline(int id, CancellationToken ct, [FromQuery] int skip = 0, [FromQuery] int take = 30) =>
        service.Timeline(ActorId, id, skip, take, ct);

    [HttpPost]
    [Authorize(Roles = RoleNames.Veterinarian)]
    [ProducesResponseType(typeof(InjuryCaseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(OpenInjuryCaseRequest request, CancellationToken ct)
    {
        var result = await service.CreateCase(ActorId, request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}/summary")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<InjuryCaseDto> Summary(int id, UpdateCaseSummaryRequest request, CancellationToken ct) => service.UpdateSummary(ActorId, id, request, ct);

    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<InjuryCaseDto> Note(int id, CaseNoteRequest request, CancellationToken ct) => service.AddNote(ActorId, id, request, ct);

    [HttpPost("{id:int}/exams")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<InjuryCaseDto> LinkExam(int id, LinkCaseExamRequest request, CancellationToken ct) => service.LinkExam(ActorId, id, request, ct);

    [HttpPost("{id:int}/status")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<InjuryCaseDto> ChangeStatus(int id, CaseStatusRequest request, CancellationToken ct) => service.ChangeStatus(ActorId, id, request, ct);

    [HttpPost("{id:int}/training-locks")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    [ProducesResponseType(typeof(TrainingLockDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Lock(int id, CaseLockRequest request, CancellationToken ct) =>
        StatusCode(201, await service.LockTraining(ActorId, id, request, ct));

    [HttpPost("{id:int}/care-tasks")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    [ProducesResponseType(typeof(MedicalCareDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Assign(int id, AssignMedicalCareRequest request, CancellationToken ct) =>
        StatusCode(201, await service.AssignTask(ActorId, id, request, ct));

    [HttpGet("care-tasks")]
    [Authorize(Roles = TaskReaders)]
    public Task<PageDto<MedicalCareDto>> Tasks(CancellationToken ct, [FromQuery] int? caseId = null,
        [FromQuery] bool overdueOnly = false, [FromQuery] int skip = 0, [FromQuery] int take = 30) =>
        service.ListTasks(ActorId, caseId, overdueOnly, skip, take, ct);

    [HttpGet("care-overview")]
    [Authorize(Roles = RoleNames.Veterinarian + "," + RoleNames.ClubManager)]
    public Task<CareOverviewDto> Overview(CancellationToken ct) => service.Overview(ActorId, ct);

    [HttpGet("assignable-grooms")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<IReadOnlyList<AssignableGroomDto>> Grooms(CancellationToken ct) => service.AssignableGrooms(ActorId, ct);

    [HttpPost("care-tasks/{taskId:int}/start")]
    [Authorize(Roles = RoleNames.Groom)]
    public Task<MedicalCareDto> Start(int taskId, CareResultRequest request, CancellationToken ct) => service.ExecuteTask(ActorId, taskId, "start", request, ct);

    [HttpPost("care-tasks/{taskId:int}/complete")]
    [Authorize(Roles = RoleNames.Groom)]
    public Task<MedicalCareDto> Complete(int taskId, CareResultRequest request, CancellationToken ct) => service.ExecuteTask(ActorId, taskId, "complete", request, ct);

    [HttpPost("care-tasks/{taskId:int}/unable")]
    [Authorize(Roles = RoleNames.Groom)]
    public Task<MedicalCareDto> Unable(int taskId, CareResultRequest request, CancellationToken ct) => service.ExecuteTask(ActorId, taskId, "unable", request, ct);

    [HttpPost("care-tasks/{taskId:int}/cancel")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<MedicalCareDto> Cancel(int taskId, CareResultRequest request, CancellationToken ct) => service.ExecuteTask(ActorId, taskId, "cancel", request, ct);

    [HttpPut("care-tasks/{taskId:int}/assignment")]
    [Authorize(Roles = RoleNames.Veterinarian)]
    public Task<MedicalCareDto> Reassign(int taskId, ReassignCareRequest request, CancellationToken ct) => service.ReassignTask(ActorId, taskId, request, ct);
}
