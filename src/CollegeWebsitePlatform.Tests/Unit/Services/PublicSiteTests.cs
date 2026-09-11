using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace CollegeWebsitePlatform.Tests.Unit.Services;

/// <summary>
/// Unit tests for ISiteService — public-facing website data.
/// </summary>
public class PublicSiteTests
{
    private readonly Mock<ISiteService> _siteServiceMock;

    public PublicSiteTests()
    {
        _siteServiceMock = new Mock<ISiteService>();
    }

    [Fact]
    public async Task GetSiteConfigAsync_ReturnsCompleteConfig()
    {
        var config = new SiteConfigDto
        {
            Tenant = new TenantDto
            {
                Id = Guid.NewGuid(),
                TenantCode = "RNC",
                Name = "RNC College"
            },
            Theme = new ThemeDto
            {
                Id = Guid.NewGuid(),
                Name = "Modern",
                Code = "modern",
                IsActive = true
            },
            PrimaryMenu = new List<MenuItemDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Home", SortOrder = 0 },
                new() { Id = Guid.NewGuid(), Title = "About", SortOrder = 1 },
                new() { Id = Guid.NewGuid(), Title = "Courses", SortOrder = 2 }
            },
            EnabledFeatures = new List<string> { "PAGES", "NEWS", "EVENTS", "GALLERY" },
            Settings = new TenantSettingsDto
            {
                SiteName = "RNC College of Technology",
                ContactEmail = "info@rnc.college"
            },
            Banners = new List<BannerDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Welcome", ImageUrl = "https://example.com/banner.jpg" }
            }
        };

        _siteServiceMock.Setup(s => s.GetSiteConfigAsync()).ReturnsAsync(config);

        var result = await _siteServiceMock.Object.GetSiteConfigAsync();

        result.Should().NotBeNull();
        result.Tenant!.TenantCode.Should().Be("RNC");
        result.Theme!.Name.Should().Be("Modern");
        result.PrimaryMenu.Should().HaveCount(3);
        result.EnabledFeatures.Should().Contain("NEWS");
        result.Banners.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPageBySlugAsync_WithPublishedPage_ReturnsPage()
    {
        var page = new PageDto
        {
            Id = Guid.NewGuid(),
            Slug = "home",
            Title = "Home Page",
            IsPublished = true,
            Sections = new List<PageSectionDto>
            {
                new() { Id = Guid.NewGuid(), SectionType = "HERO", SortOrder = 0 }
            }
        };

        _siteServiceMock.Setup(s => s.GetPageBySlugAsync("home", false)).ReturnsAsync(page);

        var result = await _siteServiceMock.Object.GetPageBySlugAsync("home");

        result.Should().NotBeNull();
        result!.Slug.Should().Be("home");
        result.Sections.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPageBySlugAsync_WithUnpublishedPage_ReturnsNull()
    {
        _siteServiceMock
            .Setup(s => s.GetPageBySlugAsync("draft-page", false))
            .ReturnsAsync((PageDto?)null);

        var result = await _siteServiceMock.Object.GetPageBySlugAsync("draft-page");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublishedNewsAsync_ReturnsNewsList()
    {
        var news = new List<NewsDto>
        {
            new() { Id = Guid.NewGuid(), Title = "News 1", IsPublished = true },
            new() { Id = Guid.NewGuid(), Title = "News 2", IsPublished = true }
        };

        _siteServiceMock.Setup(s => s.GetPublishedNewsAsync(10)).ReturnsAsync(news);

        var result = await _siteServiceMock.Object.GetPublishedNewsAsync(10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveBannersAsync_ReturnsBanners()
    {
        var banners = new List<BannerDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Banner 1", IsActive = true }
        };

        _siteServiceMock.Setup(s => s.GetActiveBannersAsync()).ReturnsAsync(banners);

        var result = await _siteServiceMock.Object.GetActiveBannersAsync();

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActiveDepartmentsAsync_ReturnsDepartments()
    {
        var departments = new List<DepartmentDto>
        {
            new() { Id = Guid.NewGuid(), Name = "CS", Code = "CS", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Math", Code = "MATH", IsActive = true }
        };

        _siteServiceMock.Setup(s => s.GetActiveDepartmentsAsync()).ReturnsAsync(departments);

        var result = await _siteServiceMock.Object.GetActiveDepartmentsAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveCoursesAsync_WithDepartmentFilter_ReturnsFilteredCourses()
    {
        var deptId = Guid.NewGuid();
        var courses = new List<CourseDto>
        {
            new() { Id = Guid.NewGuid(), DepartmentId = deptId, Name = "BCA", IsActive = true }
        };

        _siteServiceMock.Setup(s => s.GetActiveCoursesAsync(deptId)).ReturnsAsync(courses);

        var result = await _siteServiceMock.Object.GetActiveCoursesAsync(deptId);

        result.Should().HaveCount(1);
        result.First().DepartmentId.Should().Be(deptId);
    }

    [Fact]
    public async Task GetPublishedTestimonialsAsync_ReturnsTestimonials()
    {
        var testimonials = new List<TestimonialDto>
        {
            new() { Id = Guid.NewGuid(), StudentName = "Student 1", Rating = 5 },
            new() { Id = Guid.NewGuid(), StudentName = "Student 2", Rating = 4 }
        };

        _siteServiceMock.Setup(s => s.GetPublishedTestimonialsAsync(10)).ReturnsAsync(testimonials);

        var result = await _siteServiceMock.Object.GetPublishedTestimonialsAsync(10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitContactFormAsync_ReturnsSubmission()
    {
        var request = new CreateContactSubmissionRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Subject = "Admission Inquiry",
            Message = "I want to know about admissions"
        };
        var submission = new ContactSubmissionDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Status = "NEW"
        };

        _siteServiceMock.Setup(s => s.SubmitContactFormAsync(request)).ReturnsAsync(submission);

        var result = await _siteServiceMock.Object.SubmitContactFormAsync(request);

        result.Should().NotBeNull();
        result!.Status.Should().Be("NEW");
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WithEnabledFeature_ReturnsTrue()
    {
        _siteServiceMock.Setup(s => s.IsFeatureEnabledAsync("NEWS")).ReturnsAsync(true);

        var result = await _siteServiceMock.Object.IsFeatureEnabledAsync("NEWS");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WithDisabledFeature_ReturnsFalse()
    {
        _siteServiceMock.Setup(s => s.IsFeatureEnabledAsync("ADMISSIONS")).ReturnsAsync(false);

        var result = await _siteServiceMock.Object.IsFeatureEnabledAsync("ADMISSIONS");

        result.Should().BeFalse();
    }
}
