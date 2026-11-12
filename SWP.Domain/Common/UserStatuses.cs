namespace SWP.Domain.Common;

public static class UserStatuses
{
    public const string Active = "Active";

    public const string Pending = "Pending";

    /// <summary> Tài khoản bị khóa bởi Quản lý CLB — không đăng nhập được. </summary>
    public const string Inactive = "Inactive";

    /// <summary> Chủ ngựa bị từ chối duyệt — không đăng nhập được. </summary>
    public const string Rejected = "Rejected";
}
