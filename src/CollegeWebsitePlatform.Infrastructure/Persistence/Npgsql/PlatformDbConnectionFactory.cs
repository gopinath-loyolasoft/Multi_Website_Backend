using System.Data;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CollegeWebsitePlatform.Infrastructure.Persistence.Npgsql;

public class PlatformDbConnectionFactory : IPlatformDbConnectionFactory
{
    private readonly string _connectionString;

    public PlatformDbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PlatformConnection") 
            ?? throw new InvalidOperationException("Connection string 'PlatformConnection' is not configured.");
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
