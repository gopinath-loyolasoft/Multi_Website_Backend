using System.Text.Json;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Platform;

public class TemplateRepository
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public TemplateRepository(IPlatformDbConnectionFactory platformDb) => _platformDb = platformDb;

    public async Task<IEnumerable<TemplateDto>> GetAllAsync()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT t.id AS Id, t.code AS Code, t.name AS Name, t.category AS Category,
                   t.description AS Description, t.default_theme_id AS DefaultThemeId,
                   t.recommended_features::text AS RecommendedFeaturesRaw,
                   t.supported_themes::text AS SupportedThemesRaw,
                   t.starter_pages::text AS StarterPagesRaw,
                   t.starter_menus::text AS StarterMenusRaw,
                   t.is_active AS IsActive, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   th.code AS DefaultThemeCode, th.name AS DefaultThemeName,
                   th.configuration::text AS ThemeConfigRaw,
                   (SELECT COUNT(*)::int FROM tenants tn WHERE tn.template_code = t.code) AS CollegesCount
            FROM templates t
            LEFT JOIN themes th ON th.id = t.default_theme_id
            WHERE t.is_active = TRUE
            ORDER BY t.code ASC;";

        var rows = await conn.QueryAsync<dynamic>(sql);
        return rows.Select(MapRowToDto);
    }

    public async Task<TemplateDto?> GetByIdAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT t.id AS Id, t.code AS Code, t.name AS Name, t.category AS Category,
                   t.description AS Description, t.default_theme_id AS DefaultThemeId,
                   t.recommended_features::text AS RecommendedFeaturesRaw,
                   t.supported_themes::text AS SupportedThemesRaw,
                   t.starter_pages::text AS StarterPagesRaw,
                   t.starter_menus::text AS StarterMenusRaw,
                   t.is_active AS IsActive, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   th.code AS DefaultThemeCode, th.name AS DefaultThemeName,
                   th.configuration::text AS ThemeConfigRaw,
                   (SELECT COUNT(*)::int FROM tenants tn WHERE tn.template_code = t.code) AS CollegesCount
            FROM templates t
            LEFT JOIN themes th ON th.id = t.default_theme_id
            WHERE t.id = @id;";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        return row != null ? MapRowToDto(row) : null;
    }

    public async Task<TemplateDto?> GetByCodeAsync(string code)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT t.id AS Id, t.code AS Code, t.name AS Name, t.category AS Category,
                   t.description AS Description, t.default_theme_id AS DefaultThemeId,
                   t.recommended_features::text AS RecommendedFeaturesRaw,
                   t.supported_themes::text AS SupportedThemesRaw,
                   t.starter_pages::text AS StarterPagesRaw,
                   t.starter_menus::text AS StarterMenusRaw,
                   t.is_active AS IsActive, t.created_at AS CreatedAt, t.updated_at AS UpdatedAt,
                   th.code AS DefaultThemeCode, th.name AS DefaultThemeName,
                   th.configuration::text AS ThemeConfigRaw,
                   (SELECT COUNT(*)::int FROM tenants tn WHERE tn.template_code = t.code) AS CollegesCount
            FROM templates t
            LEFT JOIN themes th ON th.id = t.default_theme_id
            WHERE t.code = @code;";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { code });
        return row != null ? MapRowToDto(row) : null;
    }

    public async Task<Guid> CreateAsync(CreateTemplateRequest req)
    {
        using var conn = _platformDb.CreateConnection();
        var id = Guid.NewGuid();
        var recommendedFeaturesJson = JsonSerializer.Serialize(req.RecommendedFeatures ?? new List<string>());
        var supportedThemesJson = JsonSerializer.Serialize(req.SupportedThemes ?? new List<Guid>());
        var starterPagesJson = JsonSerializer.Serialize(req.StarterPages ?? new List<object>());
        var starterMenusJson = JsonSerializer.Serialize(req.StarterMenus ?? new List<object>());

        var sql = @"
            INSERT INTO templates (id, code, name, category, description, default_theme_id,
                                   recommended_features, supported_themes, starter_pages, starter_menus,
                                   is_active, created_at, updated_at)
            VALUES (@id, @Code, @Name, @Category, @Description, @DefaultThemeId,
                    @recommendedFeaturesJson::jsonb, @supportedThemesJson::jsonb,
                    @starterPagesJson::jsonb, @starterMenusJson::jsonb,
                    @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";

        await conn.ExecuteAsync(sql, new
        {
            id,
            req.Code,
            req.Name,
            req.Category,
            req.Description,
            req.DefaultThemeId,
            recommendedFeaturesJson,
            supportedThemesJson,
            starterPagesJson,
            starterMenusJson,
            req.IsActive
        });

        return id;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTemplateRequest req)
    {
        using var conn = _platformDb.CreateConnection();
        var recommendedFeaturesJson = JsonSerializer.Serialize(req.RecommendedFeatures ?? new List<string>());
        var supportedThemesJson = JsonSerializer.Serialize(req.SupportedThemes ?? new List<Guid>());
        var starterPagesJson = JsonSerializer.Serialize(req.StarterPages ?? new List<object>());
        var starterMenusJson = JsonSerializer.Serialize(req.StarterMenus ?? new List<object>());

        var sql = @"
            UPDATE templates 
            SET name = @Name, category = @Category, description = @Description,
                default_theme_id = @DefaultThemeId,
                recommended_features = @recommendedFeaturesJson::jsonb,
                supported_themes = @supportedThemesJson::jsonb,
                starter_pages = @starterPagesJson::jsonb,
                starter_menus = @starterMenusJson::jsonb,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new
        {
            id,
            req.Name,
            req.Category,
            req.Description,
            req.DefaultThemeId,
            recommendedFeaturesJson,
            supportedThemesJson,
            starterPagesJson,
            starterMenusJson,
            req.IsActive
        });

        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _platformDb.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM templates WHERE id = @id;", new { id });
        return rows > 0;
    }

    public async Task<bool> AssignToTenantAsync(Guid tenantId, string templateCode)
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            UPDATE tenants
            SET template_code = @templateCode,
                default_theme_id = COALESCE((SELECT default_theme_id FROM templates WHERE code = @templateCode), default_theme_id),
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @tenantId;";

        var rows = await conn.ExecuteAsync(sql, new { tenantId, templateCode });
        return rows > 0;
    }

    private static TemplateDto MapRowToDto(dynamic r)
    {
        var recommendedFeatures = new List<string>();
        var supportedThemes = new List<Guid>();
        object? starterPages = null;
        object? starterMenus = null;
        object? themeConfig = null;
        int collegesCount = 0;

        try
        {
            if (r.collegescount != null)
                collegesCount = Convert.ToInt32(r.collegescount);
        }
        catch { }

        try
        {
            if (r.recommendedfeaturesraw != null)
                recommendedFeatures = JsonSerializer.Deserialize<List<string>>((string)r.recommendedfeaturesraw.ToString()) ?? new List<string>();
        }
        catch { }

        try
        {
            if (r.supportedthemesraw != null)
                supportedThemes = JsonSerializer.Deserialize<List<Guid>>((string)r.supportedthemesraw.ToString()) ?? new List<Guid>();
        }
        catch { }

        try
        {
            if (r.starterpagesraw != null)
                starterPages = JsonSerializer.Deserialize<object>((string)r.starterpagesraw.ToString());
        }
        catch { }

        try
        {
            if (r.startermenusraw != null)
                starterMenus = JsonSerializer.Deserialize<object>((string)r.startermenusraw.ToString());
        }
        catch { }

        try
        {
            if (r.themeconfigraw != null)
                themeConfig = JsonSerializer.Deserialize<object>((string)r.themeconfigraw.ToString());
        }
        catch { }

        return new TemplateDto
        {
            Id = r.id,
            Code = r.code,
            Name = r.name,
            Category = r.category,
            Description = r.description,
            DefaultThemeId = r.defaultthemeid,
            DefaultThemeCode = r.defaultthemecode,
            DefaultThemeName = r.defaultthemename,
            ThemeConfiguration = themeConfig,
            SupportedThemes = supportedThemes,
            RecommendedFeatures = recommendedFeatures,
            StarterPages = starterPages,
            StarterMenus = starterMenus,
            IsActive = r.isactive,
            CollegesCount = collegesCount,
            CreatedAt = r.createdat ?? DateTime.UtcNow,
            UpdatedAt = r.updatedat ?? DateTime.UtcNow
        };
    }
}
