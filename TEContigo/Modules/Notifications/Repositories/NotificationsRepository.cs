using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Notifications.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Notifications.Repositories;

public class NotificationsRepository : INotificationsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public NotificationsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<NotificationsModel>> GetByUserIdAsync(
        long userId,
        int pageNumber,
        int pageSize)
    {
        const string countSql = """
            SELECT COUNT(*) FROM Notifications
            WHERE user_id = @UserId;
        """;

        const string dataSql = """
            SELECT id AS Id,
                   user_id AS UserId,
                   type AS Type,
                   title AS Title,
                   message AS Message,
                   reference_type AS ReferenceType,
                   reference_id AS ReferenceId,
                   is_read AS IsRead,
                   created_at AS CreatedAt
            FROM Notifications
            WHERE user_id = @UserId
            ORDER BY created_at DESC, id DESC
            LIMIT @PageSize OFFSET @Offset;
        """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql, new { UserId = userId });

        var offset = (pageNumber - 1) * pageSize;

        var items = await connection.QueryAsync<NotificationsModel>(
            dataSql,
            new { UserId = userId, PageSize = pageSize, Offset = offset });

        return new PagedResultDto<NotificationsModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> CountUnreadAsync(long userId)
    {
        const string sql = """
            SELECT COUNT(*) FROM Notifications
            WHERE user_id = @UserId AND is_read = FALSE;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<bool> HasUnreadAsync(
        long userId,
        string type,
        string referenceType,
        long referenceId)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM Notifications
                WHERE user_id = @UserId
                  AND type = @Type
                  AND reference_type = @ReferenceType
                  AND reference_id = @ReferenceId
                  AND is_read = FALSE
            );
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new
            {
                UserId = userId,
                Type = type,
                ReferenceType = referenceType,
                ReferenceId = referenceId
            });
    }

    public async Task<long> CreateAsync(NotificationsModel notification)
    {
        const string sql = """
            INSERT INTO Notifications (
                user_id, type, title, message,
                reference_type, reference_id, is_read, created_at
            )
            VALUES (
                @UserId, @Type, @Title, @Message,
                @ReferenceType, @ReferenceId, @IsRead, @CreatedAt
            )
            RETURNING id;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(sql, notification);
    }

    public async Task<bool> MarkAsReadAsync(long id, long userId)
    {
        const string sql = """
            UPDATE Notifications
            SET is_read = TRUE
            WHERE id = @Id AND user_id = @UserId;
        """;

        using var connection = _connectionFactory.CreateConnection();

        var affected = await connection.ExecuteAsync(
            sql, new { Id = id, UserId = userId });

        return affected > 0;
    }

    public async Task<int> MarkAllAsReadAsync(long userId)
    {
        const string sql = """
            UPDATE Notifications
            SET is_read = TRUE
            WHERE user_id = @UserId AND is_read = FALSE;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteAsync(sql, new { UserId = userId });
    }

    public async Task<int> MarkReferenceAsReadAsync(
        long userId,
        string referenceType,
        long referenceId)
    {
        const string sql = """
            UPDATE Notifications
            SET is_read = TRUE
            WHERE user_id = @UserId
              AND reference_type = @ReferenceType
              AND reference_id = @ReferenceId
              AND is_read = FALSE;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteAsync(
            sql,
            new { UserId = userId, ReferenceType = referenceType, ReferenceId = referenceId });
    }
}