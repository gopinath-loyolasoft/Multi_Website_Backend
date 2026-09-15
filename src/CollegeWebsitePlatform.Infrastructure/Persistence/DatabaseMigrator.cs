using Dapper;
using Npgsql;

namespace CollegeWebsitePlatform.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static async Task InitializeAndMigrateAsync(string masterConnectionString)
    {
        var platformDbName = "dinamic_college_website";

        // 1. Ensure Platform Database exists on PostgreSQL instance
        using (var masterConn = new NpgsqlConnection(masterConnectionString))
        {
            await masterConn.OpenAsync();
            await EnsureDatabaseExistsAsync(masterConn, platformDbName);
        }

        // 2. Build platform connection string
        var builder = new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = platformDbName
        };
        var platformConnectionString = builder.ConnectionString;

        // 3. Migrate Platform Tables (dinamic_college_website)
        await MigratePlatformTablesAsync(platformConnectionString);

        // 4. Seed Platform Initial Data (Themes, Tenants, Domains, Features, Users, Roles, Permissions)
        await SeedPlatformTablesAsync(platformConnectionString, masterConnectionString);

        // 5. Ensure Demo Tenant Databases exist only if they are active in tenants table
        using (var platformConn = new NpgsqlConnection(platformConnectionString))
        {
            await platformConn.OpenAsync();

            var rncActive = await platformConn.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM tenants WHERE tenant_code = 'RNC' AND is_deleted = FALSE AND status = 'ACTIVE';");
            if (rncActive.HasValue)
            {
                await ProvisionTenantDatabaseAsync("RNC", "college_rnc", masterConnectionString, platformConnectionString, Guid.Parse("11111111-1111-1111-1111-111111111111"));
            }

            var ktsActive = await platformConn.ExecuteScalarAsync<int?>(
                "SELECT 1 FROM tenants WHERE tenant_code = 'KTS' AND is_deleted = FALSE AND status = 'ACTIVE';");
            if (ktsActive.HasValue)
            {
                await ProvisionTenantDatabaseAsync("KTS", "college_kts", masterConnectionString, platformConnectionString, Guid.Parse("22222222-2222-2222-2222-222222222222"));
            }
        }

        // 6. Run Tenant Schema Migrations across active, non-deleted tenants whose databases physically exist
        using (var platformConn = new NpgsqlConnection(platformConnectionString))
        using (var masterConn = new NpgsqlConnection(masterConnectionString))
        {
            await platformConn.OpenAsync();
            await masterConn.OpenAsync();

            var tenants = await platformConn.QueryAsync<(string cs, string templateCode)>(
                "SELECT db_connection_string, template_code FROM tenants WHERE db_connection_string IS NOT NULL AND is_deleted = FALSE AND status = 'ACTIVE';");

            foreach (var t in tenants)
            {
                try
                {
                    var cs = t.cs;
                    var csBuilder = new NpgsqlConnectionStringBuilder(cs);
                    var dbName = csBuilder.Database;

                    // Verify database physically exists on PostgreSQL instance before attempting connection
                    if (!string.IsNullOrWhiteSpace(dbName))
                    {
                        var dbExists = await masterConn.ExecuteScalarAsync<int?>(
                            "SELECT 1 FROM pg_database WHERE datname = @dbName;", new { dbName });
                        if (!dbExists.HasValue)
                        {
                            Console.WriteLine($"[Tenant Migration Note] Tenant database '{dbName}' does not exist on PostgreSQL server. Skipping.");
                            continue;
                        }
                    }

                    await MigrateTenantTablesAsync(cs);

                    // Upgrade legacy banner URLs to template-specific local SVG assets
                    var templateFolder = t.templateCode switch
                    {
                        "ARTS_SCIENCE_MODERN" => "arts",
                        "MEDICAL_MODERN" => "medical",
                        _ => "engineering"
                    };

                    using (var tConn = new NpgsqlConnection(cs))
                    {
                        await tConn.OpenAsync();
                        await tConn.ExecuteAsync(@"
                            UPDATE banners SET image_url = @b1 WHERE image_url = '/assets/banner1.jpg';
                            UPDATE banners SET image_url = @b2 WHERE image_url = '/assets/banner2.jpg';",
                            new { b1 = $"/assets/templates/{templateFolder}/banner1.svg", b2 = $"/assets/templates/{templateFolder}/banner2.svg" });

                        // Seed template-specific statistics counter cards if none exist
                        var statsCount = await tConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM stats_items WHERE is_deleted = FALSE;");
                        if (statsCount == 0)
                        {
                            if (t.templateCode == "ARTS_SCIENCE_MODERN")
                            {
                                await tConn.ExecuteAsync(@"
                                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                                    ('8,200+', '8,200', '', '+', 'Active Students', 'Users', 1, TRUE),
                                    ('42+', '42', '', '+', 'Degree Programs', 'BookOpen', 2, TRUE),
                                    ('310+', '310', '', '+', 'Faculty Mentors', 'Award', 3, TRUE),
                                    ('96.8%', '96.8', '', '%', 'Graduation Rate', 'GraduationCap', 4, TRUE);");
                            }
                            else if (t.templateCode == "MEDICAL_MODERN")
                            {
                                await tConn.ExecuteAsync(@"
                                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                                    ('1,200+', '1,200', '', '+', 'Hospital Beds', 'Building2', 1, TRUE),
                                    ('240+', '240', '', '+', 'Clinical Faculty', 'Users', 2, TRUE),
                                    ('3,500+', '3,500', '', '+', 'Daily OPD Patients', 'Users', 3, TRUE),
                                    ('28+', '28', '', '+', 'Super Specialties', 'Award', 4, TRUE);");
                            }
                            else
                            {
                                await tConn.ExecuteAsync(@"
                                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                                    ('1,800+', '1,800', '', '+', 'Graduated Students', 'GraduationCap', 1, TRUE),
                                    ('143+', '143', '', '+', 'Research Faculty', 'Users', 2, TRUE),
                                    ('63+', '63', '', '+', 'Patents Filed', 'FileCheck', 3, TRUE),
                                    ('98.4%', '98.4', '', '%', 'Placement Record', 'TrendingUp', 4, TRUE);");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Tenant Migration Note] {ex.Message}");
                }
            }
        }
    }

    private static async Task EnsureDatabaseExistsAsync(NpgsqlConnection conn, string dbName)
    {
        var checkDbSql = "SELECT 1 FROM pg_database WHERE datname = @dbName;";
        var exists = await conn.ExecuteScalarAsync<int?>(checkDbSql, new { dbName });
        if (!exists.HasValue)
        {
            await conn.ExecuteAsync($"CREATE DATABASE \"{dbName}\";");
        }
    }

    public static async Task MigratePlatformTablesAsync(string platformConnectionString)
    {
        using var conn = new NpgsqlConnection(platformConnectionString);
        await conn.OpenAsync();

        // Check if existing tenants table uses legacy integer id
        var idType = await conn.ExecuteScalarAsync<string>(@"
            SELECT data_type FROM information_schema.columns 
            WHERE table_name = 'tenants' AND column_name = 'id';");

        if (idType != null && !string.Equals(idType, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DB Migration] Detected legacy integer platform tables. Dropping to re-create with UUID...");
            await conn.ExecuteAsync(@"
                DROP TABLE IF EXISTS audit_logs CASCADE;
                DROP TABLE IF EXISTS user_tenants CASCADE;
                DROP TABLE IF EXISTS role_permissions CASCADE;
                DROP TABLE IF EXISTS refresh_tokens CASCADE;
                DROP TABLE IF EXISTS users CASCADE;
                DROP TABLE IF EXISTS permissions CASCADE;
                DROP TABLE IF EXISTS roles CASCADE;
                DROP TABLE IF EXISTS tenant_features CASCADE;
                DROP TABLE IF EXISTS features CASCADE;
                DROP TABLE IF EXISTS tenant_database_registry CASCADE;
                DROP TABLE IF EXISTS domains CASCADE;
                DROP TABLE IF EXISTS themes CASCADE;
                DROP TABLE IF EXISTS platform_settings CASCADE;
                DROP TABLE IF EXISTS tenants CASCADE;
            ");
        }

        var migrationSql = @"
        CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

        CREATE TABLE IF NOT EXISTS tenants (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_code VARCHAR(50) UNIQUE NOT NULL,
            name VARCHAR(150) NOT NULL,
            slug VARCHAR(100) UNIQUE NOT NULL,
            db_connection_string TEXT NOT NULL,
            status VARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
            default_theme_id UUID NULL,
            login_template VARCHAR(50) DEFAULT 'template2',
            provisioning_status VARCHAR(50) NOT NULL DEFAULT 'COMPLETED',
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );
        ALTER TABLE tenants ADD COLUMN IF NOT EXISTS login_template VARCHAR(50) DEFAULT 'template2';

        CREATE TABLE IF NOT EXISTS domains (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
            domain VARCHAR(255) UNIQUE NOT NULL,
            domain_type VARCHAR(50) NOT NULL DEFAULT 'PRIMARY',
            is_primary BOOLEAN NOT NULL DEFAULT FALSE,
            is_verified BOOLEAN NOT NULL DEFAULT FALSE,
            verification_token VARCHAR(100),
            verified_at TIMESTAMP WITH TIME ZONE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS themes (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            name VARCHAR(100) NOT NULL,
            code VARCHAR(50) UNIQUE NOT NULL,
            configuration JSONB NOT NULL,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS users (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NULL REFERENCES tenants(id) ON DELETE CASCADE,
            username VARCHAR(100) UNIQUE NOT NULL,
            email VARCHAR(255) UNIQUE NOT NULL,
            password_hash VARCHAR(255) NOT NULL,
            full_name VARCHAR(150) NOT NULL,
            role VARCHAR(50) NOT NULL DEFAULT 'CollegeAdmin',
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS roles (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            name VARCHAR(100) NOT NULL,
            code VARCHAR(50) UNIQUE NOT NULL,
            description TEXT,
            is_system BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS permissions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            code VARCHAR(100) UNIQUE NOT NULL,
            name VARCHAR(150) NOT NULL,
            module VARCHAR(50) NOT NULL,
            description TEXT,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS role_permissions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
            permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
            CONSTRAINT uq_role_permission UNIQUE (role_id, permission_id)
        );

        CREATE TABLE IF NOT EXISTS user_tenants (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
            role_id UUID NULL REFERENCES roles(id) ON DELETE SET NULL,
            is_primary BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT uq_user_tenant UNIQUE (user_id, tenant_id)
        );

        CREATE TABLE IF NOT EXISTS refresh_tokens (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            token VARCHAR(500) UNIQUE NOT NULL,
            expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
            is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS features (
            code VARCHAR(50) PRIMARY KEY,
            name VARCHAR(100) NOT NULL,
            description TEXT,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS tenant_features (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
            feature_code VARCHAR(50) NOT NULL REFERENCES features(code) ON DELETE CASCADE,
            is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT uq_tenant_feature UNIQUE (tenant_id, feature_code)
        );

        CREATE TABLE IF NOT EXISTS tenant_database_registry (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
            database_name VARCHAR(100) UNIQUE NOT NULL,
            host VARCHAR(100) NOT NULL DEFAULT 'localhost',
            port INT NOT NULL DEFAULT 5433,
            db_user VARCHAR(100) NOT NULL DEFAULT 'postgres',
            status VARCHAR(50) NOT NULL DEFAULT 'HEALTHY',
            current_migration_version VARCHAR(20) NOT NULL DEFAULT 'V8',
            last_health_check_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS audit_logs (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            user_id UUID NULL,
            username VARCHAR(100) NOT NULL DEFAULT 'system',
            tenant_id UUID NULL,
            action VARCHAR(100) NOT NULL,
            entity_type VARCHAR(100) NOT NULL,
            entity_id VARCHAR(100),
            details TEXT,
            ip_address VARCHAR(50),
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS platform_settings (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            setting_key VARCHAR(100) UNIQUE NOT NULL,
            setting_value TEXT NOT NULL,
            description TEXT,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS templates (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            code VARCHAR(50) UNIQUE NOT NULL,
            name VARCHAR(150) NOT NULL,
            category VARCHAR(50) NOT NULL,
            description TEXT,
            recommended_features JSONB NOT NULL DEFAULT '[]'::jsonb,
            starter_pages JSONB NOT NULL DEFAULT '[]'::jsonb,
            starter_menus JSONB NOT NULL DEFAULT '[]'::jsonb,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        ALTER TABLE templates ADD COLUMN IF NOT EXISTS default_theme_id UUID NULL REFERENCES themes(id) ON DELETE SET NULL;
        ALTER TABLE templates ADD COLUMN IF NOT EXISTS supported_themes JSONB NOT NULL DEFAULT '[]'::jsonb;
        ALTER TABLE features ADD COLUMN IF NOT EXISTS id UUID DEFAULT gen_random_uuid();
        UPDATE features SET id = gen_random_uuid() WHERE id IS NULL;
        ALTER TABLE features ADD COLUMN IF NOT EXISTS category VARCHAR(50) NOT NULL DEFAULT 'CMS';
        ALTER TABLE features ADD COLUMN IF NOT EXISTS icon VARCHAR(50) NOT NULL DEFAULT 'cube';
        ALTER TABLE features ADD COLUMN IF NOT EXISTS sort_order INT NOT NULL DEFAULT 0;
        ALTER TABLE features ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;

        ALTER TABLE domains ADD COLUMN IF NOT EXISTS verification_token VARCHAR(100);
        ALTER TABLE domains ADD COLUMN IF NOT EXISTS verified_at TIMESTAMP WITH TIME ZONE;
        ALTER TABLE domains ADD COLUMN IF NOT EXISTS is_verified BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE domains ADD COLUMN IF NOT EXISTS is_primary BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE domains ADD COLUMN IF NOT EXISTS domain_type VARCHAR(50) NOT NULL DEFAULT 'PRIMARY';

        -- Platform Soft Delete Columns
        ALTER TABLE tenants ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE tenants ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE domains ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE domains ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE themes ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE themes ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE users ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE users ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE roles ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE roles ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE tenant_database_registry ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE tenant_database_registry ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;";

        await conn.ExecuteAsync(migrationSql);
    }

    public static async Task MigrateTenantTablesAsync(string tenantConnectionString)
    {
        using var conn = new NpgsqlConnection(tenantConnectionString);
        await conn.OpenAsync();

        // Check if existing pages table uses legacy integer id
        var idType = await conn.ExecuteScalarAsync<string>(@"
            SELECT data_type FROM information_schema.columns 
            WHERE table_name = 'pages' AND column_name = 'id';");

        if (idType != null && !string.Equals(idType, "uuid", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[DB Migration] Detected legacy integer tenant tables. Dropping to re-create with UUID...");
            await conn.ExecuteAsync(@"
                DROP TABLE IF EXISTS page_versions CASCADE;
                DROP TABLE IF EXISTS page_sections CASCADE;
                DROP TABLE IF EXISTS menu_items CASCADE;
                DROP TABLE IF EXISTS menus CASCADE;
                DROP TABLE IF EXISTS news CASCADE;
                DROP TABLE IF EXISTS events CASCADE;
                DROP TABLE IF EXISTS banners CASCADE;
                DROP TABLE IF EXISTS gallery_images CASCADE;
                DROP TABLE IF EXISTS galleries CASCADE;
                DROP TABLE IF EXISTS faculty CASCADE;
                DROP TABLE IF EXISTS courses CASCADE;
                DROP TABLE IF EXISTS departments CASCADE;
                DROP TABLE IF EXISTS testimonials CASCADE;
                DROP TABLE IF EXISTS media CASCADE;
                DROP TABLE IF EXISTS tenant_settings CASCADE;
                DROP TABLE IF EXISTS contact_submissions CASCADE;
                DROP TABLE IF EXISTS pages CASCADE;
            ");
        }

        var migrationSql = @"
        CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

        CREATE TABLE IF NOT EXISTS pages (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            slug VARCHAR(100) UNIQUE NOT NULL,
            title VARCHAR(255) NOT NULL,
            meta_description TEXT,
            is_published BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS page_sections (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            page_id UUID NOT NULL REFERENCES pages(id) ON DELETE CASCADE,
            section_type VARCHAR(50) NOT NULL,
            title VARCHAR(255),
            subtitle VARCHAR(255),
            content_json JSONB NOT NULL DEFAULT '{}'::jsonb,
            sort_order INT NOT NULL DEFAULT 0,
            is_visible BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS page_versions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            page_id UUID NOT NULL REFERENCES pages(id) ON DELETE CASCADE,
            version_number INT NOT NULL,
            title VARCHAR(255) NOT NULL,
            slug VARCHAR(255) NOT NULL,
            meta_description TEXT,
            content_json JSONB NOT NULL DEFAULT '[]'::jsonb,
            created_by VARCHAR(100) NOT NULL DEFAULT 'admin',
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT uq_page_version UNIQUE (page_id, version_number)
        );

        CREATE TABLE IF NOT EXISTS menus (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            code VARCHAR(50) UNIQUE NOT NULL,
            title VARCHAR(100) NOT NULL,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS menu_items (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            menu_id UUID NOT NULL REFERENCES menus(id) ON DELETE CASCADE,
            parent_id UUID NULL REFERENCES menu_items(id) ON DELETE CASCADE,
            item_type VARCHAR(50) NOT NULL DEFAULT 'PAGE',
            page_id UUID NULL REFERENCES pages(id) ON DELETE SET NULL,
            title VARCHAR(100) NOT NULL,
            url VARCHAR(255) NULL,
            sort_order INT NOT NULL DEFAULT 0,
            is_visible BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS news (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            title VARCHAR(255) NOT NULL,
            slug VARCHAR(255) UNIQUE NOT NULL,
            summary TEXT NOT NULL,
            content TEXT NOT NULL,
            image_url VARCHAR(500),
            published_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            is_published BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS events (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            title VARCHAR(255) NOT NULL,
            description TEXT NOT NULL,
            event_date TIMESTAMP WITH TIME ZONE NOT NULL,
            location VARCHAR(255) NOT NULL,
            image_url VARCHAR(500),
            is_published BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS banners (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            title VARCHAR(255) NOT NULL,
            subtitle VARCHAR(255),
            image_url VARCHAR(500) NOT NULL,
            cta_text VARCHAR(100),
            cta_url VARCHAR(255),
            badge_text VARCHAR(150),
            secondary_cta_text VARCHAR(100),
            secondary_cta_url VARCHAR(255),
            pillars_json TEXT,
            sort_order INT NOT NULL DEFAULT 0,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );
        ALTER TABLE banners ADD COLUMN IF NOT EXISTS badge_text VARCHAR(150);
        ALTER TABLE banners ADD COLUMN IF NOT EXISTS secondary_cta_text VARCHAR(100);
        ALTER TABLE banners ADD COLUMN IF NOT EXISTS secondary_cta_url VARCHAR(255);
        ALTER TABLE banners ADD COLUMN IF NOT EXISTS pillars_json TEXT;

        CREATE TABLE IF NOT EXISTS galleries (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            title VARCHAR(255) NOT NULL,
            slug VARCHAR(255) UNIQUE NOT NULL,
            description TEXT,
            cover_image_url VARCHAR(500),
            category VARCHAR(100) DEFAULT 'Campus',
            is_published BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );
        ALTER TABLE galleries ADD COLUMN IF NOT EXISTS category VARCHAR(100) DEFAULT 'Campus';

        CREATE TABLE IF NOT EXISTS gallery_images (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            gallery_id UUID NOT NULL REFERENCES galleries(id) ON DELETE CASCADE,
            image_url VARCHAR(500) NOT NULL,
            caption VARCHAR(255),
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS departments (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            name VARCHAR(255) NOT NULL,
            code VARCHAR(50) UNIQUE NOT NULL,
            description TEXT,
            head_of_department VARCHAR(150),
            email VARCHAR(150),
            phone VARCHAR(50),
            image_url VARCHAR(500),
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS courses (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            department_id UUID NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
            name VARCHAR(255) NOT NULL,
            code VARCHAR(50) UNIQUE NOT NULL,
            degree_level VARCHAR(50) NOT NULL DEFAULT 'UNDERGRADUATE',
            duration_years NUMERIC(3,1) NOT NULL DEFAULT 3.0,
            description TEXT,
            eligibility TEXT,
            fee_structure TEXT,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS faculty (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            department_id UUID NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
            name VARCHAR(150) NOT NULL,
            designation VARCHAR(150) NOT NULL,
            qualification VARCHAR(255) NOT NULL,
            specialization VARCHAR(255),
            email VARCHAR(150),
            profile_image_url VARCHAR(500),
            bio TEXT,
            sort_order INT NOT NULL DEFAULT 0,
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS testimonials (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            student_name VARCHAR(150) NOT NULL,
            batch_year VARCHAR(50),
            course_name VARCHAR(150),
            content TEXT NOT NULL,
            photo_url VARCHAR(500),
            current_company VARCHAR(150),
            rating INT NOT NULL DEFAULT 5,
            is_published BOOLEAN NOT NULL DEFAULT TRUE,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS media (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            file_name VARCHAR(255) NOT NULL,
            file_path VARCHAR(500) NOT NULL,
            content_type VARCHAR(100) NOT NULL,
            file_size_bytes BIGINT NOT NULL,
            alt_text VARCHAR(255),
            uploaded_by VARCHAR(100) NOT NULL DEFAULT 'admin',
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS tenant_settings (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            site_name VARCHAR(200) NOT NULL,
            tagline VARCHAR(300),
            logo_url VARCHAR(500),
            contact_email VARCHAR(150),
            contact_phone VARCHAR(50),
            address TEXT,
            social_links JSONB DEFAULT '{}'::jsonb,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS contact_submissions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            name VARCHAR(150) NOT NULL,
            email VARCHAR(150) NOT NULL,
            phone VARCHAR(50),
            subject VARCHAR(255) NOT NULL,
            message TEXT NOT NULL,
            status VARCHAR(50) NOT NULL DEFAULT 'NEW',
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS notices (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            title VARCHAR(255) NOT NULL,
            slug VARCHAR(255) UNIQUE NOT NULL,
            summary TEXT NOT NULL,
            content TEXT NOT NULL,
            pdf_url VARCHAR(500),
            category VARCHAR(50) NOT NULL DEFAULT 'GENERAL',
            publish_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            expiry_date TIMESTAMP WITH TIME ZONE,
            is_published BOOLEAN NOT NULL DEFAULT FALSE,
            is_important BOOLEAN NOT NULL DEFAULT FALSE,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        -- Tenant Tables Soft Delete Columns
        ALTER TABLE pages ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE pages ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE page_sections ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE page_sections ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE menus ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE menus ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE menu_items ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE menu_items ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE news ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE news ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE events ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE events ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE banners ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE banners ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE galleries ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE galleries ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE gallery_images ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE gallery_images ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE departments ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE departments ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE courses ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE courses ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE faculty ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE faculty ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE testimonials ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE testimonials ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE media ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE media ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE contact_submissions ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE contact_submissions ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        ALTER TABLE notices ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE notices ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE NULL;

        -- Marquee Bar Settings & Items
        CREATE TABLE IF NOT EXISTS marquee_settings (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            speed VARCHAR(20) NOT NULL DEFAULT 'medium',
            pause_on_hover BOOLEAN NOT NULL DEFAULT TRUE,
            bg_color VARCHAR(50) DEFAULT '#0f172a',
            text_color VARCHAR(50) DEFAULT '#f8fafc',
            badge_bg_color VARCHAR(50) DEFAULT '#f59e0b',
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS marquee_items (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            badge_text VARCHAR(50),
            message TEXT NOT NULL,
            link_url VARCHAR(255),
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
            deleted_at TIMESTAMP WITH TIME ZONE NULL
        );

        -- Marquee Items Schedule Enhancements
        ALTER TABLE marquee_items ADD COLUMN IF NOT EXISTS start_date TIMESTAMP WITH TIME ZONE NULL;
        ALTER TABLE marquee_items ADD COLUMN IF NOT EXISTS end_date TIMESTAMP WITH TIME ZONE NULL;

        -- Menu Items Enhancements
        ALTER TABLE menu_items ADD COLUMN IF NOT EXISTS target_type VARCHAR(50) NOT NULL DEFAULT 'DYNAMIC_MODULE';
        ALTER TABLE menu_items ADD COLUMN IF NOT EXISTS open_in_new_tab BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE menu_items ADD COLUMN IF NOT EXISTS icon VARCHAR(50) DEFAULT 'FileText';

        -- Key Highlights & Statistics Bar Items (CMS360 Style)
        CREATE TABLE IF NOT EXISTS stats_items (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            metric VARCHAR(50) NOT NULL,
            value VARCHAR(50),
            prefix VARCHAR(20) DEFAULT '',
            suffix VARCHAR(20) DEFAULT '',
            label VARCHAR(150) NOT NULL,
            icon VARCHAR(50) DEFAULT 'Award',
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            sort_order INT NOT NULL DEFAULT 0,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
            deleted_at TIMESTAMP WITH TIME ZONE NULL
        );

        ALTER TABLE stats_items ADD COLUMN IF NOT EXISTS value VARCHAR(50);
        ALTER TABLE stats_items ADD COLUMN IF NOT EXISTS prefix VARCHAR(20) DEFAULT '';
        ALTER TABLE stats_items ADD COLUMN IF NOT EXISTS suffix VARCHAR(20) DEFAULT '';

        UPDATE stats_items SET value = metric WHERE value IS NULL;
        UPDATE stats_items SET prefix = '' WHERE prefix IS NULL;
        UPDATE stats_items SET suffix = '' WHERE suffix IS NULL;

        -- Quote / Welcome Message Management
        CREATE TABLE IF NOT EXISTS quote_settings (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            quote_text TEXT NOT NULL,
            author_name VARCHAR(150) NOT NULL,
            designation VARCHAR(150) NOT NULL,
            author_image_url VARCHAR(500),
            is_active BOOLEAN NOT NULL DEFAULT TRUE,
            updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );

        ALTER TABLE quote_settings ADD COLUMN IF NOT EXISTS sub_text VARCHAR(250) DEFAULT '';

        UPDATE quote_settings 
        SET author_image_url = '/assets/templates/common/leader_portrait.svg' 
        WHERE author_image_url = '/assets/principal.jpg' 
           OR author_image_url IS NULL 
           OR author_image_url = '';

        -- Soft Delete Partial Unique Indexes
        CREATE UNIQUE INDEX IF NOT EXISTS idx_pages_slug_not_deleted ON pages (LOWER(slug)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_news_slug_not_deleted ON news (LOWER(slug)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_departments_code_not_deleted ON departments (LOWER(code)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_courses_code_not_deleted ON courses (department_id, LOWER(code)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_galleries_slug_not_deleted ON galleries (LOWER(slug)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_notices_slug_not_deleted ON notices (LOWER(slug)) WHERE is_deleted = FALSE;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_menus_code_not_deleted ON menus (LOWER(code)) WHERE is_deleted = FALSE;

        -- Section Settings, Publishing Lifecycles, and Preview Sessions
        ALTER TABLE page_sections ADD COLUMN IF NOT EXISTS settings_json JSONB NOT NULL DEFAULT '{}'::jsonb;

        ALTER TABLE courses ADD COLUMN IF NOT EXISTS is_published BOOLEAN NOT NULL DEFAULT TRUE;
        ALTER TABLE courses ADD COLUMN IF NOT EXISTS published_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE courses ADD COLUMN IF NOT EXISTS published_by VARCHAR(100) DEFAULT 'admin';

        ALTER TABLE departments ADD COLUMN IF NOT EXISTS is_published BOOLEAN NOT NULL DEFAULT TRUE;
        ALTER TABLE departments ADD COLUMN IF NOT EXISTS published_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE departments ADD COLUMN IF NOT EXISTS published_by VARCHAR(100) DEFAULT 'admin';

        ALTER TABLE faculty ADD COLUMN IF NOT EXISTS is_published BOOLEAN NOT NULL DEFAULT TRUE;
        ALTER TABLE faculty ADD COLUMN IF NOT EXISTS published_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
        ALTER TABLE faculty ADD COLUMN IF NOT EXISTS published_by VARCHAR(100) DEFAULT 'admin';

        ALTER TABLE testimonials ADD COLUMN IF NOT EXISTS is_published BOOLEAN NOT NULL DEFAULT TRUE;
        ALTER TABLE testimonials ADD COLUMN IF NOT EXISTS published_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;

        CREATE TABLE IF NOT EXISTS preview_sessions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            preview_token VARCHAR(255) UNIQUE NOT NULL,
            created_by VARCHAR(100) NOT NULL DEFAULT 'admin',
            expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
        );
        CREATE INDEX IF NOT EXISTS idx_preview_token ON preview_sessions(preview_token);

        -- V9 Enhancements: Favicon, Course Tuition Fee & Syllabus PDF, Admission Inquiries, Login Template, Login Bg Image
        ALTER TABLE tenant_settings ADD COLUMN IF NOT EXISTS favicon_url VARCHAR(500) DEFAULT '/favicon.ico';
        ALTER TABLE tenant_settings ADD COLUMN IF NOT EXISTS login_template VARCHAR(50) DEFAULT 'template2';
        ALTER TABLE tenant_settings ADD COLUMN IF NOT EXISTS login_bg_image_url TEXT;
        ALTER TABLE courses ADD COLUMN IF NOT EXISTS tuition_fee VARCHAR(100) DEFAULT '$5,000 / Semester';
        ALTER TABLE courses ADD COLUMN IF NOT EXISTS syllabus_pdf_url VARCHAR(500);

        CREATE TABLE IF NOT EXISTS admission_inquiries (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            student_name VARCHAR(150) NOT NULL,
            email VARCHAR(150) NOT NULL,
            phone_number VARCHAR(50) NOT NULL,
            program_id UUID REFERENCES courses(id) ON DELETE SET NULL,
            program_name VARCHAR(255) NOT NULL,
            notes TEXT,
            status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
            is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
            deleted_at TIMESTAMP WITH TIME ZONE NULL
        );
        CREATE INDEX IF NOT EXISTS idx_admission_inquiries_status ON admission_inquiries(status);";

        await conn.ExecuteAsync(migrationSql);
    }

    public static async Task<string> ProvisionTenantDatabaseAsync(
        string tenantCode, 
        string dbName, 
        string masterConnectionString, 
        string platformConnectionString, 
        Guid tenantId,
        string templateCode = "ENGINEERING_MODERN",
        string? collegeName = null)
    {
        // 1. Create physical DB on PostgreSQL
        using (var masterConn = new NpgsqlConnection(masterConnectionString))
        {
            await masterConn.OpenAsync();
            await EnsureDatabaseExistsAsync(masterConn, dbName);
        }

        // 2. Build tenant connection string
        var builder = new NpgsqlConnectionStringBuilder(masterConnectionString)
        {
            Database = dbName
        };
        var tenantCs = builder.ConnectionString;

        // 3. Migrate tenant schema tables (V1..V8)
        await MigrateTenantTablesAsync(tenantCs);

        // 4. Update tenants table & tenant_database_registry in Platform DB
        using (var platformConn = new NpgsqlConnection(platformConnectionString))
        {
            await platformConn.OpenAsync();

            await platformConn.ExecuteAsync(@"
                UPDATE tenants 
                SET db_connection_string = @tenantCs, provisioning_status = 'COMPLETED', updated_at = CURRENT_TIMESTAMP 
                WHERE id = @tenantId;",
                new { tenantCs, tenantId });

            await platformConn.ExecuteAsync(@"
                INSERT INTO tenant_database_registry (tenant_id, database_name, host, port, db_user, status, current_migration_version, last_health_check_at)
                VALUES (@tenantId, @dbName, @host, @port, @user, 'HEALTHY', 'V8', CURRENT_TIMESTAMP)
                ON CONFLICT (database_name) DO UPDATE 
                SET status = 'HEALTHY', last_health_check_at = CURRENT_TIMESTAMP, current_migration_version = 'V8';",
                new { tenantId, dbName, host = builder.Host, port = builder.Port, user = builder.Username });
        }

        // 5. Seed initial content if tenant is new
        await SeedInitialTenantContentAsync(tenantCs, tenantCode, templateCode, collegeName);

        return tenantCs;
    }

    private static async Task SeedPlatformTablesAsync(string platformCs, string masterCs)
    {
        using var conn = new NpgsqlConnection(platformCs);
        await conn.OpenAsync();

        var themeBlueId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var themeEmeraldId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var themeTealId = Guid.Parse("55555555-5555-5555-5555-555555555550");
        var themeCrimsonId = Guid.Parse("66666666-6666-6666-6666-666666666660");

        // 1. Themes
        var blueThemeJson = @"{
            ""themeName"": ""Royal Blue & Gold"",
            ""primaryColor"": ""#1e40af"",
            ""secondaryColor"": ""#f59e0b"",
            ""accentColor"": ""#3b82f6"",
            ""fontFamily"": ""Inter, sans-serif"",
            ""headerStyle"": ""solid"",
            ""badgeText"": ""Excellence in Higher Education""
        }";

        var emeraldThemeJson = @"{
            ""themeName"": ""Emerald & Crimson"",
            ""primaryColor"": ""#065f46"",
            ""secondaryColor"": ""#dc2626"",
            ""accentColor"": ""#10b981"",
            ""fontFamily"": ""Merriweather, serif"",
            ""headerStyle"": ""bordered"",
            ""badgeText"": ""Inspiring Leaders of Tomorrow""
        }";

        var tealThemeJson = @"{
            ""themeName"": ""Clinical Teal & Cyan"",
            ""primaryColor"": ""#0f766e"",
            ""secondaryColor"": ""#06b6d4"",
            ""accentColor"": ""#14b8a6"",
            ""fontFamily"": ""Plus Jakarta Sans, sans-serif"",
            ""headerStyle"": ""solid"",
            ""badgeText"": ""Dedicated to Healing, Research & Care""
        }";

        var crimsonThemeJson = @"{
            ""themeName"": ""Crimson & Oxford Navy"",
            ""primaryColor"": ""#991b1b"",
            ""secondaryColor"": ""#1e293b"",
            ""accentColor"": ""#d97706"",
            ""fontFamily"": ""Playfair Display, serif"",
            ""headerStyle"": ""bordered"",
            ""badgeText"": ""Century of Global Academic Distinction""
        }";

        await conn.ExecuteAsync(@"
            INSERT INTO themes (id, name, code, configuration, is_active) VALUES 
            (@themeBlueId, 'Modern Royal Blue', 'THEME_BLUE', @blueThemeJson::jsonb, TRUE),
            (@themeEmeraldId, 'Classic Emerald Forest', 'THEME_EMERALD', @emeraldThemeJson::jsonb, TRUE),
            (@themeTealId, 'Clinical Healthcare Teal', 'THEME_TEAL', @tealThemeJson::jsonb, TRUE),
            (@themeCrimsonId, 'Academic Crimson & Navy', 'THEME_CRIMSON', @crimsonThemeJson::jsonb, TRUE)
            ON CONFLICT (code) DO UPDATE SET
                name = EXCLUDED.name,
                configuration = EXCLUDED.configuration,
                is_active = EXCLUDED.is_active;",
            new { themeBlueId, themeEmeraldId, themeTealId, themeCrimsonId, blueThemeJson, emeraldThemeJson, tealThemeJson, crimsonThemeJson });

        // 2. Roles & Permissions
        var roleSuperAdminId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var roleCollegeAdminId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var roleEditorId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        await conn.ExecuteAsync(@"
            INSERT INTO roles (id, name, code, description, is_system) VALUES 
            (@roleSuperAdminId, 'Platform Super Administrator', 'SuperAdmin', 'Unrestricted master access across all platform tenants, databases, and global settings', TRUE),
            (@roleCollegeAdminId, 'College Principal Administrator', 'CollegeAdmin', 'Scoped administrative rights to manage own college website, pages, news, and staff', TRUE),
            (@roleEditorId, 'Content Editor', 'Editor', 'Scoped access to edit pages, news, events, and media without administrative settings rights', TRUE)
            ON CONFLICT (code) DO NOTHING;",
            new { roleSuperAdminId, roleCollegeAdminId, roleEditorId });

        await conn.ExecuteAsync(@"
            INSERT INTO permissions (code, name, module, description) VALUES 
            ('PAGE_VIEW', 'View Pages', 'PAGES', 'Permission to view draft and published pages'),
            ('PAGE_MANAGE', 'Manage Pages', 'PAGES', 'Permission to create, update, and delete page sections'),
            ('PAGE_PUBLISH', 'Publish Pages', 'PAGES', 'Permission to toggle page publication and versions'),
            ('NEWS_MANAGE', 'Manage News', 'NEWS', 'Permission to author and publish campus news articles'),
            ('EVENT_MANAGE', 'Manage Events', 'EVENTS', 'Permission to manage campus calendar events'),
            ('BANNER_MANAGE', 'Manage Banners', 'BANNERS', 'Permission to manage hero and promotional banners'),
            ('GALLERY_MANAGE', 'Manage Gallery', 'GALLERY', 'Permission to upload campus photo albums'),
            ('ACADEMIC_MANAGE', 'Manage Academics', 'ACADEMICS', 'Permission to manage departments, courses, and faculty'),
            ('MEDIA_MANAGE', 'Manage Media', 'MEDIA', 'Permission to upload and delete files in media library'),
            ('MENU_MANAGE', 'Manage Menus', 'MENUS', 'Permission to create and arrange navigation hierarchy'),
            ('SETTINGS_MANAGE', 'Manage Tenant Settings', 'SETTINGS', 'Permission to update college profile and contact details'),
            ('CONTACT_MANAGE', 'Manage Contact Inquiries', 'CONTACT', 'Permission to review and update contact submission status'),
            ('TENANT_MANAGE', 'Manage Platform Tenants', 'SUPERADMIN', 'SuperAdmin privilege to provision and suspend colleges'),
            ('USER_MANAGE', 'Manage Users & Assignments', 'SUPERADMIN', 'SuperAdmin privilege to assign administrators to colleges'),
            ('DATABASE_MANAGE', 'Manage Databases & Migrations', 'SUPERADMIN', 'SuperAdmin privilege to run migrations and health checks')
            ON CONFLICT (code) DO NOTHING;");

        // Role permissions - SuperAdmin gets all permissions
        await conn.ExecuteAsync(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
            WHERE r.code = 'SuperAdmin'
            ON CONFLICT (role_id, permission_id) DO NOTHING;");

        // Role permissions - CollegeAdmin gets CMS and Academics permissions
        await conn.ExecuteAsync(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
            WHERE r.code = 'CollegeAdmin'
            AND p.module IN ('PAGES', 'NEWS', 'EVENTS', 'BANNERS', 'GALLERY', 'ACADEMIC', 'ACADEMICS', 'MEDIA', 'MENUS', 'SETTINGS', 'CONTACT')
            ON CONFLICT (role_id, permission_id) DO NOTHING;");

        // Role permissions - Content Editor
        await conn.ExecuteAsync(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r CROSS JOIN permissions p
            WHERE r.code = 'Editor'
            AND p.module IN ('PAGES', 'NEWS', 'EVENTS', 'BANNERS', 'GALLERY', 'ACADEMIC', 'ACADEMICS', 'MEDIA')
            ON CONFLICT (role_id, permission_id) DO NOTHING;");

        // 3. Features (All 18 categorized platform features)
        await conn.ExecuteAsync(@"
            INSERT INTO features (code, name, description, category, icon, sort_order, is_active) VALUES 
            ('PAGES', 'Pages Management', 'Manage custom dynamic web pages and sections', 'CMS', 'FileText', 1, TRUE),
            ('SECTIONS', 'Page Sections', 'Modular section building blocks for custom layouts', 'CMS', 'Layout', 2, TRUE),
            ('MENUS', 'Navigation Menus', 'Multi-level header and footer navigation menus', 'CMS', 'Menu', 3, TRUE),
            ('BANNERS', 'Hero & Promo Banners', 'Rotational image banners and calls-to-action', 'Content', 'Image', 4, TRUE),
            ('MARQUEE', 'Ticker & Alerts', 'Scrolling announcement tickers and breaking updates', 'CMS', 'Megaphone', 5, TRUE),
            ('NEWS', 'Campus News', 'Publish articles, press releases, and campus achievements', 'Content', 'Newspaper', 6, TRUE),
            ('EVENTS', 'Events & Calendar', 'Conferences, symposia, and academic calendar dates', 'Content', 'Calendar', 7, TRUE),
            ('NOTICES', 'Official Notices', 'Circulars, PDF announcements, and student notifications', 'Content', 'Bell', 8, TRUE),
            ('DEPARTMENTS', 'Academic Departments', 'Departmental profiles, vision, and head of department info', 'Academic', 'Building2', 9, TRUE),
            ('COURSES', 'Courses & Programs', 'Undergraduate, graduate, and doctoral degree programs', 'Academic', 'BookOpen', 10, TRUE),
            ('FACULTY', 'Faculty Directory', 'Academic staff profiles, qualifications, and research areas', 'Academic', 'Users', 11, TRUE),
            ('GALLERY', 'Photo Galleries', 'Campus infrastructure, labs, sports, and festival albums', 'Media', 'Camera', 12, TRUE),
            ('MEDIA', 'Media Library', 'Centralized document, image, and asset storage', 'Media', 'Folder', 13, TRUE),
            ('TESTIMONIALS', 'Student Testimonials', 'Alumni quotes, ratings, and student reviews', 'Content', 'MessageSquare', 14, TRUE),
            ('CONTACT', 'Contact & Inquiries', 'Campus address, inquiry submission forms, and map', 'Communication', 'Mail', 15, TRUE),
            ('STATS', 'Milestones & Counters', 'Accreditations and metric stats', 'Content', 'BarChart2', 16, TRUE),
            ('QUOTE', 'Principal / Dean Quote', 'Leadership message and official greeting block', 'Content', 'Quote', 17, TRUE)
            ON CONFLICT (code) DO UPDATE SET
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                category = EXCLUDED.category,
                icon = EXCLUDED.icon,
                sort_order = EXCLUDED.sort_order,
                updated_at = CURRENT_TIMESTAMP;

            -- Ensure all registered tenants have all features enabled in tenant_features
            INSERT INTO tenant_features (tenant_id, feature_code, is_enabled)
            SELECT t.id, f.code, TRUE
            FROM tenants t
            CROSS JOIN features f
            WHERE f.is_active = TRUE
            ON CONFLICT (tenant_id, feature_code) DO NOTHING;");

        // 3b. Seed Standard Templates
        await conn.ExecuteAsync(@"
            INSERT INTO templates (code, name, category, description, recommended_features, default_theme_id, starter_pages, starter_menus, is_active) VALUES
            (
                'ENGINEERING_MODERN',
                'Modern Engineering & Technology',
                'ENGINEERING',
                'Tailored for engineering, polytechnic, and technical colleges with lab highlights, research metrics, and accreditation showcases.',
                '[""PAGES"",""MENUS"",""BANNERS"",""NEWS"",""EVENTS"",""NOTICES"",""DEPARTMENTS"",""COURSES"",""FACULTY"",""GALLERY"",""MEDIA"",""CONTACT"",""STATS"",""MARQUEE""]'::jsonb,
                @themeBlueId,
                '[]'::jsonb,
                '[]'::jsonb,
                TRUE
            ),
            (
                'ARTS_SCIENCE_MODERN',
                'Classic Arts & Science Academy',
                'ARTS_SCIENCE',
                'Designed for multi-disciplinary liberal arts, science, and commerce institutions with scholarly publications, faculty portfolios, and rich cultural galleries.',
                '[""PAGES"",""MENUS"",""BANNERS"",""NEWS"",""EVENTS"",""NOTICES"",""DEPARTMENTS"",""COURSES"",""FACULTY"",""GALLERY"",""MEDIA"",""TESTIMONIALS"",""CONTACT"",""QUOTE"",""MARQUEE""]'::jsonb,
                @themeEmeraldId,
                '[]'::jsonb,
                '[]'::jsonb,
                TRUE
            ),
            (
                'MEDICAL_MODERN',
                'Clinical Healthcare & Medical Sciences',
                'MEDICAL',
                'Built for medical colleges, dental academies, and nursing schools featuring clinical rotatory schedules, hospital OPD statistics, trauma care units, and research labs.',
                '[""PAGES"",""MENUS"",""BANNERS"",""NEWS"",""EVENTS"",""NOTICES"",""DEPARTMENTS"",""COURSES"",""FACULTY"",""MEDIA"",""CONTACT"",""STATS""]'::jsonb,
                @themeTealId,
                '[]'::jsonb,
                '[]'::jsonb,
                TRUE
            ),
            (
                'UNIVERSITY_MODERN',
                'Comprehensive University Modern',
                'UNIVERSITY',
                'Prestigious collegiate aesthetic designed for multi-disciplinary universities with research centers, faculties, and international admissions.',
                '[""PAGES"",""MENUS"",""MEDIA"",""CONTACT"",""NEWS"",""EVENTS"",""NOTICES"",""DEPARTMENTS"",""COURSES"",""FACULTY"",""ADMISSIONS"",""BANNERS"",""MARQUEE"",""STATS"",""TESTIMONIALS""]'::jsonb,
                @themeCrimsonId,
                '[]'::jsonb,
                '[]'::jsonb,
                TRUE
            )
            ON CONFLICT (code) DO UPDATE SET
                name = EXCLUDED.name,
                category = EXCLUDED.category,
                description = EXCLUDED.description,
                recommended_features = EXCLUDED.recommended_features,
                default_theme_id = EXCLUDED.default_theme_id,
                updated_at = CURRENT_TIMESTAMP;",
            new { themeBlueId, themeEmeraldId, themeTealId, themeCrimsonId });

        // 4. Platform Settings
        var settingsCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM platform_settings;");
        if (settingsCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO platform_settings (setting_key, setting_value, description) VALUES 
                ('PLATFORM_NAME', 'Multi-Tenant Dynamic College Engine', 'Main title of the SaaS platform'),
                ('DEFAULT_TENANT_STATUS', 'ACTIVE', 'Default status for newly provisioned colleges'),
                ('ALLOW_PUBLIC_REGISTRATION', 'false', 'Flag for open public college onboarding')
                ON CONFLICT (setting_key) DO NOTHING;");
        }

        // 5. Tenants & Domains
        var rncTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ktsTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var tenantCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tenants;");
        if (tenantCount == 0)
        {
            var defaultCs = "Host=localhost;Port=5433;Database=college_rnc;Username=postgres;Password=root;Include Error Detail=true";
            var ktsCs = "Host=localhost;Port=5433;Database=college_kts;Username=postgres;Password=root;Include Error Detail=true";

            await conn.ExecuteAsync(@"
                INSERT INTO tenants (id, tenant_code, name, slug, db_connection_string, status, default_theme_id, provisioning_status) VALUES 
                (@rncTenantId, 'RNC', 'RNC Arts & Science College', 'rnc-college', @defaultCs, 'ACTIVE', @themeBlueId, 'COMPLETED'),
                (@ktsTenantId, 'KTS', 'KTS Institute of Technology & Management', 'kts-tech', @ktsCs, 'ACTIVE', @themeEmeraldId, 'COMPLETED')
                ON CONFLICT (tenant_code) DO NOTHING;",
                new { rncTenantId, ktsTenantId, defaultCs, ktsCs, themeBlueId, themeEmeraldId });

            // Domains
            await conn.ExecuteAsync(@"
                INSERT INTO domains (tenant_id, domain, domain_type, is_primary, is_verified) VALUES 
                (@rncTenantId, 'rnc.localhost', 'PRIMARY', TRUE, TRUE),
                (@rncTenantId, 'localhost', 'SECONDARY', FALSE, TRUE),
                (@ktsTenantId, 'kts.localhost', 'PRIMARY', TRUE, TRUE)
                ON CONFLICT (domain) DO NOTHING;",
                new { rncTenantId, ktsTenantId });

            // Seed tenant features
            await conn.ExecuteAsync(@"
                INSERT INTO tenant_features (tenant_id, feature_code, is_enabled) VALUES 
                (@rncTenantId, 'PAGES', TRUE), (@rncTenantId, 'NEWS', TRUE), (@rncTenantId, 'EVENTS', TRUE), (@rncTenantId, 'BANNERS', TRUE), (@rncTenantId, 'GALLERY', TRUE), (@rncTenantId, 'DEPARTMENTS', TRUE), (@rncTenantId, 'COURSES', TRUE), (@rncTenantId, 'FACULTY', TRUE), (@rncTenantId, 'TESTIMONIALS', TRUE), (@rncTenantId, 'ADMISSIONS', TRUE),
                (@ktsTenantId, 'PAGES', TRUE), (@ktsTenantId, 'NEWS', TRUE), (@ktsTenantId, 'EVENTS', TRUE), (@ktsTenantId, 'BANNERS', TRUE), (@ktsTenantId, 'GALLERY', TRUE), (@ktsTenantId, 'DEPARTMENTS', TRUE), (@ktsTenantId, 'COURSES', TRUE), (@ktsTenantId, 'FACULTY', TRUE), (@ktsTenantId, 'TESTIMONIALS', TRUE), (@ktsTenantId, 'ADMISSIONS', TRUE)
                ON CONFLICT (tenant_id, feature_code) DO NOTHING;",
                new { rncTenantId, ktsTenantId });
        }

        // 6. Users (SuperAdmin + CollegeAdmins)
        var superAdminHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123", 12);
        var collegeAdminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12);

        var superUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var rncAdminUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var ktsAdminUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        await conn.ExecuteAsync(@"
            INSERT INTO users (id, tenant_id, username, email, password_hash, full_name, role, is_active) VALUES 
            (@superUserId, NULL, 'superadmin', 'superadmin@platform.local', @superAdminHash, 'Global Super Administrator', 'SuperAdmin', TRUE)
            ON CONFLICT (username) DO UPDATE 
            SET password_hash = @superAdminHash, role = 'SuperAdmin', is_active = TRUE;",
            new { superUserId, superAdminHash });

        // Seed RNC demo admin user only if RNC tenant exists
        var rncExists = await conn.ExecuteScalarAsync<int?>("SELECT 1 FROM tenants WHERE id = @rncTenantId;", new { rncTenantId });
        if (rncExists.HasValue)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO users (id, tenant_id, username, email, password_hash, full_name, role, is_active) VALUES 
                (@rncAdminUserId, @rncTenantId, 'rnc-admin', 'admin@rnc.edu', @collegeAdminHash, 'RNC Principal Admin', 'CollegeAdmin', TRUE)
                ON CONFLICT (username) DO UPDATE SET password_hash = @collegeAdminHash;",
                new { rncAdminUserId, rncTenantId, collegeAdminHash });

            await conn.ExecuteAsync(@"
                INSERT INTO user_tenants (user_id, tenant_id, role_id, is_primary) VALUES 
                (@rncAdminUserId, @rncTenantId, @roleCollegeAdminId, TRUE)
                ON CONFLICT (user_id, tenant_id) DO NOTHING;",
                new { rncAdminUserId, rncTenantId, roleCollegeAdminId });
        }

        // Seed KTS demo admin user only if KTS tenant exists
        var ktsExists = await conn.ExecuteScalarAsync<int?>("SELECT 1 FROM tenants WHERE id = @ktsTenantId;", new { ktsTenantId });
        if (ktsExists.HasValue)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO users (id, tenant_id, username, email, password_hash, full_name, role, is_active) VALUES 
                (@ktsAdminUserId, @ktsTenantId, 'kts-admin', 'admin@kts.edu', @collegeAdminHash, 'KTS Director Admin', 'CollegeAdmin', TRUE)
                ON CONFLICT (username) DO UPDATE SET password_hash = @collegeAdminHash;",
                new { ktsAdminUserId, ktsTenantId, collegeAdminHash });

            await conn.ExecuteAsync(@"
                INSERT INTO user_tenants (user_id, tenant_id, role_id, is_primary) VALUES 
                (@ktsAdminUserId, @ktsTenantId, @roleCollegeAdminId, TRUE)
                ON CONFLICT (user_id, tenant_id) DO NOTHING;",
                new { ktsAdminUserId, ktsTenantId, roleCollegeAdminId });
        }
    }

    public static async Task SeedInitialTenantContentAsync(string tenantCs, string tenantCode, string templateCode = "ENGINEERING_MODERN", string? collegeName = null)
    {
        using var conn = new NpgsqlConnection(tenantCs);
        await conn.OpenAsync();

        // 1. Initial Settings
        var settingsCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tenant_settings;");
        if (settingsCount == 0)
        {
            var collegeTitle = !string.IsNullOrWhiteSpace(collegeName) 
                ? collegeName.Trim() 
                : (tenantCode == "RNC" ? "RNC Arts & Science College" : "KTS Institute of Technology & Management");
            var tagline = tenantCode == "RNC" ? "Empowering Minds, Inspiring Futures Since 1985" : "Leading Innovation, Engineering Tomorrow";
            var email = tenantCode == "RNC" ? "contact@rnc.edu" : $"info@{tenantCode.ToLowerInvariant()}.edu";
            var phone = tenantCode == "RNC" ? "+91 (044) 2834-9100" : "+91 (044) 4721-8200";
            var address = tenantCode == "RNC" ? "104 University Enclave, Chennai, TN - 600028" : "45 Innovation Corridor, Tech Park, Chennai - 600113";
            var socialJson = @"{""facebook"": ""https://facebook.com"", ""twitter"": ""https://twitter.com"", ""linkedin"": ""https://linkedin.com"", ""youtube"": ""https://youtube.com""}";

            await conn.ExecuteAsync(@"
                INSERT INTO tenant_settings (site_name, tagline, logo_url, contact_email, contact_phone, address, social_links)
                VALUES (@collegeTitle, @tagline, '/assets/logo.png', @email, @phone, @address, @socialJson::jsonb);",
                new { collegeTitle, tagline, email, phone, address, socialJson });
        }
        else if (!string.IsNullOrWhiteSpace(collegeName))
        {
            await conn.ExecuteAsync(@"
                UPDATE tenant_settings 
                SET site_name = @collegeName;",
                new { collegeName = collegeName.Trim() });
        }

        // 2. Initial Banners (Template-Aware Local Assets)
        var templateFolder = templateCode switch
        {
            "ARTS_SCIENCE_MODERN" => "arts",
            "MEDICAL_MODERN" => "medical",
            _ => "engineering"
        };

        var bannerCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM banners;");
        if (bannerCount == 0)
        {
            if (templateCode == "ARTS_SCIENCE_MODERN")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO banners (title, subtitle, image_url, cta_text, cta_url, sort_order, is_active) VALUES 
                    ('Admissions Open for Arts & Sciences 2026-2027', 'Legacy of academic distinction, humanities & scientific research excellence', '/assets/templates/arts/banner1.svg', 'Apply for Admission', '/admissions', 1, TRUE),
                    ('Ranked #1 for Humanities, Sciences & Research Archives', 'Over 100,000+ volumes, modern research labs & postgraduate mentorship', '/assets/templates/arts/banner2.svg', 'Explore Programs', '/courses', 2, TRUE);");
            }
            else if (templateCode == "MEDICAL_MODERN")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO banners (title, subtitle, image_url, cta_text, cta_url, sort_order, is_active) VALUES 
                    ('Admissions Open for Medical & Allied Health Sciences 2026-2027', 'NMC recognized medical college & 1,200-bed multispecialty hospital', '/assets/templates/medical/banner1.svg', 'Medical Admissions', '/admissions', 1, TRUE),
                    ('Excellence in Clinical Rotations & Modern Anatomy Labs', '24/7 super-specialty trauma care, advanced simulation & surgical skills labs', '/assets/templates/medical/banner2.svg', 'Hospital & Clinics', '/courses', 2, TRUE);");
            }
            else
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO banners (title, subtitle, image_url, cta_text, cta_url, sort_order, is_active) VALUES 
                    ('Admissions Open for Engineering & Technology 2026-2027', 'AICTE Approved Tier-1 engineering institution shaping future tech leaders', '/assets/templates/engineering/banner1.svg', 'Apply Online Now', '/admissions', 1, TRUE),
                    ('Top Tier Tech Placements & Advanced Robotics Labs', 'Over 1,500+ placement offers with top Fortune 500 tech leaders', '/assets/templates/engineering/banner2.svg', 'Explore Tech Programs', '/courses', 2, TRUE);");
            }
        }
        else
        {
            // Automatically upgrade existing legacy placeholders to new local template assets
            await conn.ExecuteAsync(@"
                UPDATE banners SET image_url = @b1 WHERE image_url = '/assets/banner1.jpg';
                UPDATE banners SET image_url = @b2 WHERE image_url = '/assets/banner2.jpg';",
                new { b1 = $"/assets/templates/{templateFolder}/banner1.svg", b2 = $"/assets/templates/{templateFolder}/banner2.svg" });
        }

        // 2b. Initial Marquee Settings & Announcements
        var marqueeCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM marquee_items;");
        if (marqueeCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO marquee_settings (is_active, speed, pause_on_hover, bg_color, text_color, badge_bg_color)
                VALUES (TRUE, 'medium', TRUE, '#0f172a', '#f8fafc', '#f59e0b');

                INSERT INTO marquee_items (badge_text, message, link_url, is_active, sort_order) VALUES 
                ('ADMISSIONS 2026', 'Applications open for UG, PG & Ph.D. degree programs | Merit scholarships and early-bird fee concessions available', '/admissions', TRUE, 1),
                ('CAMPUS DRIVE', 'Campus placement drive sets milestone with 1,200+ offers across Microsoft, Google, TCS, and Deloitte', '/news', TRUE, 2),
                ('EXAM NOTICE', 'Semester examination timetables and hall tickets published on student portal', '/notices', TRUE, 3);");
        }

        // 2c. Initial Stats Items (Key Highlights & Metrics - CMS360 Image 4)
        var statsCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM stats_items;");
        if (statsCount == 0)
        {
            if (templateCode == "ARTS_SCIENCE_MODERN" || tenantCode == "RNC" || tenantCode == "RCM")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                    ('8,200+', '8,200', '', '+', 'Active Students', 'Users', 1, TRUE),
                    ('42+', '42', '', '+', 'Degree Programs', 'BookOpen', 2, TRUE),
                    ('310+', '310', '', '+', 'Faculty Mentors', 'Award', 3, TRUE),
                    ('96.8%', '96.8', '', '%', 'Graduation Rate', 'GraduationCap', 4, TRUE);");
            }
            else if (templateCode == "MEDICAL_MODERN")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                    ('1,200+', '1,200', '', '+', 'Hospital Beds', 'Building2', 1, TRUE),
                    ('240+', '240', '', '+', 'Clinical Faculty', 'Users', 2, TRUE),
                    ('3,500+', '3,500', '', '+', 'Daily OPD Patients', 'Users', 3, TRUE),
                    ('28+', '28', '', '+', 'Super Specialties', 'Award', 4, TRUE);");
            }
            else
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO stats_items (metric, value, prefix, suffix, label, icon, sort_order, is_active) VALUES 
                    ('1,800+', '1,800', '', '+', 'Graduated Students', 'GraduationCap', 1, TRUE),
                    ('143+', '143', '', '+', 'Research Faculty', 'Users', 2, TRUE),
                    ('63+', '63', '', '+', 'Patents Filed', 'FileCheck', 3, TRUE),
                    ('98.4%', '98.4', '', '%', 'Placement Record', 'TrendingUp', 4, TRUE);");
            }
        }

        // 2d. Initial Quote Management (Welcome Quote Banner)
        var quoteCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM quote_settings;");
        if (quoteCount == 0)
        {
            var quoteAuthor = templateCode == "ARTS_SCIENCE_MODERN" ? "Dr. Aruna Krishnan, Ph.D." : templateCode == "MEDICAL_MODERN" ? "Dr. A. S. Sundaram, MS, MCh" : "Dr. Rajesh Kumar, Ph.D.";
            var quoteDesignation = templateCode == "ARTS_SCIENCE_MODERN" ? "Principal & Dean of Arts & Science" : templateCode == "MEDICAL_MODERN" ? "Dean & Medical Director" : "Dean of Engineering & Research";
            var quoteText = templateCode == "MEDICAL_MODERN" 
                ? "Our mission is to heal with compassion, advance scientific discovery, and nurture doctors who treat patients with the highest ethical commitment."
                : "For decades, our institution has stood at the intersection of classical scholarship, cutting-edge innovation, and ethical leadership.";
            var quoteSubText = templateCode == "MEDICAL_MODERN"
                ? "Serving humanity with distinction since 1992"
                : "Dedicated to transformative education and character building";

            await conn.ExecuteAsync(@"
                INSERT INTO quote_settings (quote_text, author_name, designation, author_image_url, sub_text, is_active)
                VALUES (@quoteText, @quoteAuthor, @quoteDesignation, '/assets/templates/common/leader_portrait.svg', @quoteSubText, TRUE);",
                new { quoteText, quoteAuthor, quoteDesignation, quoteSubText });
        }

        // 3. Initial Departments & Courses & Faculty
        var deptCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM departments;");
        if (deptCount == 0)
        {
            var dept1Id = Guid.NewGuid();
            var dept2Id = Guid.NewGuid();

            if (templateCode == "ARTS_SCIENCE_MODERN" || tenantCode == "RNC")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO departments (id, name, code, description, head_of_department, email, is_active) VALUES 
                    (@dept1Id, 'Department of Computer Science', 'CS', 'Fostering computing innovations, software engineering, and artificial intelligence.', 'Dr. Aruna Krishnan, Ph.D.', 'cs@rnc.edu', TRUE),
                    (@dept2Id, 'Department of Commerce & Management', 'BCOM', 'Excellence in financial economics, business analytics, and entrepreneurial leadership.', 'Prof. R. Sundaram, M.Phil', 'commerce@rnc.edu', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO courses (department_id, name, code, degree_level, duration_years, description, eligibility, is_active) VALUES 
                    (@dept1Id, 'B.Sc Computer Science (AI & ML)', 'BSC-CS-AIML', 'UNDERGRADUATE', 3.0, 'Comprehensive curriculum covering algorithms, machine learning, and cloud computing.', 'Higher Secondary with Mathematics / Computer Science (min 60%)', TRUE),
                    (@dept1Id, 'M.Sc Data Analytics', 'MSC-DA', 'POSTGRADUATE', 2.0, 'Advanced statistical computing, big data systems, and predictive modeling.', 'B.Sc CS / BCA / B.Tech (min 55%)', TRUE),
                    (@dept2Id, 'Bachelor of Commerce (Honours)', 'BCOM-HONS', 'UNDERGRADUATE', 3.0, 'Corporate finance, auditing, taxation laws, and fintech operations.', 'Higher Secondary with Commerce / Accountancy', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO faculty (department_id, name, designation, qualification, specialization, email, sort_order, is_active) VALUES 
                    (@dept1Id, 'Dr. Aruna Krishnan', 'Professor & Head', 'Ph.D. in Artificial Intelligence (IIT Madras)', 'Deep Learning & Natural Language Processing', 'aruna.k@rnc.edu', 1, TRUE),
                    (@dept1Id, 'Prof. Vigneshwaran S', 'Assistant Professor', 'M.Tech, M.S.', 'Distributed Systems & Cloud Architecture', 'vignesh.s@rnc.edu', 2, TRUE),
                    (@dept2Id, 'Prof. R. Sundaram', 'Associate Professor & Head', 'Ph.D., M.Phil (Commerce)', 'Corporate Accounting & International Finance', 'sundaram.r@rnc.edu', 1, TRUE);",
                    new { dept1Id, dept2Id });
            }
            else if (templateCode == "MEDICAL_MODERN")
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO departments (id, name, code, description, head_of_department, email, is_active) VALUES 
                    (@dept1Id, 'Department of General Medicine & Surgery', 'MED', 'Excellence in clinical training, patient diagnostics, and evidence-based medicine.', 'Dr. A. S. Sundaram, MS, MCh', 'dean.med@hospital.edu', TRUE),
                    (@dept2Id, 'Department of Nursing & Allied Health', 'NUR', 'Compassionate clinical nursing, critical care, and community health services.', 'Prof. Mary Joseph, M.Sc (Nursing)', 'nursing@hospital.edu', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO courses (department_id, name, code, degree_level, duration_years, description, eligibility, is_active) VALUES 
                    (@dept1Id, 'Bachelor of Medicine & Surgery (MBBS)', 'MBBS', 'UNDERGRADUATE', 5.5, 'Comprehensive medical degree including 1 year rotatory internship.', 'Class 12 PCB (min 60%) + NEET-UG Qualified', TRUE),
                    (@dept2Id, 'B.Sc Nursing (Honours)', 'BSC-NUR', 'UNDERGRADUATE', 4.0, 'Clinical care, pharmacology, emergency response, and intensive patient management.', 'Class 12 PCB (min 50%)', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO faculty (department_id, name, designation, qualification, specialization, email, sort_order, is_active) VALUES 
                    (@dept1Id, 'Dr. A. S. Sundaram', 'Dean & Chief Surgeon', 'MS, MCh (AIIMS)', 'Minimally Invasive Surgery & Cardiology', 'dean.med@hospital.edu', 1, TRUE),
                    (@dept2Id, 'Prof. Mary Joseph', 'Professor & Principal', 'M.Sc (Nursing), Ph.D.', 'Critical Care & Pediatric Nursing', 'nursing@hospital.edu', 1, TRUE);",
                    new { dept1Id, dept2Id });
            }
            else
            {
                await conn.ExecuteAsync(@"
                    INSERT INTO departments (id, name, code, description, head_of_department, email, is_active) VALUES 
                    (@dept1Id, 'Department of Robotics & Mechatronics', 'ROBOTICS', 'Leading advanced robotics, embedded systems, and autonomous automation research.', 'Dr. Rajesh Kumar, Ph.D.', 'robotics@kts.edu', TRUE),
                    (@dept2Id, 'Department of AI & Data Engineering', 'AIDE', 'Next generation deep neural architectures, machine perception, and edge AI.', 'Dr. S. Meenakshi, Ph.D.', 'aide@kts.edu', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO courses (department_id, name, code, degree_level, duration_years, description, eligibility, is_active) VALUES 
                    (@dept1Id, 'B.Tech Robotics & Automation', 'BTECH-ROB', 'UNDERGRADUATE', 4.0, 'Robotics design, kinematics, computer vision, and industrial automation.', 'Class 12 PCM with JEE / State Entrance Qualification', TRUE),
                    (@dept2Id, 'B.Tech Artificial Intelligence & Machine Learning', 'BTECH-AIML', 'UNDERGRADUATE', 4.0, 'Deep learning architectures, natural language understanding, and autonomous systems.', 'Class 12 PCM (min 60%)', TRUE);",
                    new { dept1Id, dept2Id });

                await conn.ExecuteAsync(@"
                    INSERT INTO faculty (department_id, name, designation, qualification, specialization, email, sort_order, is_active) VALUES 
                    (@dept1Id, 'Dr. Rajesh Kumar', 'Dean & Professor', 'Ph.D. in Robotics (IISc Bangalore)', 'Planetary Rovers & Computer Vision', 'rajesh.k@kts.edu', 1, TRUE),
                    (@dept2Id, 'Dr. S. Meenakshi', 'Professor & Head', 'Ph.D. in Data Science', 'Scalable Machine Learning & Reinforcement Learning', 'meenakshi.s@kts.edu', 1, TRUE);",
                    new { dept1Id, dept2Id });
            }
        }

        // 4. Initial Testimonials
        var testCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM testimonials;");
        if (testCount == 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO testimonials (student_name, batch_year, course_name, content, photo_url, current_company, rating, is_published, sort_order) VALUES 
                ('Priya Dharshini', 'Batch of 2025', 'B.Sc Computer Science', 'The research culture and faculty mentorship at this institution prepared me for my career at Microsoft. World class labs and supportive environment!', '/assets/student1.jpg', 'Microsoft Software Engineer', 5, TRUE, 1),
                ('Karthik Ramanathan', 'Batch of 2024', 'B.Com Honours', 'An extraordinary journey of leadership and professional skill building. The placement training was second to none.', '/assets/student2.jpg', 'Deloitte Financial Analyst', 5, TRUE, 2);");
        }

        // 5. Initial Pages & Sections
        var pageCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM pages;");
        if (pageCount > 0) return;

        var pageId = Guid.NewGuid();

        if (tenantCode == "RNC")
        {
            await conn.ExecuteAsync(@"
                INSERT INTO pages (id, slug, title, meta_description, is_published) 
                VALUES (@pageId, 'home', 'Welcome to RNC Arts & Science College', 'Premier institution for arts and science education', TRUE);",
                new { pageId });

            var heroContent = @"{
                ""badge"": ""NAAC 'A+' Grade Accredited"",
                ""heading"": ""Shaping Global Leaders in Arts, Science & Commerce"",
                ""description"": ""Established in 1985, RNC College empowers 8,000+ students with world-class facilities, holistic curriculum, and 98% placement records."",
                ""ctaText"": ""Explore Admissions 2026"",
                ""ctaLink"": ""/admissions"",
                ""secondaryCtaText"": ""Virtual Campus Tour"",
                ""secondaryCtaLink"": ""/about"",
                ""stats"": [
                    {""label"": ""Active Students"", ""value"": ""8,200+""},
                    {""label"": ""Degree Programs"", ""value"": ""42+""},
                    {""label"": ""Faculty Members"", ""value"": ""310+""},
                    {""label"": ""Placement Rate"", ""value"": ""98.4%""}
                ]
            }";


            await conn.ExecuteAsync(@"
                INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible) VALUES 
                (@pageId, 'HERO', 'Hero Banner', 'Main campus showcase', @heroContent::jsonb, '{""columns"": 1, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""gradient"", ""spacing"": ""large""}'::jsonb, 1, TRUE),
                (@pageId, 'STATISTICS', 'Campus Highlights', 'Excellence in Numbers', '{}'::jsonb, '{""columns"": 4, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 2, TRUE),
                (@pageId, 'QUOTE', 'Leadership Vision', 'A Message from our Leadership', '{}'::jsonb, '{""columns"": 1, ""cardVariant"": ""elevated"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 3, TRUE),
                (@pageId, 'DEPARTMENTS', 'Academic Divisions', 'Explore our faculties', '{""showFeaturedOnly"":false}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 4, TRUE),
                (@pageId, 'COURSES', 'Featured Programs', 'Undergraduate & Postgraduate offerings', '{""limit"":6}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 5, TRUE),
                (@pageId, 'NEWS', 'Campus Headlines', 'Latest news & updates', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 6, TRUE),
                (@pageId, 'EVENTS', 'Upcoming Events', 'Seminars, workshops and sports', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 7, TRUE);",
                new { pageId, heroContent });

            // Create Page Version 1
            await conn.ExecuteAsync(@"
                INSERT INTO page_versions (page_id, version_number, title, slug, meta_description, content_json, created_by)
                VALUES (@pageId, 1, 'Welcome to RNC Arts & Science College', 'home', 'Premier institution for arts and science education', '[]'::jsonb, 'system');",
                new { pageId });

            // Navigation Menu
            var menuId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO menus (id, code, title) VALUES (@menuId, 'MAIN_NAV', 'Main Navigation Bar');", new { menuId });

            await conn.ExecuteAsync(@"
                INSERT INTO menu_items (menu_id, parent_id, item_type, page_id, title, url, sort_order, is_visible) VALUES 
                (@menuId, NULL, 'PAGE', @pageId, 'Home', '/', 1, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'About Us', '/about', 2, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Departments', '/departments', 3, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Courses', '/courses', 4, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Faculty', '/faculty', 5, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'News', '/news', 6, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Events', '/events', 7, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Contact', '/contact', 8, TRUE);",
                new { menuId, pageId });

            // News Items
            await conn.ExecuteAsync(@"
                INSERT INTO news (title, slug, summary, content, published_date, is_published) VALUES 
                ('RNC Signs Research MoU with Cambridge University', 'rnc-mou-cambridge', 'Strategic partnership opens exchange programs for 200+ postgraduate scholars.', '<p>RNC College has entered into a landmark bilateral academic exchange with Cambridge University to foster cross-disciplinary artificial intelligence and humanities research.</p>', CURRENT_TIMESTAMP, TRUE),
                ('Annual Cultural Fest - Rhythm 2026 Announced', 'annual-cultural-fest-rhythm-2026', 'Three days of vibrant music, drama, and artistic exhibitions featuring 40+ colleges.', '<p>The flagship inter-collegiate festival Rhythm 2026 will be held from March 20-22 on the central campus quadrangle with special celebrity performances.</p>', CURRENT_TIMESTAMP - INTERVAL '2 days', TRUE),
                ('Placement Drive 2026 Achieves 1,200+ Offers in Week 1', 'placement-drive-2026-record', 'Top tier recruiters including Google, Microsoft, TCS, and Deloitte visit campus.', '<p>Over 1,200 students received high-package offers on Day 1 of campus placements with the highest international offer reaching 45 LPA.</p>', CURRENT_TIMESTAMP - INTERVAL '5 days', TRUE);");

            // Event Items
            await conn.ExecuteAsync(@"
                INSERT INTO events (title, description, event_date, location, is_published) VALUES 
                ('International Symposium on Next-Gen Artificial Intelligence', 'Two-day international summit bringing leading data scientists and industry pioneers together.', CURRENT_TIMESTAMP + INTERVAL '10 days', 'Auditorium Hall A, Science Complex', TRUE),
                ('Alumni Grand Reunion 2026', 'Reconnecting 40 graduating batches with networking dinner and campus heritage walk.', CURRENT_TIMESTAMP + INTERVAL '24 days', 'Central Sports Grounds', TRUE),
                ('National Level Inter-College Cricket Championship', '32 collegiate teams competing for the prestigious Chancellor Golden Cup.', CURRENT_TIMESTAMP + INTERVAL '30 days', 'RNC Olympic Sports Arena', TRUE);");
        }
        else if (tenantCode == "KTS")
        {
            await conn.ExecuteAsync(@"
                INSERT INTO pages (id, slug, title, meta_description, is_published) 
                VALUES (@pageId, 'home', 'KTS Institute of Technology & Management', 'Next generation engineering and technological innovations', TRUE);",
                new { pageId });

            var heroContent = @"{
                ""badge"": ""Autonomous • Ranked #1 Private Engineering Institute"",
                ""heading"": ""Pioneering the Frontiers of Technology, Robotics & AI"",
                ""description"": ""KTS Institute fosters cutting-edge technological education with 50+ specialized labs, patent incubation centers, and direct industry co-ops."",
                ""ctaText"": ""Apply for B.Tech & MBA 2026"",
                ""ctaLink"": ""/admissions"",
                ""secondaryCtaText"": ""Explore R&D Labs"",
                ""secondaryCtaLink"": ""/research"",
                ""stats"": [
                    {""label"": ""Engineering Students"", ""value"": ""6,500+""},
                    {""label"": ""Patents Published"", ""value"": ""128+""},
                    {""label"": ""Industry Partners"", ""value"": ""85+""},
                    {""label"": ""Highest Package"", ""value"": ""52 LPA""}
                ]
            }";


            await conn.ExecuteAsync(@"
                INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible) VALUES 
                (@pageId, 'HERO', 'Hero Banner', 'Next generation tech banner', @heroContent::jsonb, '{""columns"": 1, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""gradient"", ""spacing"": ""large""}'::jsonb, 1, TRUE),
                (@pageId, 'STATISTICS', 'Innovation Metrics', 'Pioneering Research & Excellence', '{}'::jsonb, '{""columns"": 4, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 2, TRUE),
                (@pageId, 'QUOTE', 'Dean''s Vision', 'Engineering the Future', '{}'::jsonb, '{""columns"": 1, ""cardVariant"": ""elevated"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 3, TRUE),
                (@pageId, 'DEPARTMENTS', 'Engineering Departments', 'Advanced engineering disciplines', '{""showFeaturedOnly"":false}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 4, TRUE),
                (@pageId, 'COURSES', 'Featured Degrees', 'B.Tech, M.Tech and Doctoral Programs', '{""limit"":6}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 5, TRUE),
                (@pageId, 'NEWS', 'Campus Headlines', 'Latest tech updates', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 6, TRUE),
                (@pageId, 'EVENTS', 'Upcoming Events', 'Hackathons and tech symposiums', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 7, TRUE);",
                new { pageId, heroContent });

            // Create Page Version 1
            await conn.ExecuteAsync(@"
                INSERT INTO page_versions (page_id, version_number, title, slug, meta_description, content_json, created_by)
                VALUES (@pageId, 1, 'KTS Institute of Technology & Management', 'home', 'Next generation engineering and technological innovations', '[]'::jsonb, 'system');",
                new { pageId });

            // Navigation Menu
            var menuId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO menus (id, code, title) VALUES (@menuId, 'MAIN_NAV', 'Main Navigation Bar');", new { menuId });

            await conn.ExecuteAsync(@"
                INSERT INTO menu_items (menu_id, parent_id, item_type, page_id, title, url, sort_order, is_visible) VALUES 
                (@menuId, NULL, 'PAGE', @pageId, 'Home', '/', 1, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'About KTS', '/about', 2, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Departments', '/departments', 3, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Programs', '/courses', 4, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Faculty', '/faculty', 5, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Campus News', '/news', 6, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Tech Events', '/events', 7, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Contact', '/contact', 8, TRUE);",
                new { menuId, pageId });

            // News Items
            await conn.ExecuteAsync(@"
                INSERT INTO news (title, slug, summary, content, published_date, is_published) VALUES 
                ('KTS Robotics Club Wins First Prize in Asian Rover Challenge', 'kts-robotics-asian-rover-first-prize', 'Autonomous Martian rover designed by 3rd year students beats 60 international finalists.', '<p>Team Astra from KTS Department of Mechatronics clinched 1st position with their multi-terrain planetary rover showcasing computer vision and autonomous sample retrieval.</p>', CURRENT_TIMESTAMP, TRUE),
                ('State-of-the-Art Semiconductor Fabrication Lab Inaugurated', 'semi-conductor-fab-lab-inauguration', 'USD 2.5M joint facility launched with leading microchip manufacturers.', '<p>The new cleanroom facility will allow undergraduate and PhD students to fabricate custom VLSI silicon wafers directly on campus.</p>', CURRENT_TIMESTAMP - INTERVAL '3 days', TRUE);");

        }
        else
        {
            // Generic default starter content for any new college created dynamically by SuperAdmin
            var collegeTitle = !string.IsNullOrWhiteSpace(tenantCode) ? $"{tenantCode} College of Higher Education" : "College of Higher Education";
            await conn.ExecuteAsync(@"
                INSERT INTO pages (id, slug, title, meta_description, is_published) 
                VALUES (@pageId, 'home', @title, @desc, TRUE);",
                new { pageId, title = $"Welcome to {collegeTitle}", desc = $"Official website of {collegeTitle}" });

            var heroContent = @"{
                ""badge"": ""Excellence in Higher Education"",
                ""heading"": ""Empowering Minds, Shaping Future Innovators"",
                ""description"": ""Welcome to our modern academic campus. We offer accredited degree programs, state-of-the-art laboratory facilities, and comprehensive career development."",
                ""ctaText"": ""Explore Programs"",
                ""ctaLink"": ""/courses"",
                ""secondaryCtaText"": ""Contact Us"",
                ""secondaryCtaLink"": ""/contact"",
                ""stats"": [
                    {""label"": ""Academic Programs"", ""value"": ""15+""},
                    {""label"": ""Faculty Members"", ""value"": ""85+""},
                    {""label"": ""Graduation Rate"", ""value"": ""96%""},
                    {""label"": ""Campus Facilities"", ""value"": ""24/7""}
                ]
            }";

            await conn.ExecuteAsync(@"
                INSERT INTO page_sections (page_id, section_type, title, subtitle, content_json, settings_json, sort_order, is_visible) VALUES 
                (@pageId, 'HERO', 'Hero Banner', 'Campus showcase', @heroContent::jsonb, '{""columns"": 1, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""gradient"", ""spacing"": ""large""}'::jsonb, 1, TRUE),
                (@pageId, 'STATISTICS', 'Campus at a Glance', 'Excellence in Numbers', '{}'::jsonb, '{""columns"": 4, ""cardVariant"": ""modern"", ""alignment"": ""center"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 2, TRUE),
                (@pageId, 'QUOTE', 'Leadership Message', 'Guiding Principles', '{}'::jsonb, '{""columns"": 1, ""cardVariant"": ""elevated"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 3, TRUE),
                (@pageId, 'DEPARTMENTS', 'Academic Divisions', 'Explore our faculties', '{""showFeaturedOnly"":false}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 4, TRUE),
                (@pageId, 'COURSES', 'Featured Programs', 'Undergraduate & Postgraduate offerings', '{""limit"":6}'::jsonb, '{""columns"": 3, ""cardVariant"": ""bordered"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 5, TRUE),
                (@pageId, 'NEWS', 'Campus Headlines', 'Latest news & announcements', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 6, TRUE),
                (@pageId, 'EVENTS', 'Upcoming Events', 'Seminars and workshops', '{}'::jsonb, '{""columns"": 3, ""cardVariant"": ""modern"", ""alignment"": ""left"", ""background"": ""light"", ""spacing"": ""normal""}'::jsonb, 7, TRUE);",
                new { pageId, heroContent });

            // Create Page Version 1
            await conn.ExecuteAsync(@"
                INSERT INTO page_versions (page_id, version_number, title, slug, meta_description, content_json, created_by)
                VALUES (@pageId, 1, @title, 'home', @desc, '[]'::jsonb, 'system');",
                new { pageId, title = $"Welcome to {collegeTitle}", desc = $"Official website of {collegeTitle}" });

            // Navigation Menu
            var menuId = Guid.NewGuid();
            await conn.ExecuteAsync(@"
                INSERT INTO menus (id, code, title) VALUES (@menuId, 'MAIN_NAV', 'Main Navigation Bar');", new { menuId });

            await conn.ExecuteAsync(@"
                INSERT INTO menu_items (menu_id, parent_id, item_type, page_id, title, url, sort_order, is_visible) VALUES 
                (@menuId, NULL, 'PAGE', @pageId, 'Home', '/', 1, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'About', '/about', 2, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Departments', '/departments', 3, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Courses', '/courses', 4, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Faculty', '/faculty', 5, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'News', '/news', 6, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Events', '/events', 7, TRUE),
                (@menuId, NULL, 'INTERNAL_URL', NULL, 'Contact', '/contact', 8, TRUE);",
                new { menuId, pageId });

            // News Items
            await conn.ExecuteAsync(@"
                INSERT INTO news (title, slug, summary, content, published_date, is_published) VALUES 
                ('Welcome to the New Academic Year 2026-2027', 'welcome-academic-year-2026', 'Orientation sessions and inaugural lectures commence next Monday across all departments.', '<p>We welcome all incoming undergraduate and postgraduate scholars to our campus for the new academic semester.</p>', CURRENT_TIMESTAMP, TRUE);");

            // Event Items
            await conn.ExecuteAsync(@"
                INSERT INTO events (title, description, event_date, location, is_published) VALUES 
                ('Annual Campus Orientation & Induction Day', 'Comprehensive introductory program for all freshmen covering curriculum, clubs, and campus tour.', CURRENT_TIMESTAMP + INTERVAL '7 days', 'Main Auditorium', TRUE);");
        }
    }
}
