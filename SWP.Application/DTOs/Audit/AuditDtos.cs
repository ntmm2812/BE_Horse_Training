namespace SWP.Application.DTOs.Audit;

/// <summary> Ghi một dòng nhật ký. Người thực hiện luôn lấy từ JWT, không nhận từ client. </summary>
public class CreateAuditLogRequestDto
{
    public string? Action { get; set; }

    public string? TargetTable { get; set; }

    public int? TargetId { get; set; }

    public string? TargetLabel { get; set; }
}

public class AuditLogQueryDto
{
    public int? UserId { get; set; }

    /// <summary> Ngày bắt đầu (tính cả ngày này, theo giờ địa phương). </summary>
    public DateOnly? From { get; set; }

    /// <summary> Ngày kết thúc (tính cả ngày này, theo giờ địa phương). </summary>
    public DateOnly? To { get; set; }

    public string? Keyword { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public class AuditLogItemDto
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string? ActorRole { get; set; }

    public string Action { get; set; } = string.Empty;

    public string TargetTable { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    public string? TargetLabel { get; set; }

    /// <summary> Giờ địa phương, dạng yyyy-MM-ddTHH:mm:ss. </summary>
    public string CreatedAt { get; set; } = string.Empty;
}

public class AuditLogPageDto
{
    public IReadOnlyList<AuditLogItemDto> Items { get; set; } = [];

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
