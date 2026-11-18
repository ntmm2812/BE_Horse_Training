using System;
using System.Collections.Generic;

namespace SWP.Domain.Models.Entity;

public class AuditLog
{
    public long LogId { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public int? RecordId { get; set; }

    /// <summary> Tên đối tượng lúc ghi (vd tên ngựa) để hiển thị cả khi bản ghi đã bị xóa. </summary>
    public string? TargetLabel { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Ipaddress { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
