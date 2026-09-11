using System.Text.Json;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Domain.Entities;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class ThemeRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public ThemeRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<Theme>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, name, code, configuration::text AS Configuration, is_active AS IsActive,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM themes
            WHERE is_deleted = FALSE
            ORDER BY created_at ASC;";

        return await conn.QueryAsync<Theme>(sql);
    }

    public async Task<Theme?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, name, code, configuration::text AS Configuration, is_active AS IsActive,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM themes
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<Theme>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(string name, string code, string configJson)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO themes (name, code, configuration, is_active, created_at, updated_at)
            VALUES (@name, @code, @configJson::jsonb, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { name, code, configJson });
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string configJson, bool isActive)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE themes 
            SET name = @name, configuration = @configJson::jsonb, is_active = @isActive, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, name, configJson, isActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE themes SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
