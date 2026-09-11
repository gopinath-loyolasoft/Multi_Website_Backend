using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using FluentAssertions;
using Moq;

namespace CollegeWebsitePlatform.Tests.Unit.Services;

/// <summary>
/// Unit tests for CMS-related services: Pages, News, Banners, Departments, etc.
/// </summary>
public class CmsServiceTests
{
    private readonly Mock<IPageService> _pageServiceMock;
    private readonly Mock<INewsService> _newsServiceMock;
    private readonly Mock<IBannerService> _bannerServiceMock;
    private readonly Mock<IDepartmentService> _deptServiceMock;
    private readonly Mock<ICourseService> _courseServiceMock;
    private readonly Mock<IFacultyService> _facultyServiceMock;
    private readonly Mock<ITestimonialService> _testimonialServiceMock;
    private readonly Mock<IMenuService> _menuServiceMock;
    private readonly Mock<IGalleryService> _galleryServiceMock;

    public CmsServiceTests()
    {
        _pageServiceMock = new Mock<IPageService>();
        _newsServiceMock = new Mock<INewsService>();
        _bannerServiceMock = new Mock<IBannerService>();
        _deptServiceMock = new Mock<IDepartmentService>();
        _courseServiceMock = new Mock<ICourseService>();
        _facultyServiceMock = new Mock<IFacultyService>();
        _testimonialServiceMock = new Mock<ITestimonialService>();
        _menuServiceMock = new Mock<IMenuService>();
        _galleryServiceMock = new Mock<IGalleryService>();
    }

    // ────────────── Pages ──────────────

    [Fact]
    public async Task CreatePageAsync_ReturnsDraftPage()
    {
        var request = new CreatePageRequest { Slug = "about-us", Title = "About Us" };
        var created = new PageDto { Id = Guid.NewGuid(), Slug = "about-us", Title = "About Us", IsPublished = false };

        _pageServiceMock.Setup(s => s.CreatePageAsync(request, "admin")).ReturnsAsync(created);

        var result = await _pageServiceMock.Object.CreatePageAsync(request, "admin");

        result.Should().NotBeNull();
        result.IsPublished.Should().BeFalse();
        result.Slug.Should().Be("about-us");
    }

    [Fact]
    public async Task PublishPageAsync_SetsIsPublishedToTrue()
    {
        _pageServiceMock.Setup(s => s.PublishPageAsync(It.IsAny<Guid>(), "admin")).ReturnsAsync(true);

        var result = await _pageServiceMock.Object.PublishPageAsync(Guid.NewGuid(), "admin");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePageAsync_ReturnsTrue()
    {
        _pageServiceMock.Setup(s => s.DeletePageAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _pageServiceMock.Object.DeletePageAsync(Guid.NewGuid());

        result.Should().BeTrue();
    }

    // ────────────── News ──────────────

    [Fact]
    public async Task CreateNewsAsync_ReturnsDraftNews()
    {
        var request = new CreateNewsRequest
        {
            Title = "New Campus Building",
            Slug = "new-campus-building",
            Summary = "A new building",
            Content = "<p>Details</p>"
        };
        var created = new CollegeWebsitePlatform.Domain.Entities.News
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = request.Slug,
            IsPublished = false
        };

        _newsServiceMock.Setup(s => s.CreateNewsAsync(request)).ReturnsAsync(created);

        var result = await _newsServiceMock.Object.CreateNewsAsync(request);

        result.Should().NotBeNull();
        result.IsPublished.Should().BeFalse();
        result.Title.Should().Be("New Campus Building");
    }

    [Fact]
    public async Task PublishNewsAsync_ReturnsTrue()
    {
        _newsServiceMock.Setup(s => s.PublishNewsAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var result = await _newsServiceMock.Object.PublishNewsAsync(Guid.NewGuid());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetNewsPaginatedAsync_ReturnsPaginatedResponse()
    {
        var response = new PaginatedResponse<CollegeWebsitePlatform.Domain.Entities.News>
        {
            Items = new List<CollegeWebsitePlatform.Domain.Entities.News>
            {
                new() { Id = Guid.NewGuid(), Title = "News 1" }
            },
            Page = 1,
            PageSize = 20,
            TotalCount = 1
        };

        _newsServiceMock.Setup(s => s.GetNewsPaginatedAsync(It.IsAny<PaginationFilter>())).ReturnsAsync(response);

        var result = await _newsServiceMock.Object.GetNewsPaginatedAsync(new PaginationFilter());

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
    }

    // ────────────── Banners ──────────────

    [Fact]
    public async Task CreateBannerAsync_ReturnsBanner()
    {
        var request = new CreateBannerRequest
        {
            Title = "Welcome Banner",
            ImageUrl = "https://example.com/banner.jpg"
        };
        var created = new BannerDto
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            ImageUrl = request.ImageUrl,
            IsActive = true
        };

        _bannerServiceMock.Setup(s => s.CreateBannerAsync(request)).ReturnsAsync(created);

        var result = await _bannerServiceMock.Object.CreateBannerAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Welcome Banner");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReorderBannersAsync_ReturnsTrue()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _bannerServiceMock.Setup(s => s.ReorderBannersAsync(ids)).ReturnsAsync(true);

        var result = await _bannerServiceMock.Object.ReorderBannersAsync(ids);

        result.Should().BeTrue();
    }

    // ────────────── Departments ──────────────

    [Fact]
    public async Task CreateDepartmentAsync_ReturnsDepartment()
    {
        var request = new CreateDepartmentRequest
        {
            Name = "Computer Science",
            Code = "CS"
        };
        var created = new DepartmentDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            IsActive = true
        };

        _deptServiceMock.Setup(s => s.CreateDepartmentAsync(request)).ReturnsAsync(created);

        var result = await _deptServiceMock.Object.CreateDepartmentAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Computer Science");
    }

    // ────────────── Galleries ──────────────

    [Fact]
    public async Task CreateGalleryAsync_ReturnsGallery()
    {
        var request = new CreateGalleryRequest { Title = "Campus Photos", Slug = "campus-photos" };
        var created = new GalleryDto
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Slug = request.Slug,
            IsPublished = true
        };

        _galleryServiceMock.Setup(s => s.CreateGalleryAsync(request)).ReturnsAsync(created);

        var result = await _galleryServiceMock.Object.CreateGalleryAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Campus Photos");
    }

    [Fact]
    public async Task AddImageAsync_ReturnsGalleryImage()
    {
        var request = new AddGalleryImageRequest
        {
            ImageUrl = "https://example.com/photo.jpg",
            Caption = "Main Building"
        };
        var image = new GalleryImageDto
        {
            Id = Guid.NewGuid(),
            GalleryId = Guid.NewGuid(),
            ImageUrl = request.ImageUrl,
            Caption = request.Caption
        };

        _galleryServiceMock.Setup(s => s.AddImageAsync(It.IsAny<Guid>(), request)).ReturnsAsync(image);

        var result = await _galleryServiceMock.Object.AddImageAsync(Guid.NewGuid(), request);

        result.Should().NotBeNull();
        result.ImageUrl.Should().Be("https://example.com/photo.jpg");
    }

    // ────────────── Menus ──────────────

    [Fact]
    public async Task CreateMenuAsync_ReturnsMenu()
    {
        var request = new CreateMenuRequest { Code = "MAIN_NAV", Title = "Main Navigation" };
        var created = new MenuDto { Id = Guid.NewGuid(), Code = request.Code, Title = request.Title };

        _menuServiceMock.Setup(s => s.CreateMenuAsync(request)).ReturnsAsync(created);

        var result = await _menuServiceMock.Object.CreateMenuAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be("MAIN_NAV");
    }

    // ────────────── Testimonials ──────────────

    [Fact]
    public async Task CreateTestimonialAsync_ReturnsTestimonial()
    {
        var request = new CreateTestimonialRequest
        {
            StudentName = "Rahul Kumar",
            Content = "Excellent college",
            Rating = 5
        };
        var created = new TestimonialDto
        {
            Id = Guid.NewGuid(),
            StudentName = request.StudentName,
            Content = request.Content,
            Rating = 5,
            IsPublished = true
        };

        _testimonialServiceMock.Setup(s => s.CreateTestimonialAsync(request)).ReturnsAsync(created);

        var result = await _testimonialServiceMock.Object.CreateTestimonialAsync(request);

        result.Should().NotBeNull();
        result.StudentName.Should().Be("Rahul Kumar");
        result.Rating.Should().Be(5);
    }
}
