using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class UserTenantRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public UserTenantRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<UserTenantDto>> GetByUserIdAsync(Guid userId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT ut.id, ut.user_id AS UserId, u.username, ut.tenant_id AS TenantId,
                   t.tenant_code AS TenantCode, t.name AS TenantName,
                   ut.role_id AS RoleId, r.name AS RoleName, ut.is_primary AS IsPrimary, ut.created_at AS CreatedAt
            FROM user_tenants ut
            INNER JOIN users u ON u.id = ut.user_id
            INNER JOIN tenants t ON t.id = ut.tenant_id
            LEFT JOIN roles r ON r.id = ut.role_id
            WHERE ut.user_id = @userId
            ORDER BY ut.is_primary DESC, ut.created_at ASC;";

        return await conn.QueryAsync<UserTenantDto>(sql, new { userId });
    }

    public async Task<bool> AssignTenantsAsync(Guid userId, List<Guid> tenantIds, Guid? roleId)
    {
        using var conn = _platformDb.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM user_tenants WHERE user_id = @userId;", new { userId });

        for (int i = 0; i < tenantIds.Count; i++)
        {
            var isPrimary = (i == 0);
            var sql = @"
                INSERT INTO user_tenants (user_id, tenant_id, role_id, is_primary, created_at)
                VALUES (@userId, @tenantId, @roleId, @isPrimary, CURRENT_TIMESTAMP)
                ON CONFLICT (user_id, tenant_id) DO UPDATE 
                SET role_id = @roleId, is_primary = @isPrimary;";

            await conn.ExecuteAsync(sql, new { userId, tenantId = tenantIds[i], roleId, isPrimary });
        }

        return true;
    }

    public async Task<bool> IsUserAssignedToTenantAsync(Guid userId, Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "SELECT COUNT(1) FROM user_tenants WHERE user_id = @userId AND tenant_id = @tenantId;";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { userId, tenantId });
        return count > 0;
    }

    /// <summary>Maps a single user to a single tenant — used during college provisioning.</summary>
    public async Task CreateAsync(Guid userId, Guid tenantId, bool isPrimary = true)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO user_tenants (user_id, tenant_id, role_id, is_primary, created_at)
            VALUES (@userId, @tenantId, NULL, @isPrimary, CURRENT_TIMESTAMP)
            ON CONFLICT (user_id, tenant_id) DO UPDATE SET is_primary = @isPrimary;";
        await conn.ExecuteAsync(sql, new { userId, tenantId, isPrimary });
    }
}
