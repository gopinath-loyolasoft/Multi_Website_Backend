namespace CollegeWebsitePlatform.Api.Middleware;

/// <summary>
/// Attaches a unique X-Correlation-ID to every request so logs can be traced end-to-end.
/// Reads from the incoming header if the client sends one; otherwise generates a new GUID.
/// The same value is echoed back in the response header.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Read or generate the Correlation ID
        var correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existing)
            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        // 2. Store in HttpContext.Items for downstream use
        context.Items["CorrelationId"] = correlationId;

        // 3. Echo back in response header
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
