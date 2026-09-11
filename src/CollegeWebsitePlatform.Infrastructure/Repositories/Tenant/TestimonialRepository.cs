using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class TestimonialRepository
{
    private readonly ITenantDbConnectionFactory _db;

    public TestimonialRepository(ITenantDbConnectionFactory db) => _db = db;

    public async Task<PaginatedResponse<TestimonialDto>> GetAllPaginatedAsync(PaginationFilter filter, bool publishedOnly = false)
    {
        using var conn = _db.CreateConnection();
        var page = filter.Page > 0 ? filter.Page : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var offset = (page - 1) * pageSize;

        var conditions = new List<string> { "is_deleted = FALSE" };
        var param = new DynamicParameters();

        if (publishedOnly) conditions.Add("is_published = TRUE");
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(LOWER(student_name) LIKE LOWER(@Search) OR LOWER(course_name) LIKE LOWER(@Search) OR LOWER(current_company) LIKE LOWER(@Search))");
            param.Add("Search", $"%{filter.Search.Trim()}%");
        }

        var whereClause = "WHERE " + string.Join(" AND ", conditions);

        var totalCount = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM testimonials {whereClause};", param);

        param.Add("Limit", pageSize);
        param.Add("Offset", offset);

        var dataSql = $@"
            SELECT id, student_name AS StudentName, batch_year AS BatchYear, course_name AS CourseName, 
                   content, photo_url AS PhotoUrl, current_company AS CurrentCompany, rating, 
                   is_published AS IsPublished, sort_order AS SortOrder
            FROM testimonials 
            {whereClause}
            ORDER BY sort_order ASC, created_at DESC 
            LIMIT @Limit OFFSET @Offset;";

        var items = (await conn.QueryAsync<TestimonialDto>(dataSql, param)).ToList();

        return new PaginatedResponse<TestimonialDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<IEnumerable<TestimonialDto>> GetAllPublishedAsync(int limit = 10)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, student_name AS StudentName, batch_year AS BatchYear, course_name AS CourseName, 
                   content, photo_url AS PhotoUrl, current_company AS CurrentCompany, rating, 
                   is_published AS IsPublished, sort_order AS SortOrder
            FROM testimonials 
            WHERE is_published = TRUE AND is_deleted = FALSE 
            ORDER BY sort_order ASC, created_at DESC 
            LIMIT @limit;";

        return await conn.QueryAsync<TestimonialDto>(sql, new { limit });
    }

    public async Task<TestimonialDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, student_name AS StudentName, batch_year AS BatchYear, course_name AS CourseName, 
                   content, photo_url AS PhotoUrl, current_company AS CurrentCompany, rating, 
                   is_published AS IsPublished, sort_order AS SortOrder
            FROM testimonials 
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<TestimonialDto>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(CreateTestimonialRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO testimonials (student_name, batch_year, course_name, content, photo_url, current_company, rating, is_published, sort_order, created_at)
            VALUES (@StudentName, @BatchYear, @CourseName, @Content, @PhotoUrl, @CurrentCompany, @Rating, TRUE, @SortOrder, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTestimonialRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE testimonials 
            SET student_name = @StudentName, batch_year = @BatchYear, course_name = @CourseName, 
                content = @Content, photo_url = @PhotoUrl, current_company = @CurrentCompany, 
                rating = @Rating, sort_order = @SortOrder 
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.StudentName, req.BatchYear, req.CourseName, req.Content, req.PhotoUrl, req.CurrentCompany, req.Rating, req.SortOrder });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.ExecuteAsync("UPDATE testimonials SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP WHERE id = @id;", new { id });
        return rows > 0;
    }
}
