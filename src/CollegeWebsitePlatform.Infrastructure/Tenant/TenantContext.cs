using CollegeWebsitePlatform.Application.Common.Interfaces;

namespace CollegeWebsitePlatform.Infrastructure.Tenant;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; } = Guid.Empty;
    public string TenantCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string DbConnectionString { get; private set; } = string.Empty;
    public Guid? DefaultThemeId { get; private set; }
    public string TemplateCode { get; private set; } = "ENGINEERING_MODERN";
    public bool IsResolved => TenantId != Guid.Empty && !string.IsNullOrWhiteSpace(DbConnectionString);

    public void SetTenant(Guid id, string code, string name, string slug, string dbConnectionString, Guid? defaultThemeId, string templateCode = "ENGINEERING_MODERN")
    {
        TenantId = id;
        TenantCode = code;
        Name = name;
        Slug = slug;
        DbConnectionString = dbConnectionString;
        DefaultThemeId = defaultThemeId;
        TemplateCode = string.IsNullOrWhiteSpace(templateCode) ? "ENGINEERING_MODERN" : templateCode;
    }
}
