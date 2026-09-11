using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Entities;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class UserRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public UserRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, tenant_id AS TenantId, username, email, password_hash AS PasswordHash, 
                   full_name AS FullName, role, is_active AS IsActive,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users
            WHERE LOWER(username) = LOWER(@username) AND is_active = TRUE AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { username });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, tenant_id AS TenantId, username, email, password_hash AS PasswordHash, 
                   full_name AS FullName, role, is_active AS IsActive,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM users
            WHERE id = @id AND is_active = TRUE AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { id });
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT u.id, u.tenant_id AS TenantId, t.tenant_code AS TenantCode, u.username, u.email, 
                   u.full_name AS FullName, u.role, u.is_active AS IsActive, u.created_at AS CreatedAt
            FROM users u
            LEFT JOIN tenants t ON t.id = u.tenant_id
            WHERE u.is_deleted = FALSE
            ORDER BY u.created_at ASC;";

        return await conn.QueryAsync<UserDto>(sql);
    }

    public async Task<IEnumerable<UserDto>> GetByTenantIdAsync(Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT DISTINCT u.id, u.tenant_id AS TenantId, t.tenant_code AS TenantCode, u.username, u.email, 
                   u.full_name AS FullName, u.role, u.is_active AS IsActive, u.created_at AS CreatedAt
            FROM users u
            LEFT JOIN tenants t ON t.id = COALESCE(u.tenant_id, @tenantId)
            WHERE (u.tenant_id = @tenantId OR EXISTS (SELECT 1 FROM user_tenants ut WHERE ut.user_id = u.id AND ut.tenant_id = @tenantId))
              AND u.is_deleted = FALSE
            ORDER BY u.created_at ASC;";

        return await conn.QueryAsync<UserDto>(sql, new { tenantId });
    }

    public async Task<Guid> CreateAsync(Guid? tenantId, string username, string email, string passwordHash, string fullName, string role)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO users (tenant_id, username, email, password_hash, full_name, role, is_active, created_at, updated_at)
            VALUES (@tenantId, @username, @email, @passwordHash, @fullName, @role, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { tenantId, username, email, passwordHash, fullName, role });
    }

    public async Task<bool> UpdateAsync(Guid id, string fullName, string email, bool isActive)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE users SET full_name = @fullName, email = @email, is_active = @isActive, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, fullName, email, isActive });
        return rows > 0;
    }

    public async Task<bool> UpdatePasswordAsync(Guid id, string passwordHash)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE users SET password_hash = @passwordHash, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, passwordHash });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE users SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
