using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class RoleRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public RoleRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "SELECT id, name, code, description, is_system AS IsSystem FROM roles WHERE is_deleted = FALSE ORDER BY name ASC;";
        var roles = (await conn.QueryAsync<RoleDto>(sql)).ToList();

        foreach (var r in roles)
        {
            var pSql = @"
                SELECT p.id, p.code, p.name, p.module, p.description
                FROM permissions p
                INNER JOIN role_permissions rp ON rp.permission_id = p.id
                WHERE rp.role_id = @roleId
                ORDER BY p.module ASC, p.name ASC;";
            var perms = await conn.QueryAsync<PermissionDto>(pSql, new { roleId = r.Id });
            r.Permissions = perms.ToList();
        }

        return roles;
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "SELECT id, name, code, description, is_system AS IsSystem FROM roles WHERE id = @id AND is_deleted = FALSE;";
        var role = await conn.QueryFirstOrDefaultAsync<RoleDto>(sql, new { id });
        if (role == null) return null;

        var pSql = @"
            SELECT p.id, p.code, p.name, p.module, p.description
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            WHERE rp.role_id = @id
            ORDER BY p.module ASC, p.name ASC;";
        var perms = await conn.QueryAsync<PermissionDto>(pSql, new { id });
        role.Permissions = perms.ToList();

        return role;
    }

    public async Task<Guid> CreateAsync(string name, string code, string? description)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO roles (name, code, description, is_system, created_at)
            VALUES (@name, UPPER(@code), @description, FALSE, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, new { name, code, description });
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string? description)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE roles SET name = @name, description = @description WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, name, description });
        return rows > 0;
    }

    public async Task<bool> UpdatePermissionsAsync(Guid roleId, List<Guid> permissionIds)
    {
        using var conn = _platformDb.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM role_permissions WHERE role_id = @roleId;", new { roleId });
        foreach (var pid in permissionIds)
        {
            await conn.ExecuteAsync("INSERT INTO role_permissions (role_id, permission_id) VALUES (@roleId, @pid);", new { roleId, pid });
        }
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE roles SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id AND is_system = FALSE;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
