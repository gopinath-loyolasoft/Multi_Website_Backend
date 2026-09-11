using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CollegeWebsitePlatform.Api.Middleware;

/// <summary>
/// Action filter that checks whether a specific tenant feature is enabled
/// before allowing the controller action to execute. If the feature is
/// disabled, returns 403 Forbidden with a FEATURE_DISABLED error.
///
/// Usage: [FeatureCheck("NEWS")] on controller or action.
/// SuperAdmin bypasses all feature checks.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class FeatureCheckAttribute : ActionFilterAttribute
{
    private readonly string _featureCode;

    public FeatureCheckAttribute(string featureCode) => _featureCode = featureCode;

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        // SuperAdmin bypasses feature checks
        if (user.IsInRole(CollegeWebsitePlatform.Domain.Constants.UserRole.SuperAdmin))
        {
            await next();
            return;
        }

        // Resolve tenant context
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        if (tenantContext == null || tenantContext.TenantId == Guid.Empty)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                ApiResponse<object>.Fail("Tenant context not resolved."))
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        // Check feature
        var featureRepo = context.HttpContext.RequestServices.GetRequiredService<CollegeWebsitePlatform.Infrastructure.Repositories.Platform.FeatureRepository>();
        var isEnabled = await featureRepo.IsFeatureEnabledForTenantAsync(tenantContext.TenantId, _featureCode);

        if (!isEnabled)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                ApiResponse<object>.Fail($"The '{_featureCode}' feature is not enabled for this college."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireFeatureAttribute : FeatureCheckAttribute
{
    public RequireFeatureAttribute(string featureCode) : base(featureCode) { }
}
