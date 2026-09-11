namespace CollegeWebsitePlatform.Domain.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

public class Tenant : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DbConnectionString { get; set; } = string.Empty;
    public string Status { get; set; } = "ACTIVE";
    public Guid? DefaultThemeId { get; set; }
    public string TemplateCode { get; set; } = "ENGINEERING_MODERN";
    public string LoginTemplate { get; set; } = "template2";
    public string ProvisioningStatus { get; set; } = "COMPLETED";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Domain : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string DomainType { get; set; } = "PRIMARY";
    public bool IsPrimary { get; set; } = false;
    public bool IsVerified { get; set; } = true;
    public DateTime? VerifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Theme : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Configuration { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Feature
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "CMS";
    public string Icon { get; set; } = "cube";
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class TenantFeature
{
    public Guid TenantId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TemplateModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DefaultThemeId { get; set; }
    public string RecommendedFeaturesJson { get; set; } = "[]";
    public string SupportedThemesJson { get; set; } = "[]";
    public string StarterPagesJson { get; set; } = "[]";
    public string StarterMenusJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class User : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "CollegeAdmin";
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Role : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// -------------------------------------------------------------
// Tenant Specific Entities
// -------------------------------------------------------------

public class Page : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; } = false;
    public int SectionCount { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PageSection : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageId { get; set; }
    public string SectionType { get; set; } = "HERO";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string ContentJson { get; set; } = "{}";
    public string SettingsJson { get; set; } = "{}";
    public int SortOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PageVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PageId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public string ContentJson { get; set; } = "[]";
    public string CreatedBy { get; set; } = "admin";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Menu : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Name => Title;
    public string Location => Code;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MenuItem : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MenuId { get; set; }
    public Guid? ParentId { get; set; }
    public string ItemType { get; set; } = "PAGE";
    public string TargetType { get; set; } = "DYNAMIC_MODULE"; // INTERNAL_PAGE, DYNAMIC_MODULE, SECTION_ANCHOR, EXTERNAL_LINK, FILE_DOWNLOAD
    public Guid? PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsVisible { get; set; } = true;
    public bool OpenInNewTab { get; set; } = false;
    public string? Icon { get; set; } = "FileText";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class News : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Event : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Banner : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public string? BadgeText { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string? PillarsJson { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Gallery : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class GalleryImage : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GalleryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Department : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HeadOfDepartment { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Course : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DegreeLevel { get; set; } = "UNDERGRADUATE";
    public decimal DurationYears { get; set; } = 3.0m;
    public string? Description { get; set; }
    public string? Eligibility { get; set; }
    public string? FeeStructure { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Faculty : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Qualification { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Email { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Testimonial : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StudentName { get; set; } = string.Empty;
    public string? BatchYear { get; set; }
    public string? CourseName { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? CurrentCompany { get; set; }
    public int Rating { get; set; } = 5;
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Media : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AltText { get; set; }
    public string UploadedBy { get; set; } = "admin";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TenantSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SiteName { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string SocialLinksJson { get; set; } = "{}";
    public string LoginTemplate { get; set; } = "template2";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ContactSubmission : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "NEW";
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Notice : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public string Category { get; set; } = "GENERAL";
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsImportant { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MarqueeSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsActive { get; set; } = true;
    public string Speed { get; set; } = "medium"; // slow, medium, fast
    public bool PauseOnHover { get; set; } = true;
    public string BgColor { get; set; } = "#0f172a";
    public string TextColor { get; set; } = "#f8fafc";
    public string BadgeBgColor { get; set; } = "#f59e0b";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MarqueeItem : ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? BadgeText { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PreviewSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PreviewToken { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "admin";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

