using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Entities;

namespace CollegeWebsitePlatform.Application.Interfaces;

public interface ISiteService
{
    Task<SiteConfigDto> GetSiteConfigAsync();
    Task<ThemeDto?> GetSiteThemeAsync();
    Task<PageDto?> GetPageBySlugAsync(string slug, bool includeDraft = false);
    Task<MenuDto?> GetMenuByCodeAsync(string code);
    Task<IEnumerable<NewsDto>> GetPublishedNewsAsync(int limit = 10);
    Task<NewsDto?> GetNewsBySlugAsync(string slug);
    Task<IEnumerable<EventDto>> GetPublishedEventsAsync(int limit = 10);
    Task<EventDto?> GetEventByIdAsync(Guid id);
    Task<IEnumerable<BannerDto>> GetActiveBannersAsync();
    Task<IEnumerable<GalleryDto>> GetPublishedGalleriesAsync(int limit = 12);
    Task<GalleryDto?> GetGalleryBySlugAsync(string slug);
    Task<IEnumerable<DepartmentDto>> GetActiveDepartmentsAsync();
    Task<DepartmentDto?> GetDepartmentByCodeAsync(string code);
    Task<IEnumerable<CourseDto>> GetActiveCoursesAsync(Guid? departmentId = null);
    Task<CourseDto?> GetCourseByIdAsync(Guid id);
    Task<IEnumerable<FacultyDto>> GetFacultyDirectoryAsync(Guid? departmentId = null);
    Task<bool> IsFeatureEnabledAsync(string featureCode);
    Task<TenantSettingsDto?> GetContactSettingsAsync();
    Task<ContactSubmissionDto> SubmitContactFormAsync(CreateContactSubmissionRequest request);
    Task<bool> ValidatePreviewTokenAsync(string token);
}

public interface IPageService
{
    Task<PaginatedResponse<Page>> GetPagesPaginatedAsync(PaginationFilter filter);
    Task<IEnumerable<Page>> GetAllPagesAsync();
    Task<PageDto?> GetPageDetailsAsync(Guid id);
    Task<PageDto> CreatePageAsync(CreatePageRequest request, string currentUser);
    Task<PageDto> UpdatePageAsync(Guid id, UpdatePageRequest request, string currentUser);
    Task<bool> DeletePageAsync(Guid id);
    Task<bool> PublishPageAsync(Guid id, string currentUser);
    Task<bool> UnpublishPageAsync(Guid id, string currentUser);
    Task<PageDto?> GetPagePreviewAsync(Guid id);
    Task<IEnumerable<PageVersionDto>> GetVersionsAsync(Guid pageId);
    Task<PageVersionDto?> GetVersionDetailAsync(Guid pageId, int versionNumber);
    Task<PageDto?> RestoreVersionAsync(Guid pageId, int versionNumber, string currentUser);
    Task<PageEditorDto?> GetPageEditorAsync(Guid pageId);
    Task<PageDto> SavePageDraftAsync(Guid pageId, SavePageDraftRequest request, string currentUser);
    Task<CreatePreviewSessionResponse> CreatePreviewSessionAsync(Guid pageId, string currentUser);
}

public interface ISectionService
{
    Task<IEnumerable<PageSectionDto>> GetSectionsByPageIdAsync(Guid pageId);
    Task<PageSectionDto> CreateSectionAsync(Guid pageId, CreateSectionRequest request);
    Task<PageSectionDto> UpdateSectionAsync(Guid pageId, Guid sectionId, UpdateSectionRequest request);
    Task<bool> DeleteSectionAsync(Guid pageId, Guid sectionId);
    Task<bool> ReorderSectionsAsync(Guid pageId, List<Guid> sectionIds);
    Task<IEnumerable<PageSectionDto>> SeedDefaultSectionsAsync(Guid pageId);
    IEnumerable<SectionTypeDto> GetSectionTypes();
}

public interface IMenuService
{
    Task<IEnumerable<Menu>> GetAllMenusAsync();
    Task<MenuDto?> GetMenuDetailsAsync(Guid id);
    Task<MenuDto> CreateMenuAsync(CreateMenuRequest request);
    Task<bool> UpdateMenuAsync(Guid id, UpdateMenuRequest request);
    Task<bool> DeleteMenuAsync(Guid id);
    Task<IEnumerable<MenuItemDto>> GetMenuItemsAsync(Guid menuId);
    Task<MenuItemDto> AddMenuItemAsync(Guid menuId, CreateMenuItemRequest request);
    Task<bool> UpdateMenuItemAsync(Guid menuId, Guid itemId, UpdateMenuItemRequest request);
    Task<bool> SetMenuItemVisibilityAsync(Guid menuId, Guid itemId, bool isVisible);
    Task<bool> DeleteMenuItemAsync(Guid menuId, Guid itemId);
    Task<bool> ReorderMenuItemsAsync(Guid menuId, List<Guid> itemIds);
}

public interface IMarqueeService
{
    Task<MarqueeDto> GetMarqueeDataAsync(bool activeOnly = false);
    Task<MarqueeSettingsDto> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(UpdateMarqueeSettingsRequest request);
    Task<IEnumerable<MarqueeItemDto>> GetItemsAsync(bool activeOnly = false);
    Task<MarqueeItemDto?> GetItemByIdAsync(Guid id);
    Task<MarqueeItemDto> CreateItemAsync(CreateMarqueeItemRequest request);
    Task<bool> UpdateItemAsync(Guid id, UpdateMarqueeItemRequest request);
    Task<bool> SetItemActiveStatusAsync(Guid id, bool isActive);
    Task<bool> DeleteItemAsync(Guid id);
    Task<bool> ReorderItemsAsync(List<Guid> itemIds);
}

public interface INewsService
{
    Task<PaginatedResponse<News>> GetNewsPaginatedAsync(PaginationFilter filter);
    Task<News?> GetNewsByIdAsync(Guid id);
    Task<News> CreateNewsAsync(CreateNewsRequest request);
    Task<News> UpdateNewsAsync(Guid id, UpdateNewsRequest request);
    Task<bool> DeleteNewsAsync(Guid id);
    Task<bool> PublishNewsAsync(Guid id);
    Task<bool> UnpublishNewsAsync(Guid id);
}

public interface IEventService
{
    Task<PaginatedResponse<Event>> GetEventsPaginatedAsync(PaginationFilter filter);
    Task<Event?> GetEventByIdAsync(Guid id);
    Task<Event> CreateEventAsync(CreateEventRequest request);
    Task<Event> UpdateEventAsync(Guid id, UpdateEventRequest request);
    Task<bool> DeleteEventAsync(Guid id);
    Task<bool> PublishEventAsync(Guid id);
    Task<bool> UnpublishEventAsync(Guid id);
}

public interface IBannerService
{
    Task<IEnumerable<BannerDto>> GetAllBannersAsync();
    Task<BannerDto?> GetBannerByIdAsync(Guid id);
    Task<BannerDto> CreateBannerAsync(CreateBannerRequest request);
    Task<bool> UpdateBannerAsync(Guid id, UpdateBannerRequest request);
    Task<bool> DeleteBannerAsync(Guid id);
    Task<bool> ReorderBannersAsync(List<Guid> bannerIds);
}

public interface IGalleryService
{
    Task<PaginatedResponse<GalleryDto>> GetGalleriesPaginatedAsync(PaginationFilter filter);
    Task<GalleryDto?> GetGalleryByIdAsync(Guid id);
    Task<GalleryDto?> GetGalleryBySlugAsync(string slug);
    Task<GalleryDto> CreateGalleryAsync(CreateGalleryRequest request);
    Task<bool> UpdateGalleryAsync(Guid id, UpdateGalleryRequest request);
    Task<bool> DeleteGalleryAsync(Guid id);
    Task<GalleryImageDto> AddImageAsync(Guid galleryId, AddGalleryImageRequest request);
    Task<bool> DeleteImageAsync(Guid galleryId, Guid imageId);
}

public interface IDepartmentService
{
    Task<PaginatedResponse<DepartmentDto>> GetDepartmentsPaginatedAsync(PaginationFilter filter);
    Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id);
    Task<DepartmentDto?> GetDepartmentByCodeAsync(string code);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<bool> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request);
    Task<bool> DeleteDepartmentAsync(Guid id);
}

public interface ICourseService
{
    Task<PaginatedResponse<CourseDto>> GetCoursesPaginatedAsync(PaginationFilter filter, Guid? departmentId = null);
    Task<CourseDto?> GetCourseByIdAsync(Guid id);
    Task<CourseDto> CreateCourseAsync(CreateCourseRequest request);
    Task<bool> UpdateCourseAsync(Guid id, UpdateCourseRequest request);
    Task<bool> DeleteCourseAsync(Guid id);
}

public interface IFacultyService
{
    Task<PaginatedResponse<FacultyDto>> GetFacultyPaginatedAsync(PaginationFilter filter, Guid? departmentId = null);
    Task<FacultyDto?> GetFacultyByIdAsync(Guid id);
    Task<FacultyDto> CreateFacultyAsync(CreateFacultyRequest request);
    Task<bool> UpdateFacultyAsync(Guid id, UpdateFacultyRequest request);
    Task<bool> DeleteFacultyAsync(Guid id);
}

public interface INoticeService
{
    Task<PaginatedResponse<Notice>> GetNoticesPaginatedAsync(PaginationFilter filter);
    Task<Notice?> GetNoticeByIdAsync(Guid id);
    Task<NoticeDto?> GetNoticeBySlugAsync(string slug);
    Task<Notice> CreateNoticeAsync(CreateNoticeRequest request);
    Task<Notice> UpdateNoticeAsync(Guid id, UpdateNoticeRequest request);
    Task<bool> DeleteNoticeAsync(Guid id);
    Task<bool> PublishNoticeAsync(Guid id);
    Task<bool> UnpublishNoticeAsync(Guid id);
}

public interface IMediaService
{
    Task<PaginatedResponse<MediaDto>> GetMediaPaginatedAsync(MediaFilterRequest filter);
    Task<IEnumerable<MediaDto>> GetAllMediaAsync();
    Task<MediaDto?> GetMediaByIdAsync(Guid id);
    Task<MediaDto> UploadMediaAsync(Stream fileStream, string fileName, string contentType, long fileSizeBytes, string? altText, string currentUser);
    Task<bool> DeleteMediaAsync(Guid id);
}

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync();
}

public interface ISettingsService
{
    Task<TenantSettingsDto> GetSettingsAsync();
    Task<TenantSettingsDto> UpdateSettingsAsync(UpdateTenantSettingsRequest request);
}

public interface IContactService
{
    Task<PaginatedResponse<ContactSubmissionDto>> GetSubmissionsPaginatedAsync(PaginationFilter filter);
    Task<IEnumerable<ContactSubmissionDto>> GetAllSubmissionsAsync();
    Task<ContactSubmissionDto?> GetSubmissionByIdAsync(Guid id);
    Task<ContactSubmissionDto> CreateSubmissionAsync(CreateContactSubmissionRequest request);
    Task<bool> UpdateSubmissionStatusAsync(Guid id, string status);
    Task<bool> DeleteSubmissionAsync(Guid id);
}

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password);
    Task<RefreshTokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    /// <summary>Returns typed CurrentUserDto with permissions and tenant memberships for /auth/me.</summary>
    Task<CurrentUserDto?> GetCurrentUserProfileAsync(Guid userId);
    // Legacy kept for internal use
    Task<CollegeWebsitePlatform.Domain.Entities.User?> GetCurrentUserAsync(Guid userId);
}

public interface ISuperAdminService
{
    Task<SuperAdminDashboardStatsDto> GetDashboardStatsAsync();

    // Colleges
    Task<IEnumerable<TenantDto>> GetCollegesAsync();
    Task<CollegeDetailsDto?> GetCollegeDetailsAsync(Guid id);
    Task<TenantSettingsDto> GetCollegeSettingsAsync(Guid tenantId);
    Task<TenantSettingsDto> UpdateCollegeSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request, string currentAdminUser);
    Task<TenantDto> CreateCollegeWorkflowAsync(CreateCollegeWorkflowRequest request, string currentAdminUser);
    Task<TenantDto> UpdateCollegeAsync(Guid id, UpdateCollegeRequest request, string currentAdminUser);
    Task<bool> ActivateCollegeAsync(Guid id, string currentAdminUser);
    Task<bool> SuspendCollegeAsync(Guid id, string currentAdminUser);
    Task<bool> DeleteCollegeAsync(Guid id, bool dropDatabase, string currentAdminUser);

    // Domains
    Task<IEnumerable<DomainDto>> GetDomainsAsync();
    Task<DomainDto> CreateDomainAsync(CreateDomainRequest request, string currentAdminUser);
    Task<bool> UpdateDomainAsync(Guid id, UpdateDomainRequest request, string currentAdminUser);
    Task<bool> DeleteDomainAsync(Guid domainId, string currentAdminUser);
    Task<bool> VerifyDomainAsync(Guid domainId, string currentAdminUser);
    Task<bool> SetPrimaryDomainAsync(Guid domainId, string currentAdminUser);

    // Databases & Migrations
    Task<IEnumerable<DatabaseRegistryDto>> GetDatabasesAsync();
    Task<DatabaseRegistryDto?> GetDatabaseByTenantIdAsync(Guid tenantId);
    Task<DatabaseRegistryDto> ProvisionDatabaseAsync(Guid tenantId, string currentAdminUser);
    Task<DatabaseRegistryDto> VerifyDatabaseAsync(Guid tenantId, string currentAdminUser);
    Task<List<MigrationHistoryDto>> GetMigrationsAsync();
    Task<MigrationStatusDto> GetMigrationStatusForTenantAsync(Guid tenantId);
    Task<DatabaseRegistryDto> MigrateTenantDatabaseAsync(Guid tenantId, string currentAdminUser);

    // Themes & Templates
    Task<IEnumerable<TemplateDto>> GetTemplatesAsync();
    Task<TemplateDto?> GetTemplateByIdAsync(Guid id);
    Task<TemplateDto> CreateTemplateAsync(CreateTemplateRequest request, string currentAdminUser);
    Task<TemplateDto> UpdateTemplateAsync(Guid id, UpdateTemplateRequest request, string currentAdminUser);
    Task<bool> DeleteTemplateAsync(Guid id, string currentAdminUser);
    Task<TemplatePreviewDto?> GetTemplatePreviewAsync(string templateCode);
    Task<bool> AssignTemplateAsync(Guid tenantId, string templateCode, string currentAdminUser);

    Task<IEnumerable<ThemeDto>> GetThemesAsync();
    Task<ThemeDto?> GetThemeByIdAsync(Guid id);
    Task<ThemeDto> CreateThemeAsync(CreateThemeRequest request, string currentAdminUser);
    Task<bool> UpdateThemeAsync(Guid id, UpdateThemeRequest request, string currentAdminUser);
    Task<bool> DeleteThemeAsync(Guid id, string currentAdminUser);
    Task<bool> AssignThemeAsync(Guid tenantId, Guid themeId, string currentAdminUser);

    // Features
    Task<IEnumerable<FeatureDto>> GetPlatformFeaturesAsync();
    Task<FeatureSummaryDto> GetFeatureSummaryAsync();
    Task<FeatureDto> CreateFeatureAsync(CreateFeatureRequest request, string currentAdminUser);
    Task<FeatureDto> UpdateFeatureAsync(Guid id, UpdateFeatureRequest request, string currentAdminUser);
    Task<bool> ToggleFeatureStatusAsync(Guid id, string currentAdminUser);
    Task<FeatureCollegeUsageDto> GetFeatureCollegeUsageAsync(string featureCode);
    Task<List<TenantFeatureDto>> GetTenantFeaturesAsync(Guid tenantId);
    Task<bool> UpdateTenantFeaturesAsync(Guid tenantId, Dictionary<string, bool> features, string currentAdminUser);

    // Roles & Permissions
    Task<IEnumerable<RoleDto>> GetRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(Guid id);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, string currentAdminUser);
    Task<bool> UpdateRoleAsync(Guid id, UpdateRoleRequest request, string currentAdminUser);
    Task<bool> UpdateRolePermissionsAsync(Guid roleId, List<Guid> permissionIds, string currentAdminUser);
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync();

    // Users & User-Tenants
    Task<IEnumerable<UserDto>> GetAdminUsersAsync();
    Task<UserDto> CreateAdminUserAsync(CreateAdminUserRequest request, string currentAdminUser);
    Task<bool> UpdateAdminUserAsync(Guid id, UpdateAdminUserRequest request, string currentAdminUser);
    Task<IEnumerable<UserTenantDto>> GetUserTenantsAsync(Guid userId);
    Task<bool> AssignUserTenantsAsync(Guid userId, AssignUserTenantsRequest request, string currentAdminUser);

    // Audit Logs
    Task<PaginatedResponse<AuditLogDto>> GetFilteredAuditLogsAsync(AuditLogFilterRequest filter);
}

public interface IAuditService
{
    Task LogActionAsync(string username, string action, string entityType, string? entityId = null, string? details = null, Guid? tenantId = null, string? ipAddress = null);
}

public interface ICapabilityService
{
    Task<CollegeCapabilitiesDto> GetCapabilitiesAsync(Guid tenantId);
}
