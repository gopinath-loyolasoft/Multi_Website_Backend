using System.Data;

namespace CollegeWebsitePlatform.Application.Common.Interfaces;

public interface IPlatformDbConnectionFactory
{
    IDbConnection CreateConnection();
}
