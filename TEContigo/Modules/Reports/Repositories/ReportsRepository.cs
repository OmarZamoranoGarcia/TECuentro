using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Reports.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Reports.Repositories;

public class ReportsRepository : IReportsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<ReportsModel>> GetAllAsync(int pageNumber, int pageSize)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string countSql = """
        SELECT COUNT(*) FROM Reports;
    """;

        const string dataSql = """
        SELECT
            id,
            user_id AS UserId,
            name AS Name,
            category AS Category,
            photo_path AS PhotoPath,
            description AS Description,
            status AS Status,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM Reports
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset;
    """;

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

        var offset = (pageNumber - 1) * pageSize;

        var items = await connection.QueryAsync<ReportsModel>(
            dataSql,
            new { PageSize = pageSize, Offset = offset });

        return new PagedResultDto<ReportsModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ReportsModel?> GetByIdAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT
                id,
                user_id AS UserId,
                name AS Name,
                category AS Category,
                photo_path AS PhotoPath,
                description AS Description,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM Reports
            WHERE id = @Id;
         """;

        return await connection.QueryFirstOrDefaultAsync<ReportsModel>(
            sql,
            new { Id = id });
    }

    public async Task<long> CreateAsync(ReportsModel report)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO Reports
            (
                user_id,
                name,
                category,
                photo_path,
                description,
                status,
                created_at,
                updated_at
            )
            VALUES
            (
                @UserId,
                @Name,
                @Category,
                @PhotoPath,
                @Description,
                @Status,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            )
            RETURNING id;
         """;

        return await connection.ExecuteScalarAsync<long>(
            sql,
            report);
    }

    public async Task UpdateAsync(ReportsModel report)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Reports
            SET
                name = @Name,
                category = @Category,
                photo_path = @PhotoPath,
                description = @Description,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id;
         """;

        await connection.ExecuteAsync(sql, report);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM Reports
            WHERE id = @Id;
         """;

        await connection.ExecuteAsync(
            sql,
            new { Id = id });
    }
}