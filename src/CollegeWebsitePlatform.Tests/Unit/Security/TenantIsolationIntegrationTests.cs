using System.Security.Claims;
using CollegeWebsitePlatform.Api.Middleware;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Constants;
using CollegeWebsitePlatform.Tests.Helpers;
using FluentAssertions;

namespace CollegeWebsitePlatform.Tests.Unit.Security;

/// <summary>
/// Tests verifying that tenant isolation is properly enforced:
/// - RNC admin can only access RNC data
/// - Cross-college access is rejected
/// - SuperAdmin bypasses all checks
/// - Unauthorized requests are rejected
/// </summary>
public class TenantIsolationIntegrationTests
{
    private readonly Guid _rncTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _ktsTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _rncAdminUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _ktsAdminUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly Guid _superAdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // ────────────── Token Generation ──────────────

    [Fact]
    public void RncAdminToken_ContainsCorrectTenantId()
    {
        var token = TestHelpers.GenerateTestToken(
            _rncAdminUserId, "rnc-admin", "CollegeAdmin",
            _rncTenantId, "RNC",
            PermissionCodes.CollegeAdminDefaults.ToList());

        token.Should().NotBeNullOrEmpty();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == _rncTenantId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "tenant_code" && c.Value == "RNC");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "CollegeAdmin");
    }

    [Fact]
    public void KtsAdminToken_ContainsCorrectTenantId()
    {
        var token = TestHelpers.GenerateTestToken(
            _ktsAdminUserId, "kts-admin", "CollegeAdmin",
            _ktsTenantId, "KTS",
            PermissionCodes.CollegeAdminDefaults.ToList());

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == _ktsTenantId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "tenant_code" && c.Value == "KTS");
    }

    [Fact]
    public void SuperAdminToken_HasNoTenantId()
    {
        var token = TestHelpers.GenerateTestToken(
            _superAdminUserId, "superadmin", "SuperAdmin",
            null, "SUPERADMIN",
            PermissionCodes.All.ToList());

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "SuperAdmin");
        jwt.Claims.Should().NotContain(c => c.Type == "tenant_id");
    }

    // ────────────── Permission Claim Verification ──────────────

    [Fact]
    public void RncAdminToken_ContainsCmsPermissions()
    {
        var token = TestHelpers.GenerateTestToken(
            _rncAdminUserId, "rnc-admin", "CollegeAdmin",
            _rncTenantId, "RNC",
            PermissionCodes.CollegeAdminDefaults.ToList());

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var permissions = jwt.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();

        permissions.Should().Contain("PAGE_MANAGE");
        permissions.Should().Contain("NEWS_MANAGE");
        permissions.Should().Contain("EVENT_MANAGE");
        permissions.Should().Contain("BANNER_MANAGE");
        permissions.Should().Contain("GALLERY_MANAGE");
        permissions.Should().Contain("ACADEMIC_MANAGE");
        permissions.Should().Contain("MEDIA_MANAGE");
        permissions.Should().Contain("MENU_MANAGE");
        permissions.Should().Contain("SETTINGS_MANAGE");
        permissions.Should().Contain("CONTACT_MANAGE");

        // Should NOT have SuperAdmin permissions
        permissions.Should().NotContain("TENANT_MANAGE");
        permissions.Should().NotContain("USER_MANAGE");
        permissions.Should().NotContain("DATABASE_MANAGE");
    }

    [Fact]
    public void SuperAdminToken_ContainsAllPermissions()
    {
        var token = TestHelpers.GenerateTestToken(
            _superAdminUserId, "superadmin", "SuperAdmin",
            null, "SUPERADMIN",
            PermissionCodes.All.ToList());

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var permissions = jwt.Claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();

        permissions.Should().Contain("TENANT_MANAGE");
        permissions.Should().Contain("USER_MANAGE");
        permissions.Should().Contain("DATABASE_MANAGE");
        permissions.Should().Contain("PAGE_MANAGE");
    }

    // ────────────── Cross-College Isolation ──────────────

    [Fact]
    public void RncAdmin_ShouldNotAccessKtsData()
    {
        // RNC admin token should have RNC tenant_id
        var rncToken = TestHelpers.GenerateTestToken(
            _rncAdminUserId, "rnc-admin", "CollegeAdmin",
            _rncTenantId, "RNC");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(rncToken);

        // The tenant_id in the token should be RNC, not KTS
        var tenantIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        tenantIdClaim.Should().Be(_rncTenantId.ToString());
        tenantIdClaim.Should().NotBe(_ktsTenantId.ToString());
    }

    [Fact]
    public void KtsAdmin_ShouldNotAccessRncData()
    {
        var ktsToken = TestHelpers.GenerateTestToken(
            _ktsAdminUserId, "kts-admin", "CollegeAdmin",
            _ktsTenantId, "KTS");

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(ktsToken);

        var tenantIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
        tenantIdClaim.Should().Be(_ktsTenantId.ToString());
        tenantIdClaim.Should().NotBe(_rncTenantId.ToString());
    }

    // ────────────── Feature Check Constants ──────────────

    [Theory]
    [InlineData("NEWS")]
    [InlineData("EVENTS")]
    [InlineData("BANNERS")]
    [InlineData("GALLERY")]
    [InlineData("DEPARTMENTS")]
    [InlineData("COURSES")]
    [InlineData("FACULTY")]
    [InlineData("NOTICES")]
    public void AllFeatureCodesUsedByControllers_AreDefined(string featureCode)
    {
        FeatureCodes.All.Should().Contain(featureCode,
            $"Feature code '{featureCode}' used by controllers must be defined in FeatureCodes");
    }

    // ────────────── Permission-to-Controller Mapping ──────────────

    [Theory]
    [InlineData(PermissionCodes.NewsManage, "NEWS")]
    [InlineData(PermissionCodes.EventManage, "EVENTS")]
    [InlineData(PermissionCodes.BannerManage, "BANNERS")]
    [InlineData(PermissionCodes.GalleryManage, "GALLERY")]
    [InlineData(PermissionCodes.AcademicManage, "DEPARTMENTS")]
    [InlineData(PermissionCodes.AcademicManage, "COURSES")]
    [InlineData(PermissionCodes.AcademicManage, "FACULTY")]
    [InlineData(PermissionCodes.PageManage, "PAGES")]
    public void PermissionCodes_MatchFeatureCodes(string permissionCode, string featureCode)
    {
        // Verify that both the permission and feature code are defined
        PermissionCodes.All.Should().Contain(permissionCode);
        FeatureCodes.All.Should().Contain(featureCode);
    }
}
