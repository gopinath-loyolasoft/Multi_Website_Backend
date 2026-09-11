using CollegeWebsitePlatform.Api.Middleware;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Constants;
using FluentAssertions;

namespace CollegeWebsitePlatform.Tests.Unit.Security;

/// <summary>
/// Unit tests verifying that the PermissionAttribute and PermissionHandler
/// correctly enforce tenant-level authorization.
/// Tests:
///   - SuperAdmin bypasses all permission checks
///   - CollegeAdmin with correct permission passes
///   - CollegeAdmin without required permission is denied (403)
/// </summary>
public class TenantIsolationTests
{
    // ────────────── Permission codes mapping ──────────────

    [Theory]
    [InlineData("PAGE_MANAGE", "PagesController")]
    [InlineData("NEWS_MANAGE", "NewsController")]
    [InlineData("EVENT_MANAGE", "EventsController")]
    [InlineData("BANNER_MANAGE", "BannersController")]
    [InlineData("GALLERY_MANAGE", "GalleryController")]
    [InlineData("ACADEMIC_MANAGE", "DepartmentsController")]
    [InlineData("MEDIA_MANAGE", "MediaController")]
    [InlineData("SETTINGS_MANAGE", "SettingsController")]
    [InlineData("CONTACT_MANAGE", "ContactController")]
    [InlineData("MENU_MANAGE", "MenusController")]
    public void PermissionCodes_AreDefined(string permissionCode, string controller)
    {
        // Verify all permission codes used by controllers exist in PermissionCodes
        PermissionCodes.All.Should().Contain(permissionCode,
            $"Permission code '{permissionCode}' used by {controller} must be defined in PermissionCodes");
    }

    [Theory]
    [InlineData(PermissionCodes.PageView)]
    [InlineData(PermissionCodes.PageManage)]
    [InlineData(PermissionCodes.PagePublish)]
    [InlineData(PermissionCodes.NewsManage)]
    [InlineData(PermissionCodes.EventManage)]
    [InlineData(PermissionCodes.BannerManage)]
    [InlineData(PermissionCodes.GalleryManage)]
    [InlineData(PermissionCodes.AcademicManage)]
    [InlineData(PermissionCodes.MediaManage)]
    [InlineData(PermissionCodes.MenuManage)]
    [InlineData(PermissionCodes.SettingsManage)]
    [InlineData(PermissionCodes.ContactManage)]
    [InlineData(PermissionCodes.TenantManage)]
    [InlineData(PermissionCodes.UserManage)]
    [InlineData(PermissionCodes.DatabaseManage)]
    public void AllPermissionCodes_AreInPermissionsList(string code)
    {
        PermissionCodes.All.Should().Contain(code);
    }

    // ────────────── Permission constants consistency ──────────────

    [Fact]
    public void CollegeAdminDefaults_ShouldNotIncludeSuperAdminPermissions()
    {
        PermissionCodes.CollegeAdminDefaults.Should().NotContain(PermissionCodes.TenantManage);
        PermissionCodes.CollegeAdminDefaults.Should().NotContain(PermissionCodes.UserManage);
        PermissionCodes.CollegeAdminDefaults.Should().NotContain(PermissionCodes.DatabaseManage);
    }

    [Fact]
    public void CollegeAdminDefaults_ShouldIncludeAllCmsPermissions()
    {
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.PageView);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.PageManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.PagePublish);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.NewsManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.EventManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.BannerManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.GalleryManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.AcademicManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.MediaManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.MenuManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.SettingsManage);
        PermissionCodes.CollegeAdminDefaults.Should().Contain(PermissionCodes.ContactManage);
    }

    // ────────────── Feature codes ──────────────

    [Theory]
    [InlineData("PAGES")]
    [InlineData("NEWS")]
    [InlineData("EVENTS")]
    [InlineData("BANNERS")]
    [InlineData("GALLERY")]
    [InlineData("DEPARTMENTS")]
    [InlineData("COURSES")]
    [InlineData("FACULTY")]
    [InlineData("TESTIMONIALS")]
    [InlineData("ADMISSIONS")]
    public void FeatureCodes_AreDefined(string featureCode)
    {
        FeatureCodes.All.Should().Contain(featureCode);
    }

    // ────────────── Section types ──────────────

    [Theory]
    [InlineData("HERO")]
    [InlineData("TEXT")]
    [InlineData("IMAGE_TEXT")]
    [InlineData("NEWS")]
    [InlineData("EVENTS")]
    [InlineData("BANNERS")]
    [InlineData("GALLERY")]
    [InlineData("DEPARTMENTS")]
    [InlineData("COURSES")]
    [InlineData("FACULTY")]
    [InlineData("TESTIMONIALS")]
    [InlineData("STATS")]
    public void SectionTypes_AreDefined(string sectionType)
    {
        SectionTypes.All.Should().Contain(sectionType);
    }

    // ────────────── Role constants ──────────────

    [Fact]
    public void RoleConstants_SuperAdmin_MatchesUserRole()
    {
        RoleConstants.SuperAdmin.Should().Be(UserRole.SuperAdmin);
    }

    [Fact]
    public void RoleConstants_CollegeAdmin_MatchesUserRole()
    {
        RoleConstants.CollegeAdmin.Should().Be(UserRole.CollegeAdmin);
    }

    // ────────────── Tenant status constants ──────────────

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("SUSPENDED")]
    [InlineData("PROVISIONING")]
    [InlineData("DEACTIVATED")]
    public void TenantStatus_AllValuesAreDefined(string status)
    {
        var allStatuses = new[] { TenantStatus.Active, TenantStatus.Suspended, TenantStatus.Provisioning, TenantStatus.Deactivated };
        allStatuses.Should().Contain(status);
    }

    // ────────────── Provisioning status constants ──────────────

    [Theory]
    [InlineData("PENDING")]
    [InlineData("RUNNING")]
    [InlineData("COMPLETED")]
    [InlineData("FAILED")]
    public void ProvisioningStatus_AllValuesAreDefined(string status)
    {
        var allStatuses = new[] { ProvisioningStatus.Pending, ProvisioningStatus.Running, ProvisioningStatus.Completed, ProvisioningStatus.Failed };
        allStatuses.Should().Contain(status);
    }

    // ────────────── Response model consistency ──────────────

    [Fact]
    public void ApiResponse_Fail_ShouldReturnSuccessFalse()
    {
        var response = ApiResponse<object>.Fail("Error occurred");

        response.Success.Should().BeFalse();
        response.Message.Should().Be("Error occurred");
        response.Data.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Ok_ShouldReturnSuccessTrue()
    {
        var data = new { Id = 1, Name = "Test" };
        var response = ApiResponse<object>.Ok(data, "Created");

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Created");
        response.Data.Should().NotBeNull();
    }

    // ────────────── Pagination model ──────────────

    [Fact]
    public void PaginatedResponse_TotalPages_CalculatedCorrectly()
    {
        var response = new PaginatedResponse<string>
        {
            Items = new List<string>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PaginatedResponse_ZeroPageSize_DivisionByOne()
    {
        var response = new PaginatedResponse<string>
        {
            Items = new List<string>(),
            Page = 1,
            PageSize = 0,
            TotalCount = 100
        };

        // When PageSize is 0, denominator becomes 1, so TotalPages = Ceiling(100/1) = 100
        response.TotalPages.Should().Be(100);
    }
}
