using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class DepartmentRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public DepartmentRepository(ITenantDbConnectionFactory db) => _db = db;

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
            SELECT id, name, code, description, head_of_department AS HeadOfDepartment, email, phone, image_url AS ImageUrl, is_active AS IsActive
            FROM departments 
            {whereClause}
            ORDER BY name ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<DepartmentDto>(dataSql, param)).ToList();

        return new PaginatedResponse<DepartmentDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT id, name, code, description, head_of_department AS HeadOfDepartment, email, phone, image_url AS ImageUrl, is_active AS IsActive FROM departments WHERE id = @id AND is_deleted = FALSE;";
        var dept = await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { id });
        if (dept == null) return null;

        var courses = await conn.QueryAsync<CourseDto>(
            "SELECT id, department_id AS DepartmentId, name, code, degree_level AS DegreeLevel, duration_years AS DurationYears, description, eligibility, fee_structure AS FeeStructure, is_active AS IsActive FROM courses WHERE department_id = @id AND is_deleted = FALSE ORDER BY name ASC;",
            new { id });
        dept.Courses = courses.ToList();

        var faculty = await conn.QueryAsync<FacultyDto>(
            "SELECT id, department_id AS DepartmentId, name, designation, qualification, specialization, email, profile_image_url AS ProfileImageUrl, bio, sort_order AS SortOrder, is_active AS IsActive FROM faculty WHERE department_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, name ASC;",
            new { id });
        dept.Faculty = faculty.ToList();

        return dept;
    }

    public async Task<DepartmentDto?> GetByCodeAsync(string code)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT id, name, code, description, head_of_department AS HeadOfDepartment, email, phone, image_url AS ImageUrl, is_active AS IsActive FROM departments WHERE LOWER(code) = LOWER(@code) AND is_deleted = FALSE;";
        var dept = await conn.QueryFirstOrDefaultAsync<DepartmentDto>(sql, new { code });
        if (dept == null) return null;

        var courses = await conn.QueryAsync<CourseDto>(
            "SELECT id, department_id AS DepartmentId, name, code, degree_level AS DegreeLevel, duration_years AS DurationYears, description, eligibility, fee_structure AS FeeStructure, is_active AS IsActive FROM courses WHERE department_id = @id AND is_deleted = FALSE ORDER BY name ASC;",
            new { id = dept.Id });
        dept.Courses = courses.ToList();

        var faculty = await conn.QueryAsync<FacultyDto>(
            "SELECT id, department_id AS DepartmentId, name, designation, qualification, specialization, email, profile_image_url AS ProfileImageUrl, bio, sort_order AS SortOrder, is_active AS IsActive FROM faculty WHERE department_id = @id AND is_deleted = FALSE ORDER BY sort_order ASC, name ASC;",
            new { id = dept.Id });
        dept.Faculty = faculty.ToList();

        return dept;
    }

    public async Task<Guid> CreateAsync(CreateDepartmentRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO departments (name, code, description, head_of_department, email, phone, image_url, is_active, created_at, updated_at)
            VALUES (@Name, UPPER(@Code), @Description, @HeadOfDepartment, @Email, @Phone, @ImageUrl, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateDepartmentRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE departments 
            SET name = @Name, code = UPPER(@Code), description = @Description, head_of_department = @HeadOfDepartment, 
                email = @Email, phone = @Phone, image_url = @ImageUrl, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.Name, req.Code, req.Description, req.HeadOfDepartment, req.Email, req.Phone, req.ImageUrl, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE departments SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
