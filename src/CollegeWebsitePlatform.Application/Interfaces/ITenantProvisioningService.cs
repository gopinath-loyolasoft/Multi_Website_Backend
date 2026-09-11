using CollegeWebsitePlatform.Application.DTOs;

namespace CollegeWebsitePlatform.Application.Interfaces;

// ─── Request / Result models ──────────────────────────────────────────────────

public class ProvisionCollegeRequest
{
    public string CollegeName  { get; set; } = string.Empty;
    public string TenantCode   { get; set; } = string.Empty;  // e.g. "RNC"
    public string Slug         { get; set; } = string.Empty;  // e.g. "rnc-college"
    public string PrimaryDomain { get; set; } = string.Empty; // e.g. "rnc.localhost"
    public Guid?  ThemeId      { get; set; }
    public string? TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public List<string> EnabledFeatures { get; set; } = new();

    // Initial CollegeAdmin credentials
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminEmail    { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}

public class ProvisioningResult
{
    public bool   Success             { get; set; }
    public string Message             { get; set; } = string.Empty;
    public Guid   TenantId            { get; set; }
    public Guid   DomainId            { get; set; }
    public Guid   AdminUserId         { get; set; }
    public string DatabaseName        { get; set; } = string.Empty;
    public string DbConnectionString  { get; set; } = string.Empty;
    public List<string> StepsCompleted { get; set; } = new();
    public string? FailedAt           { get; set; }
}

// ─── Service Interface ────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates the full lifecycle of creating a new college tenant on the platform.
/// Each college provisioning goes through a 15-step pipeline to ensure the tenant
/// DB, schema, seed data, admin user, theme, and feature flags are all ready before
/// the tenant status is set to ACTIVE.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Full 15-step provisioning pipeline:
    ///  1. Validate uniqueness (tenantCode, slug, domain)
    ///  2. Generate database name from slug
    ///  3. INSERT tenant (PROVISIONING)
    ///  4. INSERT primary domain
    ///  5. CREATE PostgreSQL database
    ///  6. Run tenant schema migrations
    ///  7. Seed initial tenant content (settings, menus, home page)
    ///  8. INSERT tenant_database_registry
    ///  9. Assign theme → UPDATE tenants.default_theme_id
    /// 10. INSERT tenant_features (enabled feature list)
    /// 11. CREATE CollegeAdmin user (BCrypt password)
    /// 12. INSERT user_tenants mapping
    /// 13. UPDATE tenant.status = ACTIVE, provisioning_status = COMPLETED
    /// 14. INSERT audit_log entry
    /// 15. Return ProvisioningResult
    /// </summary>
    Task<ProvisioningResult> ProvisionNewCollegeAsync(ProvisionCollegeRequest request);

    /// <summary>
    /// Safely deactivates a tenant: SUSPEND status, do NOT delete data.
    /// </summary>
    Task<bool> SuspendCollegeAsync(Guid tenantId, string reason, string performedBy);

    /// <summary>
    /// Re-activates a previously suspended college.
    /// </summary>
    Task<bool> ActivateCollegeAsync(Guid tenantId, string performedBy);

    /// <summary>
    /// Returns the current migration version and health status of a tenant's database.
    /// </summary>
    Task<MigrationStatusDto> GetMigrationStatusAsync(Guid tenantId);

    /// <summary>
    /// Re-runs any pending schema migrations on the tenant database.
    /// </summary>
    Task<bool> RunMigrationsAsync(Guid tenantId, string performedBy);

    /// <summary>
    /// Performs a connectivity and schema health check on the tenant database.
    /// </summary>
    Task<DatabaseHealthDto> VerifyTenantDatabaseAsync(Guid tenantId);
}

public class DatabaseHealthDto
{
    public bool   IsHealthy              { get; set; }
    public string DatabaseName           { get; set; } = string.Empty;
    public string Status                 { get; set; } = string.Empty;
    public long   ResponseTimeMs         { get; set; }
    public string Message                { get; set; } = string.Empty;
}
