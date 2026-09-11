using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Entities;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class BannerRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public BannerRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<BannerDto>> GetAllAsync(bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var sql = activeOnly
            ? "SELECT id, title, subtitle, image_url AS ImageUrl, cta_text AS CtaText, cta_url AS CtaUrl, badge_text AS Badge, secondary_cta_text AS SecondaryCtaText, secondary_cta_url AS SecondaryCtaUrl, pillars_json AS PillarsJson, sort_order AS SortOrder, is_active AS IsActive FROM banners WHERE is_active = TRUE AND is_deleted = FALSE ORDER BY sort_order ASC, created_at ASC;"
            : "SELECT id, title, subtitle, image_url AS ImageUrl, cta_text AS CtaText, cta_url AS CtaUrl, badge_text AS Badge, secondary_cta_text AS SecondaryCtaText, secondary_cta_url AS SecondaryCtaUrl, pillars_json AS PillarsJson, sort_order AS SortOrder, is_active AS IsActive FROM banners WHERE is_deleted = FALSE ORDER BY sort_order ASC, created_at ASC;";

        var rows = await conn.QueryAsync<dynamic>(sql);
        var result = new List<BannerDto>();
        foreach (var r in rows)
        {
            result.Add(MapRowToBannerDto(r));
        }
        return result;
    }

    public async Task<BannerDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT id, title, subtitle, image_url AS ImageUrl, cta_text AS CtaText, cta_url AS CtaUrl, badge_text AS Badge, secondary_cta_text AS SecondaryCtaText, secondary_cta_url AS SecondaryCtaUrl, pillars_json AS PillarsJson, sort_order AS SortOrder, is_active AS IsActive FROM banners WHERE id = @id AND is_deleted = FALSE;";
        var r = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        if (r == null) return null;
        return MapRowToBannerDto(r);
    }

    private static BannerDto MapRowToBannerDto(dynamic r)
    {
        var dict = (IDictionary<string, object>)r;
        string? GetString(string key) => dict.TryGetValue(key, out var val) && val != null ? val.ToString() : null;

        List<string> pillars = new();
        var pillarsRaw = GetString("pillars_json") ?? GetString("pillarsjson") ?? GetString("PillarsJson");
        if (!string.IsNullOrWhiteSpace(pillarsRaw))
        {
            try { pillars = System.Text.Json.JsonSerializer.Deserialize<List<string>>(pillarsRaw) ?? new(); }
            catch { }
        }

        Guid bannerId = Guid.Empty;
        if ((dict.TryGetValue("id", out var idObj) || dict.TryGetValue("Id", out idObj)) && idObj != null)
        {
            if (idObj is Guid g) bannerId = g;
            else _ = Guid.TryParse(idObj.ToString(), out bannerId);
        }

        int sortOrder = 0;
        if ((dict.TryGetValue("sort_order", out var so) || dict.TryGetValue("sortorder", out so) || dict.TryGetValue("SortOrder", out so)) && so != null)
        {
            try { sortOrder = Convert.ToInt32(so); } catch { }
        }

        bool isActive = true;
        if ((dict.TryGetValue("is_active", out var ia) || dict.TryGetValue("isactive", out ia) || dict.TryGetValue("IsActive", out ia)) && ia != null)
        {
            try { isActive = Convert.ToBoolean(ia); } catch { }
        }

        return new BannerDto
        {
            Id = bannerId,
            Title = GetString("title") ?? GetString("Title") ?? "",
            Subtitle = GetString("subtitle") ?? GetString("Subtitle"),
            ImageUrl = GetString("image_url") ?? GetString("imageurl") ?? GetString("ImageUrl") ?? "",
            CtaText = GetString("cta_text") ?? GetString("ctatext") ?? GetString("CtaText"),
            CtaUrl = GetString("cta_url") ?? GetString("ctaurl") ?? GetString("CtaUrl"),
            Badge = GetString("badge_text") ?? GetString("badgetext") ?? GetString("badge") ?? GetString("Badge"),
            SecondaryCtaText = GetString("secondary_cta_text") ?? GetString("secondaryctatext") ?? GetString("SecondaryCtaText"),
            SecondaryCtaUrl = GetString("secondary_cta_url") ?? GetString("secondaryctaurl") ?? GetString("SecondaryCtaUrl"),
            Pillars = pillars,
            SortOrder = sortOrder,
            IsActive = isActive
        };
    }

    public async Task<Guid> CreateAsync(CreateBannerRequest req)
    {
        using var conn = _db.CreateConnection();
        var pillarsJson = req.Pillars != null ? System.Text.Json.JsonSerializer.Serialize(req.Pillars) : null;
        var sql = @"
            INSERT INTO banners (title, subtitle, image_url, cta_text, cta_url, badge_text, secondary_cta_text, secondary_cta_url, pillars_json, sort_order, is_active, created_at, updated_at)
            VALUES (@Title, @Subtitle, @ImageUrl, @CtaText, @CtaUrl, @Badge, @SecondaryCtaText, @SecondaryCtaUrl, @pillarsJson, @SortOrder, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new {
            req.Title, req.Subtitle, req.ImageUrl, req.CtaText, req.CtaUrl,
            req.Badge, req.SecondaryCtaText, req.SecondaryCtaUrl, pillarsJson, req.SortOrder
        });
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateBannerRequest req)
    {
        using var conn = _db.CreateConnection();
        var pillarsJson = req.Pillars != null ? System.Text.Json.JsonSerializer.Serialize(req.Pillars) : null;
        var sql = @"
            UPDATE banners 
            SET title = @Title, subtitle = @Subtitle, image_url = @ImageUrl, cta_text = @CtaText, cta_url = @CtaUrl, 
                badge_text = @Badge, secondary_cta_text = @SecondaryCtaText, secondary_cta_url = @SecondaryCtaUrl, pillars_json = @pillarsJson,
                sort_order = @SortOrder, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new {
            id, req.Title, req.Subtitle, req.ImageUrl, req.CtaText, req.CtaUrl,
            req.Badge, req.SecondaryCtaText, req.SecondaryCtaUrl, pillarsJson, req.SortOrder, req.IsActive
        });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE banners SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task<bool> ReorderAsync(List<Guid> bannerIds)
    {
        using var conn = _db.CreateConnection();
        for (int i = 0; i < bannerIds.Count; i++)
        {
            await conn.ExecuteAsync("UPDATE banners SET sort_order = @sortOrder, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id = bannerIds[i], sortOrder = i + 1 });
        }
        return true;
    }
}
