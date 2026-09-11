using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class PageVersionRepository
{
    private readonly ITenantDbConnectionFactory _tenantDb;

    public PageVersionRepository(ITenantDbConnectionFactory tenantDb) => _tenantDb = tenantDb;

    public async Task<IEnumerable<PageVersionDto>> GetVersionsByPageIdAsync(Guid pageId)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, page_id AS PageId, version_number AS VersionNumber, title, slug, 
                   meta_description AS MetaDescription, content_json::text AS ContentJson, 
                   created_by AS CreatedBy, created_at AS CreatedAt
            FROM page_versions
            WHERE page_id = @pageId
            ORDER BY version_number DESC;";

        return await conn.QueryAsync<PageVersionDto>(sql, new { pageId });
    }

    public async Task<PageVersionDto?> GetVersionAsync(Guid pageId, int versionNumber)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, page_id AS PageId, version_number AS VersionNumber, title, slug, 
                   meta_description AS MetaDescription, content_json::text AS ContentJson, 
                   created_by AS CreatedBy, created_at AS CreatedAt
            FROM page_versions
            WHERE page_id = @pageId AND version_number = @versionNumber;";

        return await conn.QueryFirstOrDefaultAsync<PageVersionDto>(sql, new { pageId, versionNumber });
    }

    public async Task<Guid> CreateVersionAsync(Guid pageId, string title, string slug, string? metaDescription, string contentJson, string createdBy)
    {
        using var conn = _tenantDb.CreateConnection();
        var maxVerSql = "SELECT COALESCE(MAX(version_number), 0) + 1 FROM page_versions WHERE page_id = @pageId;";
        var nextVersion = await conn.ExecuteScalarAsync<int>(maxVerSql, new { pageId });

        var insertSql = @"
            INSERT INTO page_versions (page_id, version_number, title, slug, meta_description, content_json, created_by, created_at)
            VALUES (@pageId, @nextVersion, @title, @slug, @metaDescription, @contentJson::jsonb, @createdBy, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(insertSql, new { pageId, nextVersion, title, slug, metaDescription, contentJson, createdBy });
    }
}
