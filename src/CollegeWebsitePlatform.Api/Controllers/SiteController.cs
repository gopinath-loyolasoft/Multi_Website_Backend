using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CollegeWebsitePlatform.Api.Controllers.Site;

[ApiController]
[Route("api/v1/site")]
[AllowAnonymous]
public class SiteController : ControllerBase
{
    private readonly ISiteService _siteService;
    private readonly INoticeService _noticeService;

    public SiteController(ISiteService siteService, INoticeService noticeService)
    {
        _siteService = siteService;
        _noticeService = noticeService;
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<SiteConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfig()
    {
        var config = await _siteService.GetSiteConfigAsync();
        return Ok(ApiResponse<SiteConfigDto>.Ok(config));
    }

    [HttpGet("theme")]
    [ProducesResponseType(typeof(ApiResponse<ThemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTheme()
    {
        var theme = await _siteService.GetSiteThemeAsync();
        return Ok(ApiResponse<ThemeDto>.Ok(theme!));
    }

    [HttpGet("pages/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage([FromRoute] string slug, [FromQuery] bool preview = false, [FromQuery] string? preview_token = null)
    {
        var isAuthorizedAdmin = User.Identity?.IsAuthenticated == true;
        var isValidPreviewToken = !string.IsNullOrWhiteSpace(preview_token) && await _siteService.ValidatePreviewTokenAsync(preview_token);
        var page = await _siteService.GetPageBySlugAsync(slug, includeDraft: preview || isAuthorizedAdmin || isValidPreviewToken);
        if (page == null) return NotFound(ApiResponse<object>.Fail($"Page '{slug}' not found or is unpublished"));
        return Ok(ApiResponse<PageDto>.Ok(page));
    }

    [HttpGet("menus/{code}")]
    [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenu([FromRoute] string code)
    {
        var menu = await _siteService.GetMenuByCodeAsync(code);
        if (menu == null) return NotFound(ApiResponse<object>.Fail($"Menu '{code}' not found"));
        return Ok(ApiResponse<MenuDto>.Ok(menu));
    }

    [HttpGet("news")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NewsDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNews([FromQuery] int limit = 10)
    {
        var news = await _siteService.GetPublishedNewsAsync(limit);
        return Ok(ApiResponse<IEnumerable<NewsDto>>.Ok(news));
    }

    [HttpGet("news/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<NewsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNewsBySlug([FromRoute] string slug)
    {
        var item = await _siteService.GetNewsBySlugAsync(slug);
        if (item == null) return NotFound(ApiResponse<object>.Fail($"News '{slug}' not found"));
        return Ok(ApiResponse<NewsDto>.Ok(item));
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<EventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] int limit = 10)
    {
        var events = await _siteService.GetPublishedEventsAsync(limit);
        return Ok(ApiResponse<IEnumerable<EventDto>>.Ok(events));
    }

    [HttpGet("events/{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventById([FromRoute] Guid id)
    {
        var evt = await _siteService.GetEventByIdAsync(id);
        if (evt == null) return NotFound(ApiResponse<object>.Fail("Event not found"));
        return Ok(ApiResponse<EventDto>.Ok(evt));
    }

    [HttpGet("banners")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BannerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanners()
    {
        var list = await _siteService.GetActiveBannersAsync();
        return Ok(ApiResponse<IEnumerable<BannerDto>>.Ok(list));
    }

    [HttpGet("gallery")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GalleryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGalleries([FromQuery] int limit = 12)
    {
        var list = await _siteService.GetPublishedGalleriesAsync(limit);
        return Ok(ApiResponse<IEnumerable<GalleryDto>>.Ok(list));
    }

    [HttpGet("gallery/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<GalleryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGalleryBySlug([FromRoute] string slug)
    {
        var gallery = await _siteService.GetGalleryBySlugAsync(slug);
        if (gallery == null) return NotFound(ApiResponse<object>.Fail("Gallery not found"));
        return Ok(ApiResponse<GalleryDto>.Ok(gallery));
    }

    [HttpGet("departments")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments()
    {
        var list = await _siteService.GetActiveDepartmentsAsync();
        return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(list));
    }

    [HttpGet("departments/{code}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentByCode([FromRoute] string code)
    {
        var dept = await _siteService.GetDepartmentByCodeAsync(code);
        if (dept == null) return NotFound(ApiResponse<object>.Fail("Department not found"));
        return Ok(ApiResponse<DepartmentDto>.Ok(dept));
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CourseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] Guid? departmentId)
    {
        var list = await _siteService.GetActiveCoursesAsync(departmentId);
        return Ok(ApiResponse<IEnumerable<CourseDto>>.Ok(list));
    }

    [HttpGet("courses/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseById([FromRoute] Guid id)
    {
        var course = await _siteService.GetCourseByIdAsync(id);
        if (course == null) return NotFound(ApiResponse<object>.Fail("Course not found"));
        return Ok(ApiResponse<CourseDto>.Ok(course));
    }

    [HttpGet("faculty")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FacultyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaculty([FromQuery] Guid? departmentId)
    {
        var list = await _siteService.GetFacultyDirectoryAsync(departmentId);
        return Ok(ApiResponse<IEnumerable<FacultyDto>>.Ok(list));
    }

    [HttpGet("contact")]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContactInfo()
    {
        var info = await _siteService.GetContactSettingsAsync();
        return Ok(ApiResponse<TenantSettingsDto>.Ok(info!));
    }

    [HttpPost("contact/submissions")]
    [ProducesResponseType(typeof(ApiResponse<ContactSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitContactForm([FromBody] CreateContactSubmissionRequest request)
    {
        var submission = await _siteService.SubmitContactFormAsync(request);
        return Ok(ApiResponse<ContactSubmissionDto>.Ok(submission, "Thank you! Your inquiry has been submitted."));
    }


    [HttpGet("notices")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NoticeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotices([FromQuery] int limit = 10)
    {
        var notices = await _noticeService.GetNoticesPaginatedAsync(new PaginationFilter { Page = 1, PageSize = limit });
        var dtos = notices.Items.Select(n => new NoticeDto
        {
            Id = n.Id,
            Title = n.Title,
            Slug = n.Slug,
            Summary = n.Summary,
            Content = n.Content,
            PdfUrl = n.PdfUrl,
            Category = n.Category,
            PublishDate = n.PublishDate,
            ExpiryDate = n.ExpiryDate,
            IsPublished = n.IsPublished,
            IsImportant = n.IsImportant
        });
        return Ok(ApiResponse<IEnumerable<NoticeDto>>.Ok(dtos));
    }

    [HttpGet("notices/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<NoticeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNoticeBySlug([FromRoute] string slug)
    {
        var notice = await _noticeService.GetNoticeBySlugAsync(slug);
        if (notice == null) return NotFound(ApiResponse<object>.Fail($"Notice '{slug}' not found"));
        return Ok(ApiResponse<NoticeDto>.Ok(notice));
    }
}
