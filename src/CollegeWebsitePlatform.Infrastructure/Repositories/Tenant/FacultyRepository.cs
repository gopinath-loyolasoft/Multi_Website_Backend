using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class FacultyRepository(ITenantDbConnectionFactory db)
{
    private readonly ITenantDbConnectionFactory _db = db;

    public async Task<PaginatedResponse<FacultyDto>> GetAllPaginatedAsync(PaginationFilter filter, Guid? departmentId = null, bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "f.is_deleted = FALSE", "d.is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (activeOnly) conditions.Add("f.is_active = TRUE");
        if (departmentId.HasValue)
        {
            conditions.Add("f.department_id = @DepartmentId");
            param.Add("DepartmentId", departmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(f.name) LIKE LOWER(@Search) OR LOWER(f.designation) LIKE LOWER(@Search) OR LOWER(f.specialization) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($@"
            SELECT COUNT(*) 
            FROM faculty f 
            INNER JOIN departments d ON d.id = f.department_id
            {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT f.id, f.department_id AS DepartmentId, d.name AS DepartmentName, f.name, f.designation, 
                   f.qualification, f.specialization, f.qualification_details AS QualificationDetails,
                   f.research_interests AS ResearchInterests, f.email, f.phone, f.office_room AS OfficeRoom,
                   f.profile_image_url AS ProfileImageUrl, f.bio, f.sort_order AS SortOrder, f.is_active AS IsActive
            FROM faculty f
            INNER JOIN departments d ON d.id = f.department_id
            {whereClause}
            ORDER BY f.sort_order ASC, f.name ASC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<FacultyDto>(dataSql, param)).ToList();

        return new PaginatedResponse<FacultyDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<FacultyDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT f.id, f.department_id AS DepartmentId, d.name AS DepartmentName, f.name, f.designation, 
                   f.qualification, f.specialization, f.qualification_details AS QualificationDetails,
                   f.research_interests AS ResearchInterests, f.email, f.phone, f.office_room AS OfficeRoom,
                   f.profile_image_url AS ProfileImageUrl, f.bio, f.sort_order AS SortOrder, f.is_active AS IsActive
            FROM faculty f
            INNER JOIN departments d ON d.id = f.department_id
            WHERE f.id = @id AND f.is_deleted = FALSE AND d.is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<FacultyDto>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(CreateFacultyRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO faculty (department_id, name, designation, qualification, specialization, qualification_details, research_interests, email, phone, office_room, profile_image_url, bio, sort_order, is_active, created_at, updated_at)
            VALUES (@DepartmentId, @Name, @Designation, @Qualification, @Specialization, @QualificationDetails, @ResearchInterests, @Email, @Phone, @OfficeRoom, @ProfileImageUrl, @Bio, @SortOrder, TRUE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateFacultyRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE faculty 
            SET department_id = @DepartmentId, name = @Name, designation = @Designation, qualification = @Qualification, 
                specialization = @Specialization, qualification_details = @QualificationDetails, research_interests = @ResearchInterests,
                email = @Email, phone = @Phone, office_room = @OfficeRoom, profile_image_url = @ProfileImageUrl, 
                bio = @Bio, sort_order = @SortOrder, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.DepartmentId, req.Name, req.Designation, req.Qualification, req.Specialization, req.QualificationDetails, req.ResearchInterests, req.Email, req.Phone, req.OfficeRoom, req.ProfileImageUrl, req.Bio, req.SortOrder, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE faculty SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
