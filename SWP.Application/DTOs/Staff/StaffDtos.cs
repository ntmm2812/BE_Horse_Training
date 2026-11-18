namespace SWP.Application.DTOs.Staff;

public class StaffAccountDto
{
    public int UserID { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool MustChangePassword { get; set; }

    public string? CreatedAt { get; set; }
}

public class SaveStaffAccountRequestDto
{
    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string RoleId { get; set; } = null!;
}

public class SetStaffActiveRequestDto
{
    public bool IsActive { get; set; }
}
