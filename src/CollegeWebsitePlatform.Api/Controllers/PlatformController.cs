using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeWebsitePlatform.Api.Controllers.Platform;

[ApiController]
[Route("api/v1/platform")]
[AllowAnonymous]
public class PlatformController : ControllerBase
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public PlatformController(IPlatformDbConnectionFactory platformDb)
    {
        _platformDb = platformDb;
    }

    /// <summary>
    /// Returns the live directory of provisioned, active colleges directly from the platform PostgreSQL tables (tenants, domains, registry).
    /// </summary>
    [HttpGet("colleges")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicColleges()
    {
        using var conn = _platformDb.CreateConnection();
        var sql = @"
            SELECT t.id, t.tenant_code AS TenantCode, t.name, t.slug, t.status, 
                   t.provisioning_status AS ProvisioningStatus,
                   d.domain AS PrimaryDomain,
                   r.database_name AS DatabaseName,
                   th.name AS ThemeName,
                   t.created_at AS CreatedAt
            FROM tenants t
            LEFT JOIN domains d ON d.tenant_id = t.id AND d.is_primary = TRUE
            LEFT JOIN tenant_database_registry r ON r.tenant_id = t.id
            LEFT JOIN themes th ON th.id = t.default_theme_id
            WHERE t.status = 'ACTIVE'
            ORDER BY t.created_at ASC;";

        var list = await conn.QueryAsync<TenantSummaryDto>(sql);
        return Ok(ApiResponse<IEnumerable<TenantSummaryDto>>.Ok(list));
    }
}
