using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CollegeWebsitePlatform.Application.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace CollegeWebsitePlatform.Tests.Helpers;

/// <summary>
/// Shared utilities for generating test JWT tokens and mock test data.
/// </summary>
public static class TestHelpers
{
    private const string TestJwtKey = "SuperSecretPlatformJwtSigningKeyForMultiTenantCollegeWebsite2026!";
    private const string TestIssuer = "CollegeWebsitePlatform";
    private const string TestAudience = "CollegeWebsitePlatformAudience";

    /// <summary>
    /// Generate a valid JWT token for a given user role and permissions.
    /// </summary>
    public static string GenerateTestToken(
        Guid userId,
        string username,
        string role,
        Guid? tenantId = null,
        string tenantCode = "TEST",
        List<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("tenant_code", tenantCode)
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        if (permissions != null)
        {
            foreach (var perm in permissions)
                claims.Add(new Claim("permissions", perm));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Create a sample LoginRequest.
    /// </summary>
    public static LoginRequest CreateLoginRequest(string username = "admin", string password = "Admin@123") =>
        new() { Username = username, Password = password };

    /// <summary>
    /// Create a sample CreateNewsRequest.
    /// </summary>
    public static CreateNewsRequest CreateSampleNewsRequest() =>
        new()
        {
            Title = "Test News Article",
            Slug = "test-news-article",
            Summary = "This is a test news summary",
            Content = "<p>Test news content</p>",
            ImageUrl = "https://example.com/image.jpg"
        };

    /// <summary>
    /// Create a sample CreateEventRequest.
    /// </summary>
    public static CreateEventRequest CreateSampleEventRequest() =>
        new()
        {
            Title = "Test Event",
            Description = "Test event description",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Main Auditorium"
        };

    /// <summary>
    /// Create a sample CreateBannerRequest.
    /// </summary>
    public static CreateBannerRequest CreateSampleBannerRequest() =>
        new()
        {
            Title = "Test Banner",
            Subtitle = "Test Subtitle",
            ImageUrl = "https://example.com/banner.jpg",
            CtaText = "Learn More",
            CtaUrl = "https://example.com"
        };

    /// <summary>
    /// Create a sample CreateDepartmentRequest.
    /// </summary>
    public static CreateDepartmentRequest CreateSampleDepartmentRequest() =>
        new()
        {
            Name = "Computer Science",
            Code = "CS",
            Description = "Department of Computer Science"
        };

    /// <summary>
    /// Create a sample CreatePageRequest.
    /// </summary>
    public static CreatePageRequest CreateSamplePageRequest() =>
        new()
        {
            Slug = "test-page",
            Title = "Test Page"
        };

    /// <summary>
    /// Create a sample CreateGalleryRequest.
    /// </summary>
    public static CreateGalleryRequest CreateSampleGalleryRequest() =>
        new()
        {
            Title = "Test Gallery",
            Slug = "test-gallery",
            Description = "Test gallery description"
        };

    /// <summary>
    /// Create a sample CreateFacultyRequest.
    /// </summary>
    public static CreateFacultyRequest CreateSampleFacultyRequest(Guid departmentId) =>
        new()
        {
            DepartmentId = departmentId,
            Name = "Dr. Test Faculty",
            Designation = "Professor",
            Qualification = "Ph.D."
        };

    /// <summary>
    /// Create a sample CreateTestimonialRequest.
    /// </summary>
    public static CreateTestimonialRequest CreateSampleTestimonialRequest() =>
        new()
        {
            StudentName = "Test Student",
            Content = "Great college experience",
            Rating = 5
        };
}
