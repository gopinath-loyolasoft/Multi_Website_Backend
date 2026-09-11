using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class DatabaseRegistryRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public DatabaseRegistryRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<DatabaseRegistryDto>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT r.id, r.tenant_id AS TenantId, t.tenant_code AS TenantCode, t.name AS TenantName,
                   r.database_name AS DatabaseName,
                   r.status, r.current_migration_version AS CurrentMigrationVersion,
                   r.last_health_check_at AS LastHealthCheckAt, r.created_at AS CreatedAt
            FROM tenant_database_registry r
            INNER JOIN tenants t ON t.id = r.tenant_id
            WHERE t.is_deleted = FALSE AND r.is_deleted = FALSE
            ORDER BY r.created_at ASC;";

        return await conn.QueryAsync<DatabaseRegistryDto>(sql);
    }

    public async Task<DatabaseRegistryDto?> GetByTenantIdAsync(Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT r.id, r.tenant_id AS TenantId, t.tenant_code AS TenantCode, t.name AS TenantName,
                   r.database_name AS DatabaseName,
                   r.status, r.current_migration_version AS CurrentMigrationVersion,
                   r.last_health_check_at AS LastHealthCheckAt, r.created_at AS CreatedAt
            FROM tenant_database_registry r
            INNER JOIN tenants t ON t.id = r.tenant_id
            WHERE r.tenant_id = @tenantId AND r.is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<DatabaseRegistryDto>(sql, new { tenantId });
    }

    public async Task<bool> UpsertAsync(Guid tenantId, string dbName, string host, int port, string user, string status, string migrationVersion)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO tenant_database_registry (tenant_id, database_name, host, port, db_user, status, current_migration_version, last_health_check_at, created_at)
            VALUES (@tenantId, @dbName, @host, @port, @user, @status, @migrationVersion, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (database_name) DO UPDATE 
            SET status = @status, current_migration_version = @migrationVersion, last_health_check_at = CURRENT_TIMESTAMP;";

        var rows = await conn.ExecuteAsync(sql, new { tenantId, dbName, host, port, user, status, migrationVersion });
        return rows > 0;
    }

    /// <summary>Registers a brand-new tenant database record during provisioning.</summary>
    public async Task RegisterAsync(Guid tenantId, string dbName, string host, int port, string user)
        => await UpsertAsync(tenantId, dbName, host, port, user, "HEALTHY", "V8");

    /// <summary>Updates the migration version and status for a tenant database.</summary>
    public async Task UpdateStatusAsync(Guid tenantId, string status, string migrationVersion)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE tenant_database_registry 
            SET status = @status, current_migration_version = @migrationVersion, last_health_check_at = CURRENT_TIMESTAMP
            WHERE tenant_id = @tenantId;";
        await conn.ExecuteAsync(sql, new { tenantId, status, migrationVersion });
    }

    /// <summary>Bumps the last_health_check_at timestamp after a successful connectivity probe.</summary>
    public async Task UpdateHealthCheckAsync(Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE tenant_database_registry SET status = 'HEALTHY', last_health_check_at = CURRENT_TIMESTAMP WHERE tenant_id = @tenantId;",
            new { tenantId });
    }
}
