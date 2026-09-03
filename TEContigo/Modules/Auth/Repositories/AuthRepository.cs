using Dapper;
using TEContigo.Infrastructure.Database;
using TEContigo.Modules.Auth.Models;

namespace TEContigo.Modules.Auth.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public AuthRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        //USERS
        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM Users
                    WHERE email = @Email
                );
            """;

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { Email = email }
            );
        }

        public async Task<bool> UserExistsByControlNumberAsync(long controlNumber)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                SELECT EXISTS (
                    SELECT 1
                    FROM Users
                    WHERE control_number = @ControlNumber
                );
            """;

            return await connection.ExecuteScalarAsync<bool>(
                sql,
                new { ControlNumber = controlNumber }
            );
        }

        public async Task<long> CreateUserAsync(
        long controlNumber,
        string firstName,
        string lastNamePaternal,
        string lastNameMaternal,
        string email,
        string passwordHash,
        string role)
        {
            using var connection = _connectionFactory.CreateConnection();

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
                TRUE,
                CURRENT_TIMESTAMP
            )
            RETURNING id;
            """;

            return await connection.ExecuteScalarAsync<long>(
                sql,
                new
                {
                    ControlNumber = controlNumber,
                    FirstName = firstName,
                    LastNamePaternal = lastNamePaternal,
                    LastNameMaternal = lastNameMaternal,
                    Email = email,
                    PasswordHash = passwordHash,
                    Role = role
                }
            );
        }

        public async Task<UserAuthData?> GetUserByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT
                id AS Id,
                control_number AS ControlNumber,
                first_name AS FirstName,
                last_name_paternal AS LastNamePaternal,
                last_name_maternal AS LastNameMaternal,
                email AS Email,
                password_hash AS PasswordHash,
                role AS Role,
                email_verified AS EmailVerified
            FROM Users
            WHERE email = @Email;
            """;

            return await connection.QuerySingleOrDefaultAsync<UserAuthData>(
                sql,
                new { Email = email }
            );
        }

        public async Task<UserAuthData?> GetUserByIdAsync(long userId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT
                id AS Id,
                control_number AS ControlNumber,
                first_name AS FirstName,
                last_name_paternal AS LastNamePaternal,
                last_name_maternal AS LastNameMaternal,
                email AS Email,
                password_hash AS PasswordHash,
                role AS Role,
                email_verified AS EmailVerified
            FROM Users
            WHERE id = @UserId;
            """;

            return await connection.QuerySingleOrDefaultAsync<UserAuthData>(
                sql,
                new { UserId = userId }
            );
        }

        public async Task VerifyEmailAsync(long userId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            UPDATE Users
            SET email_verified = TRUE
            WHERE id = @UserId;
            """;

            await connection.ExecuteAsync(
                sql,
                new { UserId = userId }
            );
        }

        // PENDING USERS
        public async Task<PendingUser?> GetPendingUserByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT
                id AS Id,
                control_number AS ControlNumber,
                first_name AS FirstName,
                last_name_paternal AS LastNamePaternal,
                last_name_maternal AS LastNameMaternal,
                email AS Email,
                password_hash AS PasswordHash,
                verification_code_hash AS VerificationCodeHash,
                expires_at AS ExpiresAt,
                created_at AS CreatedAt
            FROM PendingUsers
            WHERE email = @Email;
            """;

            return await connection.QuerySingleOrDefaultAsync<PendingUser>(
                sql,
                new { Email = email }
            );
        }

        public async Task CreatePendingUserAsync(PendingUser pendingUser)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                INSERT INTO PendingUsers
                (
                    control_number,
                    first_name,
                    last_name_paternal,
                    last_name_maternal,
                    email,
                    password_hash,
                    verification_code_hash,
                    expires_at,
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
                    @VerificationCodeHash,
                    @ExpiresAt,
                    @CreatedAt
                );
            """;

            await connection.ExecuteAsync(
                sql,
                pendingUser
            );
        }

        public async Task UpdatePendingUserVerificationCodeAsync(
            long id,
            string verificationCodeHash,
            DateTime expiresAt)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                UPDATE PendingUsers
                SET
                    verification_code_hash = @VerificationCodeHash,
                    expires_at = @ExpiresAt
                WHERE id = @Id;
             """;
            await connection.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    VerificationCodeHash = verificationCodeHash,
                    ExpiresAt = expiresAt
                }
            );
        }

        public async Task DeletePendingUserAsync(long id)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            DELETE FROM PendingUsers
            WHERE id = @Id;
            """;

            await connection.ExecuteAsync(
                sql,
                new { Id = id }
            );
        }

        public async Task DeletePendingUserByEmailAsync(string email)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
                DELETE FROM PendingUsers
                WHERE email = @Email;
            """;

            await connection.ExecuteAsync(
                sql,
                new { Email = email }
            );
        }

        public async Task<long> ConfirmPendingUserAsync(PendingUser pendingUser)
        {
            using var connection = _connectionFactory.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                const string insertSql = """
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
                'USER',
                TRUE,
                CURRENT_TIMESTAMP
            )
            RETURNING id;
            """;

                var userId = await connection.ExecuteScalarAsync<long>(
                    insertSql,
                    new
                    {
                        pendingUser.ControlNumber,
                        pendingUser.FirstName,
                        pendingUser.LastNamePaternal,
                        pendingUser.LastNameMaternal,
                        pendingUser.Email,
                        pendingUser.PasswordHash
                    },
                    transaction
                );

                const string deleteSql = """
            DELETE FROM PendingUsers
            WHERE id = @Id;
            """;

                await connection.ExecuteAsync(
                    deleteSql,
                    new { pendingUser.Id },
                    transaction
                );

                transaction.Commit();

                return userId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // Refresh Tokens
        public async Task CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            INSERT INTO RefreshTokens
            (
                user_id,
                token_hash,
                expires_at,
                created_at
            )
            VALUES
            (
                @UserId,
                @TokenHash,
                @ExpiresAt,
                @CreatedAt
            );
            """;

            await connection.ExecuteAsync(
                sql,
                refreshToken
            );
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT
                id AS Id,
                user_id AS UserId,
                token_hash AS TokenHash,
                expires_at AS ExpiresAt,
                created_at AS CreatedAt,
                revoked_at AS RevokedAt
            FROM RefreshTokens
            WHERE token_hash = @TokenHash;
            """;

            return await connection.QuerySingleOrDefaultAsync<RefreshToken>(
                sql,
                new { TokenHash = tokenHash }
            );
        }

        public async Task RevokeRefreshTokenAsync(long refreshTokenId)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            UPDATE RefreshTokens
            SET revoked_at = CURRENT_TIMESTAMP
            WHERE id = @RefreshTokenId;
            """;

            await connection.ExecuteAsync(
                sql,
                new { RefreshTokenId = refreshTokenId }
            );
        }
    }
}
