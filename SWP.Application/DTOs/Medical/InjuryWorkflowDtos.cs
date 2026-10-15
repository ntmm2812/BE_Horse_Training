namespace SWP.Application.DTOs.Medical;

public class OpenInjuryCaseRequest
{
    public int HorseId { get; set; }
    public string Title { get; set; } = "";
    public string BodyRegion { get; set; } = "";
    public string Side { get; set; } = "Unspecified";
    public string Severity { get; set; } = "Mild";
    public string ClinicalNotes { get; set; } = "";
    public string OwnerSummary { get; set; } = "";
    public string TrainingGuidance { get; set; } = "";
    public DateTimeOffset? ReviewDueAt { get; set; }
}

public class CaseVersionRequest
{
    public int Version { get; set; }
}

public class UpdateCaseSummaryRequest : CaseVersionRequest
{
    public string OwnerSummary { get; set; } = "";
    public string TrainingGuidance { get; set; } = "";
    public DateTimeOffset? ReviewDueAt { get; set; }
    public string Reason { get; set; } = "";
}

public class CaseStatusRequest : CaseVersionRequest
{
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public int? ReexamRecordId { get; set; }
}

public class LinkCaseExamRequest : CaseVersionRequest
{
    public int MedicalRecordId { get; set; }
}

public class CaseNoteRequest : CaseVersionRequest
{
    public string Note { get; set; } = "";
}

public class CaseLockRequest : CaseVersionRequest
{
    public string Reason { get; set; } = "";
}

public class AssignMedicalCareRequest : CaseVersionRequest
{
    public int GroomId { get; set; }
    public string Title { get; set; } = "";
    public string Instructions { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public DateTimeOffset DueAt { get; set; }
}

public class CareResultRequest : CaseVersionRequest
{
    public string Notes { get; set; } = "";
}

public class ReassignCareRequest : CareResultRequest
{
    public int GroomId { get; set; }
    public DateTimeOffset DueAt { get; set; }
}

public record PageDto<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);

public record InjuryCaseDto(int Id, int HorseId, string HorseName, string Title, string BodyRegion,
    string Side, string Severity, string Status, string OwnerSummary, string? TrainingGuidance,
    DateTime CreatedAt, DateTime? ReviewDueAt, DateTime? ClosedAt, bool ReviewOverdue, int Version);

public record InjuryEventDto(int Id, string Kind, int ActorUserId, DateTime CreatedAt, string DetailsJson, int? MedicalRecordId);

public record MedicalCareDto(int Id, int InjuryCaseId, int HorseId, string HorseName, string Title, string? Instructions,
    int? AssignedToUserId, string? AssignedToName, string Status, string? Priority, DateTime? DueAt,
    DateTime? CompletedAt, string? ResultNotes, bool IsOverdue, double OverdueHours, int Version);

public record CareOverviewDto(int Pending, int InProgress, int UnableToComplete, int Overdue, int ReviewsOverdue);

public record AssignableGroomDto(int UserId, string FullName);
