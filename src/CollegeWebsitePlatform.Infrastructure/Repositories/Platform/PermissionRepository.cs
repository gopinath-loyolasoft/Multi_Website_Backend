using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class PermissionRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public PermissionRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "SELECT id, code, name, module, description FROM permissions ORDER BY module ASC, name ASC;";
        return await conn.QueryAsync<PermissionDto>(sql);
    }

    /// <summary>
    /// Loads all permissions assigned to a role identified by its code (e.g. "CollegeAdmin").
    /// SuperAdmin gets ALL permissions. Used to embed the permissions list into JWT claims.
    /// </summary>
    public async Task<IEnumerable<PermissionDto>> GetPermissionsByRoleCodeAsync(string roleCode)
    {
        using var conn = _platformDb.CreateConnection();

        // SuperAdmin has unrestricted access — return all permissions
        if (string.Equals(roleCode, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return await GetAllPermissionsAsync();
        }

        var sql = @"
            SELECT p.id, p.code, p.name, p.module, p.description
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            INNER JOIN roles r ON r.id = rp.role_id
            WHERE UPPER(r.code) = UPPER(@roleCode)
            ORDER BY p.module ASC, p.name ASC;";

        return await conn.QueryAsync<PermissionDto>(sql, new { roleCode });
    }
}
