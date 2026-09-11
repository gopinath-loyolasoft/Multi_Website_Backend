using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class ContactRepository
{
    private readonly ITenantDbConnectionFactory _tenantDb;

    public ContactRepository(ITenantDbConnectionFactory tenantDb) => _tenantDb = tenantDb;

    public async Task<PaginatedResponse<ContactSubmissionDto>> GetAllPaginatedAsync(PaginationFilter filter)
    {
        using var conn = _tenantDb.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(name) LIKE LOWER(@Search) OR LOWER(email) LIKE LOWER(@Search) OR LOWER(subject) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM contact_submissions {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, name, email, phone, subject, message, status, created_at AS CreatedAt
            FROM contact_submissions
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<ContactSubmissionDto>(dataSql, param)).ToList();

        return new PaginatedResponse<ContactSubmissionDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<ContactSubmissionDto>> GetAllSubmissionsAsync()
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, name, email, phone, subject, message, status, created_at AS CreatedAt
            FROM contact_submissions
            WHERE is_deleted = FALSE
            ORDER BY created_at DESC;";

        return await conn.QueryAsync<ContactSubmissionDto>(sql);
    }

    public async Task<ContactSubmissionDto?> GetByIdAsync(Guid id)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, name, email, phone, subject, message, status, created_at AS CreatedAt
            FROM contact_submissions
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<ContactSubmissionDto>(sql, new { id });
    }

    public async Task<Guid> CreateSubmissionAsync(CreateContactSubmissionRequest req)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            INSERT INTO contact_submissions (name, email, phone, subject, message, status, created_at)
            VALUES (@Name, @Email, @Phone, @Subject, @Message, 'NEW', CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = "UPDATE contact_submissions SET status = @status WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, status });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = "UPDATE contact_submissions SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
