using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CollegeWebsitePlatform.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CollegeWebsitePlatform.Infrastructure.Authentication;

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    /// <summary>
    /// Generates a signed JWT access token embedding the user's role and permission codes as claims.
    /// Permissions are stored as a single comma-separated "permissions" claim for compact token size.
    /// </summary>
    public (string Token, DateTime ExpiresAt) GenerateToken(User user, string tenantCode, IEnumerable<string>? permissions = null)
    {
        var keyString     = _config["Jwt:Key"]      ?? "SuperSecretPlatformJwtSigningKeyForMultiTenantCollegeWebsite2026!";
        var issuer        = _config["Jwt:Issuer"]   ?? "CollegeWebsitePlatform";
        var audience      = _config["Jwt:Audience"] ?? "CollegeWebsitePlatformAudience";
        var expiryMinutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out var mins) ? mins : 480;

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name,           user.Username),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim("tenant_id",               user.TenantId?.ToString() ?? string.Empty),
            new Claim("tenant_code",             tenantCode),
            new Claim("full_name",               user.FullName)
        };

        // Permissions claim — comma-separated list of codes (e.g., "PAGE_MANAGE,NEWS_MANAGE,EVENT_MANAGE")
        // SuperAdmin gets an explicit wildcard; CollegeAdmin/Editor get their assigned codes.
        if (permissions != null && permissions.Any())
        {
            claims.Add(new Claim("permissions", string.Join(",", permissions)));
        }

        var token = new JwtSecurityToken(
            issuer:            issuer,
            audience:          audience,
            claims:            claims,
            expires:           expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
