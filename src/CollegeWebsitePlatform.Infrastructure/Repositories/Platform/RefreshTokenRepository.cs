using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Domain.Entities;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class RefreshTokenRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public RefreshTokenRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT id, user_id AS UserId, token, expires_at AS ExpiresAt, is_revoked AS IsRevoked, created_at AS CreatedAt
            FROM refresh_tokens
            WHERE token = @token AND is_revoked = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { token });
    }

    public async Task SaveAsync(Guid userId, string token, DateTime expiresAt)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO refresh_tokens (user_id, token, expires_at, is_revoked, created_at)
            VALUES (@userId, @token, @expiresAt, FALSE, CURRENT_TIMESTAMP);";

        await conn.ExecuteAsync(sql, new { userId, token, expiresAt });
    }

    public async Task RevokeAsync(string token)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE refresh_tokens SET is_revoked = TRUE WHERE token = @token;";
        await conn.ExecuteAsync(sql, new { token });
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE refresh_tokens SET is_revoked = TRUE WHERE user_id = @userId;";
        await conn.ExecuteAsync(sql, new { userId });
    }
}
