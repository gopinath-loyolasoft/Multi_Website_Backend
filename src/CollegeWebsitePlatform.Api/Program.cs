using System.Text;
using CollegeWebsitePlatform.Api.Middleware;
using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.Interfaces;
using CollegeWebsitePlatform.Application.Services;
using CollegeWebsitePlatform.Infrastructure.Authentication;
using CollegeWebsitePlatform.Infrastructure.Persistence;
using CollegeWebsitePlatform.Infrastructure.Persistence.Npgsql;
using CollegeWebsitePlatform.Infrastructure.Provisioning;
using CollegeWebsitePlatform.Infrastructure.Repositories.Platform;
using CollegeWebsitePlatform.Infrastructure.Services;
using CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;
using CollegeWebsitePlatform.Infrastructure.Tenant;
using CollegeWebsitePlatform.Domain.Constants;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;

// ─── Bootstrap Serilog BEFORE the host is built so startup errors are captured ─────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId}{Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/platform-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting CollegeWebsitePlatform API host");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog from appsettings.json ──────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName());

    // ─── Read all config from IConfiguration (no more hardcoded strings) ────────────────────────
    var masterConnectionString = builder.Configuration["Platform:MasterConnectionString"]
        ?? "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true";

    var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretPlatformJwtSigningKeyForMultiTenantCollegeWebsite2026!";
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CollegeWebsitePlatform";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CollegeWebsitePlatformAudience";

    // ─── 1. Connection Factories ─────────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<IPlatformDbConnectionFactory, PlatformDbConnectionFactory>();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<ITenantDbConnectionFactory, TenantDbConnectionFactory>();

    // ─── 2. Platform Repositories ────────────────────────────────────────────────────────────────
    builder.Services.AddScoped<TenantRepository>();
    builder.Services.AddScoped<DomainRepository>();
    builder.Services.AddScoped<DatabaseRegistryRepository>();
    builder.Services.AddScoped<ThemeRepository>();
    builder.Services.AddScoped<TemplateRepository>();
    builder.Services.AddScoped<FeatureRepository>();
    builder.Services.AddScoped<RoleRepository>();
    builder.Services.AddScoped<PermissionRepository>();
    builder.Services.AddScoped<UserTenantRepository>();
    builder.Services.AddScoped<UserRepository>();
    builder.Services.AddScoped<RefreshTokenRepository>();
    builder.Services.AddScoped<AuditRepository>();

    // ─── 3. Tenant Repositories ──────────────────────────────────────────────────────────────────
    builder.Services.AddScoped<PageRepository>();
    builder.Services.AddScoped<PageVersionRepository>();
    builder.Services.AddScoped<SectionRepository>();
    builder.Services.AddScoped<PreviewSessionRepository>();
    builder.Services.AddScoped<MenuRepository>();
    builder.Services.AddScoped<NewsRepository>();
    builder.Services.AddScoped<EventRepository>();
    builder.Services.AddScoped<BannerRepository>();
    builder.Services.AddScoped<GalleryRepository>();
    builder.Services.AddScoped<DepartmentRepository>();
    builder.Services.AddScoped<CourseRepository>();
    builder.Services.AddScoped<FacultyRepository>();
    builder.Services.AddScoped<MediaRepository>();
    builder.Services.AddScoped<TenantSettingsRepository>();
    builder.Services.AddScoped<ContactRepository>();
    builder.Services.AddScoped<NoticeRepository>();
    builder.Services.AddScoped<MarqueeRepository>();

    // ─── 4. Application Services ─────────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<JwtTokenService>();
    builder.Services.AddScoped<IDnsVerificationService, DnsVerificationService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
    builder.Services.AddScoped<ISiteService, SiteService>();
    builder.Services.AddScoped<IPageService, PageService>();
    builder.Services.AddScoped<ISectionService, SectionService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<IMarqueeService, MarqueeService>();
    builder.Services.AddScoped<INewsService, NewsService>();
    builder.Services.AddScoped<IEventService, EventService>();
    builder.Services.AddScoped<IBannerService, BannerService>();
    builder.Services.AddScoped<IGalleryService, GalleryService>();
    builder.Services.AddScoped<IDepartmentService, DepartmentService>();
    builder.Services.AddScoped<ICourseService, CourseService>();
    builder.Services.AddScoped<IFacultyService, FacultyService>();
    builder.Services.AddScoped<IMediaService, MediaService>();
    builder.Services.AddScoped<ISettingsService, SettingsService>();
    builder.Services.AddScoped<IContactService, ContactService>();
    builder.Services.AddScoped<INoticeService, NoticeService>();
    builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
    builder.Services.AddScoped<ICapabilityService, CapabilityService>();

    // ─── 5. Provisioning Service (Phase 4) ───────────────────────────────────────────────────────
    builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

    // ─── 6. Permission-Based Authorization (Phase 5) ─────────────────────────────────────────────
    builder.Services.AddScoped<TenantAuthorizationFilter>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
    builder.Services.AddAuthorization(options =>
    {
        // Register named policies for each permission code
        foreach (var perm in PermissionCodes.All)
        {
            options.AddPolicy(perm, policy =>
                policy.Requirements.Add(new PermissionRequirement(perm)));
        }
    });

    // ─── Configure Form & Kestrel Limits for Large/Any File Uploads ──────────────────────────────
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = long.MaxValue;
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = int.MaxValue;
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = null; // Unlimited request size
    });

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Phase 7 — typed 400 error from model validation
            options.InvalidModelStateResponseFactory = ctx =>
            {
                var firstError = ctx.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Validation error";

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                    CollegeWebsitePlatform.Application.Common.Models.ApiResponse<object>.Fail(firstError));
            };
        });

    // ─── 7. Authentication with JWT ──────────────────────────────────────────────────────────────
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

    // ─── 8. CORS — multi-domain localhost dev ────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowDynamicLocalhost", policy =>
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                try
                {
                    var host = new Uri(origin).Host.ToLowerInvariant();
                    return host.EndsWith("localhost") 
                        || host.EndsWith("localhost.com")
                        || host == "127.0.0.1"
                        || host.Contains("localhost");
                }
                catch
                {
                    return false;
                }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });

    // ─── 9. Swagger / OpenAPI ────────────────────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "College Website Multi-Tenant Platform API",
            Version = "v1",
            Description = "SuperAdmin Control Plane | Scoped CollegeAdmin CMS | Public Dynamic Website APIs"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT Bearer token without the 'Bearer ' prefix."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // ─── Build App ───────────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ─── Database Migration on Startup ───────────────────────────────────────────────────────────
    try
    {
        Log.Information("Initializing platform database and tenant databases...");
        await DatabaseMigrator.InitializeAndMigrateAsync(masterConnectionString);
        Log.Information("Platform and tenant databases migrated successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed: {Message}", ex.Message);
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();           // 1. Attach X-Correlation-ID
    app.UseMiddleware<ExceptionHandlingMiddleware>();       // 2. Global error handler
    app.UseSerilogRequestLogging(options =>                // 3. Structured HTTP request logs
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? string.Empty);
            diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? string.Empty);
        };
    });

    app.UseCors("AllowDynamicLocalhost");                  // 4. CORS

    app.UseStaticFiles();                                  // 5. Serve /wwwroot media assets

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "College Website Platform API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Multi-Tenant College Platform API";
    });

    app.UseMiddleware<TenantResolutionMiddleware>();        // 6. Resolve tenant from domain
    app.UseAuthentication();                               // 7. JWT validation
    app.UseAuthorization();                                // 8. Permission/role checks

    app.MapControllers();

    Log.Information("API listening at  Swagger at http://localhost:5000/swagger");
    app.Run("http://localhost:5000");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
