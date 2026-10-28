namespace CollegeWebsitePlatform.Domain.Constants;

/// <summary>
/// Canonical CMS section type codes.
/// These match the 'section_type' column values stored in the tenant DB's page_sections table.
/// </summary>
public static class SectionTypes
{
    public const string Hero         = "HERO";
    public const string HeroSlider   = "HERO_SLIDER";
    public const string Text         = "TEXT";
    public const string Image        = "IMAGE";
    public const string ImageText    = "IMAGE_TEXT";
    public const string Cards        = "CARDS";
    public const string Statistics   = "STATISTICS";
    public const string IconCards    = "ICON_CARDS";
    public const string News         = "NEWS";
    public const string Events       = "EVENTS";
    public const string Notices      = "NOTICES";
    public const string Courses      = "COURSES";
    public const string Departments  = "DEPARTMENTS";
    public const string Faculty      = "FACULTY";
    public const string Gallery      = "GALLERY";
    public const string CallToAction = "CALL_TO_ACTION";
    public const string Video        = "VIDEO";
    public const string LogoGrid     = "LOGO_GRID";
    public const string Faq          = "FAQ";
    public const string Contact      = "CONTACT";
    public const string Map          = "MAP";

    // Legacy aliases
    public const string Banners      = "BANNERS";
    public const string Stats        = "STATS";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Hero, HeroSlider, Text, Image, ImageText, Cards, Statistics, IconCards,
        News, Events, Notices, Courses, Departments, Faculty, Gallery,
        CallToAction, Video, LogoGrid, Faq, Contact, Map,
        Banners, Stats
    };
}

/// <summary>
/// Platform feature codes stored in the 'features' table.
/// </summary>
public static class FeatureCodes
{
    public const string Pages        = "PAGES";
    public const string Sections     = "SECTIONS";
    public const string Menus        = "MENUS";
    public const string Themes       = "THEMES";
    public const string Banners      = "BANNERS";
    public const string Marquee      = "MARQUEE";
    public const string News         = "NEWS";
    public const string Events       = "EVENTS";
    public const string Notices      = "NOTICES";
    public const string Departments  = "DEPARTMENTS";
    public const string Courses      = "COURSES";
    public const string Faculty      = "FACULTY";
    public const string Gallery      = "GALLERY";
    public const string Media        = "MEDIA";
    public const string Contact      = "CONTACT";
    public const string Stats        = "STATS";
    public const string Quote        = "QUOTE";
    public const string Admissions   = "ADMISSIONS";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Pages, Sections, Menus, Themes, Banners, Marquee,
        News, Events, Notices, Departments, Courses,
        Faculty, Gallery, Media, Contact,
        Stats, Quote, Admissions
    };
}
