using System.Diagnostics;
using System.Text.Json;
using BCrypt.Net;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using CollegeWebsitePlatform.Domain.Constants;
using CollegeWebsitePlatform.Infrastructure.Persistence;
using CollegeWebsitePlatform.Infrastructure.Repositories.Platform;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CollegeWebsitePlatform.Infrastructure.Provisioning;

/// <summary>
/// Full 15-step college provisioning orchestrator.
/// Each step is logged so failures can be diagnosed precisely.
/// On any failure: tenant status is left as PROVISIONING (not ACTIVE) — allowing retry.
/// </summary>
public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly TenantRepository          _tenantRepo;
    private readonly DomainRepository          _domainRepo;
    private readonly DatabaseRegistryRepository _dbRegistryRepo;
    private readonly ThemeRepository           _themeRepo;
    private readonly FeatureRepository         _featureRepo;
    private readonly UserRepository            _userRepo;
    private readonly UserTenantRepository      _userTenantRepo;
    private readonly AuditRepository           _auditRepo;
    private readonly TemplateRepository        _templateRepo;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly string                    _masterConnectionString;

    public TenantProvisioningService(
        TenantRepository tenantRepo,
        DomainRepository domainRepo,
        DatabaseRegistryRepository dbRegistryRepo,
        ThemeRepository themeRepo,
        FeatureRepository featureRepo,
        UserRepository userRepo,
        UserTenantRepository userTenantRepo,
        AuditRepository auditRepo,
        TemplateRepository templateRepo,
        ILogger<TenantProvisioningService> logger,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _tenantRepo      = tenantRepo;
        _domainRepo      = domainRepo;
        _dbRegistryRepo  = dbRegistryRepo;
        _themeRepo       = themeRepo;
        _featureRepo     = featureRepo;
        _userRepo        = userRepo;
        _userTenantRepo  = userTenantRepo;
        _auditRepo       = auditRepo;
        _templateRepo    = templateRepo;
        _logger          = logger;
        _masterConnectionString = config["Platform:MasterConnectionString"]
            ?? "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true";
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // STEP-BY-STEP PROVISIONING PIPELINE
    // ──────────────────────────────────────────────────────────────────────────────

    public async Task<ProvisioningResult> ProvisionNewCollegeAsync(ProvisionCollegeRequest request)
    {
        var result = new ProvisioningResult();
        var tenantId = Guid.NewGuid();
        Guid domainId = Guid.Empty;
        Guid adminUserId = Guid.Empty;
        string dbName = string.Empty;
        string tenantCs = string.Empty;

        _logger.LogInformation("[Provisioning] Starting college provisioning for '{Name}' (Code: {Code})",
            request.CollegeName, request.TenantCode);

        try
        {
            // Fetch template blueprint if specified
            TemplateDto? templateBlueprint = null;
            var templateCodeToUse = string.IsNullOrWhiteSpace(request.TemplateCode) ? "ENGINEERING_MODERN" : request.TemplateCode;
            try
            {
                templateBlueprint = await _templateRepo.GetByCodeAsync(templateCodeToUse);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Provisioning] Could not fetch template blueprint '{Code}'", templateCodeToUse);
            }

            // Fallback theme to template's connected default theme if not provided
            if (!request.ThemeId.HasValue && templateBlueprint?.DefaultThemeId != null)
            {
                request.ThemeId = templateBlueprint.DefaultThemeId;
            }

            // ─── STEP 1: Validate uniqueness ──────────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 1/15 — Validating uniqueness");

            var existingByCode = await _tenantRepo.GetByCodeAsync(request.TenantCode);
            if (existingByCode != null)
                throw new InvalidOperationException($"A college with tenant code '{request.TenantCode}' already exists.");

            var existingDomain = await _domainRepo.GetByDomainAsync(request.PrimaryDomain);
            if (existingDomain != null)
                throw new InvalidOperationException($"Domain '{request.PrimaryDomain}' is already registered on the platform.");

            result.StepsCompleted.Add("Validation");

            // ─── STEP 2: Generate database name ──────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 2/15 — Generating database name");
            dbName = $"college_{request.Slug.ToLowerInvariant().Replace("-", "_").Replace(" ", "_")}";
            result.DatabaseName = dbName;

            // ─── STEP 3: INSERT tenant record (PROVISIONING status) ───────────────────
            _logger.LogInformation("[Provisioning] Step 3/15 — Creating tenant record");

            // Build the tenant connection string now (DB doesn't exist yet, but we'll update after creation)
            var masterBuilder = new NpgsqlConnectionStringBuilder(_masterConnectionString)
            {
                Database = dbName
            };
            tenantCs = masterBuilder.ConnectionString;

            await _tenantRepo.CreateWithIdAsync(
                tenantId,
                request.TenantCode.ToUpperInvariant(),
                request.CollegeName,
                request.Slug.ToLowerInvariant(),
                tenantCs,
                TenantStatus.Provisioning,
                request.ThemeId);

            result.TenantId = tenantId;
            result.DbConnectionString = tenantCs;
            result.StepsCompleted.Add("TenantRecord");

            // ─── STEP 4: Register primary domain ─────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 4/15 — Registering domain '{Domain}'", request.PrimaryDomain);
            domainId = await _domainRepo.CreateAsync(tenantId, request.PrimaryDomain, DomainType.Primary, isPrimary: true);
            result.DomainId = domainId;
            result.StepsCompleted.Add("DomainRegistration");

            // ─── STEP 5: CREATE PostgreSQL database ───────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 5/15 — Creating PostgreSQL database '{DbName}'", dbName);
            using (var masterConn = new NpgsqlConnection(_masterConnectionString))
            {
                await masterConn.OpenAsync();
                var dbExists = await masterConn.ExecuteScalarAsync<int?>
                    ("SELECT 1 FROM pg_database WHERE datname = @dbName;", new { dbName });
                if (!dbExists.HasValue)
                    await masterConn.ExecuteAsync($"CREATE DATABASE \"{dbName}\";");
            }
            result.StepsCompleted.Add("DatabaseCreated");

            // ─── STEP 6: Run tenant schema migrations ─────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 6/15 — Running tenant schema migrations");
            await DatabaseMigrator.MigrateTenantTablesAsync(tenantCs);
            result.StepsCompleted.Add("SchemaMigrated");

            // ─── STEP 7: Seed initial tenant content & template blueprint ─────────────
            _logger.LogInformation("[Provisioning] Step 7/15 — Seeding initial content & template blueprint '{Template}'", templateCodeToUse);
            await DatabaseMigrator.SeedInitialTenantContentAsync(tenantCs, request.TenantCode, templateCodeToUse, request.CollegeName);

            if (templateBlueprint != null)
            {
                await SeedTemplateBlueprintPagesAndMenusAsync(tenantCs, templateBlueprint, request.CollegeName);
            }
            else
            {
                await SeedMinimalTenantContentAsync(tenantCs, request.TenantCode, request.CollegeName);
            }
            result.StepsCompleted.Add("ContentSeeded");

            // ─── STEP 8: Register database in registry ────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 8/15 — Registering database in registry");
            var regBuilder = new NpgsqlConnectionStringBuilder(tenantCs);
            await _dbRegistryRepo.RegisterAsync(
                tenantId, dbName,
                regBuilder.Host ?? "localhost",
                regBuilder.Port,
                regBuilder.Username ?? "postgres");
            result.StepsCompleted.Add("RegistryUpdated");

            // ─── STEP 9: Assign theme ─────────────────────────────────────────────────
            if (request.ThemeId.HasValue)
            {
                _logger.LogInformation("[Provisioning] Step 9/15 — Assigning theme {ThemeId}", request.ThemeId);
                await _tenantRepo.UpdateThemeAsync(tenantId, request.ThemeId.Value);
            }
            else
            {
                // Try to assign template's connected default theme or first available active theme
                var themes = await _themeRepo.GetAllAsync();
                var defaultTheme = (templateBlueprint?.DefaultThemeId != null 
                    ? themes.FirstOrDefault(t => t.Id == templateBlueprint.DefaultThemeId) 
                    : null) ?? themes.FirstOrDefault(t => t.IsActive);
                if (defaultTheme != null)
                    await _tenantRepo.UpdateThemeAsync(tenantId, defaultTheme.Id);
            }
            result.StepsCompleted.Add("ThemeAssigned");

            // ─── STEP 10: Enable features ─────────────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 10/15 — Enabling features");
            var featuresToEnable = request.EnabledFeatures.Any()
                ? request.EnabledFeatures
                : FeatureCodes.All.ToList();

            await _featureRepo.SetTenantFeaturesAsync(tenantId, featuresToEnable);
            result.StepsCompleted.Add("FeaturesEnabled");

            // ─── STEP 11: Create CollegeAdmin user ────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 11/15 — Creating CollegeAdmin user '{Username}'", request.AdminUsername);
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword, 12);
            adminUserId = await _userRepo.CreateAsync(
                tenantId,
                request.AdminUsername,
                request.AdminEmail,
                passwordHash,
                request.AdminFullName,
                UserRole.CollegeAdmin);

            result.AdminUserId = adminUserId;
            result.StepsCompleted.Add("AdminUserCreated");

            // ─── STEP 12: Map user → tenant ───────────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 12/15 — Mapping admin user to tenant");
            await _userTenantRepo.CreateAsync(adminUserId, tenantId, isPrimary: true);
            result.StepsCompleted.Add("UserTenantMapped");

            // ─── STEP 13: Activate tenant ─────────────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 13/15 — Activating tenant");
            await _tenantRepo.UpdateStatusAsync(tenantId, TenantStatus.Active);
            await _tenantRepo.UpdateProvisioningStatusAsync(tenantId, ProvisioningStatus.Completed);
            result.StepsCompleted.Add("TenantActivated");

            // ─── STEP 14: Audit log ───────────────────────────────────────────────────
            _logger.LogInformation("[Provisioning] Step 14/15 — Writing audit log");
            await _auditRepo.LogAsync(
                userId: null,
                username: "system",
                tenantId: tenantId,
                action: "PROVISION_COLLEGE",
                entityType: "Tenant",
                entityId: tenantId.ToString(),
                details: $"College '{request.CollegeName}' (Code: {request.TenantCode}) provisioned. DB: {dbName}. Admin: {request.AdminUsername}.",
                ipAddress: null);
            result.StepsCompleted.Add("AuditLogged");

            // ─── STEP 15: Return result ───────────────────────────────────────────────
            result.Success = true;
            result.Message = $"College '{request.CollegeName}' provisioned successfully. Admin: {request.AdminUsername}.";
            _logger.LogInformation("[Provisioning] ✅ Step 15/15 — Provisioning complete for '{Name}' (TenantId: {TenantId}, DB: {DbName})",
                request.CollegeName, tenantId, dbName);

            return result;
        }
        catch (Exception ex)
        {
            var failedStep = result.StepsCompleted.LastOrDefault() ?? "Validation";
            _logger.LogError(ex, "[Provisioning] ❌ Failed at step after '{Step}': {Message}", failedStep, ex.Message);

            result.Success  = false;
            result.FailedAt = failedStep;
            result.Message  = ex.Message;

            // Try to set provisioning_status = FAILED for observability
            try
            {
                if (result.TenantId != Guid.Empty)
                    await _tenantRepo.UpdateProvisioningStatusAsync(result.TenantId, ProvisioningStatus.Failed);
            }
            catch { /* swallow inner error */ }

            return result;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // SUSPEND / ACTIVATE
    // ──────────────────────────────────────────────────────────────────────────────

    public async Task<bool> SuspendCollegeAsync(Guid tenantId, string reason, string performedBy)
    {
        var ok = await _tenantRepo.UpdateStatusAsync(tenantId, TenantStatus.Suspended);
        if (ok)
        {
            await _auditRepo.LogAsync(null, performedBy, tenantId, "SUSPEND_COLLEGE",
                "Tenant", tenantId.ToString(), reason, null);
            _logger.LogWarning("[Provisioning] Tenant {TenantId} SUSPENDED by {User}. Reason: {Reason}",
                tenantId, performedBy, reason);
        }
        return ok;
    }

    public async Task<bool> ActivateCollegeAsync(Guid tenantId, string performedBy)
    {
        var ok = await _tenantRepo.UpdateStatusAsync(tenantId, TenantStatus.Active);
        if (ok)
        {
            await _auditRepo.LogAsync(null, performedBy, tenantId, "ACTIVATE_COLLEGE",
                "Tenant", tenantId.ToString(), "Tenant re-activated", null);
            _logger.LogInformation("[Provisioning] Tenant {TenantId} ACTIVATED by {User}", tenantId, performedBy);
        }
        return ok;
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // MIGRATION STATUS & HEALTH CHECK
    // ──────────────────────────────────────────────────────────────────────────────

    public async Task<MigrationStatusDto> GetMigrationStatusAsync(Guid tenantId)
    {
        var registry = await _dbRegistryRepo.GetByTenantIdAsync(tenantId);
        if (registry == null)
            return new MigrationStatusDto { TenantId = tenantId, Status = "NOT_REGISTERED" };

        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        return new MigrationStatusDto
        {
            TenantId         = tenantId,
            TenantCode       = tenant?.TenantCode ?? string.Empty,
            DatabaseName     = registry.DatabaseName,
            CurrentVersion   = registry.CurrentMigrationVersion,
            Status           = registry.Status,
            LastCheckedAt    = registry.LastHealthCheckAt
        };
    }

    public async Task<bool> RunMigrationsAsync(Guid tenantId, string performedBy)
    {
        var registry = await _dbRegistryRepo.GetByTenantIdAsync(tenantId);
        if (registry == null) return false;

        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null) return false;

        try
        {
            await DatabaseMigrator.MigrateTenantTablesAsync(tenant.DbConnectionString);
            await _dbRegistryRepo.UpdateStatusAsync(tenantId, "HEALTHY", "V8");
            await _auditRepo.LogAsync(null, performedBy, tenantId, "RUN_MIGRATIONS",
                "TenantDatabase", tenantId.ToString(), $"Migrations run on {registry.DatabaseName}", null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed for tenant {TenantId}", tenantId);
            await _dbRegistryRepo.UpdateStatusAsync(tenantId, "FAILED", "UNKNOWN");
            return false;
        }
    }

    public async Task<DatabaseHealthDto> VerifyTenantDatabaseAsync(Guid tenantId)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId);
        if (tenant == null)
            return new DatabaseHealthDto { IsHealthy = false, Message = "Tenant not found" };

        var sw = Stopwatch.StartNew();
        try
        {
            using var conn = new NpgsqlConnection(tenant.DbConnectionString);
            await conn.OpenAsync();
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pages;");
            sw.Stop();

            await _dbRegistryRepo.UpdateHealthCheckAsync(tenantId);
            return new DatabaseHealthDto
            {
                IsHealthy     = true,
                DatabaseName  = new NpgsqlConnectionStringBuilder(tenant.DbConnectionString).Database ?? "",
                Status        = "HEALTHY",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Message       = $"Connected. pages={count}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DatabaseHealthDto
            {
                IsHealthy      = false,
                Status         = "UNREACHABLE",
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Message        = ex.Message
            };
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // PRIVATE — Minimal Tenant Content Seed (blank template only, no fake data)
    // ──────────────────────────────────────────────────────────────────────────────

    private static async Task SeedMinimalTenantContentAsync(string tenantCs, string tenantCode, string collegeName)
    {
        using var conn = new NpgsqlConnection(tenantCs);
        await conn.OpenAsync();

        // Blank tenant_settings
        var settingsCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tenant_settings;");
        if (settingsCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO tenant_settings (site_name, tagline, contact_email, address, social_links)
                VALUES (@siteName, @tagline, @email, @address, '{}'::jsonb);",
                new
                {
                    siteName = collegeName,
                    tagline  = "Excellence in Education",
                    email    = $"contact@{tenantCode.ToLowerInvariant()}.edu",
                    address  = "Campus Address"
                });
        }

        // Initial Home page with full suite of dynamic college module sections
        var pageCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pages;");
        if (pageCount == 0)
        {
            var pageId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO pages (id, slug, title, meta_description, is_published)
                VALUES (@pageId, 'home', @title, @desc, TRUE);",
                new { pageId, title = $"Welcome to {collegeName}", desc = $"Official website of {collegeName}" });

            var defaultSections = new[]
            {
                new { SecType = "HERO_SLIDER", Title = "Hero Slider & Highlights", Subtitle = "Campus showcase and online admissions call to action", Sort = 1 },
                new { SecType = "STATISTICS", Title = "Key Statistics & Milestones", Subtitle = "Institutional track record, accreditations, and student excellence", Sort = 2 },
                new { SecType = "QUOTE", Title = "Leadership & Vision", Subtitle = "Message from the Chancellor & Academic Directorate", Sort = 3 },
                new { SecType = "DEPARTMENTS", Title = "Academic Departments", Subtitle = "Specialized schools offering cutting-edge undergraduate & postgraduate disciplines", Sort = 4 },
                new { SecType = "COURSES", Title = "Degree Programs & Curricula", Subtitle = "Industry-aligned academic pathways and career-oriented certifications", Sort = 5 },
                new { SecType = "FACULTY", Title = "Distinguished Faculty", Subtitle = "Renowned professors, doctorate researchers, and industry fellows", Sort = 6 },
                new { SecType = "NEWS", Title = "Latest Campus News", Subtitle = "Campus events, research breakthroughs, honors, and press announcements", Sort = 7 },
                new { SecType = "EVENTS", Title = "Upcoming Campus Events", Subtitle = "Academic conferences, symposiums, student fests, and cultural celebrations", Sort = 8 },
                new { SecType = "NOTICES", Title = "Official Notice Board", Subtitle = "Important administrative updates, examination schedules, and circulars", Sort = 9 },
                new { SecType = "GALLERY", Title = "Campus Photo Gallery", Subtitle = "A visual journey across our world-class laboratories, library, and campus architecture", Sort = 10 },
                new { SecType = "TESTIMONIALS", Title = "Student Testimonials & Stories", Subtitle = "Inspiring experiences and success stories from our graduates", Sort = 11 },
                new { SecType = "CONTACT", Title = "Campus Helpdesk & Location", Subtitle = "Reach our admissions desk, administrative offices, and visit our campus", Sort = 12 }
            };

            foreach (var s in defaultSections)
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible)
                    VALUES (@pageId, @SecType, @Title, @Subtitle, '{}'::jsonb, '{""columns"":3,""cardVariant"":""standard"",""alignment"":""center"",""background"":""light"",""spacing"":""normal"",""containerWidth"":""standard""}'::jsonb, @Sort, TRUE);",
                    new { pageId, s.SecType, s.Title, s.Subtitle, s.Sort });
            }

            await conn.ExecuteAsync(@"
                INSERT INTO page_versions (page_id, version_number, title, slug, content_json, created_by)
                VALUES (@pageId, 1, @title, 'home', '[]'::jsonb, 'system');",
                new { pageId, title = $"Welcome to {collegeName}" });
        }

        // Main navigation menu
        var menuCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM menus;");
        if (menuCount == 0)
        {
            var menuId = Guid.NewGuid();
            await conn.ExecuteAsync(
                "INSERT INTO menus (id, code, title) VALUES (@menuId, 'MAIN_NAV', 'Main Navigation');",
                new { menuId });
        }
    }

    private async Task SeedTemplateBlueprintPagesAndMenusAsync(string tenantCs, TemplateDto template, string collegeName)
    {
        using var conn = new NpgsqlConnection(tenantCs);
        await conn.OpenAsync();

        // 1. Starter Pages & Sections from Template Blueprint
        if (template.StarterPages != null)
        {
            try
            {
                var pagesJson = JsonSerializer.Serialize(template.StarterPages);
                using var doc = JsonDocument.Parse(pagesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    int sortIdx = 1;
                    foreach (var pageEl in doc.RootElement.EnumerateArray())
                    {
                        var slug = pageEl.TryGetProperty("slug", out var sProp) ? sProp.GetString() ?? $"page-{sortIdx}" : $"page-{sortIdx}";
                        var title = pageEl.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "Untitled" : "Untitled";
                        title = title.Replace("Central University", collegeName);
                        var metaDesc = pageEl.TryGetProperty("metaDescription", out var mProp) ? mProp.GetString() ?? "" : "";
                        metaDesc = metaDesc.Replace("Central University", collegeName);
                        var isPublished = !pageEl.TryGetProperty("isPublished", out var pubProp) || pubProp.GetBoolean();

                        var exists = await conn.ExecuteScalarAsync<int>(
                            "SELECT COUNT(*) FROM pages WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE;",
                            new { slug });

                        Guid pageId;
                        if (exists == 0)
                        {
                            pageId = Guid.NewGuid();
                            await conn.ExecuteAsync(@"
                                INSERT INTO pages (id, slug, title, meta_description, is_published)
                                VALUES (@pageId, @slug, @title, @metaDesc, @isPublished);",
                                new { pageId, slug, title, metaDesc, isPublished });

                            await conn.ExecuteAsync(@"
                                INSERT INTO page_versions (page_id, version_number, title, slug, content_json, created_by)
                                VALUES (@pageId, 1, @title, @slug, '[]'::jsonb, 'system');",
                                new { pageId, title, slug });
                        }
                        else
                        {
                            pageId = await conn.ExecuteScalarAsync<Guid>(
                                "SELECT id FROM pages WHERE LOWER(slug) = LOWER(@slug) AND is_deleted = FALSE LIMIT 1;",
                                new { slug });
                        }

                        if (pageEl.TryGetProperty("sections", out var sectionsEl) && sectionsEl.ValueKind == JsonValueKind.Array)
                        {
                            int secIdx = 1;
                            foreach (var secEl in sectionsEl.EnumerateArray())
                            {
                                var secType = secEl.TryGetProperty("sectionType", out var stpProp) ? stpProp.GetString() ?? "TEXT" : "TEXT";
                                var secTitle = secEl.TryGetProperty("title", out var stProp) ? stProp.GetString() ?? "" : "";
                                var secSubtitle = secEl.TryGetProperty("subtitle", out var ssProp) ? ssProp.GetString() ?? "" : "";
                                var contentJson = secEl.TryGetProperty("content", out var cProp) ? cProp.GetRawText() : "{}";
                                var settingsJson = secEl.TryGetProperty("settings", out var setProp) ? setProp.GetRawText() : "{}";

                                await conn.ExecuteAsync(@"
                                    INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible)
                                    VALUES (@pageId, @secType, @secTitle, @secSubtitle, @contentJson::jsonb, @settingsJson::jsonb, @secIdx, TRUE);",
                                    new { pageId, secType, secTitle, secSubtitle, contentJson, settingsJson, secIdx });
                                secIdx++;
                            }
                        }
                        sortIdx++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Provisioning] Error seeding template starter pages");
            }
        }

        // 2. Starter Menus from Template Blueprint
        if (template.StarterMenus != null)
        {
            try
            {
                var menusJson = JsonSerializer.Serialize(template.StarterMenus);
                using var doc = JsonDocument.Parse(menusJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    var menuId = Guid.NewGuid();
                    var menuCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM menus WHERE code = 'MAIN_NAV';");
                    if (menuCount == 0)
                    {
                        await conn.ExecuteAsync(
                            "INSERT INTO menus (id, code, title) VALUES (@menuId, 'MAIN_NAV', 'Main Navigation');",
                            new { menuId });

                        int mSort = 1;
                        foreach (var itemEl in doc.RootElement.EnumerateArray())
                        {
                            var mTitle = itemEl.TryGetProperty("title", out var mtProp) ? mtProp.GetString() ?? "Link" : "Link";
                            var mUrl = itemEl.TryGetProperty("url", out var uProp) ? uProp.GetString() ?? "/" : "/";
                            var mType = itemEl.TryGetProperty("itemType", out var itProp) ? itProp.GetString() ?? "PAGE" : "PAGE";

                            var itemId = Guid.NewGuid();
                            await conn.ExecuteAsync(@"
                                INSERT INTO menu_items (id, menu_id, title, url, target_type, sort_order)
                                VALUES (@itemId, @menuId, @mTitle, @mUrl, @mType, @mSort);",
                                new { itemId, menuId, mTitle, mUrl, mType, mSort });
                            mSort++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Provisioning] Error seeding template starter menus");
            }
        }
    }
}
