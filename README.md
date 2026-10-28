# College Website Multi-Tenant Platform — Backend API (.NET 9)

A high-performance, modular, multi-tenant backend engine built with **ASP.NET Core 9**, **PostgreSQL**, **Dapper**, and **Clean Architecture**. This platform powers multiple dynamic college and university websites from a unified backend with isolated per-tenant databases, robust security, customizable themes, and a rich CMS.

---

## 🏛️ Architecture Overview

The backend follows the **Clean Architecture / Onion Architecture** pattern to guarantee strict separation of concerns, testability, and maintainability:

```
Backend/
├── CollegeWebsitePlatform.sln
└── src/
    ├── CollegeWebsitePlatform.Domain/          # Enterprise domain entities, constants, enums
    ├── CollegeWebsitePlatform.Application/     # CQRS/Service interfaces, DTOs, business rules
    ├── CollegeWebsitePlatform.Infrastructure/  # Dapper persistence, PostgreSQL migrations, auth, tenant resolution
    ├── CollegeWebsitePlatform.Api/             # ASP.NET Core Web API controllers, middleware, startup config
    └── CollegeWebsitePlatform.Tests/           # Unit, security, and tenant isolation integration tests
```

### Multi-Tenancy Architecture (Database-per-Tenant)

- **Platform Database (`dinamic_college_website`)**: Manages tenant registrations, custom domains, features, global users/roles, themes, templates, audit logs, and database registries.
- **Tenant Databases (`college_rnc`, `college_kts`, etc.)**: Completely isolated PostgreSQL database for each institution holding pages, sections, menus, news, events, courses, departments, faculty, media, and enquiries.
- **Dynamic Tenant Resolution**: Incoming requests are automatically resolved to the correct tenant context via the HTTP `Host` header (or subdomain/custom domain), connecting dynamically to that tenant's database connection string.

---

## 🧩 Core Systems & Technologies Used

Below is the complete breakdown of technologies, protocols, security models, and architectural subsystems implemented across this backend:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 INCOMING HTTP REQUEST                                   │
└──────────────────────────────────────────┬──────────────────────────────────────────────┘
                                           ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ 1. CorrelationIdMiddleware (X-Correlation-ID) & Serilog Request Logging                  │
│ 2. Global ExceptionHandlingMiddleware & Security Headers (nosniff, SAMEORIGIN)          │
│ 3. Dynamic CORS Engine (Multi-domain localhost & production tenant origins)             │
│ 4. TenantResolutionMiddleware (Resolves tenant DB via Host / Subdomain / DNS)           │
│ 5. JWT Bearer Authentication & Refresh Token Validation                                 │
│ 6. Fine-Grained Permission & Tenant Scoping Authorization Filter                        │
└──────────────────────────────────────────┬──────────────────────────────────────────────┘
                                           ▼
┌──────────────────────────────┬──────────────────────────────┬───────────────────────────┐
│     SuperAdmin Controllers   │     College Admin CMS        │     Public Site APIs      │
│  (Platform Management Plane) │  (Scoped Institution CMS)    │   (Dynamic Site Delivery) │
└──────────────┬───────────────┴──────────────┬───────────────┴─────────────┬─────────────┘
               ▼                              ▼                             ▼
┌──────────────────────────────┐ ┌─────────────────────────────┐ ┌─────────────────────────┐
│  Platform Connection Factory │ │   Tenant Connection Factory │ │ Tenant Connection Pool  │
│ (dinamic_college_website DB) │ │     (Dedicated College DB)  │ │  (Read/Write Content)   │
└──────────────────────────────┘ └─────────────────────────────┘ └─────────────────────────┘
```

### 1. 🔐 Security & Identity Management
- **JWT Bearer Authentication (`System.IdentityModel.Tokens.Jwt`)**:
  - Stateless authentication via HMAC-SHA256 signed JSON Web Tokens.
  - Carries user identity, role, tenant assignment, and token expiration timestamps.
- **Refresh Token Lifecycle**:
  - Long-lived database-persisted refresh tokens with cryptographic uniqueness (`/api/v1/auth/refresh`).
  - Active session revocation on logout (`/api/v1/auth/logout`).
- **BCrypt Password Hashing (`BCrypt.Net-Next`)**:
  - Salted and hashed passwords with a work factor of 12 for strong brute-force resistance.
- **Role & Claim-Based Authorization**:
  - Built-in roles: `SuperAdmin` (Platform-wide), `CollegeAdmin` (Tenant-scoped), and `Editor` (Content-only).
  - 15+ granular permission codes (`PAGE_MANAGE`, `NEWS_MANAGE`, `EVENT_MANAGE`, `ACADEMIC_MANAGE`, `SETTINGS_MANAGE`, etc.) evaluated via custom ASP.NET Core `IAuthorizationHandler` policies.
- **Tenant Isolation Boundaries**:
  - `TenantAuthorizationFilter` verifies that authenticated administrators cannot query or mutate data belonging to other tenant colleges.
- **Hardened HTTP Response Headers**:
  - Injects `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, and `Referrer-Policy: strict-origin-when-cross-origin`.

---

### 2. 🌐 Multi-Tenancy & DNS Domain Engine
- **Database-per-Tenant Model**:
  - Physical database isolation: Each college has its own dedicated PostgreSQL database (`college_rnc`, `college_kts`, etc.), completely separating institution data from other tenants.
- **Dynamic Tenant Resolution Middleware (`TenantResolutionMiddleware`)**:
  - Inspects incoming HTTP `Host` headers or subdomains (e.g., `rnc.localhost`, `engineering.college.edu`).
  - Queries domain mapping cache and injects the active tenant's context and connection string into `ITenantContext` for that request lifecycle.
- **DNS Verification Engine (`DnsVerificationService`)**:
  - Supports custom institutional domains (e.g., `www.rnccollege.edu.in`).
  - Issues cryptographic verification tokens and automates DNS TXT / CNAME verification before domain activation.
- **Tenant Database Registry & Health Checks**:
  - `tenant_database_registry` continuously tracks database health status (`HEALTHY`, `DEGRADED`), host, port, credentials, and applied migration versions (`V1`..`V9`).

---

### 3. 📝 Dynamic CMS & Content Management System
- **Modular Section Builder & Layout Engine**:
  - Dynamic page sections with JSONB configuration (`content_json` and `settings_json`).
  - Supported section blocks: `HERO`, `STATS`, `MARQUEE`, `QUOTE`, `DEPARTMENTS`, `COURSES`, `FACULTY`, `NEWS`, `EVENTS`, `GALLERY`, `CONTACT`, `NOTICES`, and `CUSTOM_HTML`.
- **Page Versioning & Draft/Publishing Lifecycle**:
  - Every page modification archives previous revisions to `page_versions` for instant rollback.
  - Draft vs. Published state management ensures unpublished updates remain hidden from the public site.
- **Live Preview Tokens (`preview_sessions`)**:
  - Generates secure, temporary, time-expiring preview tokens (`/api/v1/admin/preview`) allowing stakeholders to preview unpublished page drafts safely without publishing.
- **Multi-Level Navigation Menus**:
  - Hierarchical header navigation and multi-column footer menus with custom sort orders and dynamic routing targets.
- **Flash News & Marquee Ticker**:
  - Dynamic marquee ticker supporting custom speed, pause on hover, color palettes, and date-range scheduling.
- **Academic Hierarchy Management**:
  - Academic Departments with HOD profiles and contact info.
  - Courses & Degree Programs (UG, PG, Ph.D.) with eligibility, fee structures, and downloadable syllabus PDFs.
  - Faculty directory with designations, qualifications, research specializations, and bios.
- **Campus Life & Media Hub**:
  - News and press releases with rich text summaries.
  - Campus events calendar with location details and dates.
  - Categorized photo galleries with lightbox image viewer support.
  - Circulars, examination schedules, and official notices with PDF attachments.
- **Inquiry & Admission Gateway**:
  - Visitor contact submissions and admission lead capture forms stored directly in the tenant database.

---

### 4. ⚡ High-Performance Data Access & Database Architecture
- **Dapper Micro-ORM (v2.1)**:
  - Low-latency, high-throughput parameterized SQL queries for optimal database performance without entity tracking overhead.
- **Native Npgsql PostgreSQL Driver (v10.0)**:
  - Fully asynchronous I/O and connection pooling for both platform and tenant databases.
- **Connection Factories**:
  - `IPlatformDbConnectionFactory` for platform management operations.
  - `ITenantDbConnectionFactory` dynamically creating scoped tenant database connections based on `ITenantContext`.
- **Automated Database Migrations & Provisioning (`DatabaseMigrator`)**:
  - Runs automatically on startup to ensure physical databases exist, applies schema DDL migrations, and seeds baseline data.
  - Provisioning service (`TenantProvisioningService`) creates a brand-new tenant DB in seconds during college onboarding.
- **Soft-Delete Architecture**:
  - All critical platform and tenant tables implement `is_deleted` and `deleted_at`.
  - PostgreSQL partial unique indexes (e.g., `CREATE UNIQUE INDEX ... WHERE is_deleted = FALSE`) prevent slug collisions while allowing recovery of deleted records.

---

### 5. 📊 Logging, Monitoring & Observability
- **Serilog Structured Logging**:
  - Dual sinks: Colored interactive console logging and rolling file logs (`logs/platform-YYYYMMDD.log`) with 14-day automatic retention.
- **Distributed Request Tracing (Correlation ID)**:
  - `CorrelationIdMiddleware` attaches a unique `X-Correlation-ID` GUID to every incoming request and enriches all downstream Serilog log entries.
- **Global Error Handling Middleware (`ExceptionHandlingMiddleware`)**:
  - Intercepts all unhandled exceptions, logs structured diagnostics with correlation IDs, and returns standard, sanitized `ApiResponse<T>` objects without leaking sensitive internals.
- **Health Checks (`/api/v1/health`)**:
  - Real-time database connectivity and platform status probing.

---

### 6. 📁 Central Media Storage & Uploads
- **Media Library Controller (`/api/v1/admin/media`)**:
  - Multi-part form file upload pipeline configured for high-capacity media assets.
  - Strict file validation: Content-type MIME verification and extension whitelisting (`.jpg`, `.png`, `.webp`, `.svg`, `.pdf`, `.docx`, `.xlsx`).
  - Static file hosting (`app.UseStaticFiles()`) serving assets from `/wwwroot`.

---

### 7. 🔌 API Documentation & CORS
- **OpenAPI / Swagger UI (`Swashbuckle.AspNetCore`)**:
  - Complete OpenAPI v1 specification at `/swagger` with integrated JWT Bearer authentication scheme.
- **Dynamic CORS Policy**:
  - Configured to permit requests from dynamic localhost development ports (e.g. `localhost:5173`, `rnc.localhost:5173`) and registered institutional domains.

---

## 🛠️ Tech Stack & Dependencies

- **Framework**: .NET 9.0 (C# 13)
- **Web API**: ASP.NET Core Web API
- **Database**: PostgreSQL
- **Data Access**: Dapper 2.1 (Micro-ORM for maximum throughput) + Npgsql 10.0
- **Authentication**: JWT Bearer (`System.IdentityModel.Tokens.Jwt`, `Microsoft.AspNetCore.Authentication.JwtBearer`)
- **Password Hashing**: BCrypt.Net-Next 4.2
- **Logging**: Serilog (`Serilog.AspNetCore`, `Serilog.Enrichers.Environment`)
- **API Documentation**: Swashbuckle / Swagger (OpenAPI v1)
- **Testing**: xUnit, Moq, FluentAssertions

---

## 📂 Project Structure

```
Backend/
├── CollegeWebsitePlatform.sln
├── README.md
└── src/
    ├── CollegeWebsitePlatform.Api/
    │   ├── Controllers/
    │   │   ├── AuthController.cs            # Auth, token refresh, current profile
    │   │   ├── HealthController.cs          # Database & platform health check
    │   │   ├── PlatformController.cs        # Public platform metadata & lookup
    │   │   ├── SiteController.cs            # Public tenant website content APIs
    │   │   ├── AdminControllers.cs          # Tenant Admin CMS controllers
    │   │   └── SuperAdminControllers.cs     # SuperAdmin management controllers
    │   ├── Middleware/
    │   │   ├── CorrelationIdMiddleware.cs   # Attaches X-Correlation-ID
    │   │   ├── ExceptionHandlingMiddleware.cs# Standardized error response
    │   │   ├── TenantResolutionMiddleware.cs # Resolves tenant from hostname
    │   │   ├── TenantAuthorizationFilter.cs # Enforces tenant boundary
    │   │   └── PermissionMiddleware.cs      # Permission requirement handler
    │   ├── appsettings.json                 # Core settings & connection strings
    │   ├── appsettings.Development.json     # Dev overrides
    │   └── Program.cs                       # Dependency Injection & middleware pipeline
    │
    ├── CollegeWebsitePlatform.Application/
    │   ├── Common/Models/                   # ApiResponse<T>, PaginatedList<T>
    │   ├── DTOs/                            # Auth, CMS, Admin, SuperAdmin DTOs
    │   ├── Interfaces/                      # Services contracts
    │   └── Services/                        # Business logic implementations
    │
    ├── CollegeWebsitePlatform.Domain/
    │   ├── Constants/                       # PermissionCodes, Roles, SystemConstants
    │   └── Entities/                        # Tenant, Domain, Theme, User, Page, etc.
    │
    ├── CollegeWebsitePlatform.Infrastructure/
    │   ├── Authentication/                  # JwtTokenService, PasswordHasher
    │   ├── Persistence/                     # DatabaseMigrator, ConnectionFactories
    │   ├── Repositories/
    │   │   ├── Platform/                    # Tenant, Domain, Theme, User, Role repos
    │   │   └── Tenant/                      # Page, Section, Menu, News, Event repos
    │   ├── Provisioning/                    # Automated tenant DB provisioner
    │   └── Tenant/                          # TenantContext accessor
    │
    └── CollegeWebsitePlatform.Tests/
        ├── Unit/
        │   ├── Security/                    # Tenant isolation & security tests
        │   └── Services/                    # Auth, CMS, Provisioning unit tests
        └── Helpers/                         # Test fixtures and database helpers
```

---

## ⚙️ Configuration & Environment

Configuration is maintained in [`CollegeWebsitePlatform.Api/appsettings.json`](src/CollegeWebsitePlatform.Api/appsettings.json):

```json
{
  "AllowedHosts": "*",
  "FileUpload": {
    "MaxFileSizeBytes": 10485760,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf", ".doc", ".docx", ".xls", ".xlsx"]
  },
  "Platform": {
    "MasterConnectionString": "Host=localhost;Port=5433;Database=postgres;Username=postgres;Password=root;Include Error Detail=true",
    "PlatformDatabase": "dinamic_college_website"
  },
  "ConnectionStrings": {
    "PlatformConnection": "Host=localhost;Port=5433;Database=dinamic_college_website;Username=postgres;Password=root;Include Error Detail=true"
  },
  "Jwt": {
    "Key": "SuperSecretPlatformJwtSigningKeyForMultiTenantCollegeWebsite2026!",
    "Issuer": "CollegeWebsitePlatform",
    "Audience": "CollegeWebsitePlatformAudience",
    "ExpiryMinutes": 480,
    "RefreshTokenExpiryDays": 7
  }
}
```

> [!NOTE]
> Update the PostgreSQL connection credentials (`Port`, `Username`, `Password`) to match your local PostgreSQL server configuration.

---

## 🏁 Getting Started

### 1. Prerequisites
- **.NET 9.0 SDK** ([Download .NET 9](https://dotnet.microsoft.com/download/dotnet/9.0))
- **PostgreSQL 14+** running locally or on a remote server.
- Optional: **pgAdmin 4** or **DBeaver** for database inspection.

### 2. Clone and Restore Dependencies
```bash
# Navigate to the Backend folder
cd Backend

# Restore NuGet dependencies
dotnet restore
```

### 3. Build the Solution
```bash
dotnet build
```

### 4. Run the Backend API
```bash
dotnet run --project src/CollegeWebsitePlatform.Api
```

On application startup, the built-in `DatabaseMigrator` will automatically:
1. Create the `dinamic_college_website` platform database if it does not exist.
2. Run all platform schema migrations and table updates.
3. Seed default roles, 18 platform features, themes, templates, demo tenants, and administrator accounts.
4. Auto-provision and migrate physical tenant databases (`college_rnc`, `college_kts`).

The API server will start on **`http://localhost:5000`**.

### 5. Open Swagger Documentation
Navigate to:
🔗 **`http://localhost:5000/swagger`**

---

## 👤 Default Seed Credentials

| Role | Username | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Platform SuperAdmin** | `superadmin` | `SuperAdmin@123` | Global Platform & All Tenants |
| **RNC College Admin** | `rnc-admin` | `Admin@123` | `RNC Arts & Science College` |
| **KTS College Admin** | `kts-admin` | `Admin@123` | `KTS Institute of Tech` |

---

## 📡 API Endpoints Reference

### 1. Authentication (`/api/v1/auth`)
- `POST /api/v1/auth/login` — Authenticate and receive JWT access token & refresh token.
- `POST /api/v1/auth/refresh` — Refresh expired JWT access token.
- `POST /api/v1/auth/logout` — Revoke active refresh token.
- `GET /api/v1/auth/me` — Retrieve active user profile, assigned roles, and permissions.

### 2. Public Site APIs (`/api/v1/site`) *(Resolved via tenant host)*
- `GET /api/v1/site/config` — Tenant basic info, branding, logos, and social links.
- `GET /api/v1/site/theme` — Active theme colors, typography, and styling.
- `GET /api/v1/site/pages/{slug}` — Fetch page details & layout sections (Supports `?preview=true&preview_token={token}`).
- `GET /api/v1/site/menus/{code}` — Navigation tree structure (`HEADER_MAIN`, `FOOTER_QUICK_LINKS`, etc.).
- `GET /api/v1/site/banners` — Active homepage hero carousel slides.
- `GET /api/v1/site/news` & `GET /api/v1/site/news/{slug}` — Published campus news articles.
- `GET /api/v1/site/events` & `GET /api/v1/site/events/{id}` — Upcoming and past events.
- `GET /api/v1/site/gallery` & `GET /api/v1/site/gallery/{slug}` — Categorized photo albums.
- `GET /api/v1/site/departments` & `GET /api/v1/site/departments/{code}` — Academic departments.
- `GET /api/v1/site/courses` & `GET /api/v1/site/courses/{id}` — Academic courses and programs.
- `GET /api/v1/site/faculty` — Faculty directory with qualifications and bio.
- `GET /api/v1/site/notices` & `GET /api/v1/site/notices/{slug}` — Circulars & exam notices.
- `POST /api/v1/site/contact/submissions` — Submit student/visitor inquiries.

### 3. Scoped College Admin CMS (`/api/v1/admin/*`)
Requires `[Authorize]` with `CollegeAdmin` or specific module permissions:
- `api/v1/admin/pages` — CRUD pages, sections, and versions.
- `api/v1/admin/menus` — Navigation items ordering and targets.
- `api/v1/admin/marquee` — Flash announcement ticker items & settings.
- `api/v1/admin/news` — Campus news publishing.
- `api/v1/admin/events` — Campus events & calendar scheduling.
- `api/v1/admin/banners` — Banner slide management.
- `api/v1/admin/gallery` — Photo albums and image uploads.
- `api/v1/admin/departments` — Academic departments management.
- `api/v1/admin/courses` — Degree programs, eligibility, and fees.
- `api/v1/admin/faculty` — Faculty profiles and designations.
- `api/v1/admin/media` — Central media library file uploads.
- `api/v1/admin/settings` — College contact info, branding, and login templates.
- `api/v1/admin/contact` — Review and update visitor inquiry statuses.
- `api/v1/admin/notices` — Circulars and announcements.
- `api/v1/admin/dashboard` — Aggregated college metrics & activity feeds.
- `api/v1/admin/stats` — Highlights & numerical counter cards.
- `api/v1/admin/quote` — Principal/Director welcome address.
- `api/v1/admin/preview` — Generate secure temporary shareable preview tokens.

### 4. SuperAdmin Control Plane (`/api/v1/superadmin/*`)
Requires `[Authorize(Roles = "SuperAdmin")]`:
- `api/v1/superadmin/tenants` — Provision new colleges, edit details, activate/suspend, or soft-delete.
- `api/v1/superadmin/domains` — Register custom domains and verify DNS ownership tokens.
- `api/v1/superadmin/themes` — Create and customize global CSS palette themes.
- `api/v1/superadmin/features` — Toggle platform capabilities per tenant.
- `api/v1/superadmin/databases` — Database registry health monitoring and manual schema migration triggers.
- `api/v1/superadmin/audit` — Immutable platform audit trail logs.

### 5. Health Check (`/api/v1/health`)
- `GET /api/v1/health` — Platform DB connectivity and system status.

---

## 🧪 Running Tests

The test suite includes unit tests for authentication, CMS services, provisioning, and integration verification for tenant isolation:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

---

## 🔒 Security Best Practices

1. **Database Isolation**: No tenant query can access another college's database; connection strings are resolved per authenticated tenant context.
2. **Password Security**: High-work-factor BCrypt hashing (cost 12) for all stored credentials.
3. **Audit Trails**: Critical operations (tenant creation, user modification, domain changes) are recorded into `audit_logs`.
4. **CORS Configuration**: Dynamic localhost and authorized production tenant origin validation.
5. **Secure Headers**: Hardened HTTP response headers (`X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`).

---

## 📄 License & Maintainers

Maintained for the **Dynamic Multi-Tenant College Website Platform**.
All rights reserved.
