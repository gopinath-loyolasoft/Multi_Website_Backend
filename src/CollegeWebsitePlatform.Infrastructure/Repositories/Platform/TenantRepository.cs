using CollegeWebsitePlatform.Application.Common.Interfaces;
using Dapper;
using TenantEntity = CollegeWebsitePlatform.Domain.Entities.Tenant;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class TenantRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public TenantRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<TenantEntity>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, tenant_code AS TenantCode, name, slug, db_connection_string AS DbConnectionString,
                   status, default_theme_id AS DefaultThemeId, template_code AS TemplateCode,
                   COALESCE(login_template, 'template2') AS LoginTemplate, provisioning_status AS ProvisioningStatus,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tenants
            WHERE is_deleted = FALSE
            ORDER BY created_at ASC;";

        return await conn.QueryAsync<TenantEntity>(sql);
    }

    public async Task<TenantEntity?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, tenant_code AS TenantCode, name, slug, db_connection_string AS DbConnectionString,
                   status, default_theme_id AS DefaultThemeId, template_code AS TemplateCode,
                   COALESCE(login_template, 'template2') AS LoginTemplate, provisioning_status AS ProvisioningStatus,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tenants
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<TenantEntity>(sql, new { id });
    }

    public async Task<TenantEntity?> GetByDomainAsync(string domain)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT t.id, t.tenant_code AS TenantCode, t.name, t.slug, t.db_connection_string AS DbConnectionString,
                   t.status, t.default_theme_id AS DefaultThemeId, t.template_code AS TemplateCode,
                   COALESCE(t.login_template, 'template2') AS LoginTemplate, t.provisioning_status AS ProvisioningStatus,
                   t.is_deleted AS IsDeleted, t.deleted_at AS DeletedAt,
                   t.created_at AS CreatedAt, t.updated_at AS UpdatedAt
            FROM tenants t
            INNER JOIN domains d ON d.tenant_id = t.id
            WHERE LOWER(d.domain) = LOWER(@domain) AND t.is_deleted = FALSE AND d.is_deleted = FALSE
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<TenantEntity>(sql, new { domain });
    }

    public async Task<TenantEntity?> GetByCodeAsync(string tenantCode)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, tenant_code AS TenantCode, name, slug, db_connection_string AS DbConnectionString,
                   status, default_theme_id AS DefaultThemeId, template_code AS TemplateCode,
                   COALESCE(login_template, 'template2') AS LoginTemplate, provisioning_status AS ProvisioningStatus,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM tenants
            WHERE UPPER(tenant_code) = UPPER(@tenantCode) AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<TenantEntity>(sql, new { tenantCode });
    }

    public async Task<Guid> CreateAsync(string tenantCode, string name, string slug, string dbConnectionString, Guid? defaultThemeId, string templateCode = "ENGINEERING_MODERN", string loginTemplate = "template2", string status = "ACTIVE")
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO tenants (tenant_code, name, slug, db_connection_string, default_theme_id, template_code, login_template, status, provisioning_status, created_at, updated_at)
            VALUES (UPPER(@tenantCode), @name, LOWER(@slug), @dbConnectionString, @defaultThemeId, @templateCode, COALESCE(@loginTemplate, 'template2'), @status, 'COMPLETED', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { tenantCode, name, slug, dbConnectionString, defaultThemeId, templateCode, loginTemplate, status });
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string slug, Guid? defaultThemeId, string? loginTemplate = null)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE tenants 
            SET name = @name, slug = LOWER(@slug), default_theme_id = @defaultThemeId,
                login_template = COALESCE(@loginTemplate, login_template, 'template2'),
                updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, name, slug, defaultThemeId, loginTemplate });
        return rows > 0;
    }

    public async Task<bool> UpdateThemeAsync(Guid tenantId, Guid themeId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE tenants SET default_theme_id = @themeId, updated_at = CURRENT_TIMESTAMP WHERE id = @tenantId;";
        var rows = await conn.ExecuteAsync(sql, new { tenantId, themeId });
        return rows > 0;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE tenants SET status = @status, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, status });
        return rows > 0;
    }

    /// <summary>Updates the provisioning_status column (PENDING → RUNNING → COMPLETED / FAILED).</summary>
    public async Task<bool> UpdateProvisioningStatusAsync(Guid id, string provisioningStatus)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE tenants SET provisioning_status = @provisioningStatus, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, provisioningStatus });
        return rows > 0;
    }

    /// <summary>
    /// Creates a tenant with a caller-supplied UUID (used during provisioning so the TenantId
    /// is known before the database is physically created).
    /// </summary>
    public async Task CreateWithIdAsync(Guid id, string tenantCode, string name, string slug, string dbConnectionString, string status, Guid? defaultThemeId, string templateCode = "ENGINEERING_MODERN", string loginTemplate = "template2")
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO tenants (id, tenant_code, name, slug, db_connection_string, status, default_theme_id, template_code, login_template, provisioning_status, created_at, updated_at)
            VALUES (@id, UPPER(@tenantCode), @name, LOWER(@slug), @dbConnectionString, @status, @defaultThemeId, @templateCode, COALESCE(@loginTemplate, 'template2'), 'PROVISIONING', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (tenant_code) DO NOTHING;";
        await conn.ExecuteAsync(sql, new { id, tenantCode, name, slug, dbConnectionString, status, defaultThemeId, templateCode, loginTemplate });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE tenants SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
