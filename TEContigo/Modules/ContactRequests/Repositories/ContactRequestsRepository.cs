using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.ContactRequests.Models;

namespace TEContigo.Modules.ContactRequests.Repositories;

public class ContactRequestsRepository : IContactRequestsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ContactRequestsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ContactRequestsModel>> GetAllAsync()
    {
        const string sql = """
            SELECT id AS Id,
                   found_item_id AS FoundItemId,
                   requester_id AS RequesterId,
                   match_id AS MatchId,
                   status AS Status,
                   created_at AS CreatedAt,
                   responded_at AS RespondedAt
            FROM ContactRequests
            ORDER BY created_at DESC
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<ContactRequestsModel>(sql);
    }

    public async Task<IEnumerable<ContactRequestsModel>> GetAllForUserAsync(long userId)
    {
        const string sql = """
            SELECT cr.id AS Id,
                   cr.found_item_id AS FoundItemId,
                   cr.requester_id AS RequesterId,
                   cr.match_id AS MatchId,
                   cr.status AS Status,
                   cr.created_at AS CreatedAt,
                   cr.responded_at AS RespondedAt
            FROM ContactRequests cr
            INNER JOIN FoundItems fi ON fi.id = cr.found_item_id
            WHERE cr.requester_id = @UserId OR fi.user_id = @UserId
            ORDER BY cr.created_at DESC
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<ContactRequestsModel>(sql, new { UserId = userId });
    }

    public async Task<ContactRequestsModel?> GetByIdAsync(long id)
    {
        const string sql = """
            SELECT id AS Id,
                   found_item_id AS FoundItemId,
                   requester_id AS RequesterId,
                   match_id AS MatchId,
                   status AS Status,
                   created_at AS CreatedAt,
                   responded_at AS RespondedAt
            FROM ContactRequests
            WHERE id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<ContactRequestsModel>(sql, new { Id = id });
    }

    public async Task<bool> ExistsPendingAsync(long foundItemId, long requesterId)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM ContactRequests
                WHERE found_item_id = @FoundItemId
                  AND requester_id = @RequesterId
                  AND status = 'Pendiente'
            )
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new { FoundItemId = foundItemId, RequesterId = requesterId });
    }

    public async Task<long> CreateAsync(ContactRequestsModel contactRequest)
    {
        const string sql = """
            INSERT INTO ContactRequests (
                found_item_id, requester_id, match_id, status, created_at
            )
            VALUES (
                @FoundItemId, @RequesterId, @MatchId, @Status, @CreatedAt
            )
            RETURNING id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(sql, contactRequest);
    }

    public async Task<bool> TryRespondAsync(
        long id,
        string fromStatus,
        string toStatus,
        DateTime respondedAt)
    {
        const string sql = """
            UPDATE ContactRequests
            SET status = @ToStatus,
                responded_at = @RespondedAt
            WHERE id = @Id
              AND status = @FromStatus
            """;

        using var connection = _connectionFactory.CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            sql,
            new { Id = id, FromStatus = fromStatus, ToStatus = toStatus, RespondedAt = respondedAt });

        return affectedRows > 0;
    }
}