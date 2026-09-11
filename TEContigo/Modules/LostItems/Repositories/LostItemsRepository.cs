using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.LostItems.Models;

namespace TEContigo.Modules.LostItems.Repositories;

public class LostItemsRepository : ILostItemsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LostItemsRepository(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<LostItemsModel>> GetAllAsync()
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
            FROM LostItems
            ORDER BY created_at DESC;
         """;

        return await connection.QueryAsync<LostItemsModel>(sql);
    }

    public async Task<LostItemsModel?> GetByIdAsync(long id)
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
            FROM LostItems
            WHERE id = @Id;
         """;

        return await connection.QueryFirstOrDefaultAsync<LostItemsModel>(
            sql,
            new { Id = id });
    }

    public async Task<long> CreateAsync(LostItemsModel lostItem)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO LostItems
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
            lostItem);
    }

    public async Task UpdateAsync(LostItemsModel lostItem)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE LostItems
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
            lostItem);
    }

    public async Task DeleteAsync(long id)
    {
        using var connection =
            _connectionFactory.CreateConnection();

        const string sql = """
            DELETE FROM LostItems
            WHERE id = @Id;
         """;

        await connection.ExecuteAsync(
            sql,
            new { Id = id });
    }
}