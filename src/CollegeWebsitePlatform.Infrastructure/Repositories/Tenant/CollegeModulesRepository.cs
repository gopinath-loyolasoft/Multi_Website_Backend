using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class CollegeModulesRepository(ITenantDbConnectionFactory db)
{
    private readonly ITenantDbConnectionFactory _db = db;


    // ─── 2. COMPLIANCE & STATUTORY DISCLOSURES ────────────────────────────────
    public async Task<IEnumerable<ComplianceDocumentDto>> GetComplianceDocumentsAsync(string? category = null)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, title, category, document_pdf_url AS DocumentPdfUrl, file_size AS FileSize,
                   academic_year AS AcademicYear, sort_order AS SortOrder, is_active AS IsActive
            FROM compliance_documents
            WHERE is_deleted = FALSE AND is_active = TRUE
                  AND (@category IS NULL OR LOWER(category) = LOWER(@category))
            ORDER BY sort_order ASC, created_at DESC;";
        return await conn.QueryAsync<ComplianceDocumentDto>(sql, new { category });
    }

    public async Task<Guid> AddComplianceDocumentAsync(ComplianceDocumentDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO compliance_documents (title, category, document_pdf_url, file_size, academic_year, sort_order, is_active, created_at)
            VALUES (@Title, @Category, @DocumentPdfUrl, @FileSize, @AcademicYear, @SortOrder, @IsActive, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, dto);
    }

    public async Task<bool> UpdateComplianceDocumentAsync(ComplianceDocumentDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE compliance_documents
            SET title = @Title, category = @Category, document_pdf_url = @DocumentPdfUrl,
                file_size = @FileSize, academic_year = @AcademicYear, sort_order = @SortOrder, is_active = @IsActive
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, dto);
        return rows > 0;
    }

    public async Task<bool> DeleteComplianceDocumentAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE compliance_documents SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    // ─── 3. TRANSPORT & BUS ROUTES ────────────────────────────────────────────
    public async Task<IEnumerable<TransportRouteDto>> GetTransportRoutesAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, route_number AS RouteNumber, route_name AS RouteName, starting_point AS StartingPoint,
                   destination, departure_time AS DepartureTime, stops_json AS StopsJson, bus_number AS BusNumber,
                   driver_name AS DriverName, driver_phone AS DriverPhone, route_map_pdf_url AS RouteMapPdfUrl, is_active AS IsActive
            FROM transport_routes
            WHERE is_deleted = FALSE AND is_active = TRUE
            ORDER BY route_number ASC;";
        return await conn.QueryAsync<TransportRouteDto>(sql);
    }

    public async Task<Guid> AddTransportRouteAsync(TransportRouteDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO transport_routes (route_number, route_name, starting_point, destination, departure_time, stops_json, bus_number, driver_name, driver_phone, route_map_pdf_url, is_active, created_at)
            VALUES (@RouteNumber, @RouteName, @StartingPoint, @Destination, @DepartureTime, @StopsJson, @BusNumber, @DriverName, @DriverPhone, @RouteMapPdfUrl, @IsActive, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, dto);
    }

    public async Task<bool> UpdateTransportRouteAsync(TransportRouteDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE transport_routes
            SET route_number = @RouteNumber, route_name = @RouteName, starting_point = @StartingPoint,
                destination = @Destination, departure_time = @DepartureTime, stops_json = @StopsJson,
                bus_number = @BusNumber, driver_name = @DriverName, driver_phone = @DriverPhone,
                route_map_pdf_url = @RouteMapPdfUrl, is_active = @IsActive
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, dto);
        return rows > 0;
    }

    public async Task<bool> DeleteTransportRouteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE transport_routes SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    // ─── 4. HOSTELS & LIVING ──────────────────────────────────────────────────
    public async Task<IEnumerable<HostelFacilityDto>> GetHostelsAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, name, gender_type AS GenderType, capacity, room_types_json AS RoomTypesJson,
                   warden_name AS WardenName, warden_phone AS WardenPhone, mess_menu_pdf_url AS MessMenuPdfUrl,
                   rules_pdf_url AS RulesPdfUrl, image_url AS ImageUrl, is_active AS IsActive
            FROM hostel_facilities
            WHERE is_deleted = FALSE AND is_active = TRUE
            ORDER BY gender_type ASC, name ASC;";
        return await conn.QueryAsync<HostelFacilityDto>(sql);
    }

    public async Task<Guid> AddHostelAsync(HostelFacilityDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO hostel_facilities (name, gender_type, capacity, room_types_json, warden_name, warden_phone, mess_menu_pdf_url, rules_pdf_url, image_url, is_active, created_at)
            VALUES (@Name, @GenderType, @Capacity, @RoomTypesJson, @WardenName, @WardenPhone, @MessMenuPdfUrl, @RulesPdfUrl, @ImageUrl, @IsActive, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, dto);
    }

    public async Task<bool> UpdateHostelAsync(HostelFacilityDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE hostel_facilities
            SET name = @Name, gender_type = @GenderType, capacity = @Capacity, room_types_json = @RoomTypesJson,
                warden_name = @WardenName, warden_phone = @WardenPhone, mess_menu_pdf_url = @MessMenuPdfUrl,
                rules_pdf_url = @RulesPdfUrl, image_url = @ImageUrl, is_active = @IsActive
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, dto);
        return rows > 0;
    }

    public async Task<bool> DeleteHostelAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE hostel_facilities SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    // ─── 5. LIBRARY RESOURCES & QUESTION PAPERS ───────────────────────────────
    public async Task<IEnumerable<LibraryResourceDto>> GetLibraryResourcesAsync(string? resourceType = null, string? departmentCode = null)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, title, resource_type AS ResourceType, department_code AS DepartmentCode,
                   semester, academic_year AS AcademicYear, external_url AS ExternalUrl, pdf_url AS PdfUrl, is_active AS IsActive
            FROM library_resources
            WHERE is_deleted = FALSE AND is_active = TRUE
                  AND (@resourceType IS NULL OR LOWER(resource_type) = LOWER(@resourceType))
                  AND (@departmentCode IS NULL OR LOWER(department_code) = LOWER(@departmentCode))
            ORDER BY academic_year DESC, semester ASC, title ASC;";
        return await conn.QueryAsync<LibraryResourceDto>(sql, new { resourceType, departmentCode });
    }

    public async Task<Guid> AddLibraryResourceAsync(LibraryResourceDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO library_resources (title, resource_type, department_code, semester, academic_year, external_url, pdf_url, is_active, created_at)
            VALUES (@Title, @ResourceType, @DepartmentCode, @Semester, @AcademicYear, @ExternalUrl, @PdfUrl, @IsActive, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, dto);
    }

    public async Task<bool> UpdateLibraryResourceAsync(LibraryResourceDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE library_resources
            SET title = @Title, resource_type = @ResourceType, department_code = @DepartmentCode,
                semester = @Semester, academic_year = @AcademicYear, external_url = @ExternalUrl,
                pdf_url = @PdfUrl, is_active = @IsActive
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, dto);
        return rows > 0;
    }

    public async Task<bool> DeleteLibraryResourceAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE library_resources SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    // ─── 6. ALUMNI ────────────────────────────────────────────────────────────
    public async Task<IEnumerable<AlumniMemberDto>> GetAlumniMembersAsync(bool distinguishedOnly = false, bool includeAllStatuses = false)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, full_name AS FullName, batch_year AS BatchYear, degree_course AS DegreeCourse,
                   current_company AS CurrentCompany, current_designation AS CurrentDesignation,
                   testimonial_quote AS TestimonialQuote, photo_url AS PhotoUrl, linkedin_url AS LinkedinUrl,
                   is_distinguished AS IsDistinguished, status
            FROM alumni_members
            WHERE is_deleted = FALSE
                  AND (@includeAllStatuses = TRUE OR status = 'APPROVED')
                  AND (@distinguishedOnly = FALSE OR is_distinguished = TRUE)
            ORDER BY is_distinguished DESC, created_at DESC;";
        return await conn.QueryAsync<AlumniMemberDto>(sql, new { distinguishedOnly, includeAllStatuses });
    }

    public async Task<Guid> RegisterAlumniAsync(AlumniMemberDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO alumni_members (full_name, batch_year, degree_course, current_company, current_designation, testimonial_quote, photo_url, linkedin_url, is_distinguished, status, created_at)
            VALUES (@FullName, @BatchYear, @DegreeCourse, @CurrentCompany, @CurrentDesignation, @TestimonialQuote, @PhotoUrl, @LinkedinUrl, @IsDistinguished, @Status, CURRENT_TIMESTAMP)
            RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, dto);
    }

    public async Task<bool> UpdateAlumniMemberAsync(AlumniMemberDto dto)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE alumni_members
            SET full_name = @FullName, batch_year = @BatchYear, degree_course = @DegreeCourse,
                current_company = @CurrentCompany, current_designation = @CurrentDesignation,
                testimonial_quote = @TestimonialQuote, photo_url = @PhotoUrl, linkedin_url = @LinkedinUrl,
                is_distinguished = @IsDistinguished, status = @Status
            WHERE id = @Id;";
        var rows = await conn.ExecuteAsync(sql, dto);
        return rows > 0;
    }

    public async Task<bool> UpdateAlumniStatusAsync(Guid id, string status)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE alumni_members SET status = @status WHERE id = @id;", new { id, status });
        return rows > 0;
    }

    public async Task<bool> DeleteAlumniAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE alumni_members SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }

    // ─── 7. ABOUT MODULE ──────────────────────────────────────────────────────
    public async Task<AboutModuleDto> GetAboutModuleAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, history_title AS HistoryTitle, history_content AS HistoryContent,
                   history_image_url AS HistoryImageUrl, established_year AS EstablishedYear,
                   vision_text AS VisionText, mission_text AS MissionText,
                   accreditations_json AS AccreditationsJson, achievements_json AS AchievementsJson,
                   leadership_json AS LeadershipJson, core_values_json AS CoreValuesJson,
                   is_active AS IsActive, updated_at AS UpdatedAt
            FROM about_module
            ORDER BY updated_at DESC
            LIMIT 1;";

        var raw = await conn.QueryFirstOrDefaultAsync<AboutModuleDto>(sql);
        if (raw == null)
        {
            return new AboutModuleDto();
        }

        // Deserialize JSON arrays (with PropertyNameCaseInsensitive = true to handle camelCase JSON from frontend)
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        try
        {
            raw.Accreditations = System.Text.Json.JsonSerializer.Deserialize<List<AccreditationItemDto>>(raw.AccreditationsJson ?? "[]", jsonOptions)
                                 ?? [];
            raw.Achievements = System.Text.Json.JsonSerializer.Deserialize<List<AchievementItemDto>>(raw.AchievementsJson ?? "[]", jsonOptions)
                               ?? [];
            raw.Leadership = System.Text.Json.JsonSerializer.Deserialize<List<LeadershipMemberDto>>(raw.LeadershipJson ?? "[]", jsonOptions)
                             ?? [];
            raw.CoreValues = System.Text.Json.JsonSerializer.Deserialize<List<CoreValueItemDto>>(raw.CoreValuesJson ?? "[]", jsonOptions)
                             ?? [];
        }
        catch
        {
            // Leave lists empty on deserialization error
        }

        return raw;
    }

    public async Task<bool> UpsertAboutModuleAsync(UpdateAboutModuleRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO about_module (
                id, history_title, history_content, history_image_url, established_year,
                vision_text, mission_text, accreditations_json, achievements_json,
                leadership_json, core_values_json, is_active, updated_at
            )
            SELECT gen_random_uuid(), @HistoryTitle, @HistoryContent, @HistoryImageUrl, @EstablishedYear,
                   @VisionText, @MissionText, @AccreditationsJson, @AchievementsJson,
                   @LeadershipJson, @CoreValuesJson, @IsActive, NOW()
            WHERE NOT EXISTS (SELECT 1 FROM about_module);

            UPDATE about_module
            SET history_title       = @HistoryTitle,
                history_content     = @HistoryContent,
                history_image_url   = @HistoryImageUrl,
                established_year    = @EstablishedYear,
                vision_text         = @VisionText,
                mission_text        = @MissionText,
                accreditations_json = @AccreditationsJson,
                achievements_json   = @AchievementsJson,
                leadership_json     = @LeadershipJson,
                core_values_json    = @CoreValuesJson,
                is_active           = @IsActive,
                updated_at          = NOW();";

        var rows = await conn.ExecuteAsync(sql, req);
        return rows >= 0;
    }

    // ─── 10. POPUP & LIGHTBOX ANNOUNCEMENTS ─────────────────────────────────────
    public async Task<PopupAnnouncementDto?> GetActivePopupAnnouncementAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT 
                id AS Id, title AS Title, popup_type AS PopupType, image_url AS ImageUrl, 
                badge_text AS BadgeText, heading AS Heading, description AS Description, 
                video_url AS VideoUrl, target_url AS TargetUrl, cta_text AS CtaText, 
                secondary_cta_text AS SecondaryCtaText, secondary_target_url AS SecondaryTargetUrl, 
                open_in_new_tab AS OpenInNewTab, delay_seconds AS DelaySeconds, 
                display_frequency AS DisplayFrequency, target_pages AS TargetPages, 
                custom_target_slugs AS CustomTargetSlugs, start_date AS StartDate, 
                end_date AS EndDate, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM popup_announcements
            WHERE is_deleted = FALSE 
              AND is_active = TRUE 
              AND (start_date IS NULL OR start_date <= CURRENT_TIMESTAMP)
              AND (end_date IS NULL OR end_date >= CURRENT_TIMESTAMP)
            ORDER BY created_at DESC
            LIMIT 1;";
        return await conn.QueryFirstOrDefaultAsync<PopupAnnouncementDto>(sql);
    }

    public async Task<IEnumerable<PopupAnnouncementDto>> GetAllPopupsAsync()
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT 
                id AS Id, title AS Title, popup_type AS PopupType, image_url AS ImageUrl, 
                badge_text AS BadgeText, heading AS Heading, description AS Description, 
                video_url AS VideoUrl, target_url AS TargetUrl, cta_text AS CtaText, 
                secondary_cta_text AS SecondaryCtaText, secondary_target_url AS SecondaryTargetUrl, 
                open_in_new_tab AS OpenInNewTab, delay_seconds AS DelaySeconds, 
                display_frequency AS DisplayFrequency, target_pages AS TargetPages, 
                custom_target_slugs AS CustomTargetSlugs, start_date AS StartDate, 
                end_date AS EndDate, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM popup_announcements
            WHERE is_deleted = FALSE
            ORDER BY created_at DESC;";
        return await conn.QueryAsync<PopupAnnouncementDto>(sql);
    }

    public async Task<PopupAnnouncementDto?> GetPopupByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT 
                id AS Id, title AS Title, popup_type AS PopupType, image_url AS ImageUrl, 
                badge_text AS BadgeText, heading AS Heading, description AS Description, 
                video_url AS VideoUrl, target_url AS TargetUrl, cta_text AS CtaText, 
                secondary_cta_text AS SecondaryCtaText, secondary_target_url AS SecondaryTargetUrl, 
                open_in_new_tab AS OpenInNewTab, delay_seconds AS DelaySeconds, 
                display_frequency AS DisplayFrequency, target_pages AS TargetPages, 
                custom_target_slugs AS CustomTargetSlugs, start_date AS StartDate, 
                end_date AS EndDate, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM popup_announcements
            WHERE id = @id AND is_deleted = FALSE;";
        return await conn.QueryFirstOrDefaultAsync<PopupAnnouncementDto>(sql, new { id });
    }

    public async Task<Guid> CreatePopupAsync(CreateOrUpdatePopupRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO popup_announcements (
                title, popup_type, image_url, badge_text, heading, description, 
                video_url, target_url, cta_text, secondary_cta_text, secondary_target_url, 
                open_in_new_tab, delay_seconds, display_frequency, target_pages, 
                custom_target_slugs, start_date, end_date, is_active, created_at, updated_at
            ) VALUES (
                @Title, @PopupType, @ImageUrl, @BadgeText, @Heading, @Description, 
                @VideoUrl, @TargetUrl, @CtaText, @SecondaryCtaText, @SecondaryTargetUrl, 
                @OpenInNewTab, @DelaySeconds, @DisplayFrequency, @TargetPages, 
                @CustomTargetSlugs, @StartDate, @EndDate, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
            ) RETURNING id;";
        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdatePopupAsync(Guid id, CreateOrUpdatePopupRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE popup_announcements SET
                title = @Title,
                popup_type = @PopupType,
                image_url = @ImageUrl,
                badge_text = @BadgeText,
                heading = @Heading,
                description = @Description,
                video_url = @VideoUrl,
                target_url = @TargetUrl,
                cta_text = @CtaText,
                secondary_cta_text = @SecondaryCtaText,
                secondary_target_url = @SecondaryTargetUrl,
                open_in_new_tab = @OpenInNewTab,
                delay_seconds = @DelaySeconds,
                display_frequency = @DisplayFrequency,
                target_pages = @TargetPages,
                custom_target_slugs = @CustomTargetSlugs,
                start_date = @StartDate,
                end_date = @EndDate,
                is_active = @IsActive,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id AND is_deleted = FALSE;";
        var rows = await conn.ExecuteAsync(sql, new {
            Id = id,
            req.Title,
            req.PopupType,
            req.ImageUrl,
            req.BadgeText,
            req.Heading,
            req.Description,
            req.VideoUrl,
            req.TargetUrl,
            req.CtaText,
            req.SecondaryCtaText,
            req.SecondaryTargetUrl,
            req.OpenInNewTab,
            req.DelaySeconds,
            req.DisplayFrequency,
            req.TargetPages,
            req.CustomTargetSlugs,
            req.StartDate,
            req.EndDate,
            req.IsActive
        });
        return rows > 0;
    }

    public async Task<bool?> TogglePopupStatusAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE popup_announcements 
            SET is_active = NOT is_active, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = FALSE
            RETURNING is_active;";
        return await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { id });
    }

    public async Task<bool> DeletePopupAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE popup_announcements 
            SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP
            WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}

