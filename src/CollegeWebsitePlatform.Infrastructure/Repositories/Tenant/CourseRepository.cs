using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class CourseRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public CourseRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<CourseDto>> GetAllPaginatedAsync(PaginationFilter filter, Guid? departmentId = null, bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "c.is_deleted = FALSE", "d.is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (activeOnly) conditions.Add("c.is_active = TRUE");
        if (departmentId.HasValue)
        {
            conditions.Add("c.department_id = @DepartmentId");
            param.Add("DepartmentId", departmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(c.name) LIKE LOWER(@Search) OR LOWER(c.code) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($@"
            SELECT COUNT(*) 
            FROM courses c 
            INNER JOIN departments d ON d.id = c.department_id
            {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT c.id, c.department_id AS DepartmentId, d.name AS DepartmentName, c.name, c.code, c.slug,
                   c.degree_level AS DegreeLevel, c.duration_years AS DurationYears, c.total_semesters AS TotalSemesters,
                   c.regulation_year AS RegulationYear, c.intake, c.semester_syllabi_json AS SemesterSyllabiJson,
                   c.curriculum_highlights_json AS CurriculumHighlightsJson, c.description, 
                   c.eligibility, c.fee_structure AS FeeStructure, c.tuition_fee AS TuitionFee,
                   c.syllabus_pdf_url AS SyllabusPdfUrl, c.is_active AS IsActive
            FROM courses c
            INNER JOIN departments d ON d.id = c.department_id
            {whereClause}
            ORDER BY c.name ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<CourseDto>(dataSql, param)).ToList();
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.SemesterSyllabiJson))
            {
                try { item.SemesterSyllabi = System.Text.Json.JsonSerializer.Deserialize<List<SemesterSyllabusItemDto>>(item.SemesterSyllabiJson, jsonOptions) ?? new(); } catch { }
            }
            if (!string.IsNullOrWhiteSpace(item.CurriculumHighlightsJson))
            {
                try { item.CurriculumHighlights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.CurriculumHighlightsJson, jsonOptions) ?? new(); } catch { }
            }
        }

        return new PaginatedResponse<CourseDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<CourseDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT c.id, c.department_id AS DepartmentId, d.name AS DepartmentName, c.name, c.code, c.slug,
                   c.degree_level AS DegreeLevel, c.duration_years AS DurationYears, c.total_semesters AS TotalSemesters,
                   c.regulation_year AS RegulationYear, c.intake, c.semester_syllabi_json AS SemesterSyllabiJson,
                   c.curriculum_highlights_json AS CurriculumHighlightsJson, c.description, 
                   c.eligibility, c.fee_structure AS FeeStructure, c.tuition_fee AS TuitionFee,
                   c.syllabus_pdf_url AS SyllabusPdfUrl, c.is_active AS IsActive
            FROM courses c
            INNER JOIN departments d ON d.id = c.department_id
            WHERE c.id = @id AND c.is_deleted = FALSE AND d.is_deleted = FALSE;";

        var item = await conn.QueryFirstOrDefaultAsync<CourseDto>(sql, new { id });
        if (item != null)
        {
            var jsonOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            if (!string.IsNullOrWhiteSpace(item.SemesterSyllabiJson))
            {
                try { item.SemesterSyllabi = System.Text.Json.JsonSerializer.Deserialize<List<SemesterSyllabusItemDto>>(item.SemesterSyllabiJson, jsonOpt) ?? new(); } catch { }
            }
            if (!string.IsNullOrWhiteSpace(item.CurriculumHighlightsJson))
            {
                try { item.CurriculumHighlights = System.Text.Json.JsonSerializer.Deserialize<List<string>>(item.CurriculumHighlightsJson, jsonOpt) ?? new(); } catch { }
            }
        }
        return item;
    }

    public async Task<Guid> CreateAsync(CreateCourseRequest req)
    {
        using var conn = _db.CreateConnection();
        var slug = !string.IsNullOrWhiteSpace(req.Slug)
            ? CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Slug)
            : !string.IsNullOrWhiteSpace(req.Code)
                ? req.Code.ToLowerInvariant().Trim()
                : CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Name);

        var sql = @"
            INSERT INTO courses (department_id, name, code, slug, degree_level, duration_years, total_semesters, regulation_year, intake, semester_syllabi_json, curriculum_highlights_json, description, eligibility, fee_structure, tuition_fee, syllabus_pdf_url, is_active, created_at, updated_at)
            VALUES (@DepartmentId, @Name, UPPER(@Code), @slug, @DegreeLevel, @DurationYears, @TotalSemesters, @RegulationYear, @Intake, @SemesterSyllabiJson, @CurriculumHighlightsJson, @Description, @Eligibility, @FeeStructure, @TuitionFee, @SyllabusPdfUrl, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, new { req.DepartmentId, req.Name, req.Code, slug, req.DegreeLevel, req.DurationYears, req.TotalSemesters, req.RegulationYear, req.Intake, req.SemesterSyllabiJson, req.CurriculumHighlightsJson, req.Description, req.Eligibility, req.FeeStructure, req.TuitionFee, req.SyllabusPdfUrl });
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCourseRequest req)
    {
        using var conn = _db.CreateConnection();
        var slug = !string.IsNullOrWhiteSpace(req.Slug)
            ? CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Slug)
            : !string.IsNullOrWhiteSpace(req.Code)
                ? req.Code.ToLowerInvariant().Trim()
                : CollegeWebsitePlatform.Domain.Common.SlugHelper.Slugify(req.Name);

        var sql = @"
            UPDATE courses 
            SET department_id = @DepartmentId, name = @Name, code = UPPER(@Code), slug = @slug, degree_level = @DegreeLevel, 
                duration_years = @DurationYears, total_semesters = @TotalSemesters, regulation_year = @RegulationYear,
                intake = @Intake, semester_syllabi_json = @SemesterSyllabiJson, curriculum_highlights_json = @CurriculumHighlightsJson,
                description = @Description, eligibility = @Eligibility, fee_structure = @FeeStructure,
                tuition_fee = @TuitionFee, syllabus_pdf_url = @SyllabusPdfUrl, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.DepartmentId, req.Name, req.Code, slug, req.DegreeLevel, req.DurationYears, req.TotalSemesters, req.RegulationYear, req.Intake, req.SemesterSyllabiJson, req.CurriculumHighlightsJson, req.Description, req.Eligibility, req.FeeStructure, req.TuitionFee, req.SyllabusPdfUrl, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE courses SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
