using System.Security.Claims;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CollegeWebsitePlatform.Api.Middleware;

namespace CollegeWebsitePlatform.Api.Controllers.SuperAdmin;

[ApiController]
[Route("api/v1/superadmin")]
[Authorize(Roles = "SuperAdmin")]
[Permission(PermissionCodes.TenantManage)]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;

    public SuperAdminController(ISuperAdminService superAdminService) => _superAdminService = superAdminService;

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "superadmin";

    // -------------------------------------------------------------
    // Dashboard Stats
    // -------------------------------------------------------------

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<SuperAdminDashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _superAdminService.GetDashboardStatsAsync();
        return Ok(ApiResponse<SuperAdminDashboardStatsDto>.Ok(stats));
    }

    // -------------------------------------------------------------
    // Colleges Management
    // -------------------------------------------------------------

    [HttpGet("colleges")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetColleges()
    {
        var list = await _superAdminService.GetCollegesAsync();
        return Ok(ApiResponse<IEnumerable<TenantDto>>.Ok(list));
    }

    [HttpGet("colleges/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CollegeDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollegeById([FromRoute] Guid id)
    {
        var details = await _superAdminService.GetCollegeDetailsAsync(id);
        if (details == null) return NotFound(ApiResponse<object>.Fail("College not found"));
        return Ok(ApiResponse<CollegeDetailsDto>.Ok(details));
    }

    [HttpGet("colleges/{id}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollegeSettings([FromRoute] Guid id)
    {
        var settings = await _superAdminService.GetCollegeSettingsAsync(id);
        return Ok(ApiResponse<TenantSettingsDto>.Ok(settings));
    }

    [HttpPut("colleges/{id}/settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCollegeSettings([FromRoute] Guid id, [FromBody] UpdateTenantSettingsRequest request)
    {
        var updated = await _superAdminService.UpdateCollegeSettingsAsync(id, request, CurrentUser);
        return Ok(ApiResponse<TenantSettingsDto>.Ok(updated, "College branding & settings updated successfully"));
    }

    [HttpPost("colleges")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCollege([FromBody] CreateCollegeWorkflowRequest request)
    {
        var created = await _superAdminService.CreateCollegeWorkflowAsync(request, CurrentUser);
        return Created($"/api/v1/superadmin/colleges/{created.Id}", ApiResponse<TenantDto>.Ok(created, "College and database provisioned successfully"));
    }

    [HttpPut("colleges/{id}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCollege([FromRoute] Guid id, [FromBody] UpdateCollegeRequest request)
    {
        var updated = await _superAdminService.UpdateCollegeAsync(id, request, CurrentUser);
        return Ok(ApiResponse<TenantDto>.Ok(updated, "College updated successfully"));
    }

    [HttpPost("colleges/{id}/activate")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActivateCollege([FromRoute] Guid id)
    {
        var success = await _superAdminService.ActivateCollegeAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "College activated"));
    }

    [HttpPost("colleges/{id}/suspend")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuspendCollege([FromRoute] Guid id)
    {
        var success = await _superAdminService.SuspendCollegeAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "College suspended"));
    }

    [HttpDelete("colleges/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCollege([FromRoute] Guid id, [FromQuery] bool dropDatabase = false)
    {
        var success = await _superAdminService.DeleteCollegeAsync(id, dropDatabase, CurrentUser);
        if (!success) return NotFound(ApiResponse<object>.Fail("College not found or could not be deleted"));
        return Ok(ApiResponse<bool>.Ok(true, "College and associated data deleted successfully"));
    }

    // -------------------------------------------------------------
    // Domains Management
    // -------------------------------------------------------------

    [HttpGet("domains")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DomainDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDomains()
    {
        var list = await _superAdminService.GetDomainsAsync();
        return Ok(ApiResponse<IEnumerable<DomainDto>>.Ok(list));
    }

    [HttpPost("domains")]
    [ProducesResponseType(typeof(ApiResponse<DomainDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDomain([FromBody] CreateDomainRequest request)
    {
        var created = await _superAdminService.CreateDomainAsync(request, CurrentUser);
        return Created($"/api/v1/superadmin/domains/{created.Id}", ApiResponse<DomainDto>.Ok(created, "Domain mapped"));
    }

    [HttpPut("domains/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDomain([FromRoute] Guid id, [FromBody] UpdateDomainRequest request)
    {
        var success = await _superAdminService.UpdateDomainAsync(id, request, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Domain updated"));
    }

    [HttpDelete("domains/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDomain([FromRoute] Guid id)
    {
        var success = await _superAdminService.DeleteDomainAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Domain deleted"));
    }

    [HttpPost("domains/{id}/verify")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyDomain([FromRoute] Guid id)
    {
        var success = await _superAdminService.VerifyDomainAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Domain DNS verified successfully"));
    }

    [HttpPost("domains/{id}/set-primary")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPrimaryDomain([FromRoute] Guid id)
    {
        var success = await _superAdminService.SetPrimaryDomainAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Primary domain updated"));
    }

    // -------------------------------------------------------------
    // Databases & Migrations
    // -------------------------------------------------------------

    [HttpGet("databases")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DatabaseRegistryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabases()
    {
        var list = await _superAdminService.GetDatabasesAsync();
        return Ok(ApiResponse<IEnumerable<DatabaseRegistryDto>>.Ok(list));
    }

    [HttpGet("databases/{tenantId}")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseRegistryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDatabaseByTenant([FromRoute] Guid tenantId)
    {
        var db = await _superAdminService.GetDatabaseByTenantIdAsync(tenantId);
        return Ok(ApiResponse<DatabaseRegistryDto>.Ok(db!));
    }

    [HttpGet("databases/{tenantId}/health")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseRegistryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckDatabaseHealth([FromRoute] Guid tenantId)
    {
        var db = await _superAdminService.VerifyDatabaseAsync(tenantId, CurrentUser);
        return Ok(ApiResponse<DatabaseRegistryDto>.Ok(db));
    }

    [HttpPost("databases/{tenantId}/provision")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseRegistryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProvisionDatabase([FromRoute] Guid tenantId)
    {
        var db = await _superAdminService.ProvisionDatabaseAsync(tenantId, CurrentUser);
        return Ok(ApiResponse<DatabaseRegistryDto>.Ok(db, "Database provisioned"));
    }

    [HttpPost("databases/{tenantId}/verify")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseRegistryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyDatabase([FromRoute] Guid tenantId)
    {
        var db = await _superAdminService.VerifyDatabaseAsync(tenantId, CurrentUser);
        return Ok(ApiResponse<DatabaseRegistryDto>.Ok(db, "Database verified"));
    }

    [HttpGet("migrations")]
    [ProducesResponseType(typeof(ApiResponse<List<MigrationHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMigrations()
    {
        var list = await _superAdminService.GetMigrationsAsync();
        return Ok(ApiResponse<List<MigrationHistoryDto>>.Ok(list));
    }

    [HttpGet("migrations/{tenantId}")]
    [ProducesResponseType(typeof(ApiResponse<MigrationStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMigrationHistoryForTenant([FromRoute] Guid tenantId)
    {
        var status = await _superAdminService.GetMigrationStatusForTenantAsync(tenantId);
        return Ok(ApiResponse<MigrationStatusDto>.Ok(status));
    }

    [HttpGet("migrations/{tenantId}/status")]
    [ProducesResponseType(typeof(ApiResponse<MigrationStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMigrationStatusForTenant([FromRoute] Guid tenantId)
    {
        var status = await _superAdminService.GetMigrationStatusForTenantAsync(tenantId);
        return Ok(ApiResponse<MigrationStatusDto>.Ok(status));
    }

    [HttpPost("migrations/{tenantId}/migrate")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseRegistryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MigrateTenantDatabase([FromRoute] Guid tenantId)
    {
        var db = await _superAdminService.MigrateTenantDatabaseAsync(tenantId, CurrentUser);
        return Ok(ApiResponse<DatabaseRegistryDto>.Ok(db, "Tenant database migrated to latest schema"));
    }

    // -------------------------------------------------------------
    // Themes Management
    // -------------------------------------------------------------

    [HttpGet("themes")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ThemeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemes()
    {
        var list = await _superAdminService.GetThemesAsync();
        return Ok(ApiResponse<IEnumerable<ThemeDto>>.Ok(list));
    }

    [HttpGet("themes/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ThemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemeById([FromRoute] Guid id)
    {
        var theme = await _superAdminService.GetThemeByIdAsync(id);
        if (theme == null) return NotFound(ApiResponse<object>.Fail("Theme not found"));
        return Ok(ApiResponse<ThemeDto>.Ok(theme));
    }

    [HttpPost("themes")]
    [ProducesResponseType(typeof(ApiResponse<ThemeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTheme([FromBody] CreateThemeRequest request)
    {
        var created = await _superAdminService.CreateThemeAsync(request, CurrentUser);
        return Created($"/api/v1/superadmin/themes/{created.Id}", ApiResponse<ThemeDto>.Ok(created, "Theme registered"));
    }

    [HttpPut("themes/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTheme([FromRoute] Guid id, [FromBody] UpdateThemeRequest request)
    {
        var success = await _superAdminService.UpdateThemeAsync(id, request, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Theme updated"));
    }

    [HttpDelete("themes/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTheme([FromRoute] Guid id)
    {
        var success = await _superAdminService.DeleteThemeAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Theme deleted"));
    }

    [HttpPut("colleges/{tenantId}/theme")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTheme([FromRoute] Guid tenantId, [FromBody] AssignThemeRequest request)
    {
        var success = await _superAdminService.AssignThemeAsync(tenantId, request.ThemeId, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Theme assigned to college"));
    }

    // -------------------------------------------------------------
    // Templates Management
    // -------------------------------------------------------------

    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates()
    {
        var list = await _superAdminService.GetTemplatesAsync();
        return Ok(ApiResponse<IEnumerable<TemplateDto>>.Ok(list));
    }

    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplateById([FromRoute] Guid id)
    {
        var template = await _superAdminService.GetTemplateByIdAsync(id);
        if (template == null) return NotFound(ApiResponse<object>.Fail("Template not found"));
        return Ok(ApiResponse<TemplateDto>.Ok(template));
    }

    [HttpPost("templates")]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var template = await _superAdminService.CreateTemplateAsync(request, CurrentUser);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TemplateDto>.Ok(template, "Template created successfully"));
    }

    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] UpdateTemplateRequest request)
    {
        var template = await _superAdminService.UpdateTemplateAsync(id, request, CurrentUser);
        return Ok(ApiResponse<TemplateDto>.Ok(template, "Template updated successfully"));
    }

    [HttpDelete("templates/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id)
    {
        var success = await _superAdminService.DeleteTemplateAsync(id, CurrentUser);
        if (!success) return NotFound(ApiResponse<object>.Fail("Template not found or could not be deleted"));
        return Ok(ApiResponse<bool>.Ok(true, "Template deleted successfully"));
    }

    [HttpGet("templates/{code}/preview")]
    [ProducesResponseType(typeof(ApiResponse<TemplatePreviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplatePreview([FromRoute] string code)
    {
        var preview = await _superAdminService.GetTemplatePreviewAsync(code);
        if (preview == null) return NotFound(ApiResponse<object>.Fail("Template not found"));
        return Ok(ApiResponse<TemplatePreviewDto>.Ok(preview));
    }

    [HttpPost("templates/assign")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignTemplate([FromQuery] Guid tenantId, [FromBody] AssignTemplateRequest request)
    {
        var success = await _superAdminService.AssignTemplateAsync(tenantId, request.TemplateCode, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, $"Template '{request.TemplateCode}' assigned successfully"));
    }

    // -------------------------------------------------------------
    // Features Management
    // -------------------------------------------------------------

    [HttpGet("features")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeatureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatures()
    {
        var list = await _superAdminService.GetPlatformFeaturesAsync();
        return Ok(ApiResponse<IEnumerable<FeatureDto>>.Ok(list));
    }

    [HttpGet("features/summary")]
    [ProducesResponseType(typeof(ApiResponse<FeatureSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureSummary()
    {
        var summary = await _superAdminService.GetFeatureSummaryAsync();
        return Ok(ApiResponse<FeatureSummaryDto>.Ok(summary));
    }

    [HttpPost("features")]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureRequest request)
    {
        var feature = await _superAdminService.CreateFeatureAsync(request, CurrentUser);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<FeatureDto>.Ok(feature, "Feature created successfully"));
    }

    [HttpPut("features/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFeature([FromRoute] Guid id, [FromBody] UpdateFeatureRequest request)
    {
        var feature = await _superAdminService.UpdateFeatureAsync(id, request, CurrentUser);
        return Ok(ApiResponse<FeatureDto>.Ok(feature, "Feature updated successfully"));
    }

    [HttpPatch("features/{id:guid}/toggle")]
    [HttpPost("features/{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleFeature([FromRoute] Guid id)
    {
        var newStatus = await _superAdminService.ToggleFeatureStatusAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(newStatus, $"Feature status updated to {(newStatus ? "Active" : "Disabled")}"));
    }

    [HttpGet("features/{code}/colleges")]
    [ProducesResponseType(typeof(ApiResponse<FeatureCollegeUsageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureColleges([FromRoute] string code)
    {
        var usage = await _superAdminService.GetFeatureCollegeUsageAsync(code);
        return Ok(ApiResponse<FeatureCollegeUsageDto>.Ok(usage));
    }

    [HttpGet("colleges/{tenantId}/features")]
    [ProducesResponseType(typeof(ApiResponse<List<TenantFeatureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantFeatures([FromRoute] Guid tenantId)
    {
        var list = await _superAdminService.GetTenantFeaturesAsync(tenantId);
        return Ok(ApiResponse<List<TenantFeatureDto>>.Ok(list));
    }

    [HttpPut("colleges/{tenantId}/features")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTenantFeatures([FromRoute] Guid tenantId, [FromBody] Dictionary<string, bool> features)
    {
        var success = await _superAdminService.UpdateTenantFeaturesAsync(tenantId, features, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Tenant features updated"));
    }

    // -------------------------------------------------------------
    // Roles & Permissions
    // -------------------------------------------------------------

    [HttpGet("roles")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var list = await _superAdminService.GetRolesAsync();
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(list));
    }

    [HttpGet("roles/{id}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleById([FromRoute] Guid id)
    {
        var role = await _superAdminService.GetRoleByIdAsync(id);
        if (role == null) return NotFound(ApiResponse<object>.Fail("Role not found"));
        return Ok(ApiResponse<RoleDto>.Ok(role));
    }

    [HttpPost("roles")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var created = await _superAdminService.CreateRoleAsync(request, CurrentUser);
        return Created($"/api/v1/superadmin/roles/{created.Id}", ApiResponse<RoleDto>.Ok(created, "Role created"));
    }

    [HttpPut("roles/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequest request)
    {
        var success = await _superAdminService.UpdateRoleAsync(id, request, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Role updated"));
    }

    [HttpGet("roles/{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions([FromRoute] Guid id)
    {
        var role = await _superAdminService.GetRoleByIdAsync(id);
        return Ok(ApiResponse<List<PermissionDto>>.Ok(role?.Permissions ?? new List<PermissionDto>()));
    }

    [HttpPut("roles/{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRolePermissions([FromRoute] Guid id, [FromBody] UpdateRolePermissionsRequest request)
    {
        var success = await _superAdminService.UpdateRolePermissionsAsync(id, request.PermissionIds, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Role permissions updated"));
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var list = await _superAdminService.GetPermissionsAsync();
        return Ok(ApiResponse<IEnumerable<PermissionDto>>.Ok(list));
    }

    // -------------------------------------------------------------
    // Users & User-Tenants
    // -------------------------------------------------------------

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminUsers()
    {
        var list = await _superAdminService.GetAdminUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(list));
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAdminUser([FromBody] CreateAdminUserRequest request)
    {
        var created = await _superAdminService.CreateAdminUserAsync(request, CurrentUser);
        return Created($"/api/v1/superadmin/users/{created.Id}", ApiResponse<UserDto>.Ok(created, "Administrator account created"));
    }

    [HttpPut("users/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAdminUser([FromRoute] Guid id, [FromBody] UpdateAdminUserRequest request)
    {
        var success = await _superAdminService.UpdateAdminUserAsync(id, request, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "User updated"));
    }

    [HttpDelete("users/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAdminUser([FromRoute] Guid id)
    {
        try
        {
            var success = await _superAdminService.DeleteAdminUserAsync(id, CurrentUser);
            if (!success) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<bool>.Ok(true, "Administrator deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("users/{id}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetAdminUserPassword([FromRoute] Guid id, [FromBody] ResetAdminUserPasswordRequest request)
    {
        try
        {
            var success = await _superAdminService.ResetAdminUserPasswordAsync(id, request.NewPassword, CurrentUser);
            if (!success) return NotFound(ApiResponse<object>.Fail("User not found"));
            return Ok(ApiResponse<bool>.Ok(true, "Password reset successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("users/{userId}/tenants")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserTenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTenants([FromRoute] Guid userId)
    {
        var list = await _superAdminService.GetUserTenantsAsync(userId);
        return Ok(ApiResponse<IEnumerable<UserTenantDto>>.Ok(list));
    }

    [HttpPut("users/{userId}/tenants")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignUserTenants([FromRoute] Guid userId, [FromBody] AssignUserTenantsRequest request)
    {
        var success = await _superAdminService.AssignUserTenantsAsync(userId, request, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Colleges assigned to administrator"));
    }

    // -------------------------------------------------------------
    // Filtered & Paginated Audit Logs
    // -------------------------------------------------------------

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterRequest filter)
    {
        var logs = await _superAdminService.GetFilteredAuditLogsAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<AuditLogDto>>.Ok(logs));
    }

    // -------------------------------------------------------------
    // Platform Branding (SuperAdmin Settings — persisted server-side)
    // -------------------------------------------------------------

    [HttpGet("branding")]
    [ProducesResponseType(typeof(ApiResponse<PlatformBrandingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformBranding()
    {
        var branding = await _superAdminService.GetPlatformBrandingAsync();
        return Ok(ApiResponse<PlatformBrandingDto>.Ok(branding));
    }

    [HttpPut("branding")]
    [ProducesResponseType(typeof(ApiResponse<PlatformBrandingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePlatformBranding([FromBody] UpdatePlatformBrandingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlatformName))
            return BadRequest(ApiResponse<object>.Fail("Platform name is required."));

        var updated = await _superAdminService.UpdatePlatformBrandingAsync(request, CurrentUser);
        return Ok(ApiResponse<PlatformBrandingDto>.Ok(updated, "Platform branding updated successfully"));
    }
}
