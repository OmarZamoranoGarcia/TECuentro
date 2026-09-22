using Dapper;
using Npgsql;
using System.Data;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Matches.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Matches.Repositories;

public class MatchesRepository : IMatchesRepository
{
    // Dapper no mapea directamente a ValueTuple, así que usamos estos
    // records intermedios y los convertimos a tupla al final de cada método.
    private sealed record ChatRequestRow(bool LostRequested, bool FoundRequested);

    private sealed record ReturnConfirmRow(bool LostConfirmed, bool FoundConfirmed);

    private readonly IDbConnectionFactory _connectionFactory;

    public MatchesRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<MatchesModel>> GetAllAsync(int pageNumber, int pageSize)
    {
        const string countSql = """
        SELECT COUNT(*) FROM Matches;
    """;

        const string dataSql = """
        SELECT id AS Id,
               lost_item_id AS LostItemId,
               found_item_id AS FoundItemId,
               match_percentage AS MatchPercentage,
               lost_user_chat_request AS LostUserChatRequest,
               found_user_chat_request AS FoundUserChatRequest,
               status AS Status,
               lost_user_return_confirmed AS LostUserReturnConfirmed,
               found_user_return_confirmed AS FoundUserReturnConfirmed,
               created_at AS CreatedAt,
               updated_at AS UpdatedAt
        FROM Matches
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset
        """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

        var offset = (pageNumber - 1) * pageSize;

        var items = await connection.QueryAsync<MatchesModel>(
            dataSql,
            new { PageSize = pageSize, Offset = offset });

        return new PagedResultDto<MatchesModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResultDto<MatchesModel>> GetAllForUserAsync(
    long userId,
    int pageNumber,
    int pageSize)
    {
        // El COUNT necesita el mismo JOIN + WHERE que la consulta de datos,
        // porque "cuántos matches tiene este usuario" solo se puede saber
        // cruzando con LostItems/FoundItems — no es un COUNT simple de Matches.
        const string countSql = """
        SELECT COUNT(*)
        FROM Matches m
        INNER JOIN LostItems li ON li.id = m.lost_item_id
        INNER JOIN FoundItems fi ON fi.id = m.found_item_id
        WHERE li.user_id = @UserId OR fi.user_id = @UserId
        """;

        const string dataSql = """
        SELECT m.id AS Id,
               m.lost_item_id AS LostItemId,
               m.found_item_id AS FoundItemId,
               m.match_percentage AS MatchPercentage,
               m.lost_user_chat_request AS LostUserChatRequest,
               m.found_user_chat_request AS FoundUserChatRequest,
               m.status AS Status,
               m.lost_user_return_confirmed AS LostUserReturnConfirmed,
               m.found_user_return_confirmed AS FoundUserReturnConfirmed,
               m.created_at AS CreatedAt,
               m.updated_at AS UpdatedAt
        FROM Matches m
        INNER JOIN LostItems li ON li.id = m.lost_item_id
        INNER JOIN FoundItems fi ON fi.id = m.found_item_id
        WHERE li.user_id = @UserId OR fi.user_id = @UserId
        ORDER BY m.created_at DESC
        LIMIT @PageSize OFFSET @Offset
        """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql,
            new { UserId = userId });

        var offset = (pageNumber - 1) * pageSize;

        var items = await connection.QueryAsync<MatchesModel>(
            dataSql,
            new { UserId = userId, PageSize = pageSize, Offset = offset });

        return new PagedResultDto<MatchesModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<MatchesModel?> GetByIdAsync(long id)
    {
        const string sql = """
            SELECT id AS Id,
                   lost_item_id AS LostItemId,
                   found_item_id AS FoundItemId,
                   match_percentage AS MatchPercentage,
                   lost_user_chat_request AS LostUserChatRequest,
                   found_user_chat_request AS FoundUserChatRequest,
                   status AS Status,
                   lost_user_return_confirmed AS LostUserReturnConfirmed,
                   found_user_return_confirmed AS FoundUserReturnConfirmed,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM Matches
            WHERE id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<MatchesModel>(sql, new { Id = id });
    }

    public async Task<bool> ExistsAsync(long lostItemId, long foundItemId)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM Matches
                WHERE lost_item_id = @LostItemId
                  AND found_item_id = @FoundItemId
            )
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new { LostItemId = lostItemId, FoundItemId = foundItemId });
    }

    public async Task<long> CreateAsync(MatchesModel match)
    {
        const string sql = """
            INSERT INTO Matches (
                lost_item_id, found_item_id, match_percentage,
                status, created_at, updated_at
            )
            VALUES (
                @LostItemId, @FoundItemId, @MatchPercentage,
                @Status, @CreatedAt, @UpdatedAt
            )
            RETURNING id
            """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(sql, match);
    }

    public async Task<(bool LostRequested, bool FoundRequested)> ToggleChatRequestAsync(
        long matchId,
        bool isLostUser)
    {
        // Toggle simple: invierte el valor actual. La validación de que
        // el match esté en status "Pendiente" (es decir, que todavía se
        // pueda modificar) la hace el service ANTES de llamar aquí, así
        // que este método no necesita condición de WHERE adicional.
        var sql = isLostUser
            ? """
              UPDATE Matches
              SET lost_user_chat_request = NOT lost_user_chat_request,
                  updated_at = CURRENT_TIMESTAMP
              WHERE id = @MatchId
              RETURNING lost_user_chat_request AS LostRequested,
                        found_user_chat_request AS FoundRequested
              """
            : """
              UPDATE Matches
              SET found_user_chat_request = NOT found_user_chat_request,
                  updated_at = CURRENT_TIMESTAMP
              WHERE id = @MatchId
              RETURNING lost_user_chat_request AS LostRequested,
                        found_user_chat_request AS FoundRequested
              """;

        using var connection = _connectionFactory.CreateConnection();

        var result = await connection.QuerySingleAsync<ChatRequestRow>(
            sql,
            new { MatchId = matchId });

        return (result.LostRequested, result.FoundRequested);
    }

    public async Task<bool> TryTransitionStatusAsync(
        long matchId,
        string fromStatus,
        string toStatus)
    {
        const string sql = """
            UPDATE Matches
            SET status = @ToStatus,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @MatchId
              AND status = @FromStatus
            """;

        using var connection = _connectionFactory.CreateConnection();

        var affectedRows = await connection.ExecuteAsync(
            sql,
            new { MatchId = matchId, FromStatus = fromStatus, ToStatus = toStatus });

        return affectedRows > 0;
    }

    public async Task<(bool LostConfirmed, bool FoundConfirmed)?> TryConfirmReturnAsync(
        long matchId,
        bool isLostUser)
    {
        var sql = isLostUser
            ? """
              UPDATE Matches
              SET lost_user_return_confirmed = TRUE,
                  updated_at = CURRENT_TIMESTAMP
              WHERE id = @MatchId
                AND lost_user_return_confirmed = FALSE
              RETURNING lost_user_return_confirmed AS LostConfirmed,
                        found_user_return_confirmed AS FoundConfirmed
              """
            : """
              UPDATE Matches
              SET found_user_return_confirmed = TRUE,
                  updated_at = CURRENT_TIMESTAMP
              WHERE id = @MatchId
                AND found_user_return_confirmed = FALSE
              RETURNING lost_user_return_confirmed AS LostConfirmed,
                        found_user_return_confirmed AS FoundConfirmed
              """;

        using var connection = _connectionFactory.CreateConnection();

        var result = await connection.QuerySingleOrDefaultAsync<ReturnConfirmRow?>(
            sql,
            new { MatchId = matchId });

        if (result != null)
        {
            return (result.LostConfirmed, result.FoundConfirmed);
        }

        const string currentStateSql = """
            SELECT lost_user_return_confirmed AS LostConfirmed,
                   found_user_return_confirmed AS FoundConfirmed
            FROM Matches
            WHERE id = @MatchId
            """;

        var currentState = await connection.QuerySingleOrDefaultAsync<ReturnConfirmRow?>(
            currentStateSql,
            new { MatchId = matchId });

        return currentState == null
            ? null
            : (currentState.LostConfirmed, currentState.FoundConfirmed);
    }
}