using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class DomainRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public DomainRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<DomainDto>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT d.id, d.tenant_id AS TenantId, t.tenant_code AS TenantCode, 
                   d.domain, d.domain_type AS DomainType, d.is_primary AS IsPrimary, 
                   d.is_verified AS IsVerified, d.verified_at AS VerifiedAt, d.created_at AS CreatedAt
            FROM domains d
            INNER JOIN tenants t ON t.id = d.tenant_id
            WHERE d.is_deleted = FALSE AND t.is_deleted = FALSE
            ORDER BY d.created_at ASC;";

        return await conn.QueryAsync<DomainDto>(sql);
    }

    public async Task<IEnumerable<DomainDto>> GetByTenantIdAsync(Guid tenantId)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT d.id, d.tenant_id AS TenantId, t.tenant_code AS TenantCode, 
                   d.domain, d.domain_type AS DomainType, d.is_primary AS IsPrimary, 
                   d.is_verified AS IsVerified, d.verified_at AS VerifiedAt, d.created_at AS CreatedAt
            FROM domains d
            INNER JOIN tenants t ON t.id = d.tenant_id
            WHERE d.tenant_id = @tenantId AND d.is_deleted = FALSE AND t.is_deleted = FALSE
            ORDER BY d.is_primary DESC, d.created_at ASC;";

        return await conn.QueryAsync<DomainDto>(sql, new { tenantId });
    }

    public async Task<DomainDto?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT d.id, d.tenant_id AS TenantId, t.tenant_code AS TenantCode, 
                   d.domain, d.domain_type AS DomainType, d.is_primary AS IsPrimary, 
                   d.is_verified AS IsVerified, d.verified_at AS VerifiedAt, d.created_at AS CreatedAt
            FROM domains d
            INNER JOIN tenants t ON t.id = d.tenant_id
            WHERE d.id = @id AND d.is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<DomainDto>(sql, new { id });
    }

    /// <summary>Finds a domain registration by the exact domain string — used during provisioning uniqueness validation.</summary>
    public async Task<DomainDto?> GetByDomainAsync(string domain)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT d.id, d.tenant_id AS TenantId, t.tenant_code AS TenantCode, 
                   d.domain, d.domain_type AS DomainType, d.is_primary AS IsPrimary, 
                   d.is_verified AS IsVerified, d.verified_at AS VerifiedAt, d.created_at AS CreatedAt
            FROM domains d
            INNER JOIN tenants t ON t.id = d.tenant_id
            WHERE LOWER(d.domain) = LOWER(@domain) AND d.is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<DomainDto>(sql, new { domain });
    }

    public async Task<Guid> CreateAsync(Guid tenantId, string domain, string domainType = "PRIMARY", bool isPrimary = true, string? verificationToken = null)
    {
        using var conn = _platformDb.CreateConnection();

        if (isPrimary)
        {
            await conn.ExecuteAsync("UPDATE domains SET is_primary = FALSE WHERE tenant_id = @tenantId;", new { tenantId });
        }

        var sql = @"
            INSERT INTO domains (tenant_id, domain, domain_type, is_primary, is_verified, verification_token, created_at, updated_at)
            VALUES (@tenantId, LOWER(@domain), @domainType, @isPrimary, FALSE, @verificationToken, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { tenantId, domain, domainType, isPrimary, verificationToken });
    }

    public async Task<string?> GetVerificationTokenAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        return await conn.ExecuteScalarAsync<string>(
            "SELECT verification_token FROM domains WHERE id = @id;", new { id });
    }

    public async Task<bool> UpdateAsync(Guid id, string domain, string domainType)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE domains SET domain = LOWER(@domain), domain_type = @domainType, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, domain, domainType });
        return rows > 0;
    }

    public async Task<bool> VerifyAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = "UPDATE domains SET is_verified = TRUE, verified_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }

    public async Task<bool> SetPrimaryAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var dom = await GetByIdAsync(id);
        if (dom == null) return false;

        await conn.ExecuteAsync("UPDATE domains SET is_primary = FALSE WHERE tenant_id = @tenantId;", new { tenantId = dom.TenantId });
        await conn.ExecuteAsync("UPDATE domains SET is_primary = TRUE, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE domains SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
