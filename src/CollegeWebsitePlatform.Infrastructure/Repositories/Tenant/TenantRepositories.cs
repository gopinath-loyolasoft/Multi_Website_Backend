using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Entities;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class PageRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public PageRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<Page>> GetAllPaginatedAsync(PaginationFilter filter)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(title) LIKE LOWER(@Search) OR LOWER(slug) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM pages {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, slug, title, meta_description AS MetaDescription, is_published AS IsPublished,
                   (SELECT COUNT(*) FROM page_sections ps WHERE ps.page_id = pages.id AND ps.is_deleted = FALSE)::int AS SectionCount,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM pages 
            {whereClause}
            ORDER BY created_at ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<Page>(dataSql, param)).ToList();

        return new PaginatedResponse<Page> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<Page>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Page>(
            "SELECT id, slug, title, meta_description AS MetaDescription, is_published AS IsPublished, (SELECT COUNT(*) FROM page_sections ps WHERE ps.page_id = pages.id AND ps.is_deleted = FALSE)::int AS SectionCount, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM pages WHERE is_deleted = FALSE ORDER BY created_at ASC;");
    }

    public async Task<Page?> GetBySlugAsync(string slug)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Page>(
            "SELECT id, slug, title, meta_description AS MetaDescription, is_published AS IsPublished, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM pages WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE;", 
            new { slug });
    }

    public async Task<Page?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Page>(
            "SELECT id, slug, title, meta_description AS MetaDescription, is_published AS IsPublished, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM pages WHERE id = @id AND is_deleted = FALSE;", 
            new { id });
    }

    public async Task<Guid> CreateAsync(Page page)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO pages (slug, title, meta_description, is_published, created_at, updated_at) 
            VALUES (LOWER(@Slug), @Title, @MetaDescription, @IsPublished, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, page);
    }

    public async Task<bool> UpdateAsync(Page page)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE pages 
            SET slug = LOWER(@Slug), title = @Title, meta_description = @MetaDescription, is_published = @IsPublished, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, page);
        return rows > 0;
    }

    public async Task<bool> SetPublishStatusAsync(Guid id, bool isPublished)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE pages SET is_published = @isPublished, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, isPublished });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE pages SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}

public class SectionRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public SectionRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<PageSection>> GetByPageIdAsync(Guid pageId)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, page_id AS PageId, section_type AS SectionType, title, subtitle, 
                   content_json::text AS ContentJson, settings_json::text AS SettingsJson,
                   sort_order AS SortOrder, is_visible AS IsVisible, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM page_sections 
            WHERE page_id = @pageId AND is_deleted = FALSE 
            ORDER BY sort_order ASC, created_at ASC;";
        return await conn.QueryAsync<PageSection>(sql, new { pageId });
    }

    public async Task<PageSection?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, page_id AS PageId, section_type AS SectionType, title, subtitle, 
                   content_json::text AS ContentJson, settings_json::text AS SettingsJson,
                   sort_order AS SortOrder, is_visible AS IsVisible, 
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM page_sections 
            WHERE id = @id AND is_deleted = FALSE;";
        return await conn.QueryFirstOrDefaultAsync<PageSection>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(PageSection section)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible, created_at, updated_at) 
            VALUES (@PageId, @SectionType, @Title, @Subtitle, @ContentJson::jsonb, COALESCE(@SettingsJson, '{}')::jsonb, @SortOrder, @IsVisible, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, section);
    }

    public async Task<bool> UpdateAsync(PageSection section)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE page_sections 
            SET section_type = @SectionType, title = @Title, subtitle = @Subtitle, 
                content_json = @ContentJson::jsonb, settings_json = COALESCE(@SettingsJson, '{}')::jsonb,
                sort_order = @SortOrder, is_visible = @IsVisible, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, section);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE page_sections SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task UpdateSortOrderAsync(Guid id, int sortOrder)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("UPDATE page_sections SET sort_order = @sortOrder, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id, sortOrder });
    }
}

public class MenuRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public MenuRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<Menu>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<Menu>("SELECT id, code, title, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM menus WHERE is_deleted = FALSE ORDER BY created_at ASC;");
    }

    public async Task<Menu?> GetByCodeAsync(string code)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Menu>(
            "SELECT id, code, title, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM menus WHERE LOWER(code) = LOWER(@code) AND is_deleted = FALSE;", 
            new { code });
    }

    public async Task<Menu?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Menu>(
            "SELECT id, code, title, is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt, updated_at AS UpdatedAt FROM menus WHERE id = @id AND is_deleted = FALSE;", 
            new { id });
    }

    public async Task<IEnumerable<MenuItem>> GetItemsByMenuIdAsync(Guid menuId)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, menu_id AS MenuId, parent_id AS ParentId, item_type AS ItemType, 
                   COALESCE(target_type, 'DYNAMIC_MODULE') AS TargetType, page_id AS PageId,
                   title, url, sort_order AS SortOrder, is_visible AS IsVisible,
                   COALESCE(open_in_new_tab, FALSE) AS OpenInNewTab, COALESCE(icon, 'FileText') AS Icon,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt 
            FROM menu_items 
            WHERE menu_id = @menuId AND is_deleted = FALSE 
            ORDER BY sort_order ASC, created_at ASC;";
        return await conn.QueryAsync<MenuItem>(sql, new { menuId });
    }

    public async Task<Guid> CreateMenuAsync(Menu menu)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            "INSERT INTO menus (code, title, created_at, updated_at) VALUES (UPPER(@Code), @Title, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING id;", menu);
    }

    public async Task<bool> UpdateMenuAsync(Guid id, string title)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE menus SET title = @title, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id, title });
        return rows > 0;
    }

    public async Task<bool> DeleteMenuAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE menus SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task<Guid> AddMenuItemAsync(MenuItem item)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO menu_items (menu_id, parent_id, item_type, target_type, page_id, title, url, sort_order, is_visible, open_in_new_tab, icon, created_at) 
            VALUES (@MenuId, @ParentId, @ItemType, @TargetType, @PageId, @Title, @Url, @SortOrder, @IsVisible, @OpenInNewTab, @Icon, CURRENT_TIMESTAMP) 
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task<bool> UpdateMenuItemAsync(Guid id, Guid? parentId, string itemType, string targetType, Guid? pageId, string title, string? url, int sortOrder, bool isVisible, bool openInNewTab, string? icon)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE menu_items 
            SET parent_id = @parentId, item_type = @itemType, target_type = @targetType, page_id = @pageId, 
                title = @title, url = @url, sort_order = @sortOrder, is_visible = @isVisible,
                open_in_new_tab = @openInNewTab, icon = @icon
            WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, parentId, itemType, targetType, pageId, title, url, sortOrder, isVisible, openInNewTab, icon });
        return rows > 0;
    }

    public async Task<bool> SetMenuItemVisibilityAsync(Guid id, bool isVisible)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE menu_items SET is_visible = @isVisible WHERE id = @id;", new { id, isVisible });
        return rows > 0;
    }

    public async Task<bool> DeleteMenuItemAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE menu_items SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task<bool> ReorderMenuItemsAsync(List<Guid> itemIds)
    {
        using var conn = _db.CreateConnection();
        for (int i = 0; i < itemIds.Count; i++)
        {
            await conn.ExecuteAsync("UPDATE menu_items SET sort_order = @sortOrder WHERE id = @id;", new { id = itemIds[i], sortOrder = i + 1 });
        }
        return true;
    }
}

public class NewsRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public NewsRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<News>> GetAllPaginatedAsync(PaginationFilter filter, bool publishedOnly = false)
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
            conditions.Add("(LOWER(title) LIKE LOWER(@Search) OR LOWER(summary) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM news {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, title, slug, summary, content, image_url AS ImageUrl, published_date AS PublishedDate, 
                   is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM news 
            {whereClause}
            ORDER BY published_date DESC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<News>(dataSql, param)).ToList();

        return new PaginatedResponse<News> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<News>> GetAllAsync(bool publishedOnly = false, int limit = 50)
    {
        using var conn = _db.CreateConnection();
        var sql = publishedOnly
            ? "SELECT id, title, slug, summary, content, image_url AS ImageUrl, published_date AS PublishedDate, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM news WHERE is_published = TRUE AND is_deleted = FALSE ORDER BY published_date DESC LIMIT @limit;"
            : "SELECT id, title, slug, summary, content, image_url AS ImageUrl, published_date AS PublishedDate, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM news WHERE is_deleted = FALSE ORDER BY published_date DESC LIMIT @limit;";
        return await conn.QueryAsync<News>(sql, new { limit });
    }

    public async Task<News?> GetBySlugAsync(string slug)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<News>(
            "SELECT id, title, slug, summary, content, image_url AS ImageUrl, published_date AS PublishedDate, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM news WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE;", 
            new { slug });
    }

    public async Task<News?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<News>(
            "SELECT id, title, slug, summary, content, image_url AS ImageUrl, published_date AS PublishedDate, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM news WHERE id = @id AND is_deleted = FALSE;", 
            new { id });
    }

    public async Task<Guid> CreateAsync(News news)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO news (title, slug, summary, content, image_url, published_date, is_published, created_at, updated_at) 
            VALUES (@Title, LOWER(@Slug), @Summary, @Content, @ImageUrl, CURRENT_TIMESTAMP, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, news);
    }

    public async Task<bool> UpdateAsync(News news)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE news 
            SET title = @Title, slug = LOWER(@Slug), summary = @Summary, content = @Content, image_url = @ImageUrl, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, news);
        return rows > 0;
    }

    public async Task<bool> SetPublishStatusAsync(Guid id, bool isPublished)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE news SET is_published = @isPublished, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, isPublished });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE news SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}

public class EventRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public EventRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<Event>> GetAllPaginatedAsync(PaginationFilter filter, bool publishedOnly = false)
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
            conditions.Add("(LOWER(title) LIKE LOWER(@Search) OR LOWER(location) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM events {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, title, description, event_date AS EventDate, location, image_url AS ImageUrl, 
                   is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt 
            FROM events 
            {whereClause}
            ORDER BY event_date ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<Event>(dataSql, param)).ToList();

        return new PaginatedResponse<Event> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<Event>> GetAllAsync(bool publishedOnly = false, int limit = 50)
    {
        using var conn = _db.CreateConnection();
        var sql = publishedOnly
            ? "SELECT id, title, description, event_date AS EventDate, location, image_url AS ImageUrl, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM events WHERE is_published = TRUE AND is_deleted = FALSE ORDER BY event_date ASC LIMIT @limit;"
            : "SELECT id, title, description, event_date AS EventDate, location, image_url AS ImageUrl, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM events WHERE is_deleted = FALSE ORDER BY event_date ASC LIMIT @limit;";
        return await conn.QueryAsync<Event>(sql, new { limit });
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Event>(
            "SELECT id, title, description, event_date AS EventDate, location, image_url AS ImageUrl, is_published AS IsPublished, created_at AS CreatedAt, updated_at AS UpdatedAt FROM events WHERE id = @id AND is_deleted = FALSE;", 
            new { id });
    }

    public async Task<Guid> CreateAsync(Event evt)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO events (title, description, event_date, location, image_url, is_published, created_at, updated_at) 
            VALUES (@Title, @Description, @EventDate, @Location, @ImageUrl, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, evt);
    }

    public async Task<bool> UpdateAsync(Event evt)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE events 
            SET title = @Title, description = @Description, event_date = @EventDate, location = @Location, image_url = @ImageUrl, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, evt);
        return rows > 0;
    }

    public async Task<bool> SetPublishStatusAsync(Guid id, bool isPublished)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE events SET is_published = @isPublished, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, isPublished });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE events SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}

public class NoticeRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public NoticeRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<Notice>> GetAllPaginatedAsync(PaginationFilter filter, bool publishedOnly = false)
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
            conditions.Add("(LOWER(title) LIKE LOWER(@Search) OR LOWER(summary) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM notices {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, title, slug, summary, content, pdf_url AS PdfUrl, category AS Category,
                   publish_date AS PublishDate, expiry_date AS ExpiryDate,
                   is_published AS IsPublished, is_important AS IsImportant,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM notices
            {whereClause}
            ORDER BY publish_date DESC
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<Notice>(dataSql, param)).ToList();

        return new PaginatedResponse<Notice> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<Notice>> GetAllAsync(bool publishedOnly = false, int limit = 50)
    {
        using var conn = _db.CreateConnection();
        var sql = publishedOnly
            ? @"SELECT id, title, slug, summary, content, pdf_url AS PdfUrl, category AS Category,
                      publish_date AS PublishDate, expiry_date AS ExpiryDate,
                      is_published AS IsPublished, is_important AS IsImportant,
                      created_at AS CreatedAt, updated_at AS UpdatedAt
               FROM notices WHERE is_published = TRUE AND is_deleted = FALSE ORDER BY publish_date DESC LIMIT @limit;"
            : @"SELECT id, title, slug, summary, content, pdf_url AS PdfUrl, category AS Category,
                      publish_date AS PublishDate, expiry_date AS ExpiryDate,
                      is_published AS IsPublished, is_important AS IsImportant,
                      created_at AS CreatedAt, updated_at AS UpdatedAt
               FROM notices WHERE is_deleted = FALSE ORDER BY publish_date DESC LIMIT @limit;";
        return await conn.QueryAsync<Notice>(sql, new { limit });
    }

    public async Task<Notice?> GetBySlugAsync(string slug)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Notice>(
            @"SELECT id, title, slug, summary, content, pdf_url AS PdfUrl, category AS Category,
                     publish_date AS PublishDate, expiry_date AS ExpiryDate,
                     is_published AS IsPublished, is_important AS IsImportant,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM notices WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE;",
            new { slug });
    }

    public async Task<Notice?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Notice>(
            @"SELECT id, title, slug, summary, content, pdf_url AS PdfUrl, category AS Category,
                     publish_date AS PublishDate, expiry_date AS ExpiryDate,
                     is_published AS IsPublished, is_important AS IsImportant,
                     created_at AS CreatedAt, updated_at AS UpdatedAt
              FROM notices WHERE id = @id AND is_deleted = FALSE;",
            new { id });
    }

    public async Task<Guid> CreateAsync(Notice notice)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO notices (title, slug, summary, content, pdf_url, category, publish_date, expiry_date, is_published, is_important, created_at, updated_at)
            VALUES (@Title, LOWER(@Slug), @Summary, @Content, @PdfUrl, @Category, CURRENT_TIMESTAMP, @ExpiryDate, FALSE, @IsImportant, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, notice);
    }

    public async Task<bool> UpdateAsync(Notice notice)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE notices
            SET title = @Title, slug = LOWER(@Slug), summary = @Summary, content = @Content,
                pdf_url = @PdfUrl, category = @Category, expiry_date = @ExpiryDate, is_important = @IsImportant,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, notice);
        return rows > 0;
    }

    public async Task<bool> SetPublishStatusAsync(Guid id, bool isPublished)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE notices SET is_published = @isPublished, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, isPublished });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE notices SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}

public class MarqueeRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public MarqueeRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<MarqueeSettings> GetSettingsAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, is_active AS IsActive, speed, pause_on_hover AS PauseOnHover, 
                   bg_color AS BgColor, text_color AS TextColor, badge_bg_color AS BadgeBgColor, 
                   updated_at AS UpdatedAt
            FROM marquee_settings 
            LIMIT 1;";
        var settings = await conn.QueryFirstOrDefaultAsync<MarqueeSettings>(sql);
        if (settings != null) return settings;

        // Auto insert default if empty
        var defaultSettings = new MarqueeSettings();
        await conn.ExecuteAsync(@"
            INSERT INTO marquee_settings (id, is_active, speed, pause_on_hover, bg_color, text_color, badge_bg_color)
            VALUES (@Id, @IsActive, @Speed, @PauseOnHover, @BgColor, @TextColor, @BadgeBgColor)
            ON CONFLICT DO NOTHING;", defaultSettings);
        return defaultSettings;
    }

    public async Task<bool> UpdateSettingsAsync(MarqueeSettings settings)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE marquee_settings 
            SET is_active = @IsActive, speed = @Speed, pause_on_hover = @PauseOnHover,
                bg_color = @BgColor, text_color = @TextColor, badge_bg_color = @BadgeBgColor,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, settings);
        if (rows == 0)
        {
            var insertSql = @"
                INSERT INTO marquee_settings (id, is_active, speed, pause_on_hover, bg_color, text_color, badge_bg_color, updated_at)
                VALUES (@Id, @IsActive, @Speed, @PauseOnHover, @BgColor, @TextColor, @BadgeBgColor, CURRENT_TIMESTAMP);";
            rows = await conn.ExecuteAsync(insertSql, settings);
        }
        return rows > 0;
    }

    public async Task<IEnumerable<MarqueeItem>> GetItemsAsync(bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var filterSql = activeOnly 
            ? "AND is_active = TRUE AND (start_date IS NULL OR start_date <= CURRENT_TIMESTAMP) AND (end_date IS NULL OR end_date >= CURRENT_TIMESTAMP) " 
            : "";
        var sql = $@"
            SELECT id, badge_text AS BadgeText, message, link_url AS LinkUrl, 
                   is_active AS IsActive, sort_order AS SortOrder,
                   start_date AS StartDate, end_date AS EndDate,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt 
            FROM marquee_items 
            WHERE is_deleted = FALSE {filterSql}
            ORDER BY sort_order ASC, created_at ASC;";
        return await conn.QueryAsync<MarqueeItem>(sql);
    }

    public async Task<MarqueeItem?> GetItemByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, badge_text AS BadgeText, message, link_url AS LinkUrl, 
                   is_active AS IsActive, sort_order AS SortOrder,
                   start_date AS StartDate, end_date AS EndDate,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt, created_at AS CreatedAt 
            FROM marquee_items 
            WHERE id = @id AND is_deleted = FALSE;";
        return await conn.QueryFirstOrDefaultAsync<MarqueeItem>(sql, new { id });
    }

    public async Task<Guid> CreateItemAsync(MarqueeItem item)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO marquee_items (badge_text, message, link_url, is_active, sort_order, start_date, end_date, created_at)
            VALUES (@BadgeText, @Message, @LinkUrl, @IsActive, @SortOrder, @StartDate, @EndDate, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, item);
    }

    public async Task<bool> UpdateItemAsync(MarqueeItem item)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE marquee_items 
            SET badge_text = @BadgeText, message = @Message, link_url = @LinkUrl, 
                is_active = @IsActive, sort_order = @SortOrder,
                start_date = @StartDate, end_date = @EndDate 
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, item);
        return rows > 0;
    }

    public async Task<bool> SetItemActiveStatusAsync(Guid id, bool isActive)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE marquee_items SET is_active = @isActive WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id, isActive });
        return rows > 0;
    }

    public async Task<bool> DeleteItemAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = "UPDATE marquee_items SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }

    public async Task<bool> ReorderItemsAsync(List<Guid> itemIds)
    {
        using var conn = _db.CreateConnection();
        for (int i = 0; i < itemIds.Count; i++)
        {
            await conn.ExecuteAsync("UPDATE marquee_items SET sort_order = @order WHERE id = @id;", new { id = itemIds[i], order = i + 1 });
        }
        return true;
    }
}

public class PreviewSessionRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public PreviewSessionRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task CreateSessionAsync(string token, string createdBy, DateTime expiresAt)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO preview_sessions (id, preview_token, created_by, expires_at, created_at)
            VALUES (gen_random_uuid(), @token, @createdBy, @expiresAt, CURRENT_TIMESTAMP);";
        await conn.ExecuteAsync(sql, new { token, createdBy, expiresAt });
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT COUNT(*) FROM preview_sessions WHERE preview_token = @token AND expires_at > CURRENT_TIMESTAMP;";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { token });
        return count > 0;
    }
}

