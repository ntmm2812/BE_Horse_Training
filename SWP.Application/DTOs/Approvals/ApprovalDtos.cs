namespace SWP.Application.DTOs.Approvals;

public record PlanDecisionRequest(string Status, string? Note, string? Version);
public record RaceProposalRequest(int HorseId, int RaceId, string? Note);
public record RaceDecisionRequest(bool Approve, string? Note, string? Version);
public record ReadPlanNoticesRequest(int? PlanId);
public record ApprovalHistory(string Step, string Action, string By, string Note, DateTime At);
