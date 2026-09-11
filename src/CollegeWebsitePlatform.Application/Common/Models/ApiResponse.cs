namespace CollegeWebsitePlatform.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, string? errorCode = null, List<string>? errors = null) =>
        new() { Success = false, Message = message, ErrorCode = errorCode, Errors = errors };
}
