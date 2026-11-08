using Dapper;
using TEContigo.Infrastructure.Database;

namespace TEContigo.Modules.Devices.Repositories;

public class UserDevicesRepository : IUserDevicesRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserDevicesRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertAsync(long userId, string pushToken, string platform)
    {
        // Depende del UNIQUE de push_token (uq_user_devices_push_token).
        const string sql = """
            INSERT INTO UserDevices (user_id, push_token, platform, created_at, last_used_at)
            VALUES (@UserId, @PushToken, @Platform, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
            ON CONFLICT (push_token)
            DO UPDATE SET user_id = EXCLUDED.user_id,
                          platform = EXCLUDED.platform,
                          last_used_at = CURRENT_TIMESTAMP;
        """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            sql, new { UserId = userId, PushToken = pushToken, Platform = platform });
    }

    public async Task DeleteAsync(long userId, string pushToken)
    {
        const string sql = """
            DELETE FROM UserDevices
            WHERE push_token = @PushToken AND user_id = @UserId;
        """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            sql, new { UserId = userId, PushToken = pushToken });
    }

    public async Task<IEnumerable<string>> GetTokensByUserIdAsync(long userId)
    {
        const string sql = """
            SELECT push_token FROM UserDevices
            WHERE user_id = @UserId;
        """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QueryAsync<string>(sql, new { UserId = userId });
    }

    public async Task DeleteByTokensAsync(IEnumerable<string> pushTokens)
    {
        const string sql = """
            DELETE FROM UserDevices
            WHERE push_token = ANY(@Tokens);
        """;

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(sql, new { Tokens = pushTokens.ToArray() });
    }
}