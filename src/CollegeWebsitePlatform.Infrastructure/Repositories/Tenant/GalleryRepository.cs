using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class GalleryRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public GalleryRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<GalleryDto>> GetAllPaginatedAsync(PaginationFilter filter, bool publishedOnly = false)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (publishedOnly) conditions.Add("is_published = TRUE");
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(title) LIKE LOWER(@Search) OR LOWER(description) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM galleries {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, title, slug, description, cover_image_url AS CoverImageUrl, COALESCE(category, 'Campus') AS Category, is_published AS IsPublished
            FROM galleries 
            {whereClause}
            ORDER BY created_at DESC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<GalleryDto>(dataSql, param)).ToList();

        foreach (var g in items)
        {
            var images = await conn.QueryAsync<GalleryImageDto>(
                "SELECT id, gallery_id AS GalleryId, image_url AS ImageUrl, caption, sort_order AS SortOrder FROM gallery_images WHERE gallery_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, created_at ASC;",
                new { id = g.Id });
            g.Images = images.ToList();
        }

        return new PaginatedResponse<GalleryDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<GalleryDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT id, title, slug, description, cover_image_url AS CoverImageUrl, COALESCE(category, 'Campus') AS Category, is_published AS IsPublished FROM galleries WHERE id = @id AND is_deleted = FALSE;";
        var gallery = await conn.QueryFirstOrDefaultAsync<GalleryDto>(sql, new { id });
        if (gallery == null) return null;

        var images = await conn.QueryAsync<GalleryImageDto>(
            "SELECT id, gallery_id AS GalleryId, image_url AS ImageUrl, caption, sort_order AS SortOrder FROM gallery_images WHERE gallery_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, created_at ASC;",
            new { id });
        gallery.Images = images.ToList();

        return gallery;
    }

    public async Task<GalleryDto?> GetBySlugAsync(string slug)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT id, title, slug, description, cover_image_url AS CoverImageUrl, COALESCE(category, 'Campus') AS Category, is_published AS IsPublished FROM galleries WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE;";
        var gallery = await conn.QueryFirstOrDefaultAsync<GalleryDto>(sql, new { slug });
        if (gallery == null) return null;

        var images = await conn.QueryAsync<GalleryImageDto>(
            "SELECT id, gallery_id AS GalleryId, image_url AS ImageUrl, caption, sort_order AS SortOrder FROM gallery_images WHERE gallery_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, created_at ASC;",
            new { id = gallery.Id });
        gallery.Images = images.ToList();

        return gallery;
    }

    public async Task<Guid> CreateAsync(CreateGalleryRequest req)
    {
        using var conn = _db.CreateConnection();
        var category = string.IsNullOrWhiteSpace(req.Category) ? "Campus" : req.Category.Trim();
        var sql = @"
            INSERT INTO galleries (title, slug, description, cover_image_url, category, is_published, created_at, updated_at)
            VALUES (@Title, LOWER(@Slug), @Description, @CoverImageUrl, @Category, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { req.Title, req.Slug, req.Description, req.CoverImageUrl, Category = category });
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateGalleryRequest req)
    {
        using var conn = _db.CreateConnection();
        var category = string.IsNullOrWhiteSpace(req.Category) ? "Campus" : req.Category.Trim();
        var sql = @"
            UPDATE galleries 
            SET title = @Title, slug = LOWER(@Slug), description = @Description, cover_image_url = @CoverImageUrl, category = @Category, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.Title, req.Slug, req.Description, req.CoverImageUrl, Category = category });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE galleries SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task<Guid> AddImageAsync(Guid galleryId, AddGalleryImageRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO gallery_images (gallery_id, image_url, caption, sort_order, created_at)
            VALUES (@galleryId, @ImageUrl, @Caption, @SortOrder, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { galleryId, req.ImageUrl, req.Caption, req.SortOrder });
    }

    public async Task<bool> DeleteImageAsync(Guid imageId)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE gallery_images SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @imageId;", new { imageId });
        return rows > 0;
    }
}
