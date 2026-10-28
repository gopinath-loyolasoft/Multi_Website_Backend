using System.Security.Claims;
using Dapper;
using Npgsql;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Common.Models;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;
using CollegeWebsitePlatform.Domain.Constants;
using CollegeWebsitePlatform.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CollegeWebsitePlatform.Api.Middleware;

namespace CollegeWebsitePlatform.Api.Controllers;

public class UploadMediaRequest
{
    public IFormFile File { get; set; } = null!;
    public string? AltText { get; set; }
}

public class SetVisibilityRequest
{
    public bool IsVisible { get; set; }
}

public class SetActiveStatusRequest
{
    public bool IsActive { get; set; } = true;
    public bool? IsVisible { get; set; }
    public bool Active => IsVisible ?? IsActive;
}

// -------------------------------------------------------------
// Pages Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/pages")]
[Authorize]
[Permission(PermissionCodes.PageManage)]
public class PagesController : ControllerBase
{
    private readonly IPageService _pageService;

    public PagesController(IPageService pageService) => _pageService = pageService;

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "admin";

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<Page>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPages([FromQuery] PaginationFilter filter)
    {
        var pages = await _pageService.GetPagesPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<Page>>.Ok(pages));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageById([FromRoute] Guid id)
    {
        var page = await _pageService.GetPageDetailsAsync(id);
        if (page == null) return NotFound(ApiResponse<object>.Fail("Page not found"));
        return Ok(ApiResponse<PageDto>.Ok(page));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
    {
        var created = await _pageService.CreatePageAsync(request, CurrentUser);
        return Created($"/api/v1/admin/pages/{created.Id}", ApiResponse<PageDto>.Ok(created, "Page created as draft"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePage([FromRoute] Guid id, [FromBody] UpdatePageRequest request)
    {
        try
        {
            var updated = await _pageService.UpdatePageAsync(id, request, CurrentUser);
            return Ok(ApiResponse<PageDto>.Ok(updated, "Page updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePage([FromRoute] Guid id)
    {
        try
        {
            var success = await _pageService.DeletePageAsync(id);
            return Ok(ApiResponse<bool>.Ok(success, "Page deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{id}/publish")]
    [Permission(PermissionCodes.PagePublish)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishPage([FromRoute] Guid id)
    {
        var success = await _pageService.PublishPageAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Page published"));
    }

    [HttpPost("{id}/unpublish")]
    [Permission(PermissionCodes.PagePublish)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnpublishPage([FromRoute] Guid id)
    {
        var success = await _pageService.UnpublishPageAsync(id, CurrentUser);
        return Ok(ApiResponse<bool>.Ok(success, "Page unpublished"));
    }

    [HttpGet("{id}/preview")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreview([FromRoute] Guid id)
    {
        var preview = await _pageService.GetPagePreviewAsync(id);
        if (preview == null) return NotFound(ApiResponse<object>.Fail("Page preview not found"));
        return Ok(ApiResponse<PageDto>.Ok(preview));
    }

    [HttpGet("{id}/editor")]
    [ProducesResponseType(typeof(ApiResponse<PageEditorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPageEditor([FromRoute] Guid id)
    {
        var editor = await _pageService.GetPageEditorAsync(id);
        if (editor == null) return NotFound(ApiResponse<object>.Fail("Page not found"));
        return Ok(ApiResponse<PageEditorDto>.Ok(editor));
    }

    [HttpPut("{id}/draft")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveDraft([FromRoute] Guid id, [FromBody] SavePageDraftRequest request)
    {
        var page = await _pageService.SavePageDraftAsync(id, request, CurrentUser);
        return Ok(ApiResponse<PageDto>.Ok(page, "Draft saved successfully"));
    }

    [HttpGet("{pageId}/versions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PageVersionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions([FromRoute] Guid pageId)
    {
        var list = await _pageService.GetVersionsAsync(pageId);
        return Ok(ApiResponse<IEnumerable<PageVersionDto>>.Ok(list));
    }

    [HttpGet("{pageId}/versions/{versionNumber}")]
    [ProducesResponseType(typeof(ApiResponse<PageVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionDetail([FromRoute] Guid pageId, [FromRoute] int versionNumber)
    {
        var version = await _pageService.GetVersionDetailAsync(pageId, versionNumber);
        if (version == null) return NotFound(ApiResponse<object>.Fail("Version not found"));
        return Ok(ApiResponse<PageVersionDto>.Ok(version));
    }

    [HttpPost("{pageId}/versions/{versionNumber}/restore")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RestoreVersion([FromRoute] Guid pageId, [FromRoute] int versionNumber)
    {
        var restored = await _pageService.RestoreVersionAsync(pageId, versionNumber, CurrentUser);
        return Ok(ApiResponse<PageDto>.Ok(restored!, "Version restored"));
    }

    [HttpGet("{pageId}/sections")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PageSectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSections([FromRoute] Guid pageId, [FromServices] ISectionService sectionService)
    {
        var list = await sectionService.GetSectionsByPageIdAsync(pageId);
        return Ok(ApiResponse<IEnumerable<PageSectionDto>>.Ok(list));
    }

    [HttpPost("{pageId}/sections")]
    [ProducesResponseType(typeof(ApiResponse<PageSectionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSection([FromRoute] Guid pageId, [FromBody] CreateSectionRequest request, [FromServices] ISectionService sectionService)
    {
        var created = await sectionService.CreateSectionAsync(pageId, request);
        return Created($"/api/v1/admin/pages/{pageId}/sections/{created.Id}", ApiResponse<PageSectionDto>.Ok(created, "Section added"));
    }

    [HttpPut("{pageId}/sections/{sectionId}")]
    [ProducesResponseType(typeof(ApiResponse<PageSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSection([FromRoute] Guid pageId, [FromRoute] Guid sectionId, [FromBody] UpdateSectionRequest request, [FromServices] ISectionService sectionService)
    {
        var updated = await sectionService.UpdateSectionAsync(pageId, sectionId, request);
        return Ok(ApiResponse<PageSectionDto>.Ok(updated, "Section updated"));
    }

    [HttpDelete("{pageId}/sections/{sectionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSection([FromRoute] Guid pageId, [FromRoute] Guid sectionId, [FromServices] ISectionService sectionService)
    {
        var success = await sectionService.DeleteSectionAsync(pageId, sectionId);
        return Ok(ApiResponse<bool>.Ok(success, "Section deleted"));
    }

    [HttpPut("{pageId}/sections/reorder")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderSections([FromRoute] Guid pageId, [FromBody] ReorderSectionsRequest request, [FromServices] ISectionService sectionService)
    {
        var success = await sectionService.ReorderSectionsAsync(pageId, request.SectionIds);
        return Ok(ApiResponse<bool>.Ok(success, "Sections reordered"));
    }

    [HttpPost("{pageId}/sections/seed-default")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PageSectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedDefaultSections([FromRoute] Guid pageId, [FromServices] ISectionService sectionService)
    {
        var list = await sectionService.SeedDefaultSectionsAsync(pageId);
        return Ok(ApiResponse<IEnumerable<PageSectionDto>>.Ok(list, "Default module sections seeded successfully"));
    }
}

// -------------------------------------------------------------
// Section Types Catalog
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/section-types")]
[Authorize]
[Permission(PermissionCodes.PageView)]
public class SectionTypesController : ControllerBase
{
    private readonly ISectionService _sectionService;

    public SectionTypesController(ISectionService sectionService) => _sectionService = sectionService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SectionTypeDto>>), StatusCodes.Status200OK)]
    public IActionResult GetSectionTypes()
    {
        return Ok(ApiResponse<IEnumerable<SectionTypeDto>>.Ok(_sectionService.GetSectionTypes()));
    }
}

// -------------------------------------------------------------
// Menus Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/menus")]
[Authorize]
[Permission(PermissionCodes.MenuManage)]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenusController(IMenuService menuService) => _menuService = menuService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Menu>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await _menuService.GetAllMenusAsync();
        return Ok(ApiResponse<IEnumerable<Menu>>.Ok(menus));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuById([FromRoute] Guid id)
    {
        var menu = await _menuService.GetMenuDetailsAsync(id);
        if (menu == null) return NotFound(ApiResponse<object>.Fail("Menu not found"));
        return Ok(ApiResponse<MenuDto>.Ok(menu));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MenuDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuRequest request)
    {
        var created = await _menuService.CreateMenuAsync(request);
        return Created($"/api/v1/admin/menus/{created.Id}", ApiResponse<MenuDto>.Ok(created, "Menu created"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenu([FromRoute] Guid id, [FromBody] UpdateMenuRequest request)
    {
        var success = await _menuService.UpdateMenuAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Menu updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMenu([FromRoute] Guid id)
    {
        var success = await _menuService.DeleteMenuAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Menu deleted"));
    }

    [HttpGet("{menuId}/items")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MenuItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenuItems([FromRoute] Guid menuId)
    {
        var items = await _menuService.GetMenuItemsAsync(menuId);
        return Ok(ApiResponse<IEnumerable<MenuItemDto>>.Ok(items));
    }

    [HttpPost("{menuId}/items")]
    [ProducesResponseType(typeof(ApiResponse<MenuItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMenuItem([FromRoute] Guid menuId, [FromBody] CreateMenuItemRequest request)
    {
        var item = await _menuService.AddMenuItemAsync(menuId, request);
        return Created($"/api/v1/admin/menus/{menuId}/items/{item.Id}", ApiResponse<MenuItemDto>.Ok(item, "Menu item added"));
    }

    [HttpPut("{menuId}/items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMenuItem([FromRoute] Guid menuId, [FromRoute] Guid itemId, [FromBody] UpdateMenuItemRequest request)
    {
        var success = await _menuService.UpdateMenuItemAsync(menuId, itemId, request);
        return Ok(ApiResponse<bool>.Ok(success, "Menu item updated"));
    }

    [HttpPatch("{menuId}/items/{itemId}/visibility")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetMenuItemVisibility([FromRoute] Guid menuId, [FromRoute] Guid itemId, [FromBody] SetVisibilityRequest request)
    {
        var success = await _menuService.SetMenuItemVisibilityAsync(menuId, itemId, request.IsVisible);
        return Ok(ApiResponse<bool>.Ok(success, $"Menu item is now {(request.IsVisible ? "active" : "inactive")}"));
    }

    [HttpDelete("{menuId}/items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMenuItem([FromRoute] Guid menuId, [FromRoute] Guid itemId)
    {
        var success = await _menuService.DeleteMenuItemAsync(menuId, itemId);
        return Ok(ApiResponse<bool>.Ok(success, "Menu item deleted"));
    }

    [HttpPut("{menuId}/items/reorder")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderMenuItems([FromRoute] Guid menuId, [FromBody] ReorderMenuItemsRequest request)
    {
        var success = await _menuService.ReorderMenuItemsAsync(menuId, request.ItemIds);
        return Ok(ApiResponse<bool>.Ok(success, "Menu items reordered"));
    }
}

// -------------------------------------------------------------
// Top Announcement Marquee Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/marquee")]
[Authorize]
[Permission(PermissionCodes.BannerManage)]
[FeatureCheck("MARQUEE")]
public class MarqueeController : ControllerBase
{
    private readonly IMarqueeService _marqueeService;

    public MarqueeController(IMarqueeService marqueeService) => _marqueeService = marqueeService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<MarqueeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarquee()
    {
        var data = await _marqueeService.GetMarqueeDataAsync(activeOnly: false);
        return Ok(ApiResponse<MarqueeDto>.Ok(data));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<MarqueeSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _marqueeService.GetSettingsAsync();
        return Ok(ApiResponse<MarqueeSettingsDto>.Ok(settings));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateMarqueeSettingsRequest request)
    {
        var success = await _marqueeService.UpdateSettingsAsync(request);
        return Ok(ApiResponse<bool>.Ok(success, "Marquee bar settings updated"));
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MarqueeItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems([FromQuery] bool activeOnly = false)
    {
        var items = await _marqueeService.GetItemsAsync(activeOnly);
        return Ok(ApiResponse<IEnumerable<MarqueeItemDto>>.Ok(items));
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<MarqueeItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateItem([FromBody] CreateMarqueeItemRequest request)
    {
        var item = await _marqueeService.CreateItemAsync(request);
        return Created($"/api/v1/admin/marquee/items/{item.Id}", ApiResponse<MarqueeItemDto>.Ok(item, "Marquee announcement created"));
    }

    [HttpPut("items/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem([FromRoute] Guid id, [FromBody] UpdateMarqueeItemRequest request)
    {
        var success = await _marqueeService.UpdateItemAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Marquee announcement updated"));
    }

    [HttpPatch("items/{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetItemStatus([FromRoute] Guid id, [FromBody] SetActiveStatusRequest request)
    {
        var success = await _marqueeService.SetItemActiveStatusAsync(id, request.IsActive);
        return Ok(ApiResponse<bool>.Ok(success, $"Marquee announcement is now {(request.IsActive ? "active" : "inactive")}"));
    }

    [HttpDelete("items/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem([FromRoute] Guid id)
    {
        var success = await _marqueeService.DeleteItemAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Marquee announcement deleted"));
    }

    [HttpPut("items/reorder")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderItems([FromBody] ReorderMarqueeItemsRequest request)
    {
        var success = await _marqueeService.ReorderItemsAsync(request.ItemIds);
        return Ok(ApiResponse<bool>.Ok(success, "Marquee announcements reordered"));
    }
}

// -------------------------------------------------------------
// News Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/news")]
[Authorize]
[Permission(PermissionCodes.NewsManage)]
[FeatureCheck("NEWS")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService) => _newsService = newsService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<News>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNews([FromQuery] PaginationFilter filter)
    {
        var list = await _newsService.GetNewsPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<News>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<News>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNewsById([FromRoute] Guid id)
    {
        var item = await _newsService.GetNewsByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("News not found"));
        return Ok(ApiResponse<News>.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<News>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNews([FromBody] CreateNewsRequest request)
    {
        var created = await _newsService.CreateNewsAsync(request);
        return Created($"/api/v1/admin/news/{created.Id}", ApiResponse<News>.Ok(created, "News created as draft"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<News>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNews([FromRoute] Guid id, [FromBody] UpdateNewsRequest request)
    {
        var updated = await _newsService.UpdateNewsAsync(id, request);
        return Ok(ApiResponse<News>.Ok(updated, "News updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteNews([FromRoute] Guid id)
    {
        var success = await _newsService.DeleteNewsAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "News deleted"));
    }

    [HttpPost("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishNews([FromRoute] Guid id)
    {
        var success = await _newsService.PublishNewsAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "News published"));
    }

    [HttpPost("{id}/unpublish")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnpublishNews([FromRoute] Guid id)
    {
        var success = await _newsService.UnpublishNewsAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "News unpublished"));
    }
}

// -------------------------------------------------------------
// Events Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/events")]
[Authorize]
[Permission(PermissionCodes.EventManage)]
[FeatureCheck("EVENTS")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService) => _eventService = eventService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<Event>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] PaginationFilter filter)
    {
        var list = await _eventService.GetEventsPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<Event>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Event>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventById([FromRoute] Guid id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null) return NotFound(ApiResponse<object>.Fail("Event not found"));
        return Ok(ApiResponse<Event>.Ok(evt));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Event>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var created = await _eventService.CreateEventAsync(request);
        return Created($"/api/v1/admin/events/{created.Id}", ApiResponse<Event>.Ok(created, "Event created as draft"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Event>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateEvent([FromRoute] Guid id, [FromBody] UpdateEventRequest request)
    {
        var updated = await _eventService.UpdateEventAsync(id, request);
        return Ok(ApiResponse<Event>.Ok(updated, "Event updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteEvent([FromRoute] Guid id)
    {
        var success = await _eventService.DeleteEventAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Event deleted"));
    }

    [HttpPost("{id}/publish")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishEvent([FromRoute] Guid id)
    {
        var success = await _eventService.PublishEventAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Event published"));
    }

    [HttpPost("{id}/unpublish")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnpublishEvent([FromRoute] Guid id)
    {
        var success = await _eventService.UnpublishEventAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Event unpublished"));
    }
}

// -------------------------------------------------------------
// Banners Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/banners")]
[Authorize]
[Permission(PermissionCodes.BannerManage)]
[FeatureCheck("BANNERS")]
public class BannersController : ControllerBase
{
    private readonly IBannerService _bannerService;

    public BannersController(IBannerService bannerService) => _bannerService = bannerService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BannerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllBanners()
    {
        var list = await _bannerService.GetAllBannersAsync();
        return Ok(ApiResponse<IEnumerable<BannerDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BannerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBannerById([FromRoute] Guid id)
    {
        var banner = await _bannerService.GetBannerByIdAsync(id);
        if (banner == null) return NotFound(ApiResponse<object>.Fail("Banner not found"));
        return Ok(ApiResponse<BannerDto>.Ok(banner));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BannerDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBanner([FromBody] CreateBannerRequest request)
    {
        var created = await _bannerService.CreateBannerAsync(request);
        return Created($"/api/v1/admin/banners/{created.Id}", ApiResponse<BannerDto>.Ok(created, "Banner created"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBanner([FromRoute] Guid id, [FromBody] UpdateBannerRequest request)
    {
        var success = await _bannerService.UpdateBannerAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Banner updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBanner([FromRoute] Guid id)
    {
        var success = await _bannerService.DeleteBannerAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Banner deleted"));
    }

    [HttpPut("reorder")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReorderBanners([FromBody] ReorderBannersRequest request)
    {
        var success = await _bannerService.ReorderBannersAsync(request.BannerIds);
        return Ok(ApiResponse<bool>.Ok(success, "Banners reordered"));
    }
}

// -------------------------------------------------------------
// Gallery Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/gallery")]
[Authorize]
[Permission(PermissionCodes.GalleryManage)]
[FeatureCheck("GALLERY")]
public class GalleryController : ControllerBase
{
    private readonly IGalleryService _galleryService;

    public GalleryController(IGalleryService galleryService) => _galleryService = galleryService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<GalleryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGalleries([FromQuery] PaginationFilter filter)
    {
        var list = await _galleryService.GetGalleriesPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<GalleryDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GalleryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGalleryById([FromRoute] Guid id)
    {
        var gallery = await _galleryService.GetGalleryByIdAsync(id);
        if (gallery == null) return NotFound(ApiResponse<object>.Fail("Gallery not found"));
        return Ok(ApiResponse<GalleryDto>.Ok(gallery));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GalleryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGallery([FromBody] CreateGalleryRequest request)
    {
        var created = await _galleryService.CreateGalleryAsync(request);
        return Created($"/api/v1/admin/gallery/{created.Id}", ApiResponse<GalleryDto>.Ok(created, "Gallery created"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGallery([FromRoute] Guid id, [FromBody] UpdateGalleryRequest request)
    {
        var success = await _galleryService.UpdateGalleryAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Gallery updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGallery([FromRoute] Guid id)
    {
        var success = await _galleryService.DeleteGalleryAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Gallery deleted"));
    }

    [HttpPost("{galleryId}/images")]
    [ProducesResponseType(typeof(ApiResponse<GalleryImageDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddGalleryImage([FromRoute] Guid galleryId, [FromBody] AddGalleryImageRequest request)
    {
        var image = await _galleryService.AddImageAsync(galleryId, request);
        return Created($"/api/v1/admin/gallery/{galleryId}/images/{image.Id}", ApiResponse<GalleryImageDto>.Ok(image, "Image added to album"));
    }

    [HttpDelete("{galleryId}/images/{imageId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGalleryImage([FromRoute] Guid galleryId, [FromRoute] Guid imageId)
    {
        var success = await _galleryService.DeleteImageAsync(galleryId, imageId);
        return Ok(ApiResponse<bool>.Ok(success, "Image removed from album"));
    }
}

// -------------------------------------------------------------
// Departments Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/departments")]
[Authorize]
[Permission(PermissionCodes.AcademicManage)]
[FeatureCheck("DEPARTMENTS")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _deptService;

    public DepartmentsController(IDepartmentService deptService) => _deptService = deptService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments([FromQuery] PaginationFilter filter)
    {
        var list = await _deptService.GetDepartmentsPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<DepartmentDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartmentById([FromRoute] Guid id)
    {
        var dept = await _deptService.GetDepartmentByIdAsync(id);
        if (dept == null) return NotFound(ApiResponse<object>.Fail("Department not found"));
        return Ok(ApiResponse<DepartmentDto>.Ok(dept));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var created = await _deptService.CreateDepartmentAsync(request);
        return Created($"/api/v1/admin/departments/{created.Id}", ApiResponse<DepartmentDto>.Ok(created, "Department registered"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateDepartment([FromRoute] Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var success = await _deptService.UpdateDepartmentAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Department updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteDepartment([FromRoute] Guid id)
    {
        var success = await _deptService.DeleteDepartmentAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Department deleted"));
    }
}

// -------------------------------------------------------------
// Courses Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/courses")]
[Authorize]
[Permission(PermissionCodes.AcademicManage)]
[FeatureCheck("COURSES")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService) => _courseService = courseService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CourseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] PaginationFilter filter, [FromQuery] Guid? departmentId)
    {
        var list = await _courseService.GetCoursesPaginatedAsync(filter, departmentId);
        return Ok(ApiResponse<PaginatedResponse<CourseDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseById([FromRoute] Guid id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null) return NotFound(ApiResponse<object>.Fail("Course not found"));
        return Ok(ApiResponse<CourseDto>.Ok(course));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CourseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var created = await _courseService.CreateCourseAsync(request);
        return Created($"/api/v1/admin/courses/{created.Id}", ApiResponse<CourseDto>.Ok(created, "Course created"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCourse([FromRoute] Guid id, [FromBody] UpdateCourseRequest request)
    {
        var success = await _courseService.UpdateCourseAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Course updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCourse([FromRoute] Guid id)
    {
        var success = await _courseService.DeleteCourseAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Course deleted"));
    }
}

// -------------------------------------------------------------
// Faculty Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/faculty")]
[Authorize]
[Permission(PermissionCodes.AcademicManage)]
[FeatureCheck("FACULTY")]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _facultyService;

    public FacultyController(IFacultyService facultyService) => _facultyService = facultyService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<FacultyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaculty([FromQuery] PaginationFilter filter, [FromQuery] Guid? departmentId)
    {
        var list = await _facultyService.GetFacultyPaginatedAsync(filter, departmentId);
        return Ok(ApiResponse<PaginatedResponse<FacultyDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FacultyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacultyById([FromRoute] Guid id)
    {
        var faculty = await _facultyService.GetFacultyByIdAsync(id);
        if (faculty == null) return NotFound(ApiResponse<object>.Fail("Faculty profile not found"));
        return Ok(ApiResponse<FacultyDto>.Ok(faculty));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FacultyDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateFaculty([FromBody] CreateFacultyRequest request)
    {
        var created = await _facultyService.CreateFacultyAsync(request);
        return Created($"/api/v1/admin/faculty/{created.Id}", ApiResponse<FacultyDto>.Ok(created, "Faculty added"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateFaculty([FromRoute] Guid id, [FromBody] UpdateFacultyRequest request)
    {
        var success = await _facultyService.UpdateFacultyAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(success, "Faculty updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteFaculty([FromRoute] Guid id)
    {
        var success = await _facultyService.DeleteFacultyAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Faculty profile deleted"));
    }
}

// -------------------------------------------------------------
// Media Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/media")]
[Authorize]
[Permission(PermissionCodes.MediaManage)]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService) => _mediaService = mediaService;

    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name) ?? "admin";

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<MediaDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMedia([FromQuery] MediaFilterRequest filter)
    {
        var list = await _mediaService.GetMediaPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<MediaDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMediaById([FromRoute] Guid id)
    {
        var item = await _mediaService.GetMediaByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Media not found"));
        return Ok(ApiResponse<MediaDto>.Ok(item));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadMedia([FromForm] UploadMediaRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("No file was uploaded"));
        }

        using var stream = request.File.OpenReadStream();
        var media = await _mediaService.UploadMediaAsync(
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.AltText,
            CurrentUser
        );

        return Ok(ApiResponse<MediaDto>.Ok(media, "File uploaded successfully"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteMedia([FromRoute] Guid id)
    {
        var success = await _mediaService.DeleteMediaAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Media file deleted"));
    }
}

// -------------------------------------------------------------
// Settings Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize]
[Permission(PermissionCodes.SettingsManage)]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService) => _settingsService = settingsService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var s = await _settingsService.GetSettingsAsync();
        return Ok(ApiResponse<TenantSettingsDto>.Ok(s));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        var updated = await _settingsService.UpdateSettingsAsync(request);
        return Ok(ApiResponse<TenantSettingsDto>.Ok(updated, "Tenant profile settings updated"));
    }
}

// -------------------------------------------------------------
// Contact Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/contact")]
[Authorize]
[Permission(PermissionCodes.ContactManage)]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;

    public ContactController(IContactService contactService) => _contactService = contactService;

    [HttpGet("submissions")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<ContactSubmissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissions([FromQuery] PaginationFilter filter)
    {
        var list = await _contactService.GetSubmissionsPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<ContactSubmissionDto>>.Ok(list));
    }

    [HttpGet("submissions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ContactSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissionById([FromRoute] Guid id)
    {
        var item = await _contactService.GetSubmissionByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Contact submission not found"));
        return Ok(ApiResponse<ContactSubmissionDto>.Ok(item));
    }

    [HttpPost("submissions")]
    [ProducesResponseType(typeof(ApiResponse<ContactSubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSubmission([FromBody] CreateContactSubmissionRequest request)
    {
        var submission = await _contactService.CreateSubmissionAsync(request);
        return Ok(ApiResponse<ContactSubmissionDto>.Ok(submission, "Contact submission created successfully"));
    }

    [HttpPut("submissions/{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSubmissionStatus([FromRoute] Guid id, [FromBody] UpdateContactStatusRequest request)
    {
        var success = await _contactService.UpdateSubmissionStatusAsync(id, request.Status);
        return Ok(ApiResponse<bool>.Ok(success, "Contact submission status updated"));
    }

    [HttpDelete("submissions/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSubmission([FromRoute] Guid id)
    {
        var success = await _contactService.DeleteSubmissionAsync(id);
        if (!success) return NotFound(ApiResponse<object>.Fail("Contact submission not found or already deleted"));
        return Ok(ApiResponse<bool>.Ok(success, "Contact submission deleted successfully"));
    }
}

// -------------------------------------------------------------
// Notices Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/notices")]
[Authorize]
[Permission(PermissionCodes.NewsManage)]
[FeatureCheck("NOTICES")]
public class NoticesController : ControllerBase
{
    private readonly INoticeService _noticeService;

    public NoticesController(INoticeService noticeService) => _noticeService = noticeService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<Notice>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotices([FromQuery] PaginationFilter filter)
    {
        var list = await _noticeService.GetNoticesPaginatedAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<Notice>>.Ok(list));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Notice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNoticeById([FromRoute] Guid id)
    {
        var item = await _noticeService.GetNoticeByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Notice not found"));
        return Ok(ApiResponse<Notice>.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Notice>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateNotice([FromBody] CreateNoticeRequest request)
    {
        var created = await _noticeService.CreateNoticeAsync(request);
        return Created($"/api/v1/admin/notices/{created.Id}", ApiResponse<Notice>.Ok(created, "Notice created as draft"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Notice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateNotice([FromRoute] Guid id, [FromBody] UpdateNoticeRequest request)
    {
        var updated = await _noticeService.UpdateNoticeAsync(id, request);
        return Ok(ApiResponse<Notice>.Ok(updated, "Notice updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteNotice([FromRoute] Guid id)
    {
        var success = await _noticeService.DeleteNoticeAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Notice deleted"));
    }

    [HttpPost("{id}/publish")]
    [Permission(PermissionCodes.PagePublish)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishNotice([FromRoute] Guid id)
    {
        var success = await _noticeService.PublishNoticeAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Notice published"));
    }

    [HttpPost("{id}/unpublish")]
    [Permission(PermissionCodes.PagePublish)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnpublishNotice([FromRoute] Guid id)
    {
        var success = await _noticeService.UnpublishNoticeAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Notice unpublished"));
    }
}

// -------------------------------------------------------------
// Admin Dashboard Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize]
[Permission(PermissionCodes.PageView)]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AdminDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboard = await _dashboardService.GetDashboardAsync();
        return Ok(ApiResponse<AdminDashboardDto>.Ok(dashboard));
    }
}

// -------------------------------------------------------------
// Admin Profile Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/profile")]
[Authorize]
public class AdminProfileController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ISettingsService _settingsService;

    public AdminProfileController(IAuthService authService, ISettingsService settingsService)
    {
        _authService = authService;
        _settingsService = settingsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

        var profile = await _authService.GetCurrentUserProfileAsync(userId);
        if (profile == null) return NotFound(ApiResponse<object>.Fail("User not found."));
        return Ok(ApiResponse<CurrentUserDto>.Ok(profile));
    }

    [HttpGet("settings")]
    [Permission(PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _settingsService.GetSettingsAsync();
        return Ok(ApiResponse<TenantSettingsDto>.Ok(settings));
    }

    [HttpPut("settings")]
    [Permission(PermissionCodes.SettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<TenantSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsRequest request)
    {
        var updated = await _settingsService.UpdateSettingsAsync(request);
        return Ok(ApiResponse<TenantSettingsDto>.Ok(updated, "Profile settings updated"));
    }
}

// -------------------------------------------------------------
// Key Highlights & Statistics Bar Controller (CMS360 Style)
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/stats")]
[Authorize]
[FeatureCheck("STATS")]
public class StatsAdminController : ControllerBase
{
    private readonly ITenantContext _tenantContext;

    public StatsAdminController(ITenantContext tenantContext) => _tenantContext = tenantContext;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StatItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats()
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<dynamic>(@"
            SELECT id, metric, COALESCE(value, metric) AS value, COALESCE(prefix, '') AS prefix, COALESCE(suffix, '') AS suffix, 
                   label, icon, is_active, sort_order, created_at 
            FROM stats_items 
            WHERE is_deleted = FALSE 
            ORDER BY sort_order ASC, created_at ASC;");
        var list = rows.Select(r => new StatItemDto
        {
            Id = r.id,
            Metric = r.metric ?? "",
            Value = r.value ?? r.metric ?? "",
            Prefix = r.prefix ?? "",
            Suffix = r.suffix ?? "",
            Label = r.label ?? "",
            Icon = r.icon ?? "GraduationCap",
            IconName = r.icon ?? "GraduationCap",
            IsActive = r.is_active,
            SortOrder = r.sort_order,
            CreatedAt = r.created_at
        });
        return Ok(ApiResponse<IEnumerable<StatItemDto>>.Ok(list));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StatItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateStat([FromBody] CreateStatItemRequest request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var id = Guid.NewGuid();

        var val = !string.IsNullOrWhiteSpace(request.Value) ? request.Value.Trim() : (request.Metric?.Trim() ?? "");
        var pre = request.Prefix?.Trim() ?? "";
        var suf = request.Suffix?.Trim() ?? "";
        var metric = !string.IsNullOrWhiteSpace(request.Metric) ? request.Metric.Trim() : $"{pre}{val}{suf}".Trim();
        if (string.IsNullOrWhiteSpace(metric)) metric = val;
        var icon = !string.IsNullOrWhiteSpace(request.IconName) ? request.IconName.Trim() : (!string.IsNullOrWhiteSpace(request.Icon) ? request.Icon.Trim() : "GraduationCap");
        var label = request.Label?.Trim() ?? "";

        await conn.ExecuteAsync(@"
            INSERT INTO stats_items (id, metric, value, prefix, suffix, label, icon, is_active, sort_order, created_at)
            VALUES (@id, @metric, @val, @pre, @suf, @label, @icon, @isActive, @sortOrder, CURRENT_TIMESTAMP);",
            new { id, metric, val, pre, suf, label, icon, isActive = request.IsActive, sortOrder = request.SortOrder });
        
        var created = new StatItemDto
        {
            Id = id,
            Metric = metric,
            Value = val,
            Prefix = pre,
            Suffix = suf,
            Label = label,
            Icon = icon,
            IconName = icon,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow
        };
        return Created($"/api/v1/admin/stats/{id}", ApiResponse<StatItemDto>.Ok(created, "Statistic added"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStat([FromRoute] Guid id, [FromBody] CreateStatItemRequest request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();

        var val = !string.IsNullOrWhiteSpace(request.Value) ? request.Value.Trim() : (request.Metric?.Trim() ?? "");
        var pre = request.Prefix?.Trim() ?? "";
        var suf = request.Suffix?.Trim() ?? "";
        var metric = !string.IsNullOrWhiteSpace(request.Metric) ? request.Metric.Trim() : $"{pre}{val}{suf}".Trim();
        if (string.IsNullOrWhiteSpace(metric)) metric = val;
        var icon = !string.IsNullOrWhiteSpace(request.IconName) ? request.IconName.Trim() : (!string.IsNullOrWhiteSpace(request.Icon) ? request.Icon.Trim() : "GraduationCap");
        var label = request.Label?.Trim() ?? "";

        var affected = await conn.ExecuteAsync(@"
            UPDATE stats_items 
            SET metric = @metric, value = @val, prefix = @pre, suffix = @suf, label = @label, icon = @icon, is_active = @isActive, sort_order = @sortOrder
            WHERE id = @id AND is_deleted = FALSE;",
            new { id, metric, val, pre, suf, label, icon, isActive = request.IsActive, sortOrder = request.SortOrder });
        return Ok(ApiResponse<bool>.Ok(affected > 0, "Statistic updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteStat([FromRoute] Guid id)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var affected = await conn.ExecuteAsync(
            "UPDATE stats_items SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;",
            new { id });
        return Ok(ApiResponse<bool>.Ok(affected > 0, "Statistic deleted"));
    }

    [HttpPatch("{id}/visibility")]
    [HttpPatch("{id}/toggle-visibility")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleVisibility([FromRoute] Guid id, [FromBody] SetActiveStatusRequest request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var affected = await conn.ExecuteAsync(
            "UPDATE stats_items SET is_active = @isActive WHERE id = @id AND is_deleted = FALSE;",
            new { id, isActive = request.Active });
        return Ok(ApiResponse<bool>.Ok(affected > 0, "Visibility updated"));
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(ApiResponse<StatsSectionSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionSettings()
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, badge, title, subtitle, is_active, updated_at 
            FROM stats_settings 
            LIMIT 1;");

        var dto = row != null
            ? new StatsSectionSettingsDto
            {
                Id = row.id,
                Badge = row.badge ?? "Key Metrics & Institutional Record",
                Title = row.title ?? "Campus at a Glance",
                Subtitle = row.subtitle ?? "Excellence in Numbers",
                IsActive = row.is_active ?? true,
                UpdatedAt = row.updated_at ?? DateTime.UtcNow
            }
            : new StatsSectionSettingsDto();

        return Ok(ApiResponse<StatsSectionSettingsDto>.Ok(dto));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<StatsSectionSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSectionSettings([FromBody] UpdateStatsSectionSettingsRequest request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();

        var badge = !string.IsNullOrWhiteSpace(request.Badge) ? request.Badge.Trim() : "Key Metrics & Institutional Record";
        var title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title.Trim() : "Campus at a Glance";
        var subtitle = !string.IsNullOrWhiteSpace(request.Subtitle) ? request.Subtitle.Trim() : "Excellence in Numbers";

        var existingId = await conn.ExecuteScalarAsync<Guid?>("SELECT id FROM stats_settings LIMIT 1;");
        Guid id;
        if (existingId.HasValue)
        {
            id = existingId.Value;
            await conn.ExecuteAsync(@"
                UPDATE stats_settings 
                SET badge = @badge, title = @title, subtitle = @subtitle, is_active = @isActive, updated_at = CURRENT_TIMESTAMP
                WHERE id = @id;",
                new { id, badge, title, subtitle, isActive = request.IsActive });
        }
        else
        {
            id = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO stats_settings (id, badge, title, subtitle, is_active, updated_at)
                VALUES (@id, @badge, @title, @subtitle, @isActive, CURRENT_TIMESTAMP);",
                new { id, badge, title, subtitle, isActive = request.IsActive });
        }

        try
        {
            await conn.ExecuteAsync(@"
                UPDATE page_sections 
                SET title = @title, subtitle = @subtitle, updated_at = CURRENT_TIMESTAMP 
                WHERE section_type IN ('STATISTICS', 'STATS') AND is_deleted = FALSE;",
                new { title, subtitle });
        }
        catch { /* ignore if page_sections table schema is isolated */ }

        var result = new StatsSectionSettingsDto
        {
            Id = id,
            Badge = badge,
            Title = title,
            Subtitle = subtitle,
            IsActive = request.IsActive,
            UpdatedAt = DateTime.UtcNow
        };

        return Ok(ApiResponse<StatsSectionSettingsDto>.Ok(result, "Stats section settings updated"));
    }
}

// -------------------------------------------------------------
// Welcome Quote / Message Management Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/quote")]
[Authorize]
public class QuoteAdminController : ControllerBase
{
    private readonly ITenantContext _tenantContext;

    public QuoteAdminController(ITenantContext tenantContext) => _tenantContext = tenantContext;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<QuoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuote()
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, quote_text, author_name, designation, author_image_url, sub_text, is_active FROM quote_settings LIMIT 1;");
        if (row == null)
        {
            return Ok(ApiResponse<QuoteDto>.Ok(new QuoteDto
            {
                QuoteText = "Welcome to our academic community. Our mission is to foster scholarly excellence and compassionate leadership.",
                AuthorName = "Dean of Academics",
                Designation = "Principal / Dean",
                AuthorImageUrl = "/assets/templates/common/leader_portrait.svg",
                SubText = "Inspiring Scholarly Excellence",
                IsActive = true
            }));
        }
        var image = (string?)row.author_image_url;
        if (image == "/assets/principal.jpg")
        {
            image = null;
        }
        var dto = new QuoteDto
        {
            Id = row.id,
            QuoteText = row.quote_text ?? string.Empty,
            AuthorName = row.author_name ?? string.Empty,
            Designation = row.designation ?? string.Empty,
            AuthorImageUrl = image,
            SubText = row.sub_text ?? string.Empty,
            IsActive = row.is_active ?? true
        };
        return Ok(ApiResponse<QuoteDto>.Ok(dto));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<QuoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveQuote([FromBody] SaveQuoteRequest request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();

        var existing = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT id, author_image_url FROM quote_settings LIMIT 1;");

        var resolvedQuoteText = request.QuoteText?.Trim() ?? string.Empty;
        var resolvedAuthorName = request.AuthorName?.Trim() ?? string.Empty;
        var resolvedDesignation = request.GetResolvedDesignation();
        var resolvedImage = request.GetResolvedImageUrl();
        if (resolvedImage == "/assets/principal.jpg")
        {
            resolvedImage = null;
        }
        var resolvedSubText = request.SubText?.Trim() ?? string.Empty;

        if (existing != null)
        {
            Guid existingId = existing.id;
            await conn.ExecuteAsync(@"
                UPDATE quote_settings
                SET quote_text = @quoteText, 
                    author_name = @authorName, 
                    designation = @designation, 
                    author_image_url = @authorImageUrl, 
                    sub_text = @subText,
                    is_active = @isActive, 
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @id;",
                new { 
                    id = existingId, 
                    quoteText = resolvedQuoteText, 
                    authorName = resolvedAuthorName, 
                    designation = resolvedDesignation, 
                    authorImageUrl = resolvedImage, 
                    subText = resolvedSubText,
                    isActive = request.IsActive 
                });
            return Ok(ApiResponse<QuoteDto>.Ok(new QuoteDto
            {
                Id = existingId,
                QuoteText = resolvedQuoteText,
                AuthorName = resolvedAuthorName,
                Designation = resolvedDesignation,
                AuthorImageUrl = resolvedImage,
                SubText = resolvedSubText,
                IsActive = request.IsActive
            }, "Quote updated"));
        }
        else
        {
            var newId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO quote_settings (id, quote_text, author_name, designation, author_image_url, sub_text, is_active, updated_at)
                VALUES (@newId, @quoteText, @authorName, @designation, @authorImageUrl, @subText, @isActive, CURRENT_TIMESTAMP);",
                new { 
                    newId, 
                    quoteText = resolvedQuoteText, 
                    authorName = resolvedAuthorName, 
                    designation = resolvedDesignation, 
                    authorImageUrl = resolvedImage, 
                    subText = resolvedSubText,
                    isActive = request.IsActive 
                });
            return Ok(ApiResponse<QuoteDto>.Ok(new QuoteDto
            {
                Id = newId,
                QuoteText = resolvedQuoteText,
                AuthorName = resolvedAuthorName,
                Designation = resolvedDesignation,
                AuthorImageUrl = resolvedImage,
                SubText = resolvedSubText,
                IsActive = request.IsActive
            }, "Quote created"));
        }
    }

    [HttpPatch("visibility")]
    [HttpPatch("toggle-visibility")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleVisibility([FromBody] SetActiveStatusRequest? request)
    {
        using var conn = new NpgsqlConnection(_tenantContext.DbConnectionString);
        await conn.OpenAsync();

        bool targetState;
        if (request != null)
        {
            targetState = request.Active;
        }
        else
        {
            var currentState = await conn.ExecuteScalarAsync<bool?>("SELECT is_active FROM quote_settings LIMIT 1;");
            targetState = !(currentState ?? false);
        }

        await conn.ExecuteAsync("UPDATE quote_settings SET is_active = @targetState, updated_at = CURRENT_TIMESTAMP;", new { targetState });
        return Ok(ApiResponse<bool>.Ok(targetState, $"Quote visibility set to {(targetState ? "active" : "hidden")}"));
    }
}

[ApiController]
[Route("api/v1/admin/capabilities")]
[Authorize]
public class CapabilitiesController : ControllerBase
{
    private readonly ICapabilityService _capabilityService;
    private readonly ITenantContext _tenantContext;

    public CapabilitiesController(ICapabilityService capabilityService, ITenantContext tenantContext)
    {
        _capabilityService = capabilityService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CollegeCapabilitiesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilities()
    {
        var caps = await _capabilityService.GetCapabilitiesAsync(_tenantContext.TenantId);
        return Ok(ApiResponse<CollegeCapabilitiesDto>.Ok(caps));
    }
}

[ApiController]
[Route("api/v1/admin/preview")]
[Authorize]
public class PreviewController : ControllerBase
{
    private readonly IPageService _pageService;

    public PreviewController(IPageService pageService)
    {
        _pageService = pageService;
    }

    protected string CurrentUser => User.Identity?.Name ?? "admin";

    [HttpPost("session")]
    [ProducesResponseType(typeof(ApiResponse<CreatePreviewSessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePreviewSession([FromBody] CreatePreviewSessionRequest request)
    {
        var session = await _pageService.CreatePreviewSessionAsync(request.PageId, CurrentUser);
        return Ok(ApiResponse<CreatePreviewSessionResponse>.Ok(session, "Preview session created successfully"));
    }
}

// -------------------------------------------------------------
// Placements Admin Controller
// -------------------------------------------------------------

// -------------------------------------------------------------
// Compliance / Mandatory Disclosures Admin Controller
// -------------------------------------------------------------
[ApiController]
[Route("api/v1/admin/compliance")]
[Authorize]
public class ComplianceAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public ComplianceAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetDocuments([FromQuery] string? category)
    {
        var docs = await _repo.GetComplianceDocumentsAsync(category);
        return Ok(ApiResponse<IEnumerable<ComplianceDocumentDto>>.Ok(docs));
    }

    [HttpPost]
    public async Task<IActionResult> AddDocument([FromBody] ComplianceDocumentDto dto)
    {
        var id = await _repo.AddComplianceDocumentAsync(dto);
        dto.Id = id;
        return Ok(ApiResponse<ComplianceDocumentDto>.Ok(dto, "Compliance document added"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument([FromRoute] Guid id, [FromBody] ComplianceDocumentDto dto)
    {
        dto.Id = id;
        var success = await _repo.UpdateComplianceDocumentAsync(dto);
        return Ok(ApiResponse<bool>.Ok(success, "Document updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument([FromRoute] Guid id)
    {
        var success = await _repo.DeleteComplianceDocumentAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Document deleted"));
    }
}

// -------------------------------------------------------------
// Transport / Bus Routes Admin Controller
// -------------------------------------------------------------
[ApiController]
[Route("api/v1/admin/transport")]
[Authorize]
public class TransportAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public TransportAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetRoutes()
    {
        var routes = await _repo.GetTransportRoutesAsync();
        return Ok(ApiResponse<IEnumerable<TransportRouteDto>>.Ok(routes));
    }

    [HttpPost]
    public async Task<IActionResult> AddRoute([FromBody] TransportRouteDto dto)
    {
        var id = await _repo.AddTransportRouteAsync(dto);
        dto.Id = id;
        return Ok(ApiResponse<TransportRouteDto>.Ok(dto, "Transport route added"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute([FromRoute] Guid id, [FromBody] TransportRouteDto dto)
    {
        dto.Id = id;
        var success = await _repo.UpdateTransportRouteAsync(dto);
        return Ok(ApiResponse<bool>.Ok(success, "Route updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute([FromRoute] Guid id)
    {
        var success = await _repo.DeleteTransportRouteAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Route deleted"));
    }
}

// -------------------------------------------------------------
// Hostels Admin Controller
// -------------------------------------------------------------
[ApiController]
[Route("api/v1/admin/hostels")]
[Authorize]
public class HostelsAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public HostelsAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetHostels()
    {
        var hostels = await _repo.GetHostelsAsync();
        return Ok(ApiResponse<IEnumerable<HostelFacilityDto>>.Ok(hostels));
    }

    [HttpPost]
    public async Task<IActionResult> AddHostel([FromBody] HostelFacilityDto dto)
    {
        var id = await _repo.AddHostelAsync(dto);
        dto.Id = id;
        return Ok(ApiResponse<HostelFacilityDto>.Ok(dto, "Hostel facility added"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHostel([FromRoute] Guid id, [FromBody] HostelFacilityDto dto)
    {
        dto.Id = id;
        var success = await _repo.UpdateHostelAsync(dto);
        return Ok(ApiResponse<bool>.Ok(success, "Hostel updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHostel([FromRoute] Guid id)
    {
        var success = await _repo.DeleteHostelAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Hostel deleted"));
    }
}

// -------------------------------------------------------------
// Library Admin Controller
// -------------------------------------------------------------
[ApiController]
[Route("api/v1/admin/library")]
[Authorize]
public class LibraryAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public LibraryAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetResources([FromQuery] string? type, [FromQuery] string? dept)
    {
        var list = await _repo.GetLibraryResourcesAsync(type, dept);
        return Ok(ApiResponse<IEnumerable<LibraryResourceDto>>.Ok(list));
    }

    [HttpPost]
    public async Task<IActionResult> AddResource([FromBody] LibraryResourceDto dto)
    {
        var id = await _repo.AddLibraryResourceAsync(dto);
        dto.Id = id;
        return Ok(ApiResponse<LibraryResourceDto>.Ok(dto, "Library resource added"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource([FromRoute] Guid id, [FromBody] LibraryResourceDto dto)
    {
        dto.Id = id;
        var success = await _repo.UpdateLibraryResourceAsync(dto);
        return Ok(ApiResponse<bool>.Ok(success, "Library resource updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource([FromRoute] Guid id)
    {
        var success = await _repo.DeleteLibraryResourceAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Resource deleted"));
    }
}

// -------------------------------------------------------------
// Alumni Admin Controller
// -------------------------------------------------------------
[ApiController]
[Route("api/v1/admin/alumni")]
[Authorize]
public class AlumniAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public AlumniAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAlumni([FromQuery] bool distinguishedOnly = false)
    {
        var list = await _repo.GetAlumniMembersAsync(distinguishedOnly, includeAllStatuses: true);
        return Ok(ApiResponse<IEnumerable<AlumniMemberDto>>.Ok(list));
    }

    [HttpPost]
    public async Task<IActionResult> AddAlumni([FromBody] AlumniMemberDto dto)
    {
        var id = await _repo.RegisterAlumniAsync(dto);
        dto.Id = id;
        return Ok(ApiResponse<AlumniMemberDto>.Ok(dto, "Alumni record created"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAlumni([FromRoute] Guid id, [FromBody] AlumniMemberDto dto)
    {
        dto.Id = id;
        var success = await _repo.UpdateAlumniMemberAsync(dto);
        return Ok(ApiResponse<bool>.Ok(success, "Alumnus profile updated"));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid id, [FromBody] string status)
    {
        var success = await _repo.UpdateAlumniStatusAsync(id, status);
        return Ok(ApiResponse<bool>.Ok(success, "Status updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAlumni([FromRoute] Guid id)
    {
        var success = await _repo.DeleteAlumniAsync(id);
        return Ok(ApiResponse<bool>.Ok(success, "Alumni deleted"));
    }
}

// -------------------------------------------------------------
// About Module Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/about")]
[Authorize]
[Permission(PermissionCodes.PageManage)]
public class AboutController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public AboutController(CollegeModulesRepository repo) => _repo = repo;

    /// <summary>Get the current About module data for the admin CMS editor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AboutModuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAboutModule()
    {
        var data = await _repo.GetAboutModuleAsync();
        return Ok(ApiResponse<AboutModuleDto>.Ok(data));
    }

    /// <summary>Save (upsert) all About module fields.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAboutModule([FromBody] UpdateAboutModuleRequest request)
    {
        var success = await _repo.UpsertAboutModuleAsync(request);
        return Ok(ApiResponse<bool>.Ok(success, "About module saved successfully"));
    }
}

// -------------------------------------------------------------
// V12 Popup & Lightbox Announcements Admin Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/popups")]
[Authorize]
[Permission(PermissionCodes.PageManage)]
public class PopupAnnouncementsAdminController : ControllerBase
{
    private readonly CollegeModulesRepository _repo;

    public PopupAnnouncementsAdminController(CollegeModulesRepository repo) => _repo = repo;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PopupAnnouncementDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPopups()
    {
        var items = await _repo.GetAllPopupsAsync();
        return Ok(ApiResponse<IEnumerable<PopupAnnouncementDto>>.Ok(items));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PopupAnnouncementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPopupById([FromRoute] Guid id)
    {
        var item = await _repo.GetPopupByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Popup announcement not found"));
        return Ok(ApiResponse<PopupAnnouncementDto>.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePopup([FromBody] CreateOrUpdatePopupRequest request)
    {
        var id = await _repo.CreatePopupAsync(request);
        return CreatedAtAction(nameof(GetPopupById), new { id }, ApiResponse<Guid>.Ok(id, "Popup announcement created successfully"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePopup([FromRoute] Guid id, [FromBody] CreateOrUpdatePopupRequest request)
    {
        var success = await _repo.UpdatePopupAsync(id, request);
        if (!success) return NotFound(ApiResponse<object>.Fail("Popup announcement not found"));
        return Ok(ApiResponse<bool>.Ok(true, "Popup announcement updated successfully"));
    }

    [HttpPut("{id}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TogglePopupStatus([FromRoute] Guid id)
    {
        var newStatus = await _repo.TogglePopupStatusAsync(id);
        if (newStatus == null) return NotFound(ApiResponse<object>.Fail("Popup announcement not found"));
        return Ok(ApiResponse<bool>.Ok(newStatus.Value, $"Popup is now {(newStatus.Value ? "Active" : "Inactive")}"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePopup([FromRoute] Guid id)
    {
        var success = await _repo.DeletePopupAsync(id);
        if (!success) return NotFound(ApiResponse<object>.Fail("Popup announcement not found"));
        return Ok(ApiResponse<bool>.Ok(true, "Popup announcement deleted successfully"));
    }
}

// -------------------------------------------------------------
// Placement Companies Controller
// -------------------------------------------------------------

[ApiController]
[Route("api/v1/admin/placement-companies")]
[Authorize]
public class PlacementCompaniesController : ControllerBase
{
    private readonly PlacementCompanyRepository _repo;

    public PlacementCompaniesController(PlacementCompanyRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _repo.GetAllAsync(activeOnly: false);
        return Ok(ApiResponse<IEnumerable<PlacementCompanyDto>>.Ok(list));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound(ApiResponse<object>.Fail("Placement company not found"));
        return Ok(ApiResponse<PlacementCompanyDto>.Ok(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlacementCompanyRequest req)
    {
        var id = await _repo.CreateAsync(req);
        var created = await _repo.GetByIdAsync(id);
        return Created($"/api/v1/admin/placement-companies/{id}", ApiResponse<PlacementCompanyDto>.Ok(created!, "Placement company registered"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePlacementCompanyRequest req)
    {
        var ok = await _repo.UpdateAsync(id, req);
        return Ok(ApiResponse<bool>.Ok(ok, "Placement company updated"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var ok = await _repo.DeleteAsync(id);
        return Ok(ApiResponse<bool>.Ok(ok, "Placement company deleted"));
    }
}

