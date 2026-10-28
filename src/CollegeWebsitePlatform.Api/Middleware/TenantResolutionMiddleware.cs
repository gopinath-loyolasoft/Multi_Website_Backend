using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

namespace CollegeWebsitePlatform.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, TenantRepository tenantRepo)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        // Bypass routes that do not require a tenant
        if (path == "" ||
            path == "/" ||
            path == "/health" ||
            path.StartsWith("/health") ||
            path.StartsWith("/api/v1/health") ||
            path.StartsWith("/api/v1/auth") ||
            path.StartsWith("/api/v1/superadmin") ||
            path.StartsWith("/api/v1/admin/tenants") ||
            path.StartsWith("/api/v1/platform") ||
            path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // 1. Resolve host/domain from Host header, X-Tenant-Domain header, or query param
        string? domain = null;

        if (context.Request.Headers.TryGetValue("X-Tenant-Domain", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            domain = headerVal.ToString();
        }
        else if (context.Request.Query.TryGetValue("tenantDomain", out var queryVal) && !string.IsNullOrWhiteSpace(queryVal))
        {
            domain = queryVal.ToString();
        }
        else if (context.Request.Host.HasValue)
        {
            domain = context.Request.Host.Host;
        }

        // Clean domain: strip protocol, slashes, port (e.g., https://kts.localhost:3000/ -> kts.localhost)
        if (!string.IsNullOrWhiteSpace(domain))
        {
            domain = domain.Replace("http://", "").Replace("https://", "");
            var slashIdx = domain.IndexOf('/');
            if (slashIdx >= 0)
            {
                domain = domain.Substring(0, slashIdx);
            }
            var colonIdx = domain.IndexOf(':');
            if (colonIdx >= 0)
            {
                domain = domain.Substring(0, colonIdx);
            }
            domain = domain.Trim().ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(domain) || domain == "localhost" || domain == "127.0.0.1")
        {
            // Default to gp.localhost for raw localhost dev access if not specified
            domain = "gp.localhost";
        }

        // 2. Query tenant by domain with smart fallbacks
        var tenant = await tenantRepo.GetByDomainAsync(domain);

        if (tenant == null && domain.EndsWith(".loyolasoft.com"))
        {
            var prefix = domain.Replace(".loyolasoft.com", "");
            tenant = await tenantRepo.GetByCodeAsync(prefix) 
                     ?? await tenantRepo.GetBySlugAsync(prefix);
        }

        if (tenant == null && domain.EndsWith(".localhost.com"))
        {
            var altDomain = domain.Replace(".localhost.com", ".localhost");
            tenant = await tenantRepo.GetByDomainAsync(altDomain);
        }

        if (tenant == null && domain.EndsWith(".localhost"))
        {
            var altDomain = domain + ".com";
            tenant = await tenantRepo.GetByDomainAsync(altDomain);
        }

        if (tenant == null && domain.Contains('.'))
        {
            var prefix = domain.Split('.')[0];
            tenant = await tenantRepo.GetByCodeAsync(prefix)
                     ?? await tenantRepo.GetBySlugAsync(prefix);
        }

        // Fallback ONLY for raw localhost/127.0.0.1 development when no specific subdomain was supplied
        if (tenant == null && (domain == "gp.localhost" || domain == "localhost" || domain == "127.0.0.1"))
        {
            tenant = (await tenantRepo.GetAllAsync()).FirstOrDefault();
        }

        if (tenant == null || tenant.IsDeleted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail($"College website for '{domain}' is not available or has been deleted.");
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // 3. Status checks for DELETED, INACTIVE, and SUSPENDED colleges
        if (string.Equals(tenant.Status, "DELETED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tenant.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            var response = ApiResponse<object>.Fail($"College '{tenant.Name}' is no longer active on this platform.");
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        if (string.Equals(tenant.Status, "SUSPENDED", StringComparison.OrdinalIgnoreCase))
        {
            if (path.StartsWith("/api/v1/site") || path.StartsWith("/api/v1/admin"))
            {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                context.Response.ContentType = "application/json";
                var response = ApiResponse<object>.Fail($"College '{tenant.Name}' is currently SUSPENDED by the platform administrator.");
                await context.Response.WriteAsJsonAsync(response);
                return;
            }
        }

        // 4. Set Scoped ITenantContext
        tenantContext.SetTenant(
            tenant.Id,
            tenant.TenantCode,
            tenant.Name,
            tenant.Slug,
            tenant.DbConnectionString,
            tenant.DefaultThemeId,
            tenant.TemplateCode ?? "ENGINEERING_MODERN"
        );

        await _next(context);
    }
}
