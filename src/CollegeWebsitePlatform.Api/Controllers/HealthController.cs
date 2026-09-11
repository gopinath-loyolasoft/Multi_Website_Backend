using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace CollegeWebsitePlatform.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly IPlatformDbConnectionFactory _platformDb;

    public HealthController(IPlatformDbConnectionFactory platformDb)
    {
        _platformDb = platformDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        using var conn = _platformDb.CreateConnection();
        conn.Open();
        var pgVersion = await conn.ExecuteScalarAsync<string>("SELECT version();");

        return Ok(new
        {
            status = "Healthy",
            message = "PostgreSQL Connected successfully via Dapper + Npgsql",
            postgresVersion = pgVersion,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("live")]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "Live", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadiness()
    {
        try
        {
            using var conn = _platformDb.CreateConnection();
            conn.Open();
            var result = await conn.ExecuteScalarAsync<int>("SELECT 1;");
            return Ok(new { status = "Ready", database = "Connected", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }
}
