using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class MediaRepository
{
    private readonly ITenantDbConnectionFactory _tenantDb;

    public MediaRepository(ITenantDbConnectionFactory tenantDb) => _tenantDb = tenantDb;

    public async Task<PaginatedResponse<MediaDto>> GetAllPaginatedAsync(MediaFilterRequest filter)
    {
        using var conn = _tenantDb.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.ContentType))
        {
            conditions.Add("LOWER(content_type) LIKE LOWER(@ContentType)");
            param.Add("ContentType", $"%{filter.ContentType.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(file_name) LIKE LOWER(@Search) OR LOWER(alt_text) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM media {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, file_name AS FileName, file_path AS FilePath, content_type AS ContentType,
                   file_size_bytes AS FileSizeBytes, alt_text AS AltText, uploaded_by AS UploadedBy, created_at AS CreatedAt
            FROM media
            {whereClause}
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<MediaDto>(dataSql, param)).ToList();

        return new PaginatedResponse<MediaDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<MediaDto>> GetAllAsync()
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, file_name AS FileName, file_path AS FilePath, content_type AS ContentType,
                   file_size_bytes AS FileSizeBytes, alt_text AS AltText, uploaded_by AS UploadedBy, created_at AS CreatedAt
            FROM media
            WHERE is_deleted = FALSE
            ORDER BY created_at DESC;";

        return await conn.QueryAsync<MediaDto>(sql);
    }

    public async Task<MediaDto?> GetByIdAsync(Guid id)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, file_name AS FileName, file_path AS FilePath, content_type AS ContentType,
                   file_size_bytes AS FileSizeBytes, alt_text AS AltText, uploaded_by AS UploadedBy, created_at AS CreatedAt
            FROM media
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<MediaDto>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(string fileName, string filePath, string contentType, long fileSizeBytes, string? altText, string uploadedBy)
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            INSERT INTO media (file_name, file_path, content_type, file_size_bytes, alt_text, uploaded_by, created_at)
            VALUES (@fileName, @filePath, @contentType, @fileSizeBytes, @altText, @uploadedBy, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { fileName, filePath, contentType, fileSizeBytes, altText, uploadedBy });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _tenantDb.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE media SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
