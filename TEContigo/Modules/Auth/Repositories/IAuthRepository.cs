using TEContigo.Modules.Auth.Models;

namespace TEContigo.Modules.Auth.Repositories
{
    public interface IAuthRepository
    {
        // Users
        Task<bool> UserExistsByEmailAsync(string email);

        Task<bool> UserExistsByControlNumberAsync(long controlNumber);

        Task<long> CreateUserAsync(
            long controlNumber,
            string firstName,
            string lastNamePaternal,
            string lastNameMaternal,
            string email,
            string passwordHash,
            string role
        );

        Task<UserAuthData?> GetUserByEmailAsync(string email);

        Task<UserAuthData?> GetUserByIdAsync(long userId);

        Task VerifyEmailAsync(long userId);


        // Pending Users
        Task<PendingUser?> GetPendingUserByEmailAsync(string email);

        Task CreatePendingUserAsync(PendingUser pendingUser);

        Task UpdatePendingUserVerificationCodeAsync(long id, string verificationCodeHash,DateTime expiresAt);

        Task DeletePendingUserAsync(long id);

        Task DeletePendingUserByEmailAsync(string email);

        Task<long> ConfirmPendingUserAsync(PendingUser pendingUser);

        // Refresh Tokens
        Task CreateRefreshTokenAsync(RefreshToken refreshToken);

        Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash);

        Task RevokeRefreshTokenAsync(long refreshTokenId);
    }

    public class UserAuthData
    {
        public long Id { get; set; }

        public long ControlNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastNamePaternal { get; set; } = string.Empty;

        public string LastNameMaternal { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }
    }
}
