using SWP.Application.DTOs.Care;

namespace SWP.Application.Services.Interface;

public interface ICareService
{
    Task<List<CareTaskDto>> GetTasks(int groomId, CancellationToken ct);
    Task<CareTaskDto> CompleteTask(int id, int groomId, CancellationToken ct);
    Task<List<StableAssignmentDto>> GetStableAssignments(CancellationToken ct);
    Task<Dictionary<int, string>> GetStableNumbers(CancellationToken ct);
    Task<List<FeedingPlanDto>> GetFeedingPlans(CancellationToken ct);
    Task<List<CareSupplyDto>> GetSupplies(CancellationToken ct);
    Task<List<IncidentReportDto>> GetIncidents(int groomId, CancellationToken ct);
    Task<List<IncidentReportDto>> GetIncidentsForVet(int? horseId, CancellationToken ct);
    Task<IncidentReportDto> CreateIncident(CreateIncidentReportRequest request, int groomId, CancellationToken ct);
}
