using System.ComponentModel.DataAnnotations;

namespace CollegeWebsitePlatform.Application.DTOs;

// -------------------------------------------------------------
// Core Pagination & Common Models
// -------------------------------------------------------------

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; } = 0;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
}

public class PaginationFilter
{
    [Range(1, int.MaxValue, ErrorMessage = "Page must be >= 1")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    [MaxLength(200)]
    public string? Search { get; set; }

    [MaxLength(50)]
    public string? SortBy { get; set; }

    public string? SortDirection { get; set; } = "ASC";
}

// -------------------------------------------------------------
// Authentication DTOs
// -------------------------------------------------------------

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Typed response for GET /api/v1/auth/me — replaces the previous generic ObjectApiResponse.
/// Includes the user's full permission list and all tenant memberships.
/// </summary>
public class CurrentUserDto
{
    public Guid   Id          { get; set; }
    public string Username    { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string FullName    { get; set; } = string.Empty;
    public string Role        { get; set; } = string.Empty;
    public bool   IsActive    { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<TenantMembershipDto> Tenants { get; set; } = [];
}

public class TenantMembershipDto
{
    public Guid   TenantId    { get; set; }
    public string TenantCode  { get; set; } = string.Empty;
    public string TenantName  { get; set; } = string.Empty;
    public string Domain      { get; set; } = string.Empty;
    public bool   IsPrimary   { get; set; }
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// -------------------------------------------------------------
// Platform SuperAdmin: Tenants & Colleges
// -------------------------------------------------------------

public class TenantDto
{
    public Guid Id { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string ProvisioningStatus { get; set; } = "COMPLETED";
    public Guid? DefaultThemeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PrimaryDomain { get; set; }
    public string? DatabaseName { get; set; }
    public string? ThemeName { get; set; }
    public string? AdminUsername { get; set; }
    public string TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public string? LoginTemplate { get; set; }
}

public class TenantSummaryDto
{
    public Guid Id { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public string ProvisioningStatus { get; set; } = "COMPLETED";
    public string? PrimaryDomain { get; set; }
    public string? DatabaseName { get; set; }
    public string? ThemeName { get; set; }
    public string? AdminUsername { get; set; }
    public string TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public DateTime CreatedAt { get; set; }
}

public class CollegeDetailsDto
{
    public TenantDto Tenant { get; set; } = new();
    public List<DomainDto> Domains { get; set; } = [];
    public DatabaseRegistryDto? Database { get; set; }
    public ThemeDto? Theme { get; set; }
    public List<TenantFeatureDto> Features { get; set; } = [];
    public List<UserDto> Admins { get; set; } = [];
    public List<AuditLogDto> AuditLogs { get; set; } = [];
}

public class CreateCollegeWorkflowRequest
{
    public string TenantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string PrimaryDomain { get; set; } = string.Empty;
    public string? DatabaseName { get; set; }
    public Guid? DefaultThemeId { get; set; }
    public string? TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public string? LoginTemplate { get; set; } = "template2";
    public List<string>? EnabledFeatures { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = "Admin@123";
}

public class TemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DefaultThemeId { get; set; }
    public string? DefaultThemeCode { get; set; }
    public string? DefaultThemeName { get; set; }
    public object? ThemeConfiguration { get; set; }
    public List<Guid> SupportedThemes { get; set; } = [];
    public List<string> RecommendedFeatures { get; set; } = [];
    public object? StarterPages { get; set; }
    public object? StarterMenus { get; set; }
    public bool IsActive { get; set; }
    public int CollegesCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTemplateRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "ENGINEERING";

    public string? Description { get; set; }
    public Guid? DefaultThemeId { get; set; }
    public List<string> RecommendedFeatures { get; set; } = [];
    public List<Guid> SupportedThemes { get; set; } = [];
    public object StarterPages { get; set; } = new List<object>();
    public object StarterMenus { get; set; } = new List<object>();
    public bool IsActive { get; set; } = true;
}

public class UpdateTemplateRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = "ENGINEERING";

    public string? Description { get; set; }
    public Guid? DefaultThemeId { get; set; }
    public List<string> RecommendedFeatures { get; set; } = [];
    public List<Guid> SupportedThemes { get; set; } = [];
    public object StarterPages { get; set; } = new List<object>();
    public object StarterMenus { get; set; } = new List<object>();
    public bool IsActive { get; set; } = true;
}

public class AssignTemplateRequest
{
    [Required]
    public string TemplateCode { get; set; } = string.Empty;
}

public class TemplatePreviewDto
{
    public TemplateDto Template { get; set; } = new();
    public ThemeDto? DefaultTheme { get; set; }
    public object? Blueprint { get; set; }
}

public class UpdateCollegeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? DefaultThemeId { get; set; }
    public string? LoginTemplate { get; set; }
    public string? TemplateCode { get; set; }
    public string? AdminPassword { get; set; }
}

// -------------------------------------------------------------
// Domains
// -------------------------------------------------------------

public class DomainDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string DomainType { get; set; } = "PRIMARY";
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDomainRequest
{
    [Required]
    public Guid TenantId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = string.Empty;

    public string DomainType { get; set; } = "PRIMARY";
    public bool IsPrimary { get; set; } = false;
}

public class UpdateDomainRequest
{
    public string Domain { get; set; } = string.Empty;
    public string DomainType { get; set; } = "PRIMARY";
}

// -------------------------------------------------------------
// Placement Companies
// -------------------------------------------------------------

public class PlacementCompanyDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyLogoUrl { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CreatePlacementCompanyRequest
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    public string CompanyLogoUrl { get; set; } = string.Empty;

    public string? CompanyWebsite { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdatePlacementCompanyRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyLogoUrl { get; set; } = string.Empty;
    public string? CompanyWebsite { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

// -------------------------------------------------------------
// Databases & Migrations
// -------------------------------------------------------------

public class DatabaseRegistryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = "HEALTHY";
    public string CurrentMigrationVersion { get; set; } = "V8";
    public DateTime LastHealthCheckAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MigrationHistoryDto
{
    public string Version { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "SUCCESS";
    public DateTime AppliedAt { get; set; }
}

public class MigrationStatusDto
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = "V8";
    public string Status { get; set; } = "HEALTHY";
    public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    public List<MigrationHistoryDto> AppliedMigrations { get; set; } = [];
}

// -------------------------------------------------------------
// Themes
// -------------------------------------------------------------

public class ThemeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public object? Configuration { get; set; }
    public bool IsActive { get; set; }
}

public class CreateThemeRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public object Configuration { get; set; } = new();
}

public class UpdateThemeRequest
{
    public string Name { get; set; } = string.Empty;
    public object Configuration { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class AssignThemeRequest
{
    public Guid ThemeId { get; set; }
}

// -------------------------------------------------------------
// Features
// -------------------------------------------------------------

public class FeatureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "CMS";
    public string Icon { get; set; } = "cube";
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public int CollegesUsingCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FeatureSummaryDto
{
    public int TotalFeatures { get; set; }
    public int ActiveFeatures { get; set; }
    public int DisabledFeatures { get; set; }
    public int CategoriesCount { get; set; }
    public int TotalColleges { get; set; }
    public int TotalAssignments { get; set; }
}

public class CreateFeatureRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "CMS";

    [MaxLength(50)]
    public string Icon { get; set; } = "cube";

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class UpdateFeatureRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = "CMS";

    [MaxLength(50)]
    public string Icon { get; set; } = "cube";

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class FeatureCollegeUsageDto
{
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public List<CollegeFeatureStatusDto> Colleges { get; set; } = [];
}

public class CollegeFeatureStatusDto
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string PrimaryDomain { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class TenantFeatureDto
{
    public string FeatureCode { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class UpdateTenantFeaturesRequest
{
    public Dictionary<string, bool> Features { get; set; } = [];
}

// -------------------------------------------------------------
// Roles & Permissions
// -------------------------------------------------------------

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<PermissionDto> Permissions { get; set; } = [];
}

public class CreateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
    public List<Guid>? PermissionIds { get; set; }
}

public class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateRolePermissionsRequest
{
    public List<Guid> PermissionIds { get; set; } = [];
}

// -------------------------------------------------------------
// Users & User-Tenants
// -------------------------------------------------------------

public class UserDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "CollegeAdmin";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAdminUserRequest
{
    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "CollegeAdmin";
}

public class UpdateAdminUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ResetAdminUserPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

// -------------------------------------------------------------
// Platform Branding (SuperAdmin Settings)
// -------------------------------------------------------------

public class PlatformBrandingDto
{
    public string PlatformName { get; set; } = "Multi Website";
    public string PlatformSubtitle { get; set; } = "SuperAdmin Control Plane";
    public string LogoUrl { get; set; } = "/assets/superadmin-logo.png";
    public string FaviconUrl { get; set; } = "/assets/superadmin-logo.png";
    public DateTime? UpdatedAt { get; set; }
}

public class UpdatePlatformBrandingRequest
{
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformSubtitle { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string FaviconUrl { get; set; } = string.Empty;
}

public class UserTenantDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignUserTenantsRequest
{
    public List<Guid> TenantIds { get; set; } = [];
    public Guid? RoleId { get; set; }
}

// -------------------------------------------------------------
// Audit Logs
// -------------------------------------------------------------

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Username { get; set; } = "system";
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterRequest : PaginationFilter
{
    public Guid? TenantId { get; set; }
    public string? Username { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    // NOTE: free-text 'Search' and 'SortBy' are inherited from PaginationFilter.
    /// <summary>Inclusive lower bound (UTC) on created_at.</summary>
    public DateTime? FromDate { get; set; }
    /// <summary>Inclusive upper bound (UTC) on created_at.</summary>
    public DateTime? ToDate { get; set; }
}

public class SuperAdminDashboardStatsDto
{
    public int TotalColleges { get; set; }
    public int ActiveColleges { get; set; }
    public int SuspendedColleges { get; set; }
    public int TotalDomains { get; set; }
    public int TotalDatabases { get; set; }
    public int TotalCollegeAdmins { get; set; }
    public List<TenantSummaryDto> RecentColleges { get; set; } = [];
    public List<AuditLogDto> RecentActivity { get; set; } = [];
}

// -------------------------------------------------------------
// CMS: Pages, Sections, Versions
// -------------------------------------------------------------

public class PageDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public List<PageSectionDto> Sections { get; set; } = [];
}

public class CreatePageRequest
{
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    public bool? IsPublished { get; set; }
}

public class UpdatePageRequest
{
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    public bool? IsPublished { get; set; }
}

public class PageVersionDto
{
    public Guid Id { get; set; }
    public Guid PageId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public object? ContentJson { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PageSectionDto
{
    public Guid Id { get; set; }
    public Guid PageId { get; set; }
    public string SectionType { get; set; } = "HERO";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public object? Content { get; set; }
    public object? Settings { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
}

public class CreateSectionRequest
{
    [Required]
    public string SectionType { get; set; } = "HERO";

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Subtitle { get; set; }

    public object Content { get; set; } = new();
    public object? Settings { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
}

public class UpdateSectionRequest
{
    public string SectionType { get; set; } = "HERO";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public object Content { get; set; } = new();
    public object? Settings { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
}

public class SectionTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ReorderSectionsRequest
{
    public List<Guid> SectionIds { get; set; } = [];
}

public class PageEditorDto
{
    public PageDto Page { get; set; } = new();
    public List<PageSectionDto> Sections { get; set; } = [];
    public List<PageVersionDto> Versions { get; set; } = [];
    public List<SectionTypeDto> AvailableSectionTypes { get; set; } = [];
}

public class SavePageDraftRequest
{
    public string Title { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public List<DraftSectionItem> Sections { get; set; } = [];
}

public class DraftSectionItem
{
    public Guid? Id { get; set; }
    public string SectionType { get; set; } = "HERO";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public object Content { get; set; } = new();
    public object? Settings { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class CreatePreviewSessionRequest
{
    public Guid PageId { get; set; }
}

public class CreatePreviewSessionResponse
{
    public string PreviewToken { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CollegeCapabilitiesDto
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public string CollegeName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public ThemeDto? ActiveTheme { get; set; }
    public List<string> EnabledFeatures { get; set; } = [];
    public List<AdminModuleDto> NavigationModules { get; set; } = [];
}

public class AdminModuleDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string RequiredFeature { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

// -------------------------------------------------------------
// CMS: Menus
// -------------------------------------------------------------

public class MenuDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Name => Title;
    public string Location => Code;
    public List<MenuItemDto> Items { get; set; } = [];
}

public class CreateMenuRequest
{
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
}

public class UpdateMenuRequest
{
    public string Title { get; set; } = string.Empty;
}

public class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuId { get; set; }
    public Guid? ParentId { get; set; }
    public string ItemType { get; set; } = "PAGE"; // PAGE, EXTERNAL_URL, INTERNAL_URL, GROUP
    public string TargetType { get; set; } = "DYNAMIC_MODULE"; // INTERNAL_PAGE, DYNAMIC_MODULE, SECTION_ANCHOR, EXTERNAL_LINK, FILE_DOWNLOAD
    public Guid? PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool OpenInNewTab { get; set; } = false;
    public string? Icon { get; set; } = "FileText";
    public List<MenuItemDto> Children { get; set; } = [];
}

public class CreateMenuItemRequest
{
    public Guid? ParentId { get; set; }
    public string ItemType { get; set; } = "PAGE";
    public string TargetType { get; set; } = "DYNAMIC_MODULE";
    public Guid? PageId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Url { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public bool OpenInNewTab { get; set; } = false;
    public string? Icon { get; set; } = "FileText";
}

public class UpdateMenuItemRequest
{
    public Guid? ParentId { get; set; }
    public string ItemType { get; set; } = "PAGE";
    public string TargetType { get; set; } = "DYNAMIC_MODULE";
    public Guid? PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }
    public bool OpenInNewTab { get; set; } = false;
    public string? Icon { get; set; } = "FileText";
}

public class ReorderMenuItemsRequest
{
    public List<Guid> ItemIds { get; set; } = [];
}

// -------------------------------------------------------------
// CMS: Top Announcement Marquee Bar
// -------------------------------------------------------------

public class MarqueeSettingsDto
{
    public Guid Id { get; set; }
    public bool IsActive { get; set; } = true;
    public string Speed { get; set; } = "medium"; // slow, medium, fast
    public bool PauseOnHover { get; set; } = true;
    public string BgColor { get; set; } = "#0f172a";
    public string TextColor { get; set; } = "#f8fafc";
    public string BadgeBgColor { get; set; } = "#f59e0b";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UpdateMarqueeSettingsRequest
{
    public bool IsActive { get; set; } = true;
    public string Speed { get; set; } = "medium";
    public bool PauseOnHover { get; set; } = true;
    public string BgColor { get; set; } = "#0f172a";
    public string TextColor { get; set; } = "#f8fafc";
    public string BadgeBgColor { get; set; } = "#f59e0b";
}

public class MarqueeItemDto
{
    public Guid Id { get; set; }
    public string? BadgeText { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMarqueeItemRequest
{
    [MaxLength(50)]
    public string? BadgeText { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateMarqueeItemRequest
{
    [MaxLength(50)]
    public string? BadgeText { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class ReorderMarqueeItemsRequest
{
    public List<Guid> ItemIds { get; set; } = [];
}

public class MarqueeDto
{
    public MarqueeSettingsDto Settings { get; set; } = new();
    public List<MarqueeItemDto> Items { get; set; } = [];
}

// -------------------------------------------------------------
// CMS: News & Events
// -------------------------------------------------------------

public class NewsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime PublishedDate { get; set; }
    public bool IsPublished { get; set; }
}

public class CreateNewsRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }
}

public class UpdateNewsRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }
}

public class EventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; }
}

public class CreateEventRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required]
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }
}

public class UpdateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

// -------------------------------------------------------------
// CMS: Banners
// -------------------------------------------------------------

public class BannerDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public string? Badge { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public List<string> Pillars { get; set; } = [];
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBannerRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Subtitle { get; set; }

    [Required]
    [MaxLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? CtaText { get; set; }

    [MaxLength(500)]
    public string? CtaUrl { get; set; }

    [MaxLength(150)]
    public string? Badge { get; set; }

    [MaxLength(100)]
    public string? SecondaryCtaText { get; set; }

    [MaxLength(500)]
    public string? SecondaryCtaUrl { get; set; }
    public List<string>? Pillars { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateBannerRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public string? Badge { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public List<string>? Pillars { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReorderBannersRequest
{
    public List<Guid> BannerIds { get; set; } = [];
}

// -------------------------------------------------------------
// CMS: Galleries
// -------------------------------------------------------------

public class GalleryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string Category { get; set; } = "Campus";
    public bool IsPublished { get; set; }
    public List<GalleryImageDto> Images { get; set; } = [];
}

public class CreateGalleryRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(2000)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; } = "Campus";
}

public class UpdateGalleryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Category { get; set; }
}

public class GalleryImageDto
{
    public Guid Id { get; set; }
    public Guid GalleryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}

public class AddGalleryImageRequest
{
    [Required]
    [MaxLength(2000)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Caption { get; set; }
    public int SortOrder { get; set; } = 0;
}

// -------------------------------------------------------------
// CMS: Departments & Courses
// -------------------------------------------------------------

public class LaboratoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Equipment { get; set; }
    public string? ImageUrl { get; set; }
}

public class SemesterSyllabusItemDto
{
    public int SemesterNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public string? WordUrl { get; set; }
    public string? RegulationYear { get; set; } = "2024";
    public int? Credits { get; set; }
}

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HeadOfDepartment { get; set; }
    public string? HodMessage { get; set; }
    public string? HodPhotoUrl { get; set; }
    public string? VisionMission { get; set; }
    public string Stream { get; set; } = "Engineering";
    public int TotalSemesters { get; set; } = 8;
    public int LabCount { get; set; } = 0;
    public int StudentCount { get; set; } = 0;
    public string LaboratoriesJson { get; set; } = "[]";
    public List<LaboratoryItemDto> Laboratories { get; set; } = [];
    public string SyllabiJson { get; set; } = "[]";
    public List<DepartmentSyllabusItemDto> Syllabi { get; set; } = [];
    public string? SyllabusPdfUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public List<CourseDto> Courses { get; set; } = [];
    public List<FacultyDto> Faculty { get; set; } = [];
}

public class DepartmentSyllabusItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? RegulationYear { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? Description { get; set; }
}

public class CreateDepartmentRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public string? Slug { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? HeadOfDepartment { get; set; }

    public string? HodMessage { get; set; }
    public string? HodPhotoUrl { get; set; }
    public string? VisionMission { get; set; }
    public string Stream { get; set; } = "Engineering";
    public int TotalSemesters { get; set; } = 8;
    public int LabCount { get; set; } = 0;
    public int StudentCount { get; set; } = 0;
    public string LaboratoriesJson { get; set; } = "[]";
    public string SyllabiJson { get; set; } = "[]";
    public string? SyllabusPdfUrl { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    [MaxLength(2000)]
    public string? ImageUrl { get; set; }
}

public class UpdateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? HeadOfDepartment { get; set; }
    public string? HodMessage { get; set; }
    public string? HodPhotoUrl { get; set; }
    public string? VisionMission { get; set; }
    public string Stream { get; set; } = "Engineering";
    public int TotalSemesters { get; set; } = 8;
    public int LabCount { get; set; } = 0;
    public int StudentCount { get; set; } = 0;
    public string LaboratoriesJson { get; set; } = "[]";
    public string SyllabiJson { get; set; } = "[]";
    public string? SyllabusPdfUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CourseDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DegreeLevel { get; set; } = "UNDERGRADUATE";
    public decimal DurationYears { get; set; } = 3.0m;
    public int TotalSemesters { get; set; } = 8;
    public string RegulationYear { get; set; } = "2024";
    public int Intake { get; set; } = 60;
    public string SemesterSyllabiJson { get; set; } = "[]";
    public List<SemesterSyllabusItemDto> SemesterSyllabi { get; set; } = [];
    public string CurriculumHighlightsJson { get; set; } = "[]";
    public List<string> CurriculumHighlights { get; set; } = [];
    public string? Description { get; set; }
    public string? Eligibility { get; set; }
    public string? FeeStructure { get; set; }
    public string? TuitionFee { get; set; }
    public string? SyllabusPdfUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCourseRequest
{
    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    public string? Slug { get; set; }
    public string DegreeLevel { get; set; } = "UNDERGRADUATE";
    public decimal DurationYears { get; set; } = 3.0m;
    public int TotalSemesters { get; set; } = 8;
    public string RegulationYear { get; set; } = "2024";
    public int Intake { get; set; } = 60;
    public string SemesterSyllabiJson { get; set; } = "[]";
    public string CurriculumHighlightsJson { get; set; } = "[]";

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(1000)]
    public string? Eligibility { get; set; }

    public string? FeeStructure { get; set; }
    public string? TuitionFee { get; set; }
    public string? SyllabusPdfUrl { get; set; }
}

public class UpdateCourseRequest
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string DegreeLevel { get; set; } = "UNDERGRADUATE";
    public decimal DurationYears { get; set; } = 3.0m;
    public int TotalSemesters { get; set; } = 8;
    public string RegulationYear { get; set; } = "2024";
    public int Intake { get; set; } = 60;
    public string SemesterSyllabiJson { get; set; } = "[]";
    public string CurriculumHighlightsJson { get; set; } = "[]";
    public string? Description { get; set; }
    public string? Eligibility { get; set; }
    public string? FeeStructure { get; set; }
    public string? TuitionFee { get; set; }
    public string? SyllabusPdfUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

// -------------------------------------------------------------
// Placements, Compliance, Transport, Hostels, Library & Alumni DTOs
// -------------------------------------------------------------


public class ComplianceDocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "AICTE";
    public string DocumentPdfUrl { get; set; } = string.Empty;
    public string? FileSize { get; set; }
    public string? AcademicYear { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class TransportRouteDto
{
    public Guid Id { get; set; }
    public string RouteNumber { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string StartingPoint { get; set; } = string.Empty;
    public string? StartPoint { get => StartingPoint; set => StartingPoint = value ?? string.Empty; }
    public string Destination { get; set; } = "Campus";
    public string? EndPoint { get => Destination; set => Destination = value ?? "Campus"; }
    public string DepartureTime { get; set; } = string.Empty;
    public string? MorningPickupTime { get => DepartureTime; set => DepartureTime = value ?? string.Empty; }
    public string? EveningDropTime { get; set; }
    public string StopsJson { get; set; } = "[]";
    public List<string> ViaStops { get; set; } = [];
    public string? BusNumber { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? RouteMapPdfUrl { get; set; }
    public string? TimingPdfUrl { get => RouteMapPdfUrl; set => RouteMapPdfUrl = value; }
    public bool IsActive { get; set; }
}

public class HostelFacilityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GenderType { get; set; } = "BOYS";
    public string? Type { get => GenderType; set => GenderType = value ?? "BOYS"; }
    public int Capacity { get; set; }
    public int TotalRooms { get; set; } = 50;
    public string RoomTypesJson { get; set; } = "[]";
    public string? WardenName { get; set; }
    public string? WardenPhone { get; set; }
    public string? WardenEmail { get; set; }
    public string? MessMenuPdfUrl { get; set; }
    public string? RulesPdfUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? PhotoUrl { get => ImageUrl; set => ImageUrl = value; }
    public string? Description { get; set; }
    public List<string> Amenities { get; set; } = [];
    public bool IsActive { get; set; }
}

public class LibraryResourceDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "QUESTION_PAPER";
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get => DepartmentCode; set => DepartmentCode = value; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public int? Semester { get; set; }
    public string? AcademicYear { get; set; }
    public string? ExternalUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? FileUrl { get => PdfUrl; set => PdfUrl = value; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class AlumniMemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string BatchYear { get; set; } = string.Empty;
    public string? GraduationYear { get => BatchYear; set => BatchYear = value ?? string.Empty; }
    public string DegreeCourse { get; set; } = string.Empty;
    public string? Degree { get => DegreeCourse; set => DegreeCourse = value ?? string.Empty; }
    public string? CurrentCompany { get; set; }
    public string? CurrentDesignation { get; set; }
    public string? CurrentCity { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TestimonialQuote { get; set; }
    public string? Testimonial { get => TestimonialQuote; set => TestimonialQuote = value; }
    public string? PhotoUrl { get; set; }
    public string? LinkedinUrl { get; set; }
    public bool IsDistinguished { get; set; }
    public bool IsFeatured { get => IsDistinguished; set => IsDistinguished = value; }
    public string Status { get; set; } = "APPROVED";
    public bool IsApproved { get => Status == "APPROVED"; set => Status = value ? "APPROVED" : "PENDING"; }
}

// -------------------------------------------------------------
// CMS: Faculty Directory
// -------------------------------------------------------------

public class FacultyDto
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string? QualificationDetails { get; set; }
    public string? Specialization { get; set; }
    public string? ResearchInterests { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? OfficeRoom { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateFacultyRequest
{
    [Required]
    public Guid DepartmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Designation { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Qualification { get; set; } = string.Empty;

    public string? QualificationDetails { get; set; }

    [MaxLength(500)]
    public string? Specialization { get; set; }

    public string? ResearchInterests { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? OfficeRoom { get; set; }

    [MaxLength(2000)]
    public string? ProfileImageUrl { get; set; }

    [MaxLength(2000)]
    public string? Bio { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateFacultyRequest
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string? QualificationDetails { get; set; }
    public string? Specialization { get; set; }
    public string? ResearchInterests { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? OfficeRoom { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// -------------------------------------------------------------
// CMS: Media Library
// -------------------------------------------------------------

public class MediaDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AltText { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MediaFilterRequest : PaginationFilter
{
    public string? ContentType { get; set; }
}

// -------------------------------------------------------------
// CMS: Tenant Settings, Contact & Admission Inquiries
// -------------------------------------------------------------

public class TenantSettingsDto
{
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public object? SocialLinks { get; set; }
    public string? LoginTemplate { get; set; }
    public string? LoginBgImageUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateTenantSettingsRequest
{
    public string SiteName { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public object? SocialLinks { get; set; }
    public string? LoginBgImageUrl { get; set; }
}

public class AdmissionInquiryDto
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid? ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
}

public class CreateAdmissionInquiryRequest
{
    [Required]
    [MaxLength(150)]
    public string StudentName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    public Guid? ProgramId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ProgramName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateInquiryStatusRequest
{
    [Required]
    public string Status { get; set; } = "CONTACTED";
}

public class ContactSubmissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "NEW";
    public DateTime CreatedAt { get; set; }
}

public class CreateContactSubmissionRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Message { get; set; } = string.Empty;
}

public class UpdateContactStatusRequest
{
    public string Status { get; set; } = "REVIEWED";
}

// -------------------------------------------------------------
// Public Site Config
// -------------------------------------------------------------
// CMS: Notices
// -------------------------------------------------------------

public class NoticeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public string Category { get; set; } = "GENERAL";
    public DateTime PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsPublished { get; set; }
    public bool IsImportant { get; set; }
}

public class CreateNoticeRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? PdfUrl { get; set; }

    [MaxLength(50)]
    public string Category { get; set; } = "GENERAL";

    public DateTime? ExpiryDate { get; set; }
    public bool IsImportant { get; set; } = false;
}

public class UpdateNoticeRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? PdfUrl { get; set; }

    [MaxLength(50)]
    public string Category { get; set; } = "GENERAL";

    public DateTime? ExpiryDate { get; set; }
    public bool IsImportant { get; set; } = false;
}

// -------------------------------------------------------------
// Admin Dashboard
// -------------------------------------------------------------

public class AdminDashboardDto
{
    public int TotalPages { get; set; }
    public int PublishedPages { get; set; }
    public int DraftPages { get; set; }
    public int TotalNews { get; set; }
    public int PublishedNews { get; set; }
    public int TotalEvents { get; set; }
    public int PublishedEvents { get; set; }
    public int TotalNotices { get; set; }
    public int PublishedNotices { get; set; }
    public int TotalGalleries { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalCourses { get; set; }
    public int TotalFaculty { get; set; }
    public int TotalMediaFiles { get; set; }
    public int NewContactSubmissions { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// -------------------------------------------------------------
// Public Site Config
// -------------------------------------------------------------

public class SiteConfigDto
{
    public TenantDto Tenant { get; set; } = new();
    public ThemeDto? Theme { get; set; }
    public string TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public List<MenuItemDto> PrimaryMenu { get; set; } = [];
    public List<string> EnabledFeatures { get; set; } = [];
    public TenantSettingsDto? Settings { get; set; }
    public List<BannerDto> Banners { get; set; } = [];
    public MarqueeDto? Marquee { get; set; }
    public List<StatItemDto> Stats { get; set; } = [];
    public StatsSectionSettingsDto? StatsSettings { get; set; }
    public QuoteDto? Quote { get; set; }
}

public class StatsSectionSettingsDto
{
    public Guid Id { get; set; }
    public string Badge { get; set; } = "Key Metrics & Institutional Record";
    public string Title { get; set; } = "Campus at a Glance";
    public string Subtitle { get; set; } = "Excellence in Numbers";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class UpdateStatsSectionSettingsRequest
{
    [MaxLength(150)]
    public string? Badge { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    [MaxLength(500)]
    public string? Subtitle { get; set; }

    public bool IsActive { get; set; } = true;
}

public class StatItemDto
{
    public Guid Id { get; set; }
    public string Metric { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Icon { get; set; } = "GraduationCap";
    public string? IconName { get; set; } = "GraduationCap";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStatItemRequest
{
    [MaxLength(50)]
    public string? Metric { get; set; }

    [MaxLength(50)]
    public string? Value { get; set; }

    [MaxLength(20)]
    public string? Prefix { get; set; }

    [MaxLength(20)]
    public string? Suffix { get; set; }

    [Required]
    [MaxLength(150)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Icon { get; set; }

    [MaxLength(50)]
    public string? IconName { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class QuoteDto
{
    public Guid Id { get; set; }
    public string QuoteText { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string AuthorTitle => Designation;
    public string? AuthorImageUrl { get; set; }
    public string? AuthorImage => AuthorImageUrl;
    public string? SubText { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SaveQuoteRequest
{
    public string QuoteText { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? AuthorTitle { get; set; }
    public string? AuthorImageUrl { get; set; }
    public string? AuthorImage { get; set; }
    public string? SubText { get; set; }
    public bool IsActive { get; set; } = true;

    public string GetResolvedDesignation() =>
        !string.IsNullOrWhiteSpace(Designation) ? Designation.Trim() : (AuthorTitle?.Trim() ?? string.Empty);
    public string? GetResolvedImageUrl() =>
        !string.IsNullOrWhiteSpace(AuthorImageUrl) ? AuthorImageUrl.Trim() : AuthorImage?.Trim();
}

// -------------------------------------------------------------
// About Module DTOs
// -------------------------------------------------------------

/// <summary>Represents an accreditation or ranking entry in the About page.</summary>
public class AccreditationItemDto
{
    public string Body { get; set; } = string.Empty;       // e.g. NAAC, AICTE, NBA
    public string? Grade { get; set; }                     // e.g. A++, A+
    public string? Year { get; set; }                      // e.g. 2023
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
}

/// <summary>Represents a key achievement or milestone.</summary>
public class AchievementItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Year { get; set; }
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
}

/// <summary>Represents a leadership member (Principal, Director, Chairman, etc.).</summary>
public class LeadershipMemberDto
{
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Message { get; set; }
    public string? Email { get; set; }
    public int SortOrder { get; set; } = 0;
}

/// <summary>Represents a core value / institutional pillar.</summary>
public class CoreValueItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }   // Lucide icon name
    public int SortOrder { get; set; } = 0;
}

/// <summary>Full About Module DTO served to public website and admin.</summary>
public class AboutModuleDto
{
    public Guid Id { get; set; }

    // Institution Story / History
    public string? HistoryTitle { get; set; }
    public string? HistoryContent { get; set; }
    public string? HistoryImageUrl { get; set; }
    public int? EstablishedYear { get; set; }

    // Vision & Mission
    public string? VisionText { get; set; }
    public string? MissionText { get; set; }

    // Deserialized arrays
    public List<AccreditationItemDto> Accreditations { get; set; } = [];
    public List<AchievementItemDto> Achievements { get; set; } = [];
    public List<LeadershipMemberDto> Leadership { get; set; } = [];
    public List<CoreValueItemDto> CoreValues { get; set; } = [];

    // Raw JSON for update passthrough
    public string AccreditationsJson { get; set; } = "[]";
    public string AchievementsJson { get; set; } = "[]";
    public string LeadershipJson { get; set; } = "[]";
    public string CoreValuesJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Admin request to update the About module.</summary>
public class UpdateAboutModuleRequest
{
    public string? HistoryTitle { get; set; }
    public string? HistoryContent { get; set; }
    public string? HistoryImageUrl { get; set; }
    public int? EstablishedYear { get; set; }
    public string? VisionText { get; set; }
    public string? MissionText { get; set; }
    public string AccreditationsJson { get; set; } = "[]";
    public string AchievementsJson { get; set; } = "[]";
    public string LeadershipJson { get; set; } = "[]";
    public string CoreValuesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
}

// -------------------------------------------------------------
// V12 Dynamic Modal Popup / Lightbox Announcements DTOs
// -------------------------------------------------------------

public class PopupAnnouncementDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PopupType { get; set; } = "IMAGE_POSTER"; // IMAGE_POSTER, RICH_CONTENT, VIDEO
    public string? ImageUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? Heading { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string? CtaText { get; set; } = "Apply Online";
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryTargetUrl { get; set; }
    public bool OpenInNewTab { get; set; } = false;
    public int DelaySeconds { get; set; } = 1;
    public string DisplayFrequency { get; set; } = "ONCE_PER_DAY"; // EVERY_VISIT, ONCE_PER_SESSION, ONCE_PER_DAY
    public string TargetPages { get; set; } = "HOMEPAGE_ONLY"; // HOMEPAGE_ONLY, ALL_PAGES, CUSTOM_SLUGS
    public string? CustomTargetSlugs { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateOrUpdatePopupRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string PopupType { get; set; } = "IMAGE_POSTER"; // IMAGE_POSTER, RICH_CONTENT, VIDEO
    public string? ImageUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? Heading { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string? CtaText { get; set; } = "Apply Online";
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryTargetUrl { get; set; }
    public bool OpenInNewTab { get; set; } = false;
    public int DelaySeconds { get; set; } = 1;
    public string DisplayFrequency { get; set; } = "ONCE_PER_DAY";
    public string TargetPages { get; set; } = "HOMEPAGE_ONLY";
    public string? CustomTargetSlugs { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

