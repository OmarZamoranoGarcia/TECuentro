using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

using TEContigo.Modules.Auth.DTOs;
using TEContigo.Modules.Auth.Services;

namespace TEContigo.Modules.Auth.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            return Ok(result);
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
        {
            var result = await _authService.VerifyEmailAsync(dto);

            return Ok(result);
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode(ResendCodeDto dto)
        {
            var result = await _authService.ResendVerificationCodeAsync(dto);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken(RefreshTokenDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDto dto)
        {
            var result = await _authService.LogoutAsync(dto);

            return Ok(result);
        }
    }
}
