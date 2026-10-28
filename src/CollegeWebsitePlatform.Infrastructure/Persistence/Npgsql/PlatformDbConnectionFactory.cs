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
            ?? configuration["Platform:MasterConnectionString"]
            ?? "Host=localhost;Port=5433;Database=dinamic_college_website;Username=postgres;Password=root;Include Error Detail=true";
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
}
