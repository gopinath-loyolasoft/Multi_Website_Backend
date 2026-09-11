using System.Text.Json;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class TenantSettingsRepository
{
    private readonly ITenantDbConnectionFactory _tenantDb;

    public TenantSettingsRepository(ITenantDbConnectionFactory tenantDb) => _tenantDb = tenantDb;

    public async Task<TenantSettingsDto?> GetSettingsAsync()
    {
        using var conn = _tenantDb.CreateConnection();
        var sql = @"
            SELECT id, site_name AS SiteName, tagline, logo_url AS LogoUrl, favicon_url AS FaviconUrl,
                   contact_email AS ContactEmail, contact_phone AS ContactPhone,
                   address, social_links::text AS SocialLinks,
                   COALESCE(login_template, 'template2') AS LoginTemplate,
                   login_bg_image_url AS LoginBgImageUrl, updated_at AS UpdatedAt
            FROM tenant_settings
            LIMIT 1;";

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql);
        if (row == null) return null;

        return new TenantSettingsDto
        {
            Id = (Guid)row.id,
            SiteName = (string)row.sitename,
            Tagline = (string?)row.tagline,
            LogoUrl = (string?)row.logourl,
            FaviconUrl = (string?)row.faviconurl ?? "/favicon.ico",
            ContactEmail = (string?)row.contactemail,
            ContactPhone = (string?)row.contactphone,
            Address = (string?)row.address,
            SocialLinks = row.sociallinks != null ? JsonSerializer.Deserialize<object>((string)row.sociallinks) : null,
            LoginTemplate = (string?)row.logintemplate ?? "template2",
            LoginBgImageUrl = (string?)row.loginbgimageurl,
            UpdatedAt = (DateTime)row.updatedat
        };
    }

    public async Task<TenantSettingsDto> UpsertSettingsAsync(UpdateTenantSettingsRequest req)
    {
        using var conn = _tenantDb.CreateConnection();
        var socialJson = req.SocialLinks != null ? JsonSerializer.Serialize(req.SocialLinks) : "{}";

        var checkSql = "SELECT COUNT(*) FROM tenant_settings;";
        var count = await conn.ExecuteScalarAsync<int>(checkSql);

        if (count == 0)
        {
            var insertSql = @"
                INSERT INTO tenant_settings (site_name, tagline, logo_url, favicon_url, contact_email, contact_phone, address, social_links, login_bg_image_url, updated_at)
                VALUES (@SiteName, @Tagline, @LogoUrl, @FaviconUrl, @ContactEmail, @ContactPhone, @Address, @socialJson::jsonb, @LoginBgImageUrl, CURRENT_TIMESTAMP);";
            await conn.ExecuteAsync(insertSql, new { req.SiteName, req.Tagline, req.LogoUrl, req.FaviconUrl, req.ContactEmail, req.ContactPhone, req.Address, socialJson, req.LoginBgImageUrl });
        }
        else
        {
            var updateSql = @"
                UPDATE tenant_settings
                SET site_name = @SiteName, tagline = @Tagline, logo_url = @LogoUrl, favicon_url = @FaviconUrl,
                    contact_email = @ContactEmail, contact_phone = @ContactPhone,
                    address = @Address, social_links = @socialJson::jsonb,
                    login_bg_image_url = @LoginBgImageUrl, updated_at = CURRENT_TIMESTAMP;";
            await conn.ExecuteAsync(updateSql, new { req.SiteName, req.Tagline, req.LogoUrl, req.FaviconUrl, req.ContactEmail, req.ContactPhone, req.Address, socialJson, req.LoginBgImageUrl });
        }

        return (await GetSettingsAsync())!;
    }
}
