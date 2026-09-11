using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.FoundItems.Models;

namespace TEContigo.Modules.FoundItems.Repositories;

public class FoundItemsRepository : IFoundItemsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FoundItemsRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<FoundItemsModel>> GetAllAsync()
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            SELECT
                id,
                user_id AS UserId,
                category AS Category,
                article AS Article,
                color AS Color,
                location AS Location,
                description AS Description,
                photo_path AS PhotoPath,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM FoundItems
            ORDER BY created_at DESC;
            """;

        return await connection.QueryAsync<FoundItemsModel>(sql);
    }

    public async Task<FoundItemsModel?> GetByIdAsync(long id)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            SELECT
                id,
                user_id AS UserId,
                category AS Category,
                article AS Article,
                color AS Color,
                location AS Location,
                description AS Description,
                photo_path AS PhotoPath,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM FoundItems
            WHERE id = @Id;
            """;

        return await connection.QueryFirstOrDefaultAsync<FoundItemsModel>(
            sql,
            new { Id = id });
    }

    public async Task<long> CreateAsync(
        FoundItemsModel foundItem)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO FoundItems
            (
                user_id,
                category,
                article,
                color,
                location,
                description,
                photo_path,
                status,
                created_at,
                updated_at
            )
            VALUES
            (
                @UserId,
                @Category,
                @Article,
                @Color,
                @Location,
                @Description,
                @PhotoPath,
                @Status,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            )
            RETURNING id;
            """;

        return await connection.ExecuteScalarAsync<long>(
            sql,
            foundItem);
    }

    public async Task UpdateAsync(
        FoundItemsModel foundItem)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE FoundItems
            SET
                category = @Category,
                article = @Article,
                color = @Color,
                location = @Location,
                description = @Description,
                photo_path = @PhotoPath,
                status = @Status,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id;
            """;

        await connection.ExecuteAsync(
            sql,
            foundItem);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM FoundItems
            WHERE id = @Id;
            """;

        await connection.ExecuteAsync(
            sql,
            new { Id = id });
    }
}