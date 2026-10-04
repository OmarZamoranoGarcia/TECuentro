using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Messages.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Messages.Repositories;

public class MessagesRepository : IMessagesRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MessagesRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResultDto<MessagesModel>> GetByConversationIdAsync(
        long conversationId,
        int pageNumber,
        int pageSize)
    {
        const string countSql = """
            SELECT COUNT(*) FROM Messages
            WHERE conversation_id = @ConversationId;
        """;

        const string dataSql = """
            SELECT id AS Id,
                   conversation_id AS ConversationId,
                   sender_id AS SenderId,
                   content AS Content,
                   created_at AS CreatedAt,
                   read_at AS ReadAt
            FROM Messages
            WHERE conversation_id = @ConversationId
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset;
        """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            countSql, new { ConversationId = conversationId });

        var offset = (pageNumber - 1) * pageSize;

        var items = await connection.QueryAsync<MessagesModel>(
            dataSql,
            new { ConversationId = conversationId, PageSize = pageSize, Offset = offset });

        return new PagedResultDto<MessagesModel>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<long> CreateAsync(MessagesModel message)
    {
        const string sql = """
            INSERT INTO Messages (conversation_id, sender_id, content, created_at)
            VALUES (@ConversationId, @SenderId, @Content, @CreatedAt)
            RETURNING id;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<long>(sql, message);
    }

    public async Task MarkConversationAsReadAsync(long conversationId, long readerUserId)
    {
        const string sql = """
            UPDATE Messages
            SET read_at = CURRENT_TIMESTAMP
            WHERE conversation_id = @ConversationId
              AND sender_id != @ReaderUserId
              AND read_at IS NULL;
        """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            sql, new { ConversationId = conversationId, ReaderUserId = readerUserId });
    }
}