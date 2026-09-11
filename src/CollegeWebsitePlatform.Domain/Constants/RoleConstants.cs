namespace CollegeWebsitePlatform.Domain.Constants;

public static class RoleConstants
{
    public const string SuperAdmin = "SuperAdmin";
    public const string CollegeAdmin = "CollegeAdmin";
}

public static class PermissionConstants
{
    // Platform / SuperAdmin Permissions
    public const string TenantView = "TENANT_VIEW";
    public const string TenantCreate = "TENANT_CREATE";
    public const string TenantUpdate = "TENANT_UPDATE";
    public const string TenantActivate = "TENANT_ACTIVATE";
    public const string TenantSuspend = "TENANT_SUSPEND";

    public const string DomainView = "DOMAIN_VIEW";
    public const string DomainCreate = "DOMAIN_CREATE";
    public const string DomainUpdate = "DOMAIN_UPDATE";
    public const string DomainDelete = "DOMAIN_DELETE";

    public const string DatabaseView = "DATABASE_VIEW";
    public const string DatabaseProvision = "DATABASE_PROVISION";
    public const string DatabaseVerify = "DATABASE_VERIFY";

    public const string ThemeView = "THEME_VIEW";
    public const string ThemeCreate = "THEME_CREATE";
    public const string ThemeUpdate = "THEME_UPDATE";
    public const string ThemeAssign = "THEME_ASSIGN";

    public const string FeatureView = "FEATURE_VIEW";
    public const string FeatureCreate = "FEATURE_CREATE";
    public const string FeatureUpdate = "FEATURE_UPDATE";
    public const string FeatureAssign = "FEATURE_ASSIGN";

    public const string UserView = "USER_VIEW";
    public const string UserCreate = "USER_CREATE";
    public const string UserUpdate = "USER_UPDATE";
    public const string UserDisable = "USER_DISABLE";

    public const string AuditView = "AUDIT_VIEW";
    public const string MigrationView = "MIGRATION_VIEW";
    public const string MigrationRun = "MIGRATION_RUN";
    public const string SettingsManage = "SETTINGS_MANAGE";

    // Tenant / CollegeAdmin Permissions
    public const string PageView = "PAGE_VIEW";
    public const string PageCreate = "PAGE_CREATE";
    public const string PageUpdate = "PAGE_UPDATE";
    public const string PageDelete = "PAGE_DELETE";
    public const string PagePublish = "PAGE_PUBLISH";

    public const string SectionView = "SECTION_VIEW";
    public const string SectionCreate = "SECTION_CREATE";
    public const string SectionUpdate = "SECTION_UPDATE";
    public const string SectionDelete = "SECTION_DELETE";

    public const string MenuView = "MENU_VIEW";
    public const string MenuCreate = "MENU_CREATE";
    public const string MenuUpdate = "MENU_UPDATE";

    public const string NewsView = "NEWS_VIEW";
    public const string NewsCreate = "NEWS_CREATE";
    public const string NewsUpdate = "NEWS_UPDATE";
    public const string NewsDelete = "NEWS_DELETE";

    public const string EventView = "EVENT_VIEW";
    public const string EventCreate = "EVENT_CREATE";
    public const string EventUpdate = "EVENT_UPDATE";
    public const string EventDelete = "EVENT_DELETE";

    public const string MediaView = "MEDIA_VIEW";
    public const string MediaUpload = "MEDIA_UPLOAD";
    public const string MediaDelete = "MEDIA_DELETE";
}

public static class SectionTypeConstants
{
    public const string Hero = "HERO";
    public const string Text = "TEXT";
    public const string ImageText = "IMAGE_TEXT";
    public const string News = "NEWS";
    public const string Events = "EVENTS";
}
