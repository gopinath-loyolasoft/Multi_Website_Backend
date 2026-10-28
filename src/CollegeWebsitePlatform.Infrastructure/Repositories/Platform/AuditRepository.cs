using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class AuditRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public AuditRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<AuditLogDto>> GetAllAsync(int limit = 50)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT a.id, a.user_id AS UserId, a.username, a.tenant_id AS TenantId, t.tenant_code AS TenantCode,
                   a.action, a.entity_type AS EntityType, a.entity_id AS EntityId, a.details, a.ip_address AS IpAddress,
                   a.created_at AS CreatedAt
            FROM audit_logs a
            LEFT JOIN tenants t ON t.id = a.tenant_id
            ORDER BY a.created_at DESC
            LIMIT @limit;";

        return await conn.QueryAsync<AuditLogDto>(sql, new { limit });
    }

    public async Task<PaginatedResponse<AuditLogDto>> GetFilteredAsync(AuditLogFilterRequest filter)
    {
        using var conn = _platformDb.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 50;
        var offset = (page - 1) * pageSize;

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.TenantId.HasValue)
        {
            whereClauses.Add("a.tenant_id = @TenantId");
            parameters.Add("TenantId", filter.TenantId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            whereClauses.Add("LOWER(a.username) LIKE LOWER(@Username)");
            parameters.Add("Username", $"%{filter.Username.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            whereClauses.Add("LOWER(a.action) LIKE LOWER(@Action)");
            parameters.Add("Action", $"%{filter.Action.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            whereClauses.Add("LOWER(a.entity_type) LIKE LOWER(@EntityType)");
            parameters.Add("EntityType", $"%{filter.EntityType.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            whereClauses.Add("(LOWER(a.action) LIKE LOWER(@Search) OR LOWER(COALESCE(a.details, '')) LIKE LOWER(@Search) OR LOWER(a.username) LIKE LOWER(@Search))");
            parameters.Add("Search", $"%{filter.Search.Trim()}%");
        }
        if (filter.FromDate.HasValue)
        {
            whereClauses.Add("a.created_at >= @FromDate");
            parameters.Add("FromDate", filter.FromDate.Value.ToUniversalTime());
        }
        if (filter.ToDate.HasValue)
        {
            // Inclusive upper bound: extend to end of the selected day (23:59:59.999 UTC)
            var to = filter.ToDate.Value.ToUniversalTime();
            var inclusiveEnd = to.Date.AddDays(1).AddMilliseconds(-1);
            // If the caller already sent a full timestamp (not just a date), honor it as-is when it's later in the day
            if (to.TimeOfDay > TimeSpan.Zero)
                inclusiveEnd = to;
            parameters.Add("ToDate", inclusiveEnd);
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(*) FROM audit_logs a {whereSql};";
        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT a.id, a.user_id AS UserId, a.username, a.tenant_id AS TenantId, t.tenant_code AS TenantCode,
                   a.action, a.entity_type AS EntityType, a.entity_id AS EntityId, a.details, a.ip_address AS IpAddress,
                   a.created_at AS CreatedAt
            FROM audit_logs a
            LEFT JOIN tenants t ON t.id = a.tenant_id
            {whereSql}
            ORDER BY a.created_at DESC
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<AuditLogDto>(dataSql, parameters)).ToList();

        return new PaginatedResponse<AuditLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<AuditLogDto>> GetByTenantIdAsync(Guid tenantId, int limit = 20)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT a.id, a.user_id AS UserId, a.username, a.tenant_id AS TenantId, t.tenant_code AS TenantCode,
                   a.action, a.entity_type AS EntityType, a.entity_id AS EntityId, a.details, a.ip_address AS IpAddress,
                   a.created_at AS CreatedAt
            FROM audit_logs a
            LEFT JOIN tenants t ON t.id = a.tenant_id
            WHERE a.tenant_id = @tenantId
            ORDER BY a.created_at DESC
            LIMIT @limit;";

        return await conn.QueryAsync<AuditLogDto>(sql, new { tenantId, limit });
    }

    public async Task LogAsync(string username, string action, string entityType, string? entityId = null, string? details = null, Guid? tenantId = null, string? ipAddress = null)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO audit_logs (username, action, entity_type, entity_id, details, tenant_id, ip_address, created_at)
            VALUES (@username, @action, @entityType, @entityId, @details, @tenantId, @ipAddress, CURRENT_TIMESTAMP);";

        await conn.ExecuteAsync(sql, new { username, action, entityType, entityId, details, tenantId, ipAddress });
    }

    /// <summary>Overload used by TenantProvisioningService with an explicit userId argument.</summary>
    public async Task LogAsync(Guid? userId, string username, Guid? tenantId, string action, string entityType, string? entityId, string? details, string? ipAddress)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            INSERT INTO audit_logs (user_id, username, tenant_id, action, entity_type, entity_id, details, ip_address, created_at)
            VALUES (@userId, @username, @tenantId, @action, @entityType, @entityId, @details, @ipAddress, CURRENT_TIMESTAMP);";

        await conn.ExecuteAsync(sql, new { userId, username, tenantId, action, entityType, entityId, details, ipAddress });
    }
}
