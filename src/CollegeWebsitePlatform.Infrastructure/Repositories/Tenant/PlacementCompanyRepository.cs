using CollegeWebsitePlatform.Application.Common.Interfaces;
using CollegeWebsitePlatform.Application.DTOs;
using Dapper;

namespace CollegeWebsitePlatform.Infrastructure.Repositories.Tenant;

public class PlacementCompanyRepository(ITenantDbConnectionFactory db)
{
    private readonly ITenantDbConnectionFactory _db = db;

    public async Task<IEnumerable<PlacementCompanyDto>> GetAllAsync(bool activeOnly = false)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, company_name AS CompanyName, company_logo_url AS CompanyLogoUrl, 
                   company_website AS CompanyWebsite, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at AS CreatedAt
            FROM placement_companies
            WHERE is_deleted = FALSE " + (activeOnly ? "AND is_active = TRUE " : "") + @"
            ORDER BY display_order ASC, company_name ASC;";

        return await conn.QueryAsync<PlacementCompanyDto>(sql);
    }

    public async Task<PlacementCompanyDto?> GetByIdAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            SELECT id, company_name AS CompanyName, company_logo_url AS CompanyLogoUrl, 
                   company_website AS CompanyWebsite, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at AS CreatedAt
            FROM placement_companies
            WHERE id = @id AND is_deleted = FALSE;";

        return await conn.QueryFirstOrDefaultAsync<PlacementCompanyDto>(sql, new { id });
    }

    public async Task<Guid> CreateAsync(CreatePlacementCompanyRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            INSERT INTO placement_companies (company_name, company_logo_url, company_website, display_order, is_active, created_at, updated_at)
            VALUES (@CompanyName, @CompanyLogoUrl, @CompanyWebsite, @DisplayOrder, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            RETURNING id;";

        return await conn.ExecuteScalarAsync<Guid>(sql, req);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdatePlacementCompanyRequest req)
    {
        using var conn = _db.CreateConnection();
        var sql = @"
            UPDATE placement_companies
            SET company_name = @CompanyName, company_logo_url = @CompanyLogoUrl, 
                company_website = @CompanyWebsite, display_order = @DisplayOrder, 
                is_active = @IsActive, updated_at = CURRENT_TIMESTAMP
            WHERE id = @id AND is_deleted = FALSE;";

        var rows = await conn.ExecuteAsync(sql, new { id, req.CompanyName, req.CompanyLogoUrl, req.CompanyWebsite, req.DisplayOrder, req.IsActive });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"UPDATE placement_companies SET is_deleted = TRUE, deleted_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = @id;";
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }
}
