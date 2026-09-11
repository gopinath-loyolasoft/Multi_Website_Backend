using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace CollegeWebsitePlatform.Api.Middleware;

/// <summary>
/// Authorization filter applied to all CollegeAdmin CMS controllers (api/v1/admin/**).
/// 
/// Rule:
///   - SuperAdmin → always allowed (no tenant check needed)
///   - CollegeAdmin / Editor → JWT tenant_id claim MUST equal the resolved ITenantContext.TenantId
///     Otherwise → 403 Forbidden (prevents RNC admin from accessing KTS data)
/// </summary>
public class TenantAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ITenantContext _tenantContext;

    public TenantAuthorizationFilter(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // 1. Must be authenticated
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(
                ApiResponse<object>.Fail("Authentication required."));
            return Task.CompletedTask;
        }

        // 2. SuperAdmin bypasses tenant isolation
        if (user.IsInRole(UserRole.SuperAdmin))
            return Task.CompletedTask;

        // 3. Tenant must be resolved from domain
        if (!_tenantContext.IsResolved)
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail("Tenant could not be resolved for this request. Ensure the correct domain or X-Tenant-Domain header is set."))
            { StatusCode = StatusCodes.Status400BadRequest };
            return Task.CompletedTask;
        }

        // 4. JWT tenant_id must match the resolved ITenantContext.TenantId
        var jwtTenantIdClaim = user.FindFirstValue("tenant_id");

        if (string.IsNullOrWhiteSpace(jwtTenantIdClaim)
            || !Guid.TryParse(jwtTenantIdClaim, out var jwtTenantId)
            || jwtTenantId != _tenantContext.TenantId)
        {
            context.Result = new ObjectResult(
                ApiResponse<object>.Fail(
                    $"Access denied. Your account does not belong to this college. " +
                    $"You are authorized for a different tenant."))
            { StatusCode = StatusCodes.Status403Forbidden };
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
