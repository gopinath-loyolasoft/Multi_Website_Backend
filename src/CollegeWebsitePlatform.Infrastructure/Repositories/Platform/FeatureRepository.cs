using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class FeatureRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public FeatureRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<FeatureDto>> GetAllFeaturesAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT f.id AS Id, f.code AS Code, f.name AS Name, f.description AS Description,
                   f.category AS Category, f.icon AS Icon, f.sort_order AS SortOrder,
                   f.is_active AS IsActive, f.created_at AS CreatedAt, f.updated_at AS UpdatedAt,
                   (SELECT COUNT(*) FROM tenant_features tf JOIN tenants t ON t.id = tf.tenant_id WHERE tf.feature_code = f.code AND tf.is_enabled = TRUE AND t.is_deleted = FALSE) AS CollegesUsingCount
            FROM features f
            ORDER BY f.sort_order ASC, f.name ASC;";
        return await conn.QueryAsync<FeatureDto>(sql);
    }

    public async Task<FeatureDto?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT f.id AS Id, f.code AS Code, f.name AS Name, f.description AS Description,
                   f.category AS Category, f.icon AS Icon, f.sort_order AS SortOrder,
                   f.is_active AS IsActive, f.created_at AS CreatedAt, f.updated_at AS UpdatedAt,
                   (SELECT COUNT(*) FROM tenant_features tf JOIN tenants t ON t.id = tf.tenant_id WHERE tf.feature_code = f.code AND tf.is_enabled = TRUE AND t.is_deleted = FALSE) AS CollegesUsingCount
            FROM features f
            WHERE f.id = @id;";
        return await conn.QueryFirstOrDefaultAsync<FeatureDto>(sql, new { id });
    }

    public async Task<FeatureDto?> GetByCodeAsync(string code)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT f.id AS Id, f.code AS Code, f.name AS Name, f.description AS Description,
                   f.category AS Category, f.icon AS Icon, f.sort_order AS SortOrder,
                   f.is_active AS IsActive, f.created_at AS CreatedAt, f.updated_at AS UpdatedAt,
                   (SELECT COUNT(*) FROM tenant_features tf JOIN tenants t ON t.id = tf.tenant_id WHERE tf.feature_code = f.code AND tf.is_enabled = TRUE AND t.is_deleted = FALSE) AS CollegesUsingCount
            FROM features f
            WHERE f.code = @code;";
        return await conn.QueryFirstOrDefaultAsync<FeatureDto>(sql, new { code });
    }

    public async Task<FeatureSummaryDto> GetSummaryAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT 
                COUNT(*) AS TotalFeatures,
                COUNT(*) FILTER (WHERE is_active = TRUE) AS ActiveFeatures,
                COUNT(*) FILTER (WHERE is_active = FALSE) AS DisabledFeatures,
                COUNT(DISTINCT category) AS CategoriesCount,
                (SELECT COUNT(*) FROM tenants WHERE is_deleted = FALSE) AS TotalColleges,
                (SELECT COUNT(*) FROM tenant_features tf JOIN tenants t ON t.id = tf.tenant_id WHERE tf.is_enabled = TRUE AND t.is_deleted = FALSE) AS TotalAssignments
            FROM features;";
        return await conn.QueryFirstAsync<FeatureSummaryDto>(sql);
    }

    public async Task<Guid> CreateAsync(CreateFeatureRequest req)
    {
        using var conn = _platformDb.CreateConnection();
        var id = Guid.NewGuid();
        var sql = @"
            INSERT INTO features (id, code, name, description, category, icon, sort_order, is_active, created_at, updated_at)
            VALUES (@id, @Code, @Name, @Description, @Category, @Icon, @SortOrder, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
        await conn.ExecuteAsync(sql, new { id, req.Code, req.Name, req.Description, req.Category, req.Icon, req.SortOrder, req.IsActive });
        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateFeatureRequest req)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE features 
            SET name = @Name, description = @Description, category = @Category,
                icon = @Icon, sort_order = @SortOrder, is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, req.Name, req.Description, req.Category, req.Icon, req.SortOrder, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> ToggleStatusAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE features 
            SET is_active = NOT is_active, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id 
            RETURNING is_active;";
        return await conn.ExecuteScalarAsync<bool>(sql, new { id });
    }

    public async Task<FeatureCollegeUsageDto> GetFeatureCollegeUsageAsync(string featureCode)
    {
        using var conn = _platformDb.CreateConnection();
        var featureSql = "SELECT name FROM features WHERE code = @featureCode;";
        var featureName = await conn.ExecuteScalarAsync<string>(featureSql, new { featureCode }) ?? featureCode;

        var usageSql = @"
            SELECT t.id AS TenantId, t.tenant_code AS TenantCode, t.name AS TenantName,
                   COALESCE(d.domain, LOWER(t.tenant_code) || '.localhost') AS PrimaryDomain,
                   COALESCE(tf.is_enabled, FALSE) AS IsEnabled
            FROM tenants t
            LEFT JOIN domains d ON d.tenant_id = t.id AND d.is_primary = TRUE
            LEFT JOIN tenant_features tf ON tf.tenant_id = t.id AND tf.feature_code = @featureCode
            WHERE t.is_deleted = FALSE
            ORDER BY t.name ASC;";

        var colleges = (await conn.QueryAsync<CollegeFeatureStatusDto>(usageSql, new { featureCode })).ToList();
        return new FeatureCollegeUsageDto
        {
            FeatureCode = featureCode,
            FeatureName = featureName,
            Colleges = colleges
        };
    }

    public async Task<List<TenantFeatureDto>> GetFeaturesByTenantIdAsync(Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT f.code AS FeatureCode, f.name AS FeatureName, f.description AS Description,
                   COALESCE(tf.is_enabled, f.is_active) AS IsEnabled
            FROM features f
            LEFT JOIN tenant_features tf ON tf.feature_code = f.code AND tf.tenant_id = @tenantId
            ORDER BY f.sort_order ASC, f.code ASC;";

        var results = await conn.QueryAsync<TenantFeatureDto>(sql, new { tenantId });
        return results.ToList();
    }

    public async Task<bool> IsFeatureEnabledForTenantAsync(Guid tenantId, string featureCode)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT COALESCE(tf.is_enabled, f.is_active)
            FROM features f
            LEFT JOIN tenant_features tf ON tf.feature_code = f.code AND tf.tenant_id = @tenantId
            WHERE f.code = @featureCode;";

        return await conn.ExecuteScalarAsync<bool>(sql, new { tenantId, featureCode });
    }

    public async Task SetTenantFeatureAsync(Guid tenantId, string featureCode, bool isEnabled)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO tenant_features (tenant_id, feature_code, is_enabled)
            VALUES (@tenantId, @featureCode, @isEnabled)
            ON CONFLICT (tenant_id, feature_code) 
            DO UPDATE SET is_enabled = @isEnabled;";

        await conn.ExecuteAsync(sql, new { tenantId, featureCode, isEnabled });
    }

    /// <summary>Bulk-enables a list of feature codes for a tenant (used during provisioning).</summary>
    public async Task SetTenantFeaturesAsync(Guid tenantId, IEnumerable<string> featureCodes)
    {
        foreach (var code in featureCodes)
            await SetTenantFeatureAsync(tenantId, code, isEnabled: true);
    }
}
