namespace CollegeWebsitePlatform.Domain.Constants;

/// <summary>
/// Canonical permission codes used in JWT claims, role_permissions table,
/// [Permission] attribute, and authorization policies.
/// </summary>
public static class PermissionCodes
{
    // Pages
    public const string PageView    = "PAGE_VIEW";
    public const string PageManage  = "PAGE_MANAGE";
    public const string PagePublish = "PAGE_PUBLISH";

    // News
    public const string NewsManage  = "NEWS_MANAGE";

    // Events
    public const string EventManage = "EVENT_MANAGE";

    // Banners
    public const string BannerManage = "BANNER_MANAGE";

    // Gallery
    public const string GalleryManage = "GALLERY_MANAGE";

    // Academics
    public const string AcademicManage = "ACADEMIC_MANAGE";

    // Media
    public const string MediaManage = "MEDIA_MANAGE";

    // Menus
    public const string MenuManage = "MENU_MANAGE";

    // Settings
    public const string SettingsManage = "SETTINGS_MANAGE";

    // Contact
    public const string ContactManage = "CONTACT_MANAGE";

    // SuperAdmin only
    public const string TenantManage   = "TENANT_MANAGE";
    public const string UserManage      = "USER_MANAGE";
    public const string DatabaseManage  = "DATABASE_MANAGE";

    public static readonly IReadOnlyList<string> All = new[]
    {
        PageView, PageManage, PagePublish,
        NewsManage, EventManage,
        BannerManage, GalleryManage, AcademicManage,
        MediaManage, MenuManage, SettingsManage, ContactManage,
        TenantManage, UserManage, DatabaseManage
    };

    /// <summary>Permissions granted to a CollegeAdmin role by default.</summary>
    public static readonly IReadOnlyList<string> CollegeAdminDefaults = new[]
    {
        PageView, PageManage, PagePublish,
        NewsManage, EventManage,
        BannerManage, GalleryManage, AcademicManage,
        MediaManage, MenuManage, SettingsManage, ContactManage
    };
}
