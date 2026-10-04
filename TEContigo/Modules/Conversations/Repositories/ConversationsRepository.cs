using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Conversations.Models;

namespace TEContigo.Modules.Conversations.Repositories;

public class ConversationsRepository : IConversationsRepository
{
    // Dapper no mapea directamente a ValueTuple, así que usamos este
    // record intermedio y lo convertimos a tupla al final del método.
    private sealed record ReturnConfirmRow(bool RequesterConfirmed, bool FinderConfirmed);

    private readonly IDbConnectionFactory _connectionFactory;

    public ConversationsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ConversationsModel>> GetAllAsync()
    {
        const string sql = """
            SELECT id AS Id,
                   match_id AS MatchId,
                   contact_request_id AS ContactRequestId,
                   requester_confirmed_return AS RequesterConfirmedReturn,
                   finder_confirmed_return AS FinderConfirmedReturn,
                   created_at AS CreatedAt,
                   closed_at AS ClosedAt
            FROM Conversations
            ORDER BY created_at DESC
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<ConversationsModel>(sql);
    }

    public async Task<IEnumerable<ConversationsModel>> GetAllForUserAsync(long userId)
    {
        // Dos caminos posibles, unidos con LEFT JOIN: si viene de un
        // Match, el usuario participa si es dueño del LostItem o del
        // FoundItem; si viene de un ContactRequest, participa si es
        // el requester o el dueño del FoundItem.
        const string sql = """
            SELECT c.id AS Id,
                   c.match_id AS MatchId,
                   c.contact_request_id AS ContactRequestId,
                   c.requester_confirmed_return AS RequesterConfirmedReturn,
                   c.finder_confirmed_return AS FinderConfirmedReturn,
                   c.created_at AS CreatedAt,
                   c.closed_at AS ClosedAt
            FROM Conversations c
            LEFT JOIN Matches m ON m.id = c.match_id
            LEFT JOIN LostItems li ON li.id = m.lost_item_id
            LEFT JOIN FoundItems fi_m ON fi_m.id = m.found_item_id
            LEFT JOIN ContactRequests cr ON cr.id = c.contact_request_id
            LEFT JOIN FoundItems fi_cr ON fi_cr.id = cr.found_item_id
            WHERE
                (c.match_id IS NOT NULL AND (li.user_id = @UserId OR fi_m.user_id = @UserId))
                OR
                (c.contact_request_id IS NOT NULL AND (cr.requester_id = @UserId OR fi_cr.user_id = @UserId))
            ORDER BY c.created_at DESC
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<ConversationsModel>(sql, new { UserId = userId });
    }

    public async Task<ConversationsModel?> GetByIdAsync(long id)
    {
        const string sql = """
            SELECT id AS Id,
                   match_id AS MatchId,
                   contact_request_id AS ContactRequestId,
                   requester_confirmed_return AS RequesterConfirmedReturn,
                   finder_confirmed_return AS FinderConfirmedReturn,
                   created_at AS CreatedAt,
                   closed_at AS ClosedAt
            FROM Conversations
            WHERE id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<ConversationsModel>(sql, new { Id = id });
    }

    public async Task<ConversationsModel?> GetByMatchIdAsync(long matchId)
    {
        const string sql = """
            SELECT id AS Id,
                   match_id AS MatchId,
                   contact_request_id AS ContactRequestId,
                   requester_confirmed_return AS RequesterConfirmedReturn,
                   finder_confirmed_return AS FinderConfirmedReturn,
                   created_at AS CreatedAt,
                   closed_at AS ClosedAt
            FROM Conversations
            WHERE match_id = @MatchId
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<ConversationsModel>(sql, new { MatchId = matchId });
    }

    public async Task<ConversationsModel?> GetByContactRequestIdAsync(long contactRequestId)
    {
        const string sql = """
            SELECT id AS Id,
                   match_id AS MatchId,
                   contact_request_id AS ContactRequestId,
                   requester_confirmed_return AS RequesterConfirmedReturn,
                   finder_confirmed_return AS FinderConfirmedReturn,
                   created_at AS CreatedAt,
                   closed_at AS ClosedAt
            FROM Conversations
            WHERE contact_request_id = @ContactRequestId
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<ConversationsModel>(
            sql, new { ContactRequestId = contactRequestId });
    }

    public async Task<long> CreateFromMatchAsync(long matchId)
    {
        const string sql = """
            INSERT INTO Conversations (match_id, created_at)
            VALUES (@MatchId, @CreatedAt)
            RETURNING id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(
            sql, new { MatchId = matchId, CreatedAt = DateTime.UtcNow });
    }

    public async Task<long> CreateFromContactRequestAsync(long contactRequestId)
    {
        const string sql = """
            INSERT INTO Conversations (contact_request_id, created_at)
            VALUES (@ContactRequestId, @CreatedAt)
            RETURNING id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(
            sql, new { ContactRequestId = contactRequestId, CreatedAt = DateTime.UtcNow });
    }

    public async Task<(bool RequesterConfirmed, bool FinderConfirmed)?> TryConfirmReturnAsync(
        long conversationId,
        bool isRequester)
    {
        var sql = isRequester
            ? """
              UPDATE Conversations
              SET requester_confirmed_return = TRUE
              WHERE id = @ConversationId
                AND requester_confirmed_return = FALSE
              RETURNING requester_confirmed_return AS RequesterConfirmed,
                        finder_confirmed_return AS FinderConfirmed
              """
            : """
              UPDATE Conversations
              SET finder_confirmed_return = TRUE
              WHERE id = @ConversationId
                AND finder_confirmed_return = FALSE
              RETURNING requester_confirmed_return AS RequesterConfirmed,
                        finder_confirmed_return AS FinderConfirmed
              """;

        using var connection = _connectionFactory.CreateConnection();

        var result = await connection.QuerySingleOrDefaultAsync<ReturnConfirmRow?>(
            sql,
            new { ConversationId = conversationId });

        if (result != null)
        {
            return (result.RequesterConfirmed, result.FinderConfirmed);
        }

        const string currentStateSql = """
            SELECT requester_confirmed_return AS RequesterConfirmed,
                   finder_confirmed_return AS FinderConfirmed
            FROM Conversations
            WHERE id = @ConversationId
            """;

        var currentState = await connection.QuerySingleOrDefaultAsync<ReturnConfirmRow?>(
            currentStateSql,
            new { ConversationId = conversationId });

        return currentState == null
            ? null
            : (currentState.RequesterConfirmed, currentState.FinderConfirmed);
    }

    public async Task<bool> TryCloseAsync(long conversationId)
    {
        const string sql = """
            UPDATE Conversations
            SET closed_at = CURRENT_TIMESTAMP
            WHERE id = @ConversationId
              AND closed_at IS NULL
            """;

        using var connection = _connectionFactory.CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            sql, new { ConversationId = conversationId });

        return affectedRows > 0;
    }
}