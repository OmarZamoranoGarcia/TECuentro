using TEContigo.Modules.Auth.DTOs;

namespace TEContigo.Modules.Auth.Services
{
    public interface IAuthService
    {
        Task<MessageResponseDto> RegisterAsync(RegisterDto dto);

        Task<AuthResponseDto> LoginAsync(LoginDto dto);

        Task<MessageResponseDto> VerifyEmailAsync(VerifyEmailDto dto);

        Task<MessageResponseDto> ResendVerificationCodeAsync(ResendCodeDto dto);

        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto);

        Task<MessageResponseDto> LogoutAsync(RefreshTokenDto dto);
    }
}
