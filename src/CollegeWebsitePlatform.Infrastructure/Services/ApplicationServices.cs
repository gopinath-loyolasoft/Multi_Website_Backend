using System.Security.Cryptography;
using System.Text.Json;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Domain.Entities;
using CollegeWebsitePlatform.Infrastructure.Authentication;
using CollegeWebsitePlatform.Infrastructure.Persistence;
using CollegeWebsitePlatform.Infrastructure.Repositories.Platform;
using CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;
using CollegeWebsitePlatform.Infrastructure.Services;
using Dapper;
using Npgsql;

namespace CollegeWebsitePlatform.Application.Services;

public class SiteService : ISiteService
{
    private readonly ITenantContext _tenantContext;
    private readonly ThemeRepository _themeRepo;
    private readonly PageRepository _pageRepo;
    private readonly SectionRepository _sectionRepo;
    private readonly MenuRepository _menuRepo;
    private readonly NewsRepository _newsRepo;
    private readonly EventRepository _eventRepo;
    private readonly FeatureRepository _featureRepo;
    private readonly TenantSettingsRepository _settingsRepo;
    private readonly ContactRepository _contactRepo;
    private readonly BannerRepository _bannerRepo;
    private readonly GalleryRepository _galleryRepo;
    private readonly DepartmentRepository _deptRepo;
    private readonly CourseRepository _courseRepo;
    private readonly FacultyRepository _facultyRepo;
    private readonly TestimonialRepository _testimonialRepo;
    private readonly MarqueeRepository? _marqueeRepo;

    public SiteService(
        ITenantContext tenantContext,
        ThemeRepository themeRepo,
        PageRepository pageRepo,
        SectionRepository sectionRepo,
        MenuRepository menuRepo,
        NewsRepository newsRepo,
        EventRepository eventRepo,
        FeatureRepository featureRepo,
        TenantSettingsRepository settingsRepo,
        ContactRepository contactRepo,
        BannerRepository bannerRepo,
        GalleryRepository galleryRepo,
        DepartmentRepository deptRepo,
        CourseRepository courseRepo,
        FacultyRepository facultyRepo,
        TestimonialRepository testimonialRepo,
        MarqueeRepository? marqueeRepo = null,
        PreviewSessionRepository? previewRepo = null)
    {
        _tenantContext = tenantContext;
        _themeRepo = themeRepo;
        _pageRepo = pageRepo;
        _sectionRepo = sectionRepo;
        _menuRepo = menuRepo;
        _newsRepo = newsRepo;
        _eventRepo = eventRepo;
        _featureRepo = featureRepo;
        _settingsRepo = settingsRepo;
        _contactRepo = contactRepo;
        _bannerRepo = bannerRepo;
        _galleryRepo = galleryRepo;
        _deptRepo = deptRepo;
        _courseRepo = courseRepo;
        _facultyRepo = facultyRepo;
        _testimonialRepo = testimonialRepo;
        _marqueeRepo = marqueeRepo;
        _previewRepo = previewRepo;
    }

    private readonly PreviewSessionRepository? _previewRepo;

    public async Task<SiteConfigDto> GetSiteConfigAsync()
    {
        string? dbName = null;
        if (!string.IsNullOrWhiteSpace(_tenantContext.DbConnectionString))
        {
            try
            {
                dbName = new Npgsql.NpgsqlConnectionStringBuilder(_tenantContext.DbConnectionString).Database;
            }
            catch { /* fallback */ }
        }

        ThemeDto? themeDto = null;
        if (_tenantContext.DefaultThemeId.HasValue)
        {
            var theme = await _themeRepo.GetByIdAsync(_tenantContext.DefaultThemeId.Value);
            if (theme != null)
            {
                themeDto = new ThemeDto
                {
                    Id = theme.Id,
                    Name = theme.Name,
                    Code = theme.Code,
                    Configuration = JsonSerializer.Deserialize<object>(theme.Configuration),
                    IsActive = theme.IsActive
                };
            }
        }

        var templateCode = !string.IsNullOrWhiteSpace(_tenantContext.TemplateCode)
            ? _tenantContext.TemplateCode
            : (themeDto?.Code == "ARTS_GREEN" ? "ARTS_SCIENCE_MODERN"
               : themeDto?.Code == "NURSING_CARE" ? "MEDICAL_MODERN"
               : "ENGINEERING_MODERN");

        var tenantDto = new TenantDto
        {
            Id = _tenantContext.TenantId,
            TenantCode = _tenantContext.TenantCode,
            Name = _tenantContext.Name,
            Slug = _tenantContext.Slug,
            DefaultThemeId = _tenantContext.DefaultThemeId,
            DatabaseName = dbName,
            TemplateCode = templateCode
        };

        var menu = await _menuRepo.GetByCodeAsync("MAIN_NAV");
        var menuItems = new List<MenuItemDto>();
        if (menu != null)
        {
            var rawItems = await _menuRepo.GetItemsByMenuIdAsync(menu.Id);
            menuItems = MenuService.BuildMenuTree(rawItems, visibleOnly: true);
        }

        var features = await _featureRepo.GetFeaturesByTenantIdAsync(_tenantContext.TenantId);
        var enabledFeatureCodes = features.Where(f => f.IsEnabled).Select(f => f.FeatureCode).ToList();
        var settings = await _settingsRepo.GetSettingsAsync();
        if (settings != null 
            && (string.IsNullOrWhiteSpace(settings.SiteName) || settings.SiteName == "KTS Institute of Technology & Management" || settings.SiteName == "College Dynamic Portal")
            && _tenantContext.IsResolved 
            && !string.IsNullOrWhiteSpace(_tenantContext.Name) 
            && _tenantContext.TenantCode != "KTS")
        {
            settings.SiteName = _tenantContext.Name;
        }
        var banners = (await _bannerRepo.GetAllAsync(activeOnly: true)).ToList();

        MarqueeDto? marqueeDto = null;
        if (_marqueeRepo != null)
        {
            var mSettings = await _marqueeRepo.GetSettingsAsync();
            var mItems = (await _marqueeRepo.GetItemsAsync(activeOnly: true)).Select(i => new MarqueeItemDto
            {
                Id = i.Id,
                BadgeText = i.BadgeText,
                Message = i.Message,
                LinkUrl = i.LinkUrl,
                IsActive = i.IsActive,
                SortOrder = i.SortOrder,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                CreatedAt = i.CreatedAt
            }).ToList();

            marqueeDto = new MarqueeDto
            {
                Settings = new MarqueeSettingsDto
                {
                    Id = mSettings.Id,
                    IsActive = mSettings.IsActive,
                    Speed = mSettings.Speed,
                    PauseOnHover = mSettings.PauseOnHover,
                    BgColor = mSettings.BgColor,
                    TextColor = mSettings.TextColor,
                    BadgeBgColor = mSettings.BadgeBgColor,
                    UpdatedAt = mSettings.UpdatedAt
                },
                Items = mItems
            };
        }

        var statsList = new List<StatItemDto>();
        QuoteDto? quoteDto = null;
        if (!string.IsNullOrWhiteSpace(_tenantContext.DbConnectionString))
        {
            try
            {
                using var tConn = new Npgsql.NpgsqlConnection(_tenantContext.DbConnectionString);
                await tConn.OpenAsync();
                var sRows = await tConn.QueryAsync<dynamic>(@"
                    SELECT id, metric, COALESCE(value, metric) AS value, COALESCE(prefix, '') AS prefix, COALESCE(suffix, '') AS suffix, 
                           label, icon, is_active, sort_order, created_at 
                    FROM stats_items 
                    WHERE is_deleted = FALSE AND is_active = TRUE 
                    ORDER BY sort_order ASC, created_at ASC;");
                statsList = sRows.Select(r => new StatItemDto
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
                }).ToList();

                var qRow = await tConn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT id, quote_text, author_name, designation, author_image_url, sub_text, is_active FROM quote_settings WHERE is_active = TRUE LIMIT 1;");
                if (qRow != null)
                {
                    var img = (string?)qRow.author_image_url;
                    if (string.IsNullOrWhiteSpace(img) || img == "/assets/principal.jpg")
                    {
                        img = "/assets/templates/common/leader_portrait.svg";
                    }
                    quoteDto = new QuoteDto
                    {
                        Id = qRow.id,
                        QuoteText = qRow.quote_text ?? string.Empty,
                        AuthorName = qRow.author_name ?? string.Empty,
                        Designation = qRow.designation ?? string.Empty,
                        AuthorImageUrl = img,
                        SubText = qRow.sub_text ?? string.Empty,
                        IsActive = qRow.is_active ?? true
                    };
                }
            }
            catch { /* fallback if tables not yet populated */ }
        }

        return new SiteConfigDto
        {
            Tenant = tenantDto,
            Theme = themeDto,
            TemplateCode = templateCode,
            PrimaryMenu = menuItems,
            EnabledFeatures = enabledFeatureCodes,
            Settings = settings,
            Banners = banners,
            Marquee = marqueeDto,
            Stats = statsList,
            Quote = quoteDto
        };
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureCode)
    {
        return await _featureRepo.IsFeatureEnabledForTenantAsync(_tenantContext.TenantId, featureCode);
    }

    public async Task<ThemeDto?> GetSiteThemeAsync()
    {
        if (!_tenantContext.DefaultThemeId.HasValue) return null;
        var theme = await _themeRepo.GetByIdAsync(_tenantContext.DefaultThemeId.Value);
        if (theme == null) return null;

        return new ThemeDto
        {
            Id = theme.Id,
            Name = theme.Name,
            Code = theme.Code,
            Configuration = JsonSerializer.Deserialize<object>(theme.Configuration),
            IsActive = theme.IsActive
        };
    }

    public async Task<PageDto?> GetPageBySlugAsync(string slug, bool includeDraft = false)
    {
        var page = await _pageRepo.GetBySlugAsync(slug);
        if (page == null || (!page.IsPublished && !includeDraft)) return null;

        var sections = await _sectionRepo.GetByPageIdAsync(page.Id);
        var visibleSections = sections
            .Where(s => s.IsVisible || includeDraft)
            .OrderBy(s => s.SortOrder)
            .Select(s => new PageSectionDto
            {
                Id = s.Id,
                PageId = s.PageId,
                SectionType = s.SectionType,
                Title = s.Title,
                Subtitle = s.Subtitle,
                Content = !string.IsNullOrWhiteSpace(s.ContentJson) ? JsonSerializer.Deserialize<object>(s.ContentJson) : new { },
                Settings = !string.IsNullOrWhiteSpace(s.SettingsJson) ? JsonSerializer.Deserialize<object>(s.SettingsJson) : new { },
                SortOrder = s.SortOrder,
                IsVisible = s.IsVisible
            }).ToList();

        return new PageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            Title = page.Title,
            MetaDescription = page.MetaDescription,
            IsPublished = page.IsPublished,
            Sections = visibleSections
        };
    }

    public async Task<bool> ValidatePreviewTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || _previewRepo == null) return false;
        return await _previewRepo.ValidateTokenAsync(token);
    }

    public async Task<MenuDto?> GetMenuByCodeAsync(string code)
    {
        var menu = await _menuRepo.GetByCodeAsync(code);
        if (menu == null) return null;

        var items = await _menuRepo.GetItemsByMenuIdAsync(menu.Id);
        return new MenuDto
        {
            Id = menu.Id,
            Code = menu.Code,
            Title = menu.Title,
            Items = items.Select(i => new MenuItemDto
            {
                Id = i.Id,
                MenuId = i.MenuId,
                ParentId = i.ParentId,
                ItemType = i.ItemType,
                PageId = i.PageId,
                Title = i.Title,
                Url = i.Url,
                SortOrder = i.SortOrder,
                IsVisible = i.IsVisible
            }).ToList()
        };
    }

    public async Task<IEnumerable<NewsDto>> GetPublishedNewsAsync(int limit = 10)
    {
        var newsList = await _newsRepo.GetAllAsync(publishedOnly: true, limit: limit);
        return newsList.Select(n => new NewsDto
        {
            Id = n.Id,
            Title = n.Title,
            Slug = n.Slug,
            Summary = n.Summary,
            Content = n.Content,
            ImageUrl = n.ImageUrl,
            PublishedDate = n.PublishedDate,
            IsPublished = n.IsPublished
        });
    }

    public async Task<NewsDto?> GetNewsBySlugAsync(string slug)
    {
        var news = await _newsRepo.GetBySlugAsync(slug);
        if (news == null || !news.IsPublished) return null;

        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Slug = news.Slug,
            Summary = news.Summary,
            Content = news.Content,
            ImageUrl = news.ImageUrl,
            PublishedDate = news.PublishedDate,
            IsPublished = news.IsPublished
        };
    }

    public async Task<IEnumerable<EventDto>> GetPublishedEventsAsync(int limit = 10)
    {
        var events = await _eventRepo.GetAllAsync(publishedOnly: true, limit: limit);
        return events.Select(e => new EventDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            ImageUrl = e.ImageUrl,
            IsPublished = e.IsPublished
        });
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id)
    {
        var evt = await _eventRepo.GetByIdAsync(id);
        if (evt == null || !evt.IsPublished) return null;

        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            EventDate = evt.EventDate,
            Location = evt.Location,
            ImageUrl = evt.ImageUrl,
            IsPublished = evt.IsPublished
        };
    }

    public async Task<IEnumerable<BannerDto>> GetActiveBannersAsync() => await _bannerRepo.GetAllAsync(activeOnly: true);

    public async Task<IEnumerable<GalleryDto>> GetPublishedGalleriesAsync(int limit = 12)
    {
        var result = await _galleryRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = limit }, publishedOnly: true);
        return result.Items;
    }

    public async Task<GalleryDto?> GetGalleryBySlugAsync(string slug) => await _galleryRepo.GetBySlugAsync(slug);

    public async Task<IEnumerable<DepartmentDto>> GetActiveDepartmentsAsync()
    {
        var result = await _deptRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 100 }, activeOnly: true);
        return result.Items;
    }

    public async Task<DepartmentDto?> GetDepartmentByCodeAsync(string code) => await _deptRepo.GetByCodeAsync(code);

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync(Guid? departmentId = null)
    {
        var result = await _courseRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 100 }, departmentId: departmentId, activeOnly: true);
        return result.Items;
    }

    public async Task<CourseDto?> GetCourseByIdAsync(Guid id) => await _courseRepo.GetByIdAsync(id);

    public async Task<IEnumerable<FacultyDto>> GetFacultyDirectoryAsync(Guid? departmentId = null)
    {
        var result = await _facultyRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 100 }, departmentId: departmentId, activeOnly: true);
        return result.Items;
    }

    public async Task<IEnumerable<TestimonialDto>> GetPublishedTestimonialsAsync(int limit = 10)
    {
        return await _testimonialRepo.GetAllPublishedAsync(limit);
    }

    public async Task<TenantSettingsDto?> GetContactSettingsAsync() => await _settingsRepo.GetSettingsAsync();

    public async Task<ContactSubmissionDto> SubmitContactFormAsync(CreateContactSubmissionRequest request)
    {
        var id = await _contactRepo.CreateSubmissionAsync(request);
        return (await _contactRepo.GetByIdAsync(id))!;
    }
}

public class PageService : IPageService
{
    private readonly PageRepository _pageRepo;
    private readonly SectionRepository _sectionRepo;
    private readonly PageVersionRepository _versionRepo;
    private readonly PreviewSessionRepository _previewRepo;

    public PageService(
        PageRepository pageRepo,
        SectionRepository sectionRepo,
        PageVersionRepository versionRepo,
        PreviewSessionRepository previewRepo)
    {
        _pageRepo = pageRepo;
        _sectionRepo = sectionRepo;
        _versionRepo = versionRepo;
        _previewRepo = previewRepo;
    }

    public async Task<PaginatedResponse<Page>> GetPagesPaginatedAsync(PaginationFilter filter) => await _pageRepo.GetAllPaginatedAsync(filter);

    public async Task<IEnumerable<Page>> GetAllPagesAsync() => await _pageRepo.GetAllAsync();

    public async Task<PageDto?> GetPageDetailsAsync(Guid id)
    {
        var page = await _pageRepo.GetByIdAsync(id);
        if (page == null) return null;

        var sections = await _sectionRepo.GetByPageIdAsync(page.Id);
        return new PageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            Title = page.Title,
            MetaDescription = page.MetaDescription,
            IsPublished = page.IsPublished,
            Sections = sections.Select(s => new PageSectionDto
            {
                Id = s.Id,
                PageId = s.PageId,
                SectionType = s.SectionType,
                Title = s.Title,
                Subtitle = s.Subtitle,
                Content = !string.IsNullOrWhiteSpace(s.ContentJson) ? JsonSerializer.Deserialize<object>(s.ContentJson) : new { },
                Settings = !string.IsNullOrWhiteSpace(s.SettingsJson) ? JsonSerializer.Deserialize<object>(s.SettingsJson) : new { },
                SortOrder = s.SortOrder,
                IsVisible = s.IsVisible
            }).ToList()
        };
    }

    public async Task<PageDto> CreatePageAsync(CreatePageRequest request, string currentUser)
    {
        var cleanSlug = request.Slug.ToLowerInvariant().Trim().Trim('/');
        var isPublished = request.IsPublished ?? false;
        var page = new Page
        {
            Slug = cleanSlug,
            Title = request.Title.Trim(),
            MetaDescription = request.MetaDescription,
            IsPublished = isPublished
        };
        var id = await _pageRepo.CreateAsync(page);
        page.Id = id;

        // Auto-create Version 1
        await _versionRepo.CreateVersionAsync(id, page.Title, page.Slug, page.MetaDescription, "[]", currentUser);

        return new PageDto { Id = page.Id, Slug = page.Slug, Title = page.Title, MetaDescription = page.MetaDescription, IsPublished = isPublished };
    }

    public async Task<PageDto> UpdatePageAsync(Guid id, UpdatePageRequest request, string currentUser)
    {
        var existing = await _pageRepo.GetByIdAsync(id);
        var cleanSlug = request.Slug.ToLowerInvariant().Trim().Trim('/');
        if (existing != null && existing.Slug.Equals("home", StringComparison.OrdinalIgnoreCase) && !cleanSlug.Equals("home", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The home page slug is protected and cannot be changed.");
        }

        var isPublished = request.IsPublished ?? existing?.IsPublished ?? false;

        var page = new Page
        {
            Id = id,
            Slug = cleanSlug,
            Title = request.Title.Trim(),
            MetaDescription = request.MetaDescription,
            IsPublished = isPublished
        };
        await _pageRepo.UpdateAsync(page);

        // Snapshot current sections into new version
        var sections = await _sectionRepo.GetByPageIdAsync(id);
        var contentJson = JsonSerializer.Serialize(sections);
        await _versionRepo.CreateVersionAsync(id, page.Title, page.Slug, page.MetaDescription, contentJson, currentUser);

        return await GetPageDetailsAsync(id) ?? new PageDto { Id = id, Slug = page.Slug, Title = page.Title, IsPublished = isPublished };
    }

    public async Task<bool> DeletePageAsync(Guid id)
    {
        var existing = await _pageRepo.GetByIdAsync(id);
        if (existing != null && existing.Slug.Equals("home", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The home page is a required system page and cannot be deleted.");
        }
        return await _pageRepo.DeleteAsync(id);
    }

    public async Task<bool> PublishPageAsync(Guid id, string currentUser) => await _pageRepo.SetPublishStatusAsync(id, true);

    public async Task<bool> UnpublishPageAsync(Guid id, string currentUser) => await _pageRepo.SetPublishStatusAsync(id, false);

    public async Task<PageDto?> GetPagePreviewAsync(Guid id)
    {
        var page = await _pageRepo.GetByIdAsync(id);
        if (page == null) return null;

        var sections = await _sectionRepo.GetByPageIdAsync(page.Id);
        return new PageDto
        {
            Id = page.Id,
            Slug = page.Slug,
            Title = page.Title,
            MetaDescription = page.MetaDescription,
            IsPublished = page.IsPublished,
            Sections = sections.Select(s => new PageSectionDto
            {
                Id = s.Id,
                PageId = s.PageId,
                SectionType = s.SectionType,
                Title = s.Title,
                Subtitle = s.Subtitle,
                Content = !string.IsNullOrWhiteSpace(s.ContentJson) ? JsonSerializer.Deserialize<object>(s.ContentJson) : new { },
                Settings = !string.IsNullOrWhiteSpace(s.SettingsJson) ? JsonSerializer.Deserialize<object>(s.SettingsJson) : new { },
                SortOrder = s.SortOrder,
                IsVisible = s.IsVisible
            }).ToList()
        };
    }

    public async Task<IEnumerable<PageVersionDto>> GetVersionsAsync(Guid pageId) => await _versionRepo.GetVersionsByPageIdAsync(pageId);

    public async Task<PageVersionDto?> GetVersionDetailAsync(Guid pageId, int versionNumber) => await _versionRepo.GetVersionAsync(pageId, versionNumber);

    public async Task<PageDto?> RestoreVersionAsync(Guid pageId, int versionNumber, string currentUser)
    {
        var version = await _versionRepo.GetVersionAsync(pageId, versionNumber);
        if (version == null) return null;

        var page = new Page
        {
            Id = pageId,
            Title = version.Title,
            Slug = version.Slug,
            MetaDescription = version.MetaDescription,
            IsPublished = true
        };
        await _pageRepo.UpdateAsync(page);

        // Record restore event as a new version
        await _versionRepo.CreateVersionAsync(pageId, version.Title, version.Slug, version.MetaDescription, version.ContentJson?.ToString() ?? "[]", currentUser);

        return await GetPageDetailsAsync(pageId);
    }

    public async Task<PageEditorDto?> GetPageEditorAsync(Guid pageId)
    {
        var page = await GetPageDetailsAsync(pageId);
        if (page == null) return null;

        var versions = await GetVersionsAsync(pageId);
        var sectionTypes = CollegeWebsitePlatform.Domain.Constants.SectionTypes.All.Select(st => new SectionTypeDto
        {
            Code = st,
            Name = st.Replace("_", " "),
            Description = $"Content section for {st.ToLower()}",
            Icon = "Layout"
        }).ToList();

        return new PageEditorDto
        {
            Page = page,
            Sections = page.Sections,
            Versions = versions.ToList(),
            AvailableSectionTypes = sectionTypes
        };
    }

    public async Task<PageDto> SavePageDraftAsync(Guid pageId, SavePageDraftRequest request, string currentUser)
    {
        var existing = await _pageRepo.GetByIdAsync(pageId);
        if (existing == null) throw new KeyNotFoundException("Page not found");

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            existing.Title = request.Title.Trim();
            existing.MetaDescription = request.MetaDescription;
            await _pageRepo.UpdateAsync(existing);
        }

        // Synchronize sections
        var existingSections = (await _sectionRepo.GetByPageIdAsync(pageId)).ToList();
        var incomingSectionIds = request.Sections.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();

        // Delete removed sections
        foreach (var exSec in existingSections)
        {
            if (!incomingSectionIds.Contains(exSec.Id))
            {
                await _sectionRepo.DeleteAsync(exSec.Id);
            }
        }

        // Insert or update incoming sections
        foreach (var draftSec in request.Sections)
        {
            var contentJson = draftSec.Content != null ? JsonSerializer.Serialize(draftSec.Content) : "{}";
            var settingsJson = draftSec.Settings != null ? JsonSerializer.Serialize(draftSec.Settings) : "{}";

            if (draftSec.Id.HasValue && existingSections.Any(s => s.Id == draftSec.Id.Value))
            {
                await _sectionRepo.UpdateAsync(new PageSection
                {
                    Id = draftSec.Id.Value,
                    PageId = pageId,
                    SectionType = draftSec.SectionType,
                    Title = draftSec.Title,
                    Subtitle = draftSec.Subtitle,
                    ContentJson = contentJson,
                    SettingsJson = settingsJson,
                    SortOrder = draftSec.SortOrder,
                    IsVisible = draftSec.IsVisible
                });
            }
            else
            {
                await _sectionRepo.CreateAsync(new PageSection
                {
                    PageId = pageId,
                    SectionType = draftSec.SectionType,
                    Title = draftSec.Title,
                    Subtitle = draftSec.Subtitle,
                    ContentJson = contentJson,
                    SettingsJson = settingsJson,
                    SortOrder = draftSec.SortOrder,
                    IsVisible = draftSec.IsVisible
                });
            }
        }

        // Create version snapshot
        var updatedSections = await _sectionRepo.GetByPageIdAsync(pageId);
        var snapshotJson = JsonSerializer.Serialize(updatedSections);
        await _versionRepo.CreateVersionAsync(pageId, existing.Title, existing.Slug, existing.MetaDescription, snapshotJson, currentUser);

        return await GetPageDetailsAsync(pageId) ?? new PageDto { Id = pageId, Title = existing.Title, Slug = existing.Slug };
    }

    public async Task<CreatePreviewSessionResponse> CreatePreviewSessionAsync(Guid pageId, string currentUser)
    {
        var page = await _pageRepo.GetByIdAsync(pageId);
        if (page == null) throw new KeyNotFoundException("Page not found");

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(2);

        await _previewRepo.CreateSessionAsync(token, currentUser, expiresAt);

        return new CreatePreviewSessionResponse
        {
            PreviewToken = token,
            PreviewUrl = $"/{page.Slug}?preview_token={token}",
            ExpiresAt = expiresAt
        };
    }
}

public class SectionService : ISectionService
{
    private readonly SectionRepository _sectionRepo;

    public SectionService(SectionRepository sectionRepo) => _sectionRepo = sectionRepo;

    public async Task<IEnumerable<PageSectionDto>> GetSectionsByPageIdAsync(Guid pageId)
    {
        var sections = await _sectionRepo.GetByPageIdAsync(pageId);
        return sections.Select(s => new PageSectionDto
        {
            Id = s.Id,
            PageId = s.PageId,
            SectionType = s.SectionType,
            Title = s.Title,
            Subtitle = s.Subtitle,
            Content = !string.IsNullOrWhiteSpace(s.ContentJson) ? JsonSerializer.Deserialize<object>(s.ContentJson) : new { },
            Settings = !string.IsNullOrWhiteSpace(s.SettingsJson) ? JsonSerializer.Deserialize<object>(s.SettingsJson) : new { },
            SortOrder = s.SortOrder,
            IsVisible = s.IsVisible
        });
    }

    public async Task<PageSectionDto> CreateSectionAsync(Guid pageId, CreateSectionRequest request)
    {
        var contentJson = JsonSerializer.Serialize(request.Content ?? new object());
        var settingsJson = JsonSerializer.Serialize(request.Settings ?? new object());
        var section = new PageSection
        {
            PageId = pageId,
            SectionType = request.SectionType,
            Title = request.Title,
            Subtitle = request.Subtitle,
            ContentJson = contentJson,
            SettingsJson = settingsJson,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible
        };
        var id = await _sectionRepo.CreateAsync(section);
        section.Id = id;
        return new PageSectionDto
        {
            Id = section.Id,
            PageId = section.PageId,
            SectionType = section.SectionType,
            Title = section.Title,
            Subtitle = section.Subtitle,
            Content = request.Content,
            Settings = request.Settings,
            SortOrder = section.SortOrder,
            IsVisible = section.IsVisible
        };
    }

    public async Task<PageSectionDto> UpdateSectionAsync(Guid pageId, Guid sectionId, UpdateSectionRequest request)
    {
        var contentJson = JsonSerializer.Serialize(request.Content ?? new object());
        var settingsJson = JsonSerializer.Serialize(request.Settings ?? new object());
        var section = new PageSection
        {
            Id = sectionId,
            PageId = pageId,
            SectionType = request.SectionType,
            Title = request.Title,
            Subtitle = request.Subtitle,
            ContentJson = contentJson,
            SettingsJson = settingsJson,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible
        };
        await _sectionRepo.UpdateAsync(section);
        return new PageSectionDto
        {
            Id = section.Id,
            PageId = section.PageId,
            SectionType = section.SectionType,
            Title = section.Title,
            Subtitle = section.Subtitle,
            Content = request.Content,
            Settings = request.Settings,
            SortOrder = section.SortOrder,
            IsVisible = section.IsVisible
        };
    }

    public async Task<bool> DeleteSectionAsync(Guid pageId, Guid sectionId) => await _sectionRepo.DeleteAsync(sectionId);

    public async Task<bool> ReorderSectionsAsync(Guid pageId, List<Guid> sectionIds)
    {
        for (int i = 0; i < sectionIds.Count; i++)
        {
            await _sectionRepo.UpdateSortOrderAsync(sectionIds[i], i + 1);
        }
        return true;
    }

    public async Task<IEnumerable<PageSectionDto>> SeedDefaultSectionsAsync(Guid pageId)
    {
        var existingSections = (await _sectionRepo.GetByPageIdAsync(pageId)).ToList();
        var existingTypes = new HashSet<string>(existingSections.Select(s => s.SectionType.ToUpperInvariant()));

        var defaultBlueprint = new List<(string Type, string Title, string Subtitle, int Sort)>
        {
            ("HERO_SLIDER", "Hero Slider & Highlights", "Campus showcase and online admissions call to action", 1),
            ("STATISTICS", "Key Statistics & Milestones", "Institutional track record, accreditations, and student excellence", 2),
            ("QUOTE", "Leadership & Vision", "Message from the Chancellor & Academic Directorate", 3),
            ("DEPARTMENTS", "Academic Departments", "Specialized schools offering cutting-edge undergraduate & postgraduate disciplines", 4),
            ("COURSES", "Degree Programs & Curricula", "Industry-aligned academic pathways and career-oriented certifications", 5),
            ("FACULTY", "Distinguished Faculty", "Renowned professors, doctorate researchers, and industry fellows", 6),
            ("NEWS", "Latest Campus News", "Campus events, research breakthroughs, honors, and press announcements", 7),
            ("EVENTS", "Upcoming Campus Events", "Academic conferences, symposiums, student fests, and cultural celebrations", 8),
            ("NOTICES", "Official Notice Board", "Important administrative updates, examination schedules, and circulars", 9),
            ("GALLERY", "Campus Photo Gallery", "A visual journey across our world-class laboratories, library, and campus architecture", 10),
            ("TESTIMONIALS", "Placement Records & Testimonials", "Top corporate recruiters and success stories from our graduates", 11),
            ("CONTACT", "Campus Helpdesk & Location", "Reach our admissions desk, administrative offices, and visit our campus", 12)
        };

        int maxOrder = existingSections.Any() ? existingSections.Max(s => s.SortOrder) : 0;

        foreach (var item in defaultBlueprint)
        {
            // If the section type doesn't exist, or if HERO exists but not HERO_SLIDER/other types
            if (!existingTypes.Contains(item.Type) && !(item.Type == "HERO_SLIDER" && existingTypes.Contains("HERO")))
            {
                maxOrder++;
                var section = new PageSection
                {
                    PageId = pageId,
                    SectionType = item.Type,
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    ContentJson = "{}",
                    SettingsJson = "{\"columns\":3,\"cardVariant\":\"standard\",\"alignment\":\"center\",\"background\":\"light\",\"spacing\":\"normal\",\"containerWidth\":\"standard\"}",
                    SortOrder = maxOrder,
                    IsVisible = true
                };
                await _sectionRepo.CreateAsync(section);
            }
        }

        return await GetSectionsByPageIdAsync(pageId);
    }

    public IEnumerable<SectionTypeDto> GetSectionTypes()
    {
        return new List<SectionTypeDto>
        {
            new() { Code = "HERO", Name = "Hero Banner", Description = "Main showcase banner with headline, subtitle, primary/secondary CTA buttons, and background", Icon = "Sparkles" },
            new() { Code = "HERO_SLIDER", Name = "Hero Slider Banners", Description = "Multiple carousel banners rotating with slide controls and action links", Icon = "Sliders" },
            new() { Code = "QUOTE", Name = "Quote / Leadership Message", Description = "Inspirational quote from Dean, Chancellor or Leadership with portrait and credentials", Icon = "Quote" },
            new() { Code = "TEXT", Name = "Text Content", Description = "Rich formatted text block, leadership greetings, or institutional overview", Icon = "FileText" },
            new() { Code = "IMAGE", Name = "Image Showcase", Description = "High-resolution campus banner, photo frame, or visual graphic with caption", Icon = "Image" },
            new() { Code = "IMAGE_TEXT", Name = "Image + Text Split", Description = "Side-by-side narrative layout with media graphic and descriptive copy", Icon = "Layout" },
            new() { Code = "CARDS", Name = "Generic Cards Grid", Description = "Structured multi-column cards for programs, facilities, and announcements", Icon = "Grid" },
            new() { Code = "STATISTICS", Name = "Numbers & Statistics", Description = "Milestone counters (placement %, total students, faculty count, campus area)", Icon = "Award" },
            new() { Code = "ICON_CARDS", Name = "Feature Cards with Icons", Description = "Clean highlight cards showcasing campus features, labs, and student benefits", Icon = "Layers" },
            new() { Code = "NEWS", Name = "Latest Campus News", Description = "Dynamic feed pulling latest published articles from the campus news module", Icon = "Newspaper" },
            new() { Code = "EVENTS", Name = "Upcoming Events", Description = "Dynamic calendar feed showing scheduled seminars, hackathons, and sports events", Icon = "Calendar" },
            new() { Code = "NOTICES", Name = "Notices & Circulars", Description = "Urgent student notices, exam timetables, and circular download links", Icon = "Bell" },
            new() { Code = "COURSES", Name = "Courses & Degree Programs", Description = "Undergraduate, postgraduate, and doctoral degree program catalog", Icon = "BookOpen" },
            new() { Code = "DEPARTMENTS", Name = "Academic Departments", Description = "Department cards with schools, degree pathways, and HOD contact info", Icon = "Building2" },
            new() { Code = "FACULTY", Name = "Faculty Directory", Description = "Distinguished faculty directory, professor profiles, and research areas", Icon = "Users" },
            new() { Code = "GALLERY", Name = "Photo & Video Gallery", Description = "Interactive photo album grid with lightbox modal view", Icon = "Camera" },
            new() { Code = "TESTIMONIALS", Name = "Student & Alumni Reviews", Description = "Student feedback, alumni success stories, and corporate employer ratings", Icon = "MessageSquare" },
            new() { Code = "CALL_TO_ACTION", Name = "Call to Action (CTA)", Description = "High-impact conversion block encouraging online admission applications", Icon = "Send" },
            new() { Code = "VIDEO", Name = "Video Section", Description = "Embedded YouTube, Vimeo, or hosted video tour with custom thumbnail", Icon = "Video" },
            new() { Code = "LOGO_GRID", Name = "Accreditations & Recruiters", Description = "NAAC A++, NBA, NIRF ranking logos and top corporate placement partners", Icon = "CheckCircle" },
            new() { Code = "FAQ", Name = "Frequently Asked Questions", Description = "Collapsible accordion questions and answers for admissions and courses", Icon = "HelpCircle" },
            new() { Code = "CONTACT", Name = "Contact Information", Description = "Campus location address, phone numbers, admissions email, and inquiry form", Icon = "Mail" },
            new() { Code = "MAP", Name = "Interactive Campus Map", Description = "Embedded Google Maps view with campus coordinates and driving directions", Icon = "MapPin" },

            // Backward-compatible aliases
            new() { Code = "BANNERS", Name = "Promotional Slider (Legacy)", Description = "Alias for HERO_SLIDER", Icon = "Sliders" },
            new() { Code = "STATS", Name = "Counters (Legacy)", Description = "Alias for STATISTICS", Icon = "Award" }
        };
    }
}

public class MenuService : IMenuService
{
    private readonly MenuRepository _menuRepo;

    public MenuService(MenuRepository menuRepo) => _menuRepo = menuRepo;

    public static List<MenuItemDto> BuildMenuTree(IEnumerable<MenuItem> rawItems, bool visibleOnly = false)
    {
        var items = visibleOnly ? rawItems.Where(i => i.IsVisible).ToList() : rawItems.ToList();
        var lookup = items.ToLookup(i => i.ParentId);

        List<MenuItemDto> BuildChildren(Guid? parentId)
        {
            return lookup[parentId]
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.CreatedAt)
                .Select(i => new MenuItemDto
                {
                    Id = i.Id,
                    MenuId = i.MenuId,
                    ParentId = i.ParentId,
                    ItemType = i.ItemType,
                    TargetType = i.TargetType,
                    PageId = i.PageId,
                    Title = i.Title,
                    Url = i.Url,
                    SortOrder = i.SortOrder,
                    IsVisible = i.IsVisible,
                    OpenInNewTab = i.OpenInNewTab,
                    Icon = i.Icon,
                    Children = BuildChildren(i.Id)
                }).ToList();
        }

        return BuildChildren(null);
    }

    public async Task<IEnumerable<Menu>> GetAllMenusAsync() => await _menuRepo.GetAllAsync();

    public async Task<MenuDto?> GetMenuDetailsAsync(Guid id)
    {
        var menu = await _menuRepo.GetByIdAsync(id);
        if (menu == null) return null;

        var items = await _menuRepo.GetItemsByMenuIdAsync(menu.Id);
        return new MenuDto
        {
            Id = menu.Id,
            Code = menu.Code,
            Title = menu.Title,
            Items = BuildMenuTree(items, visibleOnly: false)
        };
    }

    public async Task<MenuDto> CreateMenuAsync(CreateMenuRequest request)
    {
        var menu = new Menu { Code = request.Code, Title = request.Title };
        var id = await _menuRepo.CreateMenuAsync(menu);
        menu.Id = id;
        return new MenuDto { Id = menu.Id, Code = menu.Code, Title = menu.Title };
    }

    public async Task<bool> UpdateMenuAsync(Guid id, UpdateMenuRequest request)
    {
        return await _menuRepo.UpdateMenuAsync(id, request.Title);
    }

    public async Task<bool> DeleteMenuAsync(Guid id) => await _menuRepo.DeleteMenuAsync(id);

    public async Task<IEnumerable<MenuItemDto>> GetMenuItemsAsync(Guid menuId)
    {
        var items = await _menuRepo.GetItemsByMenuIdAsync(menuId);
        return BuildMenuTree(items, visibleOnly: false);
    }

    public async Task<MenuItemDto> AddMenuItemAsync(Guid menuId, CreateMenuItemRequest request)
    {
        var item = new MenuItem
        {
            MenuId = menuId,
            ParentId = request.ParentId,
            ItemType = request.ItemType,
            TargetType = request.TargetType,
            PageId = request.PageId,
            Title = request.Title,
            Url = request.Url,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible,
            OpenInNewTab = request.OpenInNewTab,
            Icon = request.Icon
        };
        var id = await _menuRepo.AddMenuItemAsync(item);
        item.Id = id;
        return new MenuItemDto
        {
            Id = item.Id,
            MenuId = item.MenuId,
            ParentId = item.ParentId,
            ItemType = item.ItemType,
            TargetType = item.TargetType,
            PageId = item.PageId,
            Title = item.Title,
            Url = item.Url,
            SortOrder = item.SortOrder,
            IsVisible = item.IsVisible,
            OpenInNewTab = item.OpenInNewTab,
            Icon = item.Icon
        };
    }

    public async Task<bool> UpdateMenuItemAsync(Guid menuId, Guid itemId, UpdateMenuItemRequest request)
    {
        return await _menuRepo.UpdateMenuItemAsync(
            itemId, 
            request.ParentId, 
            request.ItemType, 
            request.TargetType, 
            request.PageId, 
            request.Title, 
            request.Url, 
            request.SortOrder, 
            request.IsVisible, 
            request.OpenInNewTab, 
            request.Icon);
    }

    public async Task<bool> SetMenuItemVisibilityAsync(Guid menuId, Guid itemId, bool isVisible)
    {
        return await _menuRepo.SetMenuItemVisibilityAsync(itemId, isVisible);
    }

    public async Task<bool> DeleteMenuItemAsync(Guid menuId, Guid itemId) => await _menuRepo.DeleteMenuItemAsync(itemId);

    public async Task<bool> ReorderMenuItemsAsync(Guid menuId, List<Guid> itemIds) => await _menuRepo.ReorderMenuItemsAsync(itemIds);
}

public class MarqueeService : IMarqueeService
{
    private readonly MarqueeRepository _marqueeRepo;

    public MarqueeService(MarqueeRepository marqueeRepo) => _marqueeRepo = marqueeRepo;

    public async Task<MarqueeDto> GetMarqueeDataAsync(bool activeOnly = false)
    {
        var settings = await _marqueeRepo.GetSettingsAsync();
        var items = await _marqueeRepo.GetItemsAsync(activeOnly);

        return new MarqueeDto
        {
            Settings = new MarqueeSettingsDto
            {
                Id = settings.Id,
                IsActive = settings.IsActive,
                Speed = settings.Speed,
                PauseOnHover = settings.PauseOnHover,
                BgColor = settings.BgColor,
                TextColor = settings.TextColor,
                BadgeBgColor = settings.BadgeBgColor,
                UpdatedAt = settings.UpdatedAt
            },
            Items = items.Select(i => new MarqueeItemDto
            {
                Id = i.Id,
                BadgeText = i.BadgeText,
                Message = i.Message,
                LinkUrl = i.LinkUrl,
                IsActive = i.IsActive,
                SortOrder = i.SortOrder,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                CreatedAt = i.CreatedAt
            }).ToList()
        };
    }

    public async Task<MarqueeSettingsDto> GetSettingsAsync()
    {
        var settings = await _marqueeRepo.GetSettingsAsync();
        return new MarqueeSettingsDto
        {
            Id = settings.Id,
            IsActive = settings.IsActive,
            Speed = settings.Speed,
            PauseOnHover = settings.PauseOnHover,
            BgColor = settings.BgColor,
            TextColor = settings.TextColor,
            BadgeBgColor = settings.BadgeBgColor,
            UpdatedAt = settings.UpdatedAt
        };
    }

    public async Task<bool> UpdateSettingsAsync(UpdateMarqueeSettingsRequest request)
    {
        var existing = await _marqueeRepo.GetSettingsAsync();
        existing.IsActive = request.IsActive;
        existing.Speed = request.Speed;
        existing.PauseOnHover = request.PauseOnHover;
        existing.BgColor = request.BgColor;
        existing.TextColor = request.TextColor;
        existing.BadgeBgColor = request.BadgeBgColor;
        return await _marqueeRepo.UpdateSettingsAsync(existing);
    }

    public async Task<IEnumerable<MarqueeItemDto>> GetItemsAsync(bool activeOnly = false)
    {
        var items = await _marqueeRepo.GetItemsAsync(activeOnly);
        return items.Select(i => new MarqueeItemDto
        {
            Id = i.Id,
            BadgeText = i.BadgeText,
            Message = i.Message,
            LinkUrl = i.LinkUrl,
            IsActive = i.IsActive,
            SortOrder = i.SortOrder,
            StartDate = i.StartDate,
            EndDate = i.EndDate,
            CreatedAt = i.CreatedAt
        });
    }

    public async Task<MarqueeItemDto?> GetItemByIdAsync(Guid id)
    {
        var item = await _marqueeRepo.GetItemByIdAsync(id);
        if (item == null) return null;
        return new MarqueeItemDto
        {
            Id = item.Id,
            BadgeText = item.BadgeText,
            Message = item.Message,
            LinkUrl = item.LinkUrl,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<MarqueeItemDto> CreateItemAsync(CreateMarqueeItemRequest request)
    {
        var item = new MarqueeItem
        {
            BadgeText = request.BadgeText,
            Message = request.Message,
            LinkUrl = request.LinkUrl,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };
        var id = await _marqueeRepo.CreateItemAsync(item);
        item.Id = id;
        return new MarqueeItemDto
        {
            Id = item.Id,
            BadgeText = item.BadgeText,
            Message = item.Message,
            LinkUrl = item.LinkUrl,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<bool> UpdateItemAsync(Guid id, UpdateMarqueeItemRequest request)
    {
        var item = await _marqueeRepo.GetItemByIdAsync(id);
        if (item == null) return false;

        item.BadgeText = request.BadgeText;
        item.Message = request.Message;
        item.LinkUrl = request.LinkUrl;
        item.IsActive = request.IsActive;
        item.SortOrder = request.SortOrder;
        item.StartDate = request.StartDate;
        item.EndDate = request.EndDate;
        return await _marqueeRepo.UpdateItemAsync(item);
    }

    public async Task<bool> SetItemActiveStatusAsync(Guid id, bool isActive) => await _marqueeRepo.SetItemActiveStatusAsync(id, isActive);

    public async Task<bool> DeleteItemAsync(Guid id) => await _marqueeRepo.DeleteItemAsync(id);

    public async Task<bool> ReorderItemsAsync(List<Guid> itemIds) => await _marqueeRepo.ReorderItemsAsync(itemIds);
}

public class NewsService : INewsService
{
    private readonly NewsRepository _newsRepo;

    public NewsService(NewsRepository newsRepo) => _newsRepo = newsRepo;

    public async Task<PaginatedResponse<News>> GetNewsPaginatedAsync(PaginationFilter filter) => await _newsRepo.GetAllPaginatedAsync(filter);

    public async Task<News?> GetNewsByIdAsync(Guid id) => await _newsRepo.GetByIdAsync(id);

    public async Task<News> CreateNewsAsync(CreateNewsRequest request)
    {
        var news = new News
        {
            Title = request.Title,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Summary = request.Summary,
            Content = request.Content,
            ImageUrl = request.ImageUrl,
            IsPublished = false // Always defaults to Draft
        };
        var id = await _newsRepo.CreateAsync(news);
        news.Id = id;
        return news;
    }

    public async Task<News> UpdateNewsAsync(Guid id, UpdateNewsRequest request)
    {
        var news = new News
        {
            Id = id,
            Title = request.Title,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Summary = request.Summary,
            Content = request.Content,
            ImageUrl = request.ImageUrl
        };
        await _newsRepo.UpdateAsync(news);
        return news;
    }

    public async Task<bool> DeleteNewsAsync(Guid id) => await _newsRepo.DeleteAsync(id);

    public async Task<bool> PublishNewsAsync(Guid id) => await _newsRepo.SetPublishStatusAsync(id, true);

    public async Task<bool> UnpublishNewsAsync(Guid id) => await _newsRepo.SetPublishStatusAsync(id, false);
}

public class EventService : IEventService
{
    private readonly EventRepository _eventRepo;

    public EventService(EventRepository eventRepo) => _eventRepo = eventRepo;

    public async Task<PaginatedResponse<Event>> GetEventsPaginatedAsync(PaginationFilter filter) => await _eventRepo.GetAllPaginatedAsync(filter);

    public async Task<Event?> GetEventByIdAsync(Guid id) => await _eventRepo.GetByIdAsync(id);

    public async Task<Event> CreateEventAsync(CreateEventRequest request)
    {
        var evt = new Event
        {
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            ImageUrl = request.ImageUrl,
            IsPublished = false // Always defaults to Draft
        };
        var id = await _eventRepo.CreateAsync(evt);
        evt.Id = id;
        return evt;
    }

    public async Task<Event> UpdateEventAsync(Guid id, UpdateEventRequest request)
    {
        var evt = new Event
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location,
            ImageUrl = request.ImageUrl
        };
        await _eventRepo.UpdateAsync(evt);
        return evt;
    }

    public async Task<bool> DeleteEventAsync(Guid id) => await _eventRepo.DeleteAsync(id);

    public async Task<bool> PublishEventAsync(Guid id) => await _eventRepo.SetPublishStatusAsync(id, true);

    public async Task<bool> UnpublishEventAsync(Guid id) => await _eventRepo.SetPublishStatusAsync(id, false);
}

public class BannerService : IBannerService
{
    private readonly BannerRepository _bannerRepo;

    public BannerService(BannerRepository bannerRepo) => _bannerRepo = bannerRepo;

    public async Task<IEnumerable<BannerDto>> GetAllBannersAsync() => await _bannerRepo.GetAllAsync(activeOnly: false);

    public async Task<BannerDto?> GetBannerByIdAsync(Guid id) => await _bannerRepo.GetByIdAsync(id);

    public async Task<BannerDto> CreateBannerAsync(CreateBannerRequest request)
    {
        var id = await _bannerRepo.CreateAsync(request);
        return (await _bannerRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateBannerAsync(Guid id, UpdateBannerRequest request) => await _bannerRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteBannerAsync(Guid id) => await _bannerRepo.DeleteAsync(id);

    public async Task<bool> ReorderBannersAsync(List<Guid> bannerIds) => await _bannerRepo.ReorderAsync(bannerIds);
}

public class GalleryService : IGalleryService
{
    private readonly GalleryRepository _galleryRepo;

    public GalleryService(GalleryRepository galleryRepo) => _galleryRepo = galleryRepo;

    public async Task<PaginatedResponse<GalleryDto>> GetGalleriesPaginatedAsync(PaginationFilter filter) => await _galleryRepo.GetAllPaginatedAsync(filter);

    public async Task<GalleryDto?> GetGalleryByIdAsync(Guid id) => await _galleryRepo.GetByIdAsync(id);

    public async Task<GalleryDto?> GetGalleryBySlugAsync(string slug) => await _galleryRepo.GetBySlugAsync(slug);

    public async Task<GalleryDto> CreateGalleryAsync(CreateGalleryRequest request)
    {
        var id = await _galleryRepo.CreateAsync(request);
        return (await _galleryRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateGalleryAsync(Guid id, UpdateGalleryRequest request) => await _galleryRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteGalleryAsync(Guid id) => await _galleryRepo.DeleteAsync(id);

    public async Task<GalleryImageDto> AddImageAsync(Guid galleryId, AddGalleryImageRequest request)
    {
        var id = await _galleryRepo.AddImageAsync(galleryId, request);
        return new GalleryImageDto
        {
            Id = id,
            GalleryId = galleryId,
            ImageUrl = request.ImageUrl,
            Caption = request.Caption,
            SortOrder = request.SortOrder
        };
    }

    public async Task<bool> DeleteImageAsync(Guid galleryId, Guid imageId) => await _galleryRepo.DeleteImageAsync(imageId);
}

public class DepartmentService : IDepartmentService
{
    private readonly DepartmentRepository _deptRepo;

    public DepartmentService(DepartmentRepository deptRepo) => _deptRepo = deptRepo;

    public async Task<PaginatedResponse<DepartmentDto>> GetDepartmentsPaginatedAsync(PaginationFilter filter) => await _deptRepo.GetAllPaginatedAsync(filter);

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id) => await _deptRepo.GetByIdAsync(id);

    public async Task<DepartmentDto?> GetDepartmentByCodeAsync(string code) => await _deptRepo.GetByCodeAsync(code);

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        var id = await _deptRepo.CreateAsync(request);
        return (await _deptRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request) => await _deptRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteDepartmentAsync(Guid id) => await _deptRepo.DeleteAsync(id);
}

public class CourseService : ICourseService
{
    private readonly CourseRepository _courseRepo;

    public CourseService(CourseRepository courseRepo) => _courseRepo = courseRepo;

    public async Task<PaginatedResponse<CourseDto>> GetCoursesPaginatedAsync(PaginationFilter filter, Guid? departmentId = null) => await _courseRepo.GetAllPaginatedAsync(filter, departmentId);

    public async Task<CourseDto?> GetCourseByIdAsync(Guid id) => await _courseRepo.GetByIdAsync(id);

    public async Task<CourseDto> CreateCourseAsync(CreateCourseRequest request)
    {
        var id = await _courseRepo.CreateAsync(request);
        return (await _courseRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateCourseAsync(Guid id, UpdateCourseRequest request) => await _courseRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteCourseAsync(Guid id) => await _courseRepo.DeleteAsync(id);
}

public class FacultyService : IFacultyService
{
    private readonly FacultyRepository _facultyRepo;

    public FacultyService(FacultyRepository facultyRepo) => _facultyRepo = facultyRepo;

    public async Task<PaginatedResponse<FacultyDto>> GetFacultyPaginatedAsync(PaginationFilter filter, Guid? departmentId = null) => await _facultyRepo.GetAllPaginatedAsync(filter, departmentId);

    public async Task<FacultyDto?> GetFacultyByIdAsync(Guid id) => await _facultyRepo.GetByIdAsync(id);

    public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request)
    {
        var id = await _facultyRepo.CreateAsync(request);
        return (await _facultyRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateFacultyAsync(Guid id, UpdateFacultyRequest request) => await _facultyRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteFacultyAsync(Guid id) => await _facultyRepo.DeleteAsync(id);
}

public class TestimonialService : ITestimonialService
{
    private readonly TestimonialRepository _testimonialRepo;

    public TestimonialService(TestimonialRepository testimonialRepo) => _testimonialRepo = testimonialRepo;

    public async Task<PaginatedResponse<TestimonialDto>> GetTestimonialsPaginatedAsync(PaginationFilter filter) => await _testimonialRepo.GetAllPaginatedAsync(filter);

    public async Task<IEnumerable<TestimonialDto>> GetPublishedTestimonialsAsync(int limit = 10) => await _testimonialRepo.GetAllPublishedAsync(limit);

    public async Task<TestimonialDto?> GetTestimonialByIdAsync(Guid id) => await _testimonialRepo.GetByIdAsync(id);

    public async Task<TestimonialDto> CreateTestimonialAsync(CreateTestimonialRequest request)
    {
        var id = await _testimonialRepo.CreateAsync(request);
        return (await _testimonialRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateTestimonialAsync(Guid id, UpdateTestimonialRequest request) => await _testimonialRepo.UpdateAsync(id, request);

    public async Task<bool> DeleteTestimonialAsync(Guid id) => await _testimonialRepo.DeleteAsync(id);
}

public class MediaService : IMediaService
{
    private readonly MediaRepository _mediaRepo;
    private readonly ITenantContext _tenantContext;

    public MediaService(MediaRepository mediaRepo, ITenantContext tenantContext)
    {
        _mediaRepo = mediaRepo;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedResponse<MediaDto>> GetMediaPaginatedAsync(MediaFilterRequest filter) => await _mediaRepo.GetAllPaginatedAsync(filter);

    public async Task<IEnumerable<MediaDto>> GetAllMediaAsync() => await _mediaRepo.GetAllAsync();

    public async Task<MediaDto?> GetMediaByIdAsync(Guid id) => await _mediaRepo.GetByIdAsync(id);

    public async Task<MediaDto> UploadMediaAsync(Stream fileStream, string fileName, string contentType, long fileSizeBytes, string? altText, string currentUser)
    {
        var cleanFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(cleanFileName).ToLowerInvariant();

        // Tenant-specific upload path: wwwroot/uploads/{tenantCode}/
        var tenantCode = _tenantContext.IsResolved ? _tenantContext.TenantCode.ToLowerInvariant() : "shared";
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", tenantCode);
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var uniqueFileName = $"{Guid.NewGuid()}_{cleanFileName}";
        var filePath = Path.Combine(uploadDir, uniqueFileName);

        using (var destStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(destStream);
        }

        var publicUrl = $"/uploads/{tenantCode}/{uniqueFileName}";
        var id = await _mediaRepo.CreateAsync(cleanFileName, publicUrl, contentType, fileSizeBytes, altText, currentUser);

        return new MediaDto
        {
            Id = id,
            FileName = cleanFileName,
            FilePath = publicUrl,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            AltText = altText,
            UploadedBy = currentUser,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteMediaAsync(Guid id) => await _mediaRepo.DeleteAsync(id);
}

public class SettingsService : ISettingsService
{
    private readonly TenantSettingsRepository _settingsRepo;
    private readonly ITenantContext _tenantContext;

    public SettingsService(TenantSettingsRepository settingsRepo, ITenantContext tenantContext)
    {
        _settingsRepo = settingsRepo;
        _tenantContext = tenantContext;
    }

    public async Task<TenantSettingsDto> GetSettingsAsync()
    {
        var s = await _settingsRepo.GetSettingsAsync();
        if (s != null)
        {
            if ((string.IsNullOrWhiteSpace(s.SiteName) || s.SiteName == "KTS Institute of Technology & Management" || s.SiteName == "College Dynamic Portal")
                && _tenantContext.IsResolved 
                && !string.IsNullOrWhiteSpace(_tenantContext.Name) 
                && _tenantContext.TenantCode != "KTS")
            {
                s.SiteName = _tenantContext.Name;
            }
            return s;
        }
        return new TenantSettingsDto { SiteName = _tenantContext.IsResolved ? _tenantContext.Name : "College Dynamic Portal" };
    }

    public async Task<TenantSettingsDto> UpdateSettingsAsync(UpdateTenantSettingsRequest request)
    {
        return await _settingsRepo.UpsertSettingsAsync(request);
    }
}

public class AdminDashboardService : IAdminDashboardService
{
    private readonly PageRepository _pageRepo;
    private readonly NewsRepository _newsRepo;
    private readonly EventRepository _eventRepo;
    private readonly NoticeRepository _noticeRepo;
    private readonly GalleryRepository _galleryRepo;
    private readonly DepartmentRepository _deptRepo;
    private readonly CourseRepository _courseRepo;
    private readonly FacultyRepository _facultyRepo;
    private readonly TestimonialRepository _testimonialRepo;
    private readonly MediaRepository _mediaRepo;
    private readonly ContactRepository _contactRepo;
    private readonly ITenantContext _tenantContext;

    public AdminDashboardService(
        PageRepository pageRepo, NewsRepository newsRepo, EventRepository eventRepo,
        NoticeRepository noticeRepo, GalleryRepository galleryRepo, DepartmentRepository deptRepo,
        CourseRepository courseRepo, FacultyRepository facultyRepo, TestimonialRepository testimonialRepo,
        MediaRepository mediaRepo, ContactRepository contactRepo, ITenantContext tenantContext)
    {
        _pageRepo = pageRepo;
        _newsRepo = newsRepo;
        _eventRepo = eventRepo;
        _noticeRepo = noticeRepo;
        _galleryRepo = galleryRepo;
        _deptRepo = deptRepo;
        _courseRepo = courseRepo;
        _facultyRepo = facultyRepo;
        _testimonialRepo = testimonialRepo;
        _mediaRepo = mediaRepo;
        _contactRepo = contactRepo;
        _tenantContext = tenantContext;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var pages = await _pageRepo.GetAllAsync();
        var pageList = pages.ToList();

        var newsResult = await _newsRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var newsList = newsResult.Items;

        var eventsResult = await _eventRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var eventsList = eventsResult.Items;

        var noticesResult = await _noticeRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var noticesList = noticesResult.Items;

        var galleriesResult = await _galleryRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var deptsResult = await _deptRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var coursesResult = await _courseRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var facultyResult = await _facultyRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var testimonialsResult = await _testimonialRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });
        var mediaResult = await _mediaRepo.GetAllPaginatedAsync(new MediaFilterRequest { Page = 1, PageSize = 1000 });
        var contactResult = await _contactRepo.GetAllPaginatedAsync(new PaginationFilter { Page = 1, PageSize = 1000 });

        return new AdminDashboardDto
        {
            TotalPages = pageList.Count,
            PublishedPages = pageList.Count(p => p.IsPublished),
            DraftPages = pageList.Count(p => !p.IsPublished),
            TotalNews = newsList.Count,
            PublishedNews = newsList.Count(n => n.IsPublished),
            TotalEvents = eventsList.Count,
            PublishedEvents = eventsList.Count(e => e.IsPublished),
            TotalNotices = noticesList.Count,
            PublishedNotices = noticesList.Count(n => n.IsPublished),
            TotalGalleries = galleriesResult.TotalCount,
            TotalDepartments = deptsResult.TotalCount,
            TotalCourses = coursesResult.TotalCount,
            TotalFaculty = facultyResult.TotalCount,
            TotalTestimonials = testimonialsResult.TotalCount,
            TotalMediaFiles = mediaResult.TotalCount,
            NewContactSubmissions = contactResult.Items.Count(c => c.Status == "NEW"),
            TenantName = _tenantContext.Name,
            LastUpdated = DateTime.UtcNow
        };
    }
}

public class ContactService : IContactService
{
    private readonly ContactRepository _contactRepo;

    public ContactService(ContactRepository contactRepo) => _contactRepo = contactRepo;

    public async Task<PaginatedResponse<ContactSubmissionDto>> GetSubmissionsPaginatedAsync(PaginationFilter filter) => await _contactRepo.GetAllPaginatedAsync(filter);

    public async Task<IEnumerable<ContactSubmissionDto>> GetAllSubmissionsAsync() => await _contactRepo.GetAllSubmissionsAsync();

    public async Task<ContactSubmissionDto?> GetSubmissionByIdAsync(Guid id) => await _contactRepo.GetByIdAsync(id);

    public async Task<ContactSubmissionDto> CreateSubmissionAsync(CreateContactSubmissionRequest request)
    {
        var id = await _contactRepo.CreateSubmissionAsync(request);
        return (await _contactRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateSubmissionStatusAsync(Guid id, string status) => await _contactRepo.UpdateStatusAsync(id, status);

    public async Task<bool> DeleteSubmissionAsync(Guid id) => await _contactRepo.DeleteAsync(id);
}

public class AuthService : IAuthService
{
    private readonly UserRepository _userRepo;
    private readonly TenantRepository _tenantRepo;
    private readonly RefreshTokenRepository _refreshRepo;
    private readonly JwtTokenService _jwtService;
    private readonly PermissionRepository _permRepo;
    private readonly UserTenantRepository _userTenantRepo;
    private readonly DomainRepository _domainRepo;

    public AuthService(
        UserRepository userRepo,
        TenantRepository tenantRepo,
        RefreshTokenRepository refreshRepo,
        JwtTokenService jwtService,
        PermissionRepository permRepo,
        UserTenantRepository userTenantRepo,
        DomainRepository domainRepo)
    {
        _userRepo = userRepo;
        _tenantRepo = tenantRepo;
        _refreshRepo = refreshRepo;
        _jwtService = jwtService;
        _permRepo = permRepo;
        _userTenantRepo = userTenantRepo;
        _domainRepo = domainRepo;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user == null) return null;

        var isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!isValid) return null;

        // Load tenant code
        string tenantCode = "SUPERADMIN";
        if (user.TenantId.HasValue)
        {
            var tenant = await _tenantRepo.GetByIdAsync(user.TenantId.Value);
            tenantCode = tenant?.TenantCode ?? "UNKNOWN";
        }

        // Load permissions for JWT claim
        var permissions = await _permRepo.GetPermissionsByRoleCodeAsync(user.Role);
        var permissionCodes = permissions.Select(p => p.Code).ToList();

        var (token, expiresAt) = _jwtService.GenerateToken(user, tenantCode, permissionCodes);

        // Generate Refresh Token
        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshExpiresAt = DateTime.UtcNow.AddDays(7);
        await _refreshRepo.SaveAsync(user.Id, refreshTokenString, refreshExpiresAt);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshTokenString,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            TenantId = user.TenantId,
            TenantCode = tenantCode,
            ExpiresAt = expiresAt
        };
    }

    public async Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var existing = await _refreshRepo.GetByTokenAsync(refreshToken);
        if (existing == null || existing.IsRevoked || existing.ExpiresAt < DateTime.UtcNow) return null;

        var user = await _userRepo.GetByIdAsync(existing.UserId);
        if (user == null) return null;

        string tenantCode = "SUPERADMIN";
        if (user.TenantId.HasValue)
        {
            var tenant = await _tenantRepo.GetByIdAsync(user.TenantId.Value);
            tenantCode = tenant?.TenantCode ?? "UNKNOWN";
        }

        // Reload permissions for the new token
        var permissions = await _permRepo.GetPermissionsByRoleCodeAsync(user.Role);
        var permissionCodes = permissions.Select(p => p.Code).ToList();

        var (newToken, expiresAt) = _jwtService.GenerateToken(user, tenantCode, permissionCodes);

        // Rotate Refresh Token (old revoked, new issued)
        await _refreshRepo.RevokeAsync(refreshToken);
        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await _refreshRepo.SaveAsync(user.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));

        return new RefreshTokenResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        await _refreshRepo.RevokeAsync(refreshToken);
        return true;
    }

    /// <summary>Returns a typed CurrentUserDto with permissions and tenant memberships for /auth/me.</summary>
    public async Task<CurrentUserDto?> GetCurrentUserProfileAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return null;

        // Load permissions from role
        var permissions = await _permRepo.GetPermissionsByRoleCodeAsync(user.Role);

        // Load tenant memberships
        var memberships = await _userTenantRepo.GetByUserIdAsync(userId);
        var tenantList = new List<TenantMembershipDto>();
        foreach (var m in memberships)
        {
            var domains = await _domainRepo.GetByTenantIdAsync(m.TenantId);
            var primaryDomain = domains.FirstOrDefault(d => d.IsPrimary)?.Domain
                ?? domains.FirstOrDefault()?.Domain ?? string.Empty;
            tenantList.Add(new TenantMembershipDto
            {
                TenantId   = m.TenantId,
                TenantCode = m.TenantCode,
                TenantName = m.TenantName,
                Domain     = primaryDomain,
                IsPrimary  = m.IsPrimary
            });
        }

        return new CurrentUserDto
        {
            Id          = user.Id,
            Username    = user.Username,
            Email       = user.Email,
            FullName    = user.FullName,
            Role        = user.Role,
            IsActive    = user.IsActive,
            Permissions = permissions.Select(p => p.Code).ToList(),
            Tenants     = tenantList
        };
    }

    // Legacy method kept for compatibility
    public async Task<User?> GetCurrentUserAsync(Guid userId) => await _userRepo.GetByIdAsync(userId);
}

public class AuditService : IAuditService
{
    private readonly AuditRepository _auditRepo;

    public AuditService(AuditRepository auditRepo) => _auditRepo = auditRepo;

    public async Task LogActionAsync(string username, string action, string entityType, string? entityId = null, string? details = null, Guid? tenantId = null, string? ipAddress = null)
    {
        await _auditRepo.LogAsync(username, action, entityType, entityId, details, tenantId, ipAddress);
    }
}

public class SuperAdminService : ISuperAdminService
{
    private readonly TenantRepository _tenantRepo;
    private readonly DomainRepository _domainRepo;
    private readonly DatabaseRegistryRepository _dbRepo;
    private readonly ThemeRepository _themeRepo;
    private readonly TemplateRepository _templateRepo;
    private readonly FeatureRepository _featureRepo;
    private readonly UserRepository _userRepo;
    private readonly RoleRepository _roleRepo;
    private readonly PermissionRepository _permRepo;
    private readonly UserTenantRepository _userTenantRepo;
    private readonly AuditRepository _auditRepo;
    private readonly IPlatformDbConnectionFactory _platformDb;
    private readonly IDnsVerificationService _dnsService;

    public SuperAdminService(
        TenantRepository tenantRepo,
        DomainRepository domainRepo,
        DatabaseRegistryRepository dbRepo,
        ThemeRepository themeRepo,
        TemplateRepository templateRepo,
        FeatureRepository featureRepo,
        UserRepository userRepo,
        RoleRepository roleRepo,
        PermissionRepository permRepo,
        UserTenantRepository userTenantRepo,
        AuditRepository auditRepo,
        IPlatformDbConnectionFactory platformDb,
        IDnsVerificationService dnsService)
    {
        _tenantRepo = tenantRepo;
        _domainRepo = domainRepo;
        _dbRepo = dbRepo;
        _themeRepo = themeRepo;
        _templateRepo = templateRepo;
        _featureRepo = featureRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _permRepo = permRepo;
        _userTenantRepo = userTenantRepo;
        _auditRepo = auditRepo;
        _platformDb = platformDb;
        _dnsService = dnsService;
    }

    public async Task<SuperAdminDashboardStatsDto> GetDashboardStatsAsync()
    {
        var tenants = (await _tenantRepo.GetAllAsync()).ToList();
        var domains = (await _domainRepo.GetAllAsync()).ToList();
        var databases = (await _dbRepo.GetAllAsync()).ToList();
        var users = (await _userRepo.GetAllAsync()).ToList();
        var auditLogs = (await _auditRepo.GetAllAsync(10)).ToList();

        var summaries = new List<TenantSummaryDto>();
        foreach (var t in tenants)
        {
            var dom = domains.FirstOrDefault(d => d.TenantId == t.Id && d.IsPrimary)?.Domain ?? domains.FirstOrDefault(d => d.TenantId == t.Id)?.Domain;
            var db = databases.FirstOrDefault(d => d.TenantId == t.Id)?.DatabaseName;
            var admin = users.FirstOrDefault(u => u.TenantId == t.Id)?.Username;
            summaries.Add(new TenantSummaryDto
            {
                Id = t.Id,
                TenantCode = t.TenantCode,
                Name = t.Name,
                Slug = t.Slug,
                Status = t.Status,
                ProvisioningStatus = "COMPLETED",
                PrimaryDomain = dom,
                DatabaseName = db,
                AdminUsername = admin,
                CreatedAt = t.CreatedAt
            });
        }

        return new SuperAdminDashboardStatsDto
        {
            TotalColleges = tenants.Count,
            ActiveColleges = tenants.Count(t => string.Equals(t.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)),
            SuspendedColleges = tenants.Count(t => string.Equals(t.Status, "SUSPENDED", StringComparison.OrdinalIgnoreCase)),
            TotalDomains = domains.Count,
            TotalDatabases = databases.Count,
            TotalCollegeAdmins = users.Count(u => u.Role == "CollegeAdmin"),
            RecentColleges = summaries.OrderByDescending(s => s.CreatedAt).Take(5).ToList(),
            RecentActivity = auditLogs
        };
    }

    public async Task<IEnumerable<TenantDto>> GetCollegesAsync()
    {
        var tenants = await _tenantRepo.GetAllAsync();
        var domains = await _domainRepo.GetAllAsync();
        var databases = await _dbRepo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();

        return tenants.Select(t =>
        {
            var dom = domains.FirstOrDefault(d => d.TenantId == t.Id && d.IsPrimary)?.Domain ?? domains.FirstOrDefault(d => d.TenantId == t.Id)?.Domain;
            var db = databases.FirstOrDefault(d => d.TenantId == t.Id)?.DatabaseName;
            var admin = users.FirstOrDefault(u => u.TenantId == t.Id)?.Username;

            return new TenantDto
            {
                Id = t.Id,
                TenantCode = t.TenantCode,
                Name = t.Name,
                Slug = t.Slug,
                Status = t.Status,
                ProvisioningStatus = "COMPLETED",
                DefaultThemeId = t.DefaultThemeId,
                TemplateCode = t.TemplateCode,
                LoginTemplate = t.LoginTemplate ?? "template2",
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                PrimaryDomain = dom,
                DatabaseName = db,
                AdminUsername = admin
            };
        });
    }

    public async Task<CollegeDetailsDto?> GetCollegeDetailsAsync(Guid id)
    {
        var tenant = await _tenantRepo.GetByIdAsync(id);
        if (tenant == null) return null;

        var domains = (await _domainRepo.GetByTenantIdAsync(id)).ToList();
        var database = await _dbRepo.GetByTenantIdAsync(id);
        var features = await _featureRepo.GetFeaturesByTenantIdAsync(id);
        var admins = (await _userRepo.GetByTenantIdAsync(id)).ToList();
        var auditLogs = (await _auditRepo.GetByTenantIdAsync(id, 20)).ToList();

        ThemeDto? themeDto = null;
        if (tenant.DefaultThemeId.HasValue)
        {
            var th = await _themeRepo.GetByIdAsync(tenant.DefaultThemeId.Value);
            if (th != null)
            {
                themeDto = new ThemeDto
                {
                    Id = th.Id,
                    Name = th.Name,
                    Code = th.Code,
                    Configuration = JsonSerializer.Deserialize<object>(th.Configuration),
                    IsActive = th.IsActive
                };
            }
        }

        return new CollegeDetailsDto
        {
            Tenant = new TenantDto
            {
                Id = tenant.Id,
                TenantCode = tenant.TenantCode,
                Name = tenant.Name,
                Slug = tenant.Slug,
                Status = tenant.Status,
                ProvisioningStatus = "COMPLETED",
                DefaultThemeId = tenant.DefaultThemeId,
                TemplateCode = tenant.TemplateCode,
                LoginTemplate = tenant.LoginTemplate ?? "template2",
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt,
                PrimaryDomain = domains.FirstOrDefault(d => d.IsPrimary)?.Domain ?? domains.FirstOrDefault()?.Domain,
                DatabaseName = database?.DatabaseName,
                AdminUsername = admins.FirstOrDefault()?.Username
            },
            Domains = domains,
            Database = database,
            Theme = themeDto,
            Features = features.ToList(),
            Admins = admins,
            AuditLogs = auditLogs
        };
    }

    public async Task<TenantSettingsDto> GetCollegeSettingsAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        using var conn = new NpgsqlConnection(tenant.DbConnectionString);
        await conn.OpenAsync();

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT id, site_name AS SiteName, tagline, logo_url AS LogoUrl, favicon_url AS FaviconUrl,
                   contact_email AS ContactEmail, contact_phone AS ContactPhone,
                   address, social_links::text AS SocialLinks,
                   login_bg_image_url AS LoginBgImageUrl, updated_at AS UpdatedAt
            FROM tenant_settings
            LIMIT 1;");

        if (row == null)
        {
            return new TenantSettingsDto
            {
                SiteName = tenant.Name,
                Tagline = "Excellence in Education",
                FaviconUrl = "/favicon.ico"
            };
        }

        return new TenantSettingsDto
        {
            Id = (Guid)row.id,
            SiteName = (string)row.sitename,
            Tagline = (string?)row.tagline,
            LogoUrl = (string?)row.logourl,
            FaviconUrl = (string?)row.faviconurl ?? "/favicon.ico",
            ContactEmail = (string?)row.contactemail,
            ContactPhone = (string?)row.contactphone,
            Address = (string?)row.address,
            SocialLinks = row.sociallinks != null ? JsonSerializer.Deserialize<object>((string)row.sociallinks) : null,
            LoginBgImageUrl = (string?)row.loginbgimageurl,
            UpdatedAt = (DateTime)row.updatedat
        };
    }

    public async Task<TenantSettingsDto> UpdateCollegeSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        using var conn = new NpgsqlConnection(tenant.DbConnectionString);
        await conn.OpenAsync();

        var socialJson = request.SocialLinks != null ? JsonSerializer.Serialize(request.SocialLinks) : "{}";
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tenant_settings;");

        if (count == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO tenant_settings (site_name, tagline, logo_url, favicon_url, contact_email, contact_phone, address, social_links, login_bg_image_url, updated_at)
                VALUES (@SiteName, @Tagline, @LogoUrl, @FaviconUrl, @ContactEmail, @ContactPhone, @Address, @socialJson::jsonb, @LoginBgImageUrl, CURRENT_TIMESTAMP);",
                new { request.SiteName, request.Tagline, request.LogoUrl, request.FaviconUrl, request.ContactEmail, request.ContactPhone, request.Address, socialJson, request.LoginBgImageUrl });
        }
        else
        {
            await conn.ExecuteAsync(@"
                UPDATE tenant_settings
                SET site_name = @SiteName, tagline = @Tagline, logo_url = @LogoUrl, favicon_url = @FaviconUrl,
                    contact_email = @ContactEmail, contact_phone = @ContactPhone,
                    address = @Address, social_links = @socialJson::jsonb,
                    login_bg_image_url = @LoginBgImageUrl, updated_at = CURRENT_TIMESTAMP;",
                new { request.SiteName, request.Tagline, request.LogoUrl, request.FaviconUrl, request.ContactEmail, request.ContactPhone, request.Address, socialJson, request.LoginBgImageUrl });
        }

        await _auditRepo.LogAsync(null, currentAdminUser, tenantId, "UPDATE_COLLEGE_SETTINGS", "TenantSettings", tenantId.ToString(), $"Updated title & logo settings for {tenant.Name}", null);

        return await GetCollegeSettingsAsync(tenantId);
    }

    public async Task<TenantDto> CreateCollegeWorkflowAsync(CreateCollegeWorkflowRequest request, string currentAdminUser)
    {
        var tenantCode = request.TenantCode.Trim().ToUpperInvariant();
        var slug = request.Slug.Trim().ToLowerInvariant();
        var dbName = !string.IsNullOrWhiteSpace(request.DatabaseName)
            ? request.DatabaseName.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_")
            : $"college_{tenantCode.ToLowerInvariant()}_database";
        var masterCs = "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true";
        var templateCode = !string.IsNullOrWhiteSpace(request.TemplateCode) ? request.TemplateCode.Trim().ToUpperInvariant() : "ENGINEERING_MODERN";

        // Auto-assign default theme matching templateCode if not explicitly set
        Guid? themeId = request.DefaultThemeId;
        if (!themeId.HasValue)
        {
            var allThemes = await _themeRepo.GetAllAsync();
            var targetThemeCode = templateCode switch
            {
                "ARTS_SCIENCE_MODERN" => "ARTS_GREEN",
                "MEDICAL_MODERN" => "NURSING_CARE",
                _ => "ENG_ROYAL"
            };
            themeId = allThemes.FirstOrDefault(t => t.Code == targetThemeCode)?.Id ?? allThemes.FirstOrDefault()?.Id;
        }

        // 1. Create Tenant Record
        var initialCs = $"Host=localhost;Port=5433;Database={dbName};Username=postgres;Password=root;Include Error Detail=true";
        var tenantId = await _tenantRepo.CreateAsync(tenantCode, request.Name.Trim(), slug, initialCs, themeId, templateCode, request.LoginTemplate ?? "template2", "ACTIVE");

        // 2. Create Domain Record (Production primary domain + local dev alias if needed)
        var domainName = request.PrimaryDomain.Trim().ToLowerInvariant();
        if (!domainName.Contains('.')) domainName = $"{domainName}.localhost";
        await _domainRepo.CreateAsync(tenantId, domainName, "PRIMARY", isPrimary: true);

        // If domain is a production domain (e.g. rcmcollege.com), automatically register dev alias {code}.localhost
        // so that developers can also access it on localhost:3000 seamlessly
        var devAlias = $"{tenantCode.ToLowerInvariant()}.localhost";
        if (!domainName.EndsWith(".localhost") && !domainName.EndsWith(".localhost.com"))
        {
            try
            {
                await _domainRepo.CreateAsync(tenantId, devAlias, "DEV_ALIAS", isPrimary: false);
            }
            catch
            {
                // Ignore if dev alias already taken
            }
        }

        // 3. Provision physical PostgreSQL database & run tenant migrations (V1..V8)
        var platformCs = "Host=localhost;Port=5433;Database=dinamic_college_website;Username=postgres;Password=root;Include Error Detail=true";
        var actualTenantCs = await DatabaseMigrator.ProvisionTenantDatabaseAsync(tenantCode, dbName, masterCs, platformCs, tenantId, templateCode, request.Name.Trim());

        // 4. Configure features
        var defaultFeatures = request.EnabledFeatures != null && request.EnabledFeatures.Count > 0
            ? request.EnabledFeatures
            : new List<string> { "PAGES", "NEWS", "EVENTS", "BANNERS", "GALLERY", "DEPARTMENTS", "COURSES", "FACULTY", "TESTIMONIALS", "ADMISSIONS" };

        var allFeatures = await _featureRepo.GetAllFeaturesAsync();
        foreach (var f in allFeatures)
        {
            var isEnabled = defaultFeatures.Contains(f.Code, StringComparer.OrdinalIgnoreCase);
            await _featureRepo.SetTenantFeatureAsync(tenantId, f.Code, isEnabled);
        }

        // 5. Create initial CollegeAdmin user & User-Tenant mapping
        var adminUsername = !string.IsNullOrWhiteSpace(request.AdminUsername) ? request.AdminUsername.Trim() : $"{tenantCode.ToLowerInvariant()}-admin";
        var adminEmail = !string.IsNullOrWhiteSpace(request.AdminEmail) ? request.AdminEmail.Trim() : $"admin@{tenantCode.ToLowerInvariant()}.edu";
        var adminFullName = !string.IsNullOrWhiteSpace(request.AdminFullName) ? request.AdminFullName.Trim() : $"{request.Name} Administrator";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(!string.IsNullOrWhiteSpace(request.AdminPassword) ? request.AdminPassword : "Admin@123", 12);

        var userId = await _userRepo.CreateAsync(tenantId, adminUsername, adminEmail, passwordHash, adminFullName, "CollegeAdmin");
        var role = (await _roleRepo.GetAllRolesAsync()).FirstOrDefault(r => r.Code == "CollegeAdmin");
        await _userTenantRepo.AssignTenantsAsync(userId, new List<Guid> { tenantId }, roleId: role?.Id);

        // 6. Log Audit
        await _auditRepo.LogAsync(
            currentAdminUser, 
            "CREATE_COLLEGE", 
            "TENANT", 
            tenantId.ToString(), 
            $"Created college '{request.Name}' ({tenantCode}), provisioned database '{dbName}', mapped domain '{domainName}', and generated admin '{adminUsername}'.", 
            tenantId);

        return new TenantDto
        {
            Id = tenantId,
            TenantCode = tenantCode,
            Name = request.Name,
            Slug = slug,
            Status = "ACTIVE",
            ProvisioningStatus = "COMPLETED",
            DefaultThemeId = themeId,
            PrimaryDomain = domainName,
            DatabaseName = dbName,
            AdminUsername = adminUsername,
            TemplateCode = templateCode,
            LoginTemplate = request.LoginTemplate ?? "template2"
        };
    }

    public async Task<TenantDto> UpdateCollegeAsync(Guid id, UpdateCollegeRequest request, string currentAdminUser)
    {
        await _tenantRepo.UpdateAsync(id, request.Name.Trim(), request.Slug.Trim().ToLowerInvariant(), request.DefaultThemeId, request.LoginTemplate?.Trim().ToLowerInvariant());

        // Update college administrator password if provided
        if (!string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            var admins = (await _userRepo.GetByTenantIdAsync(id)).ToList();
            var primaryAdmin = admins.FirstOrDefault();
            if (primaryAdmin != null)
            {
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword.Trim(), 12);
                await _userRepo.UpdatePasswordAsync(primaryAdmin.Id, passwordHash);
                await _auditRepo.LogAsync(currentAdminUser, "RESET_ADMIN_PASSWORD", "USER", primaryAdmin.Id.ToString(), $"Updated password for admin '{primaryAdmin.Username}' of college '{request.Name}'", id);
            }
        }

        var updated = await _tenantRepo.GetByIdAsync(id);
        if (updated != null && !string.IsNullOrWhiteSpace(updated.DbConnectionString))
        {
            try
            {
                using var tenantConn = new NpgsqlConnection(updated.DbConnectionString);
                await tenantConn.OpenAsync();
                if (!string.IsNullOrWhiteSpace(request.LoginTemplate))
                {
                    await tenantConn.ExecuteAsync(@"
                        UPDATE tenant_settings 
                        SET site_name = @name, login_template = @loginTemplate, updated_at = CURRENT_TIMESTAMP;", 
                        new { name = request.Name.Trim(), loginTemplate = request.LoginTemplate.Trim().ToLowerInvariant() });
                }
                else
                {
                    await tenantConn.ExecuteAsync("UPDATE tenant_settings SET site_name = @name, updated_at = CURRENT_TIMESTAMP;", new { name = request.Name.Trim() });
                }
            }
            catch { /* ignore if tenant DB offline */ }
        }
        await _auditRepo.LogAsync(currentAdminUser, "UPDATE_COLLEGE", "TENANT", id.ToString(), $"Updated college '{request.Name}'", id);

        var domains = await _domainRepo.GetByTenantIdAsync(id);
        var primaryDomain = domains.FirstOrDefault(d => d.IsPrimary)?.Domain ?? domains.FirstOrDefault()?.Domain;
        var adminsAfter = (await _userRepo.GetByTenantIdAsync(id)).ToList();

        return new TenantDto
        {
            Id = id,
            TenantCode = updated?.TenantCode ?? "",
            Name = updated?.Name ?? request.Name,
            Slug = updated?.Slug ?? request.Slug,
            Status = updated?.Status ?? "ACTIVE",
            ProvisioningStatus = "COMPLETED",
            DefaultThemeId = updated?.DefaultThemeId,
            TemplateCode = updated?.TemplateCode ?? "ENGINEERING_MODERN",
            LoginTemplate = updated?.LoginTemplate ?? request.LoginTemplate ?? "template2",
            PrimaryDomain = primaryDomain,
            AdminUsername = adminsAfter.FirstOrDefault()?.Username
        };
    }

    public async Task<bool> ActivateCollegeAsync(Guid id, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(id);
        if (tenant == null) return false;
        var success = await _tenantRepo.UpdateStatusAsync(id, "ACTIVE");
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "ACTIVATE_COLLEGE", "TENANT", id.ToString(), $"Activated college '{tenant.Name}' ({tenant.TenantCode})", id);
        }
        return success;
    }

    public async Task<bool> SuspendCollegeAsync(Guid id, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(id);
        if (tenant == null) return false;
        var success = await _tenantRepo.UpdateStatusAsync(id, "SUSPENDED");
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "SUSPEND_COLLEGE", "TENANT", id.ToString(), $"Suspended college '{tenant.Name}' ({tenant.TenantCode})", id);
        }
        return success;
    }

    public async Task<bool> DeleteCollegeAsync(Guid id, bool dropDatabase, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(id);
        if (tenant == null) return false;

        // 1. If physical database drop requested, terminate connections and drop database
        if (dropDatabase)
        {
            try
            {
                var dbInfo = await _dbRepo.GetByTenantIdAsync(id);
                var dbName = dbInfo?.DatabaseName;

                if (string.IsNullOrWhiteSpace(dbName) && !string.IsNullOrWhiteSpace(tenant.DbConnectionString))
                {
                    var builder = new NpgsqlConnectionStringBuilder(tenant.DbConnectionString);
                    dbName = builder.Database;
                }

                // Security safeguard: Never drop core system databases
                if (!string.IsNullOrWhiteSpace(dbName) 
                    && !dbName.Equals("postgres", StringComparison.OrdinalIgnoreCase) 
                    && !dbName.Equals("dinamic_college_website", StringComparison.OrdinalIgnoreCase))
                {
                    var masterCs = "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true";
                    using var masterConn = new NpgsqlConnection(masterCs);
                    await masterConn.OpenAsync();

                    // Terminate all active client connections to this tenant database
                    var terminateSql = @"
                        SELECT pg_terminate_backend(pid) 
                        FROM pg_stat_activity 
                        WHERE datname = @dbName AND pid <> pg_backend_pid();";
                    await masterConn.ExecuteAsync(terminateSql, new { dbName });

                    // Drop the database
                    await masterConn.ExecuteAsync($"DROP DATABASE IF EXISTS \"{dbName}\";");
                }
            }
            catch (Exception ex)
            {
                // Continue with soft deletion even if DB drop fails
                Console.WriteLine($"[DropDatabase Warning] {ex.Message}");
            }
        }

        // 2. Cascade soft-delete & cleanup across platform tables
        using var conn = _platformDb.CreateConnection();

        // Soft-delete tenant
        await conn.ExecuteAsync(
            "UPDATE tenants SET is_deleted = TRUE, status = 'DELETED', deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", 
            new { id });

        // Soft-delete associated domains
        await conn.ExecuteAsync(
            "UPDATE domains SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE tenant_id = @id;", 
            new { id });

        // Soft-delete / mark registry record
        await conn.ExecuteAsync(
            "UPDATE tenant_database_registry SET is_deleted = TRUE, status = 'DELETED', deleted_at = CURRENT_TIMESTAMP WHERE tenant_id = @id;", 
            new { id });

        // Soft-delete associated users
        await conn.ExecuteAsync(
            "UPDATE users SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE tenant_id = @id;", 
            new { id });

        // Remove mapping records
        await conn.ExecuteAsync("DELETE FROM user_tenants WHERE tenant_id = @id;", new { id });
        await conn.ExecuteAsync("DELETE FROM tenant_features WHERE tenant_id = @id;", new { id });

        // 3. Log Audit
        await _auditRepo.LogAsync(
            currentAdminUser, 
            "DELETE_COLLEGE", 
            "TENANT", 
            id.ToString(), 
            $"Deleted college '{tenant.Name}' ({tenant.TenantCode}){(dropDatabase ? " and dropped physical database" : "")}.", 
            id);

        return true;
    }

    // Domains
    public async Task<IEnumerable<DomainDto>> GetDomainsAsync() => await _domainRepo.GetAllAsync();

    public async Task<DomainDto> CreateDomainAsync(CreateDomainRequest request, string currentAdminUser)
    {
        var verificationToken = _dnsService.GenerateVerificationToken();
        var id = await _domainRepo.CreateAsync(request.TenantId, request.Domain.Trim().ToLowerInvariant(), request.DomainType, request.IsPrimary, verificationToken);
        await _auditRepo.LogAsync(currentAdminUser, "CREATE_DOMAIN", "DOMAIN", id.ToString(), $"Mapped domain '{request.Domain}' to tenant ID {request.TenantId}. Add DNS TXT record: _codebuff-verify={verificationToken}", request.TenantId);
        return new DomainDto
        {
            Id = id,
            TenantId = request.TenantId,
            Domain = request.Domain,
            DomainType = request.DomainType,
            IsPrimary = request.IsPrimary,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateDomainAsync(Guid id, UpdateDomainRequest request, string currentAdminUser)
    {
        var success = await _domainRepo.UpdateAsync(id, request.Domain, request.DomainType);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "UPDATE_DOMAIN", "DOMAIN", id.ToString(), $"Updated domain ID {id} to {request.Domain}");
        }
        return success;
    }

    public async Task<bool> DeleteDomainAsync(Guid domainId, string currentAdminUser)
    {
        var success = await _domainRepo.DeleteAsync(domainId);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "DELETE_DOMAIN", "DOMAIN", domainId.ToString(), $"Deleted domain ID {domainId}");
        }
        return success;
    }

    public async Task<bool> VerifyDomainAsync(Guid domainId, string currentAdminUser)
    {
        var domain = await _domainRepo.GetByIdAsync(domainId);
        if (domain == null) return false;

        var token = await _domainRepo.GetVerificationTokenAsync(domainId);
        if (string.IsNullOrEmpty(token)) return false;

        var result = await _dnsService.VerifyDomainAsync(domain.Domain, token);

        if (result.IsVerified)
        {
            await _domainRepo.VerifyAsync(domainId);
            await _auditRepo.LogAsync(currentAdminUser, "VERIFY_DOMAIN", "DOMAIN", domainId.ToString(), $"DNS verified for domain '{domain.Domain}'. {result.Message}");
            return true;
        }

        await _auditRepo.LogAsync(currentAdminUser, "VERIFY_DOMAIN_FAILED", "DOMAIN", domainId.ToString(), $"DNS verification failed for '{domain.Domain}'. {result.Message}");
        return false;
    }

    public async Task<bool> SetPrimaryDomainAsync(Guid domainId, string currentAdminUser)
    {
        var success = await _domainRepo.SetPrimaryAsync(domainId);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "SET_PRIMARY_DOMAIN", "DOMAIN", domainId.ToString(), $"Set primary route for domain ID {domainId}");
        }
        return success;
    }

    // Databases & Migrations
    public async Task<IEnumerable<DatabaseRegistryDto>> GetDatabasesAsync() => await _dbRepo.GetAllAsync();

    public async Task<DatabaseRegistryDto?> GetDatabaseByTenantIdAsync(Guid tenantId) => await _dbRepo.GetByTenantIdAsync(tenantId);

    public async Task<DatabaseRegistryDto> ProvisionDatabaseAsync(Guid tenantId, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        var dbName = $"college_{tenant.TenantCode.ToLowerInvariant()}";
        var masterCs = "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true";
        var platformCs = "Host=localhost;Port=5433;Database=dinamic_college_website;Username=postgres;Password=root;Include Error Detail=true";

        await DatabaseMigrator.ProvisionTenantDatabaseAsync(tenant.TenantCode, dbName, masterCs, platformCs, tenantId, tenant.TemplateCode ?? "ENGINEERING_MODERN", tenant.Name.Trim());
        await _auditRepo.LogAsync(currentAdminUser, "PROVISION_DATABASE", "DATABASE", dbName, $"Re-provisioned database '{dbName}' for tenant '{tenant.TenantCode}'", tenantId);

        return await _dbRepo.GetByTenantIdAsync(tenantId) ?? new DatabaseRegistryDto();
    }

    public async Task<DatabaseRegistryDto> VerifyDatabaseAsync(Guid tenantId, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        var dbRecord = await _dbRepo.GetByTenantIdAsync(tenantId);
        var dbName = dbRecord?.DatabaseName ?? $"college_{tenant.TenantCode.ToLowerInvariant()}";
        var status = "HEALTHY";

        try
        {
            using var conn = new NpgsqlConnection(tenant.DbConnectionString);
            await conn.OpenAsync();
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pages;");
            status = "HEALTHY";
        }
        catch (Exception)
        {
            status = "CONNECTION_ERROR";
        }

        await _dbRepo.UpsertAsync(tenantId, dbName, "localhost", 5433, "postgres", status, "V8");
        await _auditRepo.LogAsync(currentAdminUser, "VERIFY_DATABASE", "DATABASE", dbName, $"Verified database health: {status}", tenantId);

        return await _dbRepo.GetByTenantIdAsync(tenantId) ?? new DatabaseRegistryDto();
    }

    public Task<List<MigrationHistoryDto>> GetMigrationsAsync()
    {
        var list = new List<MigrationHistoryDto>
        {
            new() { Version = "V1", ScriptName = "create_pages.sql", Description = "Dynamic pages and slug indexes", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V2", ScriptName = "create_page_sections.sql", Description = "Polymorphic page sections with JSONB", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V3", ScriptName = "create_menus.sql", Description = "Navigation menu definitions", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V4", ScriptName = "create_menu_items.sql", Description = "Hierarchical menu items and routing links", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V5", ScriptName = "create_news.sql", Description = "Campus news and press releases", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V6", ScriptName = "create_events.sql", Description = "Campus events calendar", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V7", ScriptName = "create_media_settings_contact_versions.sql", Description = "Media library, settings, contact, page versions", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
            new() { Version = "V8", ScriptName = "create_cms_academics_banners_gallery.sql", Description = "Banners, galleries, departments, courses, faculty, testimonials", Status = "SUCCESS", AppliedAt = DateTime.UtcNow },
        };
        return Task.FromResult(list);
    }

    public async Task<MigrationStatusDto> GetMigrationStatusForTenantAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        var db = await _dbRepo.GetByTenantIdAsync(tenantId);
        var applied = await GetMigrationsAsync();

        return new MigrationStatusDto
        {
            TenantId = tenantId,
            TenantCode = tenant.TenantCode,
            DatabaseName = db?.DatabaseName ?? $"college_{tenant.TenantCode.ToLower()}",
            CurrentVersion = db?.CurrentMigrationVersion ?? "V8",
            Status = db?.Status ?? "HEALTHY",
            LastCheckedAt = DateTime.UtcNow,
            AppliedMigrations = applied
        };
    }

    public async Task<DatabaseRegistryDto> MigrateTenantDatabaseAsync(Guid tenantId, string currentAdminUser)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new InvalidOperationException("Tenant not found");

        await DatabaseMigrator.MigrateTenantTablesAsync(tenant.DbConnectionString);
        await _dbRepo.UpsertAsync(tenantId, $"college_{tenant.TenantCode.ToLower()}", "localhost", 5433, "postgres", "HEALTHY", "V8");
        await _auditRepo.LogAsync(currentAdminUser, "RUN_MIGRATION", "DATABASE", tenant.TenantCode, $"Executed schema migrations V1..V8 on college_{tenant.TenantCode.ToLower()}", tenantId);

        return await _dbRepo.GetByTenantIdAsync(tenantId) ?? new DatabaseRegistryDto();
    }

    // Themes
    public async Task<IEnumerable<ThemeDto>> GetThemesAsync()
    {
        var themes = await _themeRepo.GetAllAsync();
        return themes.Select(t => new ThemeDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Configuration = JsonSerializer.Deserialize<object>(t.Configuration),
            IsActive = t.IsActive
        });
    }

    public async Task<ThemeDto?> GetThemeByIdAsync(Guid id)
    {
        var t = await _themeRepo.GetByIdAsync(id);
        if (t == null) return null;
        return new ThemeDto
        {
            Id = t.Id,
            Name = t.Name,
            Code = t.Code,
            Configuration = JsonSerializer.Deserialize<object>(t.Configuration),
            IsActive = t.IsActive
        };
    }

    public async Task<ThemeDto> CreateThemeAsync(CreateThemeRequest request, string currentAdminUser)
    {
        var configJson = JsonSerializer.Serialize(request.Configuration);
        var id = await _themeRepo.CreateAsync(request.Name, request.Code, configJson);
        await _auditRepo.LogAsync(currentAdminUser, "CREATE_THEME", "THEME", id.ToString(), $"Created theme '{request.Name}' ({request.Code})");

        return new ThemeDto
        {
            Id = id,
            Name = request.Name,
            Code = request.Code,
            Configuration = request.Configuration,
            IsActive = true
        };
    }

    public async Task<bool> UpdateThemeAsync(Guid id, UpdateThemeRequest request, string currentAdminUser)
    {
        var configJson = JsonSerializer.Serialize(request.Configuration);
        var success = await _themeRepo.UpdateAsync(id, request.Name, configJson, request.IsActive);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "UPDATE_THEME", "THEME", id.ToString(), $"Updated theme '{request.Name}'");
        }
        return success;
    }

    public async Task<bool> DeleteThemeAsync(Guid id, string currentAdminUser)
    {
        var success = await _themeRepo.DeleteAsync(id);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "DELETE_THEME", "THEME", id.ToString(), $"Deleted theme ID {id}");
        }
        return success;
    }

    public async Task<bool> AssignThemeAsync(Guid tenantId, Guid themeId, string currentAdminUser)
    {
        var success = await _tenantRepo.UpdateThemeAsync(tenantId, themeId);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "ASSIGN_THEME", "TENANT", tenantId.ToString(), $"Assigned theme ID {themeId} to tenant ID {tenantId}", tenantId);
        }
        return success;
    }

    // Templates
    public async Task<IEnumerable<TemplateDto>> GetTemplatesAsync() => await _templateRepo.GetAllAsync();

    public async Task<TemplateDto?> GetTemplateByIdAsync(Guid id) => await _templateRepo.GetByIdAsync(id);

    public async Task<TemplateDto> CreateTemplateAsync(CreateTemplateRequest request, string currentAdminUser)
    {
        var id = await _templateRepo.CreateAsync(request);
        await _auditRepo.LogAsync(currentAdminUser, "CREATE_TEMPLATE", "TEMPLATE", id.ToString(), $"Created template '{request.Name}' ({request.Code})");
        return (await _templateRepo.GetByIdAsync(id))!;
    }

    public async Task<TemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, string currentAdminUser)
    {
        await _templateRepo.UpdateAsync(id, request);
        await _auditRepo.LogAsync(currentAdminUser, "UPDATE_TEMPLATE", "TEMPLATE", id.ToString(), $"Updated template '{request.Name}'");
        return (await _templateRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> DeleteTemplateAsync(Guid id, string currentAdminUser)
    {
        var template = await _templateRepo.GetByIdAsync(id);
        var success = await _templateRepo.DeleteAsync(id);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "DELETE_TEMPLATE", "TEMPLATE", id.ToString(), $"Deleted template '{template?.Name ?? id.ToString()}'");
        }
        return success;
    }

    public async Task<TemplatePreviewDto?> GetTemplatePreviewAsync(string templateCode)
    {
        var template = await _templateRepo.GetByCodeAsync(templateCode);
        if (template == null) return null;

        ThemeDto? defaultTheme = null;
        if (template.DefaultThemeId.HasValue)
        {
            defaultTheme = await GetThemeByIdAsync(template.DefaultThemeId.Value);
        }

        return new TemplatePreviewDto
        {
            Template = template,
            DefaultTheme = defaultTheme,
            Blueprint = new
            {
                starterPages = template.StarterPages,
                starterMenus = template.StarterMenus,
                recommendedFeatures = template.RecommendedFeatures
            }
        };
    }

    public async Task<bool> AssignTemplateAsync(Guid tenantId, string templateCode, string currentAdminUser)
    {
        var success = await _templateRepo.AssignToTenantAsync(tenantId, templateCode);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "ASSIGN_TEMPLATE", "TENANT", tenantId.ToString(), $"Assigned template '{templateCode}' to tenant ID {tenantId}", tenantId);
        }
        return success;
    }

    // Features
    public async Task<IEnumerable<FeatureDto>> GetPlatformFeaturesAsync() => await _featureRepo.GetAllFeaturesAsync();

    public async Task<FeatureSummaryDto> GetFeatureSummaryAsync() => await _featureRepo.GetSummaryAsync();

    public async Task<FeatureDto> CreateFeatureAsync(CreateFeatureRequest request, string currentAdminUser)
    {
        var id = await _featureRepo.CreateAsync(request);
        await _auditRepo.LogAsync(currentAdminUser, "CREATE_FEATURE", "FEATURE", id.ToString(), $"Created feature '{request.Name}' ({request.Code})");
        return (await _featureRepo.GetByIdAsync(id))!;
    }

    public async Task<FeatureDto> UpdateFeatureAsync(Guid id, UpdateFeatureRequest request, string currentAdminUser)
    {
        await _featureRepo.UpdateAsync(id, request);
        await _auditRepo.LogAsync(currentAdminUser, "UPDATE_FEATURE", "FEATURE", id.ToString(), $"Updated feature '{request.Name}'");
        return (await _featureRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> ToggleFeatureStatusAsync(Guid id, string currentAdminUser)
    {
        var newStatus = await _featureRepo.ToggleStatusAsync(id);
        var feat = await _featureRepo.GetByIdAsync(id);
        await _auditRepo.LogAsync(currentAdminUser, "TOGGLE_FEATURE", "FEATURE", id.ToString(), $"Toggled feature '{feat?.Name}' status to {(newStatus ? "ACTIVE" : "DISABLED")}");
        return newStatus;
    }

    public async Task<FeatureCollegeUsageDto> GetFeatureCollegeUsageAsync(string featureCode) => await _featureRepo.GetFeatureCollegeUsageAsync(featureCode);

    public async Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId) => await _featureRepo.GetFeaturesByTenantIdAsync(tenantId);

    public async Task<bool> UpdateTenantFeaturesAsync(Guid tenantId, Dictionary<string, bool> features, string currentAdminUser)
    {
        foreach (var kvp in features)
        {
            await _featureRepo.SetTenantFeatureAsync(tenantId, kvp.Key, kvp.Value);
        }
        await _auditRepo.LogAsync(currentAdminUser, "UPDATE_FEATURES", "TENANT", tenantId.ToString(), $"Updated features for tenant ID {tenantId}", tenantId);
        return true;
    }

    // Roles & Permissions
    public async Task<IEnumerable<RoleDto>> GetRolesAsync() => await _roleRepo.GetAllRolesAsync();

    public async Task<RoleDto?> GetRoleByIdAsync(Guid id) => await _roleRepo.GetByIdAsync(id);

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string currentAdminUser)
    {
        var id = await _roleRepo.CreateAsync(request.Name, request.Code, request.Description);
        if (request.PermissionIds != null && request.PermissionIds.Count > 0)
        {
            await _roleRepo.UpdatePermissionsAsync(id, request.PermissionIds);
        }
        await _auditRepo.LogAsync(currentAdminUser, "CREATE_ROLE", "ROLE", id.ToString(), $"Created role '{request.Name}' ({request.Code})");
        return (await _roleRepo.GetByIdAsync(id))!;
    }

    public async Task<bool> UpdateRoleAsync(Guid id, UpdateRoleRequest request, string currentAdminUser)
    {
        var success = await _roleRepo.UpdateAsync(id, request.Name, request.Description);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "UPDATE_ROLE", "ROLE", id.ToString(), $"Updated role '{request.Name}'");
        }
        return success;
    }

    public async Task<bool> UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, string currentAdminUser)
    {
        var success = await _roleRepo.UpdatePermissionsAsync(roleId, permissionIds);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "UPDATE_ROLE_PERMISSIONS", "ROLE", roleId.ToString(), $"Updated permissions for role ID {roleId}");
        }
        return success;
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync() => await _permRepo.GetAllPermissionsAsync();

    // Users & User-Tenants
    public async Task<IEnumerable<UserDto>> GetAdminUsersAsync() => await _userRepo.GetAllAsync();

    public async Task<UserDto> CreateAdminUserAsync(CreateAdminUserRequest request, string currentAdminUser)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);
        var id = await _userRepo.CreateAsync(request.TenantId, request.Username.Trim(), request.Email.Trim(), passwordHash, request.FullName.Trim(), request.Role);
        
        if (request.TenantId.HasValue)
        {
            var role = (await _roleRepo.GetAllRolesAsync()).FirstOrDefault(r => r.Code == request.Role);
            await _userTenantRepo.AssignTenantsAsync(id, new List<Guid> { request.TenantId.Value }, roleId: role?.Id);
        }

        await _auditRepo.LogAsync(currentAdminUser, "CREATE_USER", "USER", id.ToString(), $"Created user '{request.Username}' with role '{request.Role}'", request.TenantId);

        return new UserDto
        {
            Id = id,
            TenantId = request.TenantId,
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateAdminUserAsync(Guid id, UpdateAdminUserRequest request, string currentAdminUser)
    {
        var success = await _userRepo.UpdateAsync(id, request.FullName, request.Email, request.IsActive);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "UPDATE_USER", "USER", id.ToString(), $"Updated user ID {id}");
        }
        return success;
    }

    public async Task<IEnumerable<UserTenantDto>> GetUserTenantsAsync(Guid userId) => await _userTenantRepo.GetByUserIdAsync(userId);

    public async Task<bool> AssignUserTenantsAsync(Guid userId, AssignUserTenantsRequest request, string currentAdminUser)
    {
        var success = await _userTenantRepo.AssignTenantsAsync(userId, request.TenantIds, request.RoleId);
        if (success)
        {
            await _auditRepo.LogAsync(currentAdminUser, "ASSIGN_USER_TENANTS", "USER", userId.ToString(), $"Assigned {request.TenantIds.Count} colleges to user ID {userId}");
        }
        return success;
    }

    // Audit Logs
    public async Task<PaginatedResponse<AuditLogDto>> GetFilteredAuditLogsAsync(AuditLogFilterRequest filter)
    {
        return await _auditRepo.GetFilteredAsync(filter);
    }
}

public class NoticeService : INoticeService
{
    private readonly NoticeRepository _noticeRepo;

    public NoticeService(NoticeRepository noticeRepo) => _noticeRepo = noticeRepo;

    public async Task<PaginatedResponse<Notice>> GetNoticesPaginatedAsync(PaginationFilter filter) => await _noticeRepo.GetAllPaginatedAsync(filter);

    public async Task<Notice?> GetNoticeByIdAsync(Guid id) => await _noticeRepo.GetByIdAsync(id);

    public async Task<NoticeDto?> GetNoticeBySlugAsync(string slug)
    {
        var notice = await _noticeRepo.GetBySlugAsync(slug);
        if (notice == null || !notice.IsPublished) return null;

        return new NoticeDto
        {
            Id = notice.Id,
            Title = notice.Title,
            Slug = notice.Slug,
            Summary = notice.Summary,
            Content = notice.Content,
            PdfUrl = notice.PdfUrl,
            Category = notice.Category,
            PublishDate = notice.PublishDate,
            ExpiryDate = notice.ExpiryDate,
            IsPublished = notice.IsPublished,
            IsImportant = notice.IsImportant
        };
    }

    public async Task<Notice> CreateNoticeAsync(CreateNoticeRequest request)
    {
        var notice = new Notice
        {
            Title = request.Title,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Summary = request.Summary,
            Content = request.Content,
            PdfUrl = request.PdfUrl,
            Category = request.Category,
            ExpiryDate = request.ExpiryDate,
            IsImportant = request.IsImportant,
            IsPublished = false
        };
        var id = await _noticeRepo.CreateAsync(notice);
        notice.Id = id;
        return notice;
    }

    public async Task<Notice> UpdateNoticeAsync(Guid id, UpdateNoticeRequest request)
    {
        var notice = new Notice
        {
            Id = id,
            Title = request.Title,
            Slug = request.Slug.ToLowerInvariant().Trim(),
            Summary = request.Summary,
            Content = request.Content,
            PdfUrl = request.PdfUrl,
            Category = request.Category,
            ExpiryDate = request.ExpiryDate,
            IsImportant = request.IsImportant
        };
        await _noticeRepo.UpdateAsync(notice);
        return notice;
    }

    public async Task<bool> DeleteNoticeAsync(Guid id) => await _noticeRepo.DeleteAsync(id);

    public async Task<bool> PublishNoticeAsync(Guid id) => await _noticeRepo.SetPublishStatusAsync(id, true);

    public async Task<bool> UnpublishNoticeAsync(Guid id) => await _noticeRepo.SetPublishStatusAsync(id, false);
}

public class CapabilityService : ICapabilityService
{
    private readonly ITenantContext _tenantContext;
    private readonly FeatureRepository _featureRepo;
    private readonly TenantRepository _tenantRepo;
    private readonly ThemeRepository _themeRepo;
    private readonly TemplateRepository _templateRepo;

    public CapabilityService(
        ITenantContext tenantContext,
        FeatureRepository featureRepo,
        TenantRepository tenantRepo,
        ThemeRepository themeRepo,
        TemplateRepository templateRepo)
    {
        _tenantContext = tenantContext;
        _featureRepo = featureRepo;
        _tenantRepo = tenantRepo;
        _themeRepo = themeRepo;
        _templateRepo = templateRepo;
    }

    public async Task<CollegeCapabilitiesDto> GetCapabilitiesAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        var tenantCode = tenant?.TenantCode ?? _tenantContext.TenantCode ?? string.Empty;
        var collegeName = tenant?.Name ?? _tenantContext.Name ?? string.Empty;
        var templateCode = tenant?.TemplateCode ?? _tenantContext.TemplateCode ?? "ENGINEERING_MODERN";

        var template = await _templateRepo.GetByCodeAsync(templateCode);
        var templateName = template?.Name ?? templateCode;

        ThemeDto? activeTheme = null;
        var themeId = tenant?.DefaultThemeId ?? _tenantContext.DefaultThemeId;
        if (themeId.HasValue)
        {
            var theme = await _themeRepo.GetByIdAsync(themeId.Value);
            if (theme != null)
            {
                activeTheme = new ThemeDto
                {
                    Id = theme.Id,
                    Name = theme.Name,
                    Code = theme.Code,
                    Configuration = !string.IsNullOrWhiteSpace(theme.Configuration) 
                        ? JsonSerializer.Deserialize<object>(theme.Configuration) 
                        : null,
                    IsActive = theme.IsActive
                };
            }
        }

        var tenantFeatures = await _featureRepo.GetFeaturesByTenantIdAsync(tenantId);
        var enabledCodes = tenantFeatures.Where(f => f.IsEnabled).Select(f => f.FeatureCode.ToUpperInvariant()).ToHashSet();

        // Standard admin navigation modules
        var allModules = new List<AdminModuleDto>
        {
            new AdminModuleDto { Key = "dashboard", Label = "Dashboard", Path = "/admin", Icon = "LayoutDashboard", RequiredFeature = "", IsEnabled = true },
            new AdminModuleDto { Key = "pages", Label = "Pages & Sections", Path = "/admin/pages", Icon = "FileText", RequiredFeature = "PAGES", IsEnabled = enabledCodes.Contains("PAGES") },
            new AdminModuleDto { Key = "menus", Label = "Navigation Menus", Path = "/admin/menus", Icon = "Menu", RequiredFeature = "MENUS", IsEnabled = enabledCodes.Contains("MENUS") },
            new AdminModuleDto { Key = "banners", Label = "Hero Banners", Path = "/admin/banners", Icon = "Image", RequiredFeature = "BANNERS", IsEnabled = enabledCodes.Contains("BANNERS") },
            new AdminModuleDto { Key = "marquee", Label = "Announcement Bar", Path = "/admin/marquee", Icon = "Megaphone", RequiredFeature = "MARQUEE", IsEnabled = enabledCodes.Contains("MARQUEE") },
            new AdminModuleDto { Key = "news", Label = "Campus News", Path = "/admin/news", Icon = "Newspaper", RequiredFeature = "NEWS", IsEnabled = enabledCodes.Contains("NEWS") },
            new AdminModuleDto { Key = "events", Label = "Events Calendar", Path = "/admin/events", Icon = "Calendar", RequiredFeature = "EVENTS", IsEnabled = enabledCodes.Contains("EVENTS") },
            new AdminModuleDto { Key = "notices", Label = "Notices & Circulars", Path = "/admin/notices", Icon = "Bell", RequiredFeature = "NOTICES", IsEnabled = enabledCodes.Contains("NOTICES") },
            new AdminModuleDto { Key = "departments", Label = "Departments", Path = "/admin/departments", Icon = "Building2", RequiredFeature = "DEPARTMENTS", IsEnabled = enabledCodes.Contains("DEPARTMENTS") },
            new AdminModuleDto { Key = "courses", Label = "Courses & Degrees", Path = "/admin/courses", Icon = "BookOpen", RequiredFeature = "COURSES", IsEnabled = enabledCodes.Contains("COURSES") },
            new AdminModuleDto { Key = "faculty", Label = "Faculty Directory", Path = "/admin/faculty", Icon = "Users", RequiredFeature = "FACULTY", IsEnabled = enabledCodes.Contains("FACULTY") },
            new AdminModuleDto { Key = "gallery", Label = "Photo Gallery", Path = "/admin/gallery", Icon = "Camera", RequiredFeature = "GALLERY", IsEnabled = enabledCodes.Contains("GALLERY") },
            new AdminModuleDto { Key = "media", Label = "Media Library", Path = "/admin/media", Icon = "Folder", RequiredFeature = "MEDIA", IsEnabled = enabledCodes.Contains("MEDIA") },
            new AdminModuleDto { Key = "testimonials", Label = "Testimonials", Path = "/admin/testimonials", Icon = "MessageSquare", RequiredFeature = "TESTIMONIALS", IsEnabled = enabledCodes.Contains("TESTIMONIALS") },
            new AdminModuleDto { Key = "contact", Label = "Contact Inquiries", Path = "/admin/contact", Icon = "Mail", RequiredFeature = "CONTACT", IsEnabled = enabledCodes.Contains("CONTACT") },
            new AdminModuleDto { Key = "stats", Label = "Key Highlights & Stats", Path = "/admin/stats", Icon = "BarChart2", RequiredFeature = "STATS", IsEnabled = enabledCodes.Contains("STATS") },
            new AdminModuleDto { Key = "quote", Label = "Leadership Quote", Path = "/admin/quote", Icon = "Quote", RequiredFeature = "QUOTE", IsEnabled = enabledCodes.Contains("QUOTE") },
            new AdminModuleDto { Key = "placements", Label = "Placements", Path = "/admin/placements", Icon = "Briefcase", RequiredFeature = "PLACEMENTS", IsEnabled = enabledCodes.Contains("PLACEMENTS") },
            new AdminModuleDto { Key = "settings", Label = "College Profile", Path = "/admin/settings", Icon = "Settings", RequiredFeature = "", IsEnabled = true }
        };

        return new CollegeCapabilitiesDto
        {
            TenantId = tenantId,
            TenantCode = tenantCode,
            CollegeName = collegeName,
            TemplateCode = templateCode,
            TemplateName = templateName,
            ActiveTheme = activeTheme,
            EnabledFeatures = enabledCodes.ToList(),
            NavigationModules = allModules
        };
    }
}
