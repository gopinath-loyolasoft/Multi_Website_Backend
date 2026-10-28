using CollegeWebsitePlatform.Application.Common.Models;

namespace CollegeWebsitePlatform.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail(ex.Message, ErrorCodes.InvalidRequest);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail(ex.Message, ErrorCodes.NotFound);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail(ex.Message, ErrorCodes.Forbidden);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Npgsql.PostgresException pgEx)
        {
            _logger.LogWarning(pgEx, "PostgreSQL constraint error: {SqlState} - {Message}", pgEx.SqlState, pgEx.Message);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            string friendlyMessage = pgEx.SqlState switch
            {
                "23503" => "Cannot delete this record because other records (such as pages, domains, users, or modules) depend on it. Please remove or reassign linked data first.",
                "23505" => "A record with this name, code, or identifier already exists.",
                _ => $"Database operation rejected: {pgEx.MessageText}"
            };

            var response = ApiResponse<object>.Fail(friendlyMessage, ErrorCodes.InvalidRequest);
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing request: {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            var detailMsg = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An unexpected server error occurred.";
            var response = ApiResponse<object>.Fail(detailMsg, ErrorCodes.InternalError, new List<string> { ex.Message });
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
