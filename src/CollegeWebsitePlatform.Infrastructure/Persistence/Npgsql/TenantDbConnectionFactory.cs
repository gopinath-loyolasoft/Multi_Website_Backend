using System.Data;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using Npgsql;

namespace CollegeWebsitePlatform.Infrastructure.Persistence.Npgsql;

public class TenantDbConnectionFactory : ITenantDbConnectionFactory
{
    private readonly ITenantContext _tenantContext;

    public TenantDbConnectionFactory(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public IDbConnection CreateConnection()
    {
        if (!_tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Cannot create tenant DB connection: Tenant is not resolved for the current request.");
        }

        return new NpgsqlConnection(_tenantContext.DbConnectionString);
    }
}
