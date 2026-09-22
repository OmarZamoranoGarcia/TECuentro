using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Users.Models;
using TEContigo.Shared.Pagination;

namespace TEContigo.Modules.Users.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public UsersRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<PagedResultDto<UsersModel>> GetAllAsync(int pageNumber, int pageSize)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string countSql = """
        SELECT COUNT(*) FROM Users;
    """;

            const string dataSql = """
        SELECT
        id AS Id,
        control_number AS ControlNumber,
        first_name AS FirstName,
        last_name_paternal AS LastNamePaternal,
        last_name_maternal AS LastNameMaternal,
        email AS Email,
        role AS Role,
        email_verified AS EmailVerified,
        created_at AS CreatedAt
        FROM Users
        ORDER BY id
        LIMIT @PageSize OFFSET @Offset;
    """;

            var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            var offset = (pageNumber - 1) * pageSize;

            var items = await connection.QueryAsync<UsersModel>(
                dataSql,
                new { PageSize = pageSize, Offset = offset });

            return new PagedResultDto<UsersModel>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<UsersModel?> GetByIdAsync(long id)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
            SELECT
                id AS Id,
                control_number AS ControlNumber,
                first_name AS FirstName,
                last_name_paternal AS LastNamePaternal,
                last_name_maternal AS LastNameMaternal,
                email AS Email,
                role AS Role,
                email_verified AS EmailVerified,
                created_at AS CreatedAt
            FROM Users
            WHERE id = @Id;
            """;

            return await connection.QuerySingleOrDefaultAsync<UsersModel>(
                sql,
                new { Id = id }
            );
        }

        public async Task<long> CreateAsync(UsersModel user)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
            INSERT INTO Users
            (
                control_number,
                first_name,
                last_name_paternal,
                last_name_maternal,
                email,
                password_hash,
                role,
                email_verified,
                created_at
            )
            VALUES
            (
                @ControlNumber,
                @FirstName,
                @LastNamePaternal,
                @LastNameMaternal,
                @Email,
                @PasswordHash,
                @Role,
                @EmailVerified,
                CURRENT_TIMESTAMP
            )
            RETURNING id;
            """;

            return await connection.ExecuteScalarAsync<long>(
                sql,
                user
            );
        }

        public async Task UpdateAsync(UsersModel user)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
            UPDATE Users
            SET
                control_number = @ControlNumber,
                first_name = @FirstName,
                last_name_paternal = @LastNamePaternal,
                last_name_maternal = @LastNameMaternal,
                email = @Email,
                role = @Role,
                email_verified = @EmailVerified
            WHERE id = @Id;
            """;

            await connection.ExecuteAsync(
                sql,
                user
            );
        }

        public async Task DeleteAsync(long id)
        {
            using var connection =
                _connectionFactory.CreateConnection();

            const string sql = """
            DELETE FROM Users
            WHERE id = @Id;
            """;

            await connection.ExecuteAsync(
                sql,
                new { Id = id }
            );
        }
    }
}
