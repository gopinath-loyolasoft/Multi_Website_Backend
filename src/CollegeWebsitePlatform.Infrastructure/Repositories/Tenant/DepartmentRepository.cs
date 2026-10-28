using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class DepartmentRepository(ITenantDbConnectionFactory db)
{
    private readonly ITenantDbConnectionFactory _db = db;

    public async Task<PaginatedResponse<DepartmentDto>> GetAllPaginatedAsync(PaginationFilter filter, bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (activeOnly) conditions.Add("is_active = TRUE");
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(name) LIKE LOWER(@Search) OR LOWER(code) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM departments {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, name, code, slug, description, head_of_department AS HeadOfDepartment,
                   hod_message AS HodMessage, hod_photo_url AS HodPhotoUrl, vision_mission AS VisionMission,
                   stream, total_semesters AS TotalSemesters, lab_count AS LabCount, student_count AS StudentCount,
                   laboratories_json AS LaboratoriesJson, syllabi_json AS SyllabiJson, syllabus_pdf_url AS SyllabusPdfUrl,
                   email, phone, image_url AS ImageUrl, is_active AS IsActive
            FROM departments 
            {whereClause}
            ORDER BY name ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<DepartmentDto>(dataSql, param)).ToList();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.LaboratoriesJson))
            {
                try { item.Laboratories = System.Text.Json.JsonSerializer.Deserialize<List<LaboratoryItemDto>>(item.LaboratoriesJson, jsonOptions) ?? []; } catch { }
            }
            if (!string.IsNullOrWhiteSpace(item.SyllabiJson))
            {
                try { item.Syllabi = System.Text.Json.JsonSerializer.Deserialize<List<DepartmentSyllabusItemDto>>(item.SyllabiJson, jsonOptions) ?? []; } catch { }
            }
        }

        return new PaginatedResponse<DepartmentDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, name, code, slug, description, head_of_department AS HeadOfDepartment,
                   hod_message AS HodMessage, hod_photo_url AS HodPhotoUrl, vision_mission AS VisionMission,
                   stream, total_semesters AS TotalSemesters, lab_count AS LabCount, student_count AS StudentCount,
                   laboratories_json AS LaboratoriesJson, syllabi_json AS SyllabiJson, syllabus_pdf_url AS SyllabusPdfUrl,
                   email, phone, image_url AS ImageUrl, is_active AS IsActive 
            FROM departments 
            WHERE id = @id AND is_deleted = FALSE;";
        var dept = await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { id });
        if (dept == null) return null;

        var jsonOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (!string.IsNullOrWhiteSpace(dept.LaboratoriesJson))
        {
            try { dept.Laboratories = System.Text.Json.JsonSerializer.Deserialize<List<LaboratoryItemDto>>(dept.LaboratoriesJson, jsonOpt) ?? []; } catch { }
        }
        if (!string.IsNullOrWhiteSpace(dept.SyllabiJson))
        {
            try { dept.Syllabi = System.Text.Json.JsonSerializer.Deserialize<List<DepartmentSyllabusItemDto>>(dept.SyllabiJson, jsonOpt) ?? []; } catch { }
        }

        var courses = await conn.QueryAsync<CourseDto>(
            @"SELECT id, department_id AS DepartmentId, name, code, slug, degree_level AS DegreeLevel, 
                     duration_years AS DurationYears, total_semesters AS TotalSemesters, regulation_year AS RegulationYear,
                     intake, semester_syllabi_json AS SemesterSyllabiJson, curriculum_highlights_json AS CurriculumHighlightsJson,
                     description, eligibility, fee_structure AS FeeStructure, tuition_fee AS TuitionFee,
                     syllabus_pdf_url AS SyllabusPdfUrl, is_active AS IsActive 
              FROM courses 
              WHERE department_id = @id AND is_deleted = FALSE 
              ORDER BY name ASC;",
            new { id });
        dept.Courses = courses.ToList();
        foreach (var c in dept.Courses)
        {
            if (!string.IsNullOrWhiteSpace(c.SemesterSyllabiJson))
            {
                try { c.SemesterSyllabi = System.Text.Json.JsonSerializer.Deserialize<List<SemesterSyllabusItemDto>>(c.SemesterSyllabiJson, jsonOpt) ?? []; } catch { }
            }
            if (!string.IsNullOrWhiteSpace(c.CurriculumHighlightsJson))
            {
                try { c.CurriculumHighlights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(c.CurriculumHighlightsJson, jsonOpt) ?? []; } catch { }
            }
        }

        var faculty = await conn.QueryAsync<FacultyDto>(
            "SELECT id, department_id AS DepartmentId, name, designation, qualification, specialization, qualification_details AS QualificationDetails, research_interests AS ResearchInterests, email, phone, office_room AS OfficeRoom, profile_image_url AS ProfileImageUrl, bio, sort_order AS SortOrder, is_active AS IsActive FROM faculty WHERE department_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, name ASC;",
            new { id });
        dept.Faculty = faculty.ToList();

        return dept;
    }

    public async Task<DepartmentDto?> GetByCodeAsync(string code)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, name, code, slug, description, head_of_department AS HeadOfDepartment,
                   hod_message AS HodMessage, hod_photo_url AS HodPhotoUrl, vision_mission AS VisionMission,
                   stream, total_semesters AS TotalSemesters, lab_count AS LabCount, student_count AS StudentCount,
                   laboratories_json AS LaboratoriesJson, syllabi_json AS SyllabiJson, syllabus_pdf_url AS SyllabusPdfUrl,
                   email, phone, image_url AS ImageUrl, is_active AS IsActive 
            FROM departments 
            WHERE (LOWER(code) = LOWER(@code) OR LOWER(slug) = LOWER(@code)) AND is_deleted = FALSE;";
        var dept = await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { code });
        if (dept == null) return null;

        var jsonOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (!string.IsNullOrWhiteSpace(dept.LaboratoriesJson))
        {
            try { dept.Laboratories = System.Text.Json.JsonSerializer.Deserialize<List<LaboratoryItemDto>>(dept.LaboratoriesJson, jsonOpt) ?? []; } catch { }
        }
        if (!string.IsNullOrWhiteSpace(dept.SyllabiJson))
        {
            try { dept.Syllabi = System.Text.Json.JsonSerializer.Deserialize<List<DepartmentSyllabusItemDto>>(dept.SyllabiJson, jsonOpt) ?? []; } catch { }
        }

        var courses = await conn.QueryAsync<CourseDto>(
            @"SELECT id, department_id AS DepartmentId, name, code, slug, degree_level AS DegreeLevel, 
                     duration_years AS DurationYears, total_semesters AS TotalSemesters, regulation_year AS RegulationYear,
                     intake, semester_syllabi_json AS SemesterSyllabiJson, curriculum_highlights_json AS CurriculumHighlightsJson,
                     description, eligibility, fee_structure AS FeeStructure, tuition_fee AS TuitionFee,
                     syllabus_pdf_url AS SyllabusPdfUrl, is_active AS IsActive 
              FROM courses 
              WHERE department_id = @id AND is_deleted = FALSE 
              ORDER BY name ASC;",
            new { id = dept.Id });
        dept.Courses = courses.ToList();
        foreach (var c in dept.Courses)
        {
            if (!string.IsNullOrWhiteSpace(c.SemesterSyllabiJson))
            {
                try { c.SemesterSyllabi = System.Text.Json.JsonSerializer.Deserialize<List<SemesterSyllabusItemDto>>(c.SemesterSyllabiJson, jsonOpt) ?? []; } catch { }
            }
            if (!string.IsNullOrWhiteSpace(c.CurriculumHighlightsJson))
            {
                try { c.CurriculumHighlights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(c.CurriculumHighlightsJson, jsonOpt) ?? []; } catch { }
            }
        }

        var faculty = await conn.QueryAsync<FacultyDto>(
            "SELECT id, department_id AS DepartmentId, name, designation, qualification, specialization, qualification_details AS QualificationDetails, research_interests AS ResearchInterests, email, phone, office_room AS OfficeRoom, profile_image_url AS ProfileImageUrl, bio, sort_order AS SortOrder, is_active AS IsActive FROM faculty WHERE department_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, name ASC;",
            new { id = dept.Id });
        dept.Faculty = faculty.ToList();

        return dept;
    }

    public async Task<Guid> CreateAsync(CreateDepartmentRequest req)
    {
        using var conn = _db.CreateConnection();
        var slug = !string.IsNullOrWhiteSpace(req.Slug)
            ? CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Slug)
            : !string.IsNullOrWhiteSpace(req.Code)
                ? req.Code.ToLowerInvariant().Trim()
                : CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Name);

        var sql = @"
            INSERT INTO departments (name, code, slug, description, head_of_department, hod_message, hod_photo_url, vision_mission, stream, total_semesters, lab_count, student_count, laboratories_json, syllabi_json, syllabus_pdf_url, email, phone, image_url, is_active, created_at, updated_at)
            VALUES (@Name, UPPER(@Code), @slug, @Description, @HeadOfDepartment, @HodMessage, @HodPhotoUrl, @VisionMission, @Stream, @TotalSemesters, @LabCount, @StudentCount, @LaboratoriesJson, @SyllabiJson, @SyllabusPdfUrl, @Email, @Phone, @ImageUrl, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { req.Name, req.Code, slug, req.Description, req.HeadOfDepartment, req.HodMessage, req.HodPhotoUrl, req.VisionMission, req.Stream, req.TotalSemesters, req.LabCount, req.StudentCount, req.LaboratoriesJson, req.SyllabiJson, req.SyllabusPdfUrl, req.Email, req.Phone, req.ImageUrl });
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateDepartmentRequest req)
    {
        using var conn = _db.CreateConnection();
        var slug = !string.IsNullOrWhiteSpace(req.Slug)
            ? CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Slug)
            : !string.IsNullOrWhiteSpace(req.Code)
                ? req.Code.ToLowerInvariant().Trim()
                : CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Name);

        var sql = @"
            UPDATE departments 
            SET name = @Name, code = UPPER(@Code), slug = @slug, description = @Description, head_of_department = @HeadOfDepartment, 
                hod_message = @HodMessage, hod_photo_url = @HodPhotoUrl, vision_mission = @VisionMission, stream = @Stream,
                total_semesters = @TotalSemesters, lab_count = @LabCount, student_count = @StudentCount, laboratories_json = @LaboratoriesJson,
                syllabi_json = @SyllabiJson, syllabus_pdf_url = @SyllabusPdfUrl,
                email = @Email, phone = @Phone, image_url = @ImageUrl, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP 
                WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.Name, req.Code, slug, req.Description, req.HeadOfDepartment, req.HodMessage, req.HodPhotoUrl, req.VisionMission, req.Stream, req.TotalSemesters, req.LabCount, req.StudentCount, req.LaboratoriesJson, req.SyllabiJson, req.SyllabusPdfUrl, req.Email, req.Phone, req.ImageUrl, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE departments SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
