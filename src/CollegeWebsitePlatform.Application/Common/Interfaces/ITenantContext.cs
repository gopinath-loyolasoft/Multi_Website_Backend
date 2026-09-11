using System.Data;

namespace CollegeWebsitePlatform.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantCode { get; }
    string Name { get; }
    string Slug { get; }
    string DbConnectionString { get; }
    Guid? DefaultThemeId { get; }
    string TemplateCode { get; }
    bool IsResolved { get; }

    void SetTenant(Guid id, string code, string name, string slug, string dbConnectionString, Guid? defaultThemeId, string templateCode = "ENGINEERING_MODERN");
}

public interface ITenantDbConnectionFactory
{
    IDbConnection CreateConnection();
}
