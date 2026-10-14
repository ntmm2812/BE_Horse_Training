namespace SWP.Application.Common;

public static class ServiceErrorCodes
{
    public const string NotFound = "NOT_FOUND";

    public const string Validation = "VALIDATION_ERROR";

    /// <summary> Thao tác không hợp lệ với trạng thái hiện tại (vd. kích hoạt giáo án đã hoàn thành). </summary>
    public const string InvalidStatus = "INVALID_STATUS";

    public const string Conflict = "CONFLICT";

    /// <summary> Thể trạng ngựa không cho phép cường độ yêu cầu. </summary>
    public const string HorseNotFit = "HORSE_NOT_FIT";
}

public class ServiceResult<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorCode { get; init; }

    public IReadOnlyList<string> Details { get; init; } = Array.Empty<string>();

    public static ServiceResult<T> Ok(T data) =>
        new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(string errorCode, string message, IReadOnlyList<string>? details = null) =>
        new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = message,
            Details = details ?? Array.Empty<string>(),
        };

    public static ServiceResult<T> NotFound(string message) => Fail(ServiceErrorCodes.NotFound, message);

    public static ServiceResult<T> Invalid(string message) => Fail(ServiceErrorCodes.Validation, message);

    /// <summary> Chuyển một kết quả lỗi sang kiểu dữ liệu khác, giữ nguyên mã lỗi. </summary>
    public ServiceResult<TOther> AsFailure<TOther>() =>
        ServiceResult<TOther>.Fail(ErrorCode ?? ServiceErrorCodes.Validation, ErrorMessage ?? string.Empty, Details);
}
