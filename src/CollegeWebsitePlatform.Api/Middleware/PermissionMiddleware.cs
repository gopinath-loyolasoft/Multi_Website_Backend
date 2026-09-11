using Microsoft.AspNetCore.Authorization;

namespace CollegeWebsitePlatform.Api.Middleware;

/// <summary>
/// ASP.NET Core authorization requirement that checks whether the authenticated
/// user's JWT "permissions" claim contains the required permission code.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }
    public PermissionRequirement(string permissionCode) => PermissionCode = permissionCode;
}

/// <summary>
/// Handles <see cref="PermissionRequirement"/> by inspecting the "permissions" claim
/// on the current user's JWT. SuperAdmin always succeeds.
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Must be authenticated
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return Task.CompletedTask;

        // 2. SuperAdmin bypasses all permission checks
        if (context.User.IsInRole(CollegeWebsitePlatform.Domain.Constants.UserRole.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 3. Check the "permissions" claim list (comma-separated or multi-value)
        var permissionsClaim = context.User.FindAll("permissions")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissionsClaim.Contains(requirement.PermissionCode))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Attribute applied to controller actions to enforce a named permission policy.
/// Usage: [Permission(PermissionCodes.PageManage)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute
{
    private readonly string _permissionCode;
    public PermissionAttribute(string permissionCode) => _permissionCode = permissionCode;

    public override async Task OnActionExecutionAsync(
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context,
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        if (user.IsInRole(CollegeWebsitePlatform.Domain.Constants.UserRole.SuperAdmin))
        {
            await next();
            return;
        }

        var permissionsClaim = user.FindAll("permissions")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!permissionsClaim.Contains(_permissionCode))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                CollegeWebsitePlatform.Application.Common.Models.ApiResponse<object>.Fail(
                    $"You do not have the '{_permissionCode}' permission required to perform this action."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
