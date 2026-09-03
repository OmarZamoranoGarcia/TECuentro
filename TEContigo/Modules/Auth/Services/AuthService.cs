using Konscious.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TEContigo.Modules.Auth.DTOs;
using TEContigo.Modules.Auth.Models;
using TEContigo.Modules.Auth.Repositories;
using TEContigo.Modules.Email.Services;
using TEContigo.Modules.Email.DTOs;

namespace TEContigo.Modules.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthRepository _authRepository;
        private readonly IEmailService _emailService;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _emailService = emailService; 
        }

        public async Task<MessageResponseDto> RegisterAsync(RegisterDto dto)
        {
            // 1. Validate data
            if (dto.ControlNumber <= 0 ||
                string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastNamePaternal) ||
                string.IsNullOrWhiteSpace(dto.LastNameMaternal) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException(
                    "Todos los campos son obligatorios.");
            }

            // 2. Validate institutional email
            if (!dto.Email.EndsWith(
                    "@tectijuana.edu.mx",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Debes utilizar un correo institucional.");
            }

            var email = dto.Email.Trim().ToLowerInvariant();

            // 3. Check existing email
            if (await _authRepository.UserExistsByEmailAsync(email))
            {
                throw new InvalidOperationException(
                    "El correo electrónico ya está registrado.");
            }

            // 4. Check existing control number
            if (await _authRepository.UserExistsByControlNumberAsync(
                    dto.ControlNumber))
            {
                throw new InvalidOperationException(
                    "El número de control ya está registrado.");
            }

            // 5. Delete any existing pending user with the same email
            await _authRepository.DeletePendingUserByEmailAsync(email);

            // 6. Password hashing with Argon2id
            var passwordHash = await HashPasswordAsync(dto.Password);

            // 7. Verification code generation (6 digits)
            var verificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // 8. Hash Verification code with generic method (SHA256) to avoid storing it in plain text
            var verificationCodeHash = HashToken(verificationCode);

            // 9. Create PendingUser
            var pendingUser = new PendingUser
            {
                ControlNumber = dto.ControlNumber,

                FirstName = dto.FirstName.Trim(),

                LastNamePaternal = dto.LastNamePaternal.Trim(),

                LastNameMaternal = dto.LastNameMaternal.Trim(),

                Email = email,

                PasswordHash = passwordHash,

                VerificationCodeHash = verificationCodeHash,

                ExpiresAt = DateTime.UtcNow.AddMinutes(5),

                CreatedAt = DateTime.UtcNow
            };

            // 10. Save temporal register in DB
            await _authRepository.CreatePendingUserAsync(
                pendingUser);

            // 11. Send verification email with the code
            await _emailService.SendVerificationEmailAsync(
                new VerificationEmailDto
                {
                    To = email,
                    Code = verificationCode
                });

            return new MessageResponseDto
            {
                Message =
                    "Se ha enviado un código de verificación a tu correo institucional."
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user =
                await _authRepository.GetUserByEmailAsync(email);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "Correo o contraseña incorrectos.");
            }

            if (!user.EmailVerified)
            {
                throw new InvalidOperationException(
                    "El correo electrónico no ha sido verificado.");
            }

            var passwordValid =
                await VerifyPasswordAsync(
                    dto.Password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                throw new InvalidOperationException(
                    "Correo o contraseña incorrectos.");
            }

            // Access Token
            var accessToken =
                GenerateAccessToken(user);

            // Refresh Token
            var refreshToken =
                GenerateRefreshToken();

            var refreshTokenHash =
                HashToken(refreshToken);

            var refreshTokenExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            var refreshTokenModel = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.CreateRefreshTokenAsync(
                refreshTokenModel);

            var accessTokenExpirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:AccessTokenExpirationMinutes");

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = accessTokenExpirationMinutes * 60,
                Role = user.Role
            };
        }

        public async Task<MessageResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var pendingUser =
                await _authRepository.GetPendingUserByEmailAsync(email);

            if (pendingUser == null)
            {
                throw new InvalidOperationException(
                    "No existe un registro pendiente para este correo.");
            }

            if (DateTime.UtcNow > pendingUser.ExpiresAt)
            {
                throw new InvalidOperationException(
                    "El código de verificación ha expirado. Solicita uno nuevo.");
            }

            var codeHash = HashToken(dto.Code);

            if (codeHash != pendingUser.VerificationCodeHash)
            {
                throw new InvalidOperationException(
                    "El código de verificación es incorrecto.");
            }

            var userId = await _authRepository.ConfirmPendingUserAsync(pendingUser);

            return new MessageResponseDto
            {
                Message = "Correo verificado correctamente (Se creó el usuario y se borro de pendientes por verificar)."
            };
        }

        public async Task<MessageResponseDto> ResendVerificationCodeAsync(ResendCodeDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var pendingUser =
                await _authRepository.GetPendingUserByEmailAsync(email);

            if (pendingUser == null)
            {
                throw new InvalidOperationException(
                    "No existe un registro pendiente para este correo.");
            }

            var verificationCode =
                RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

            var verificationCodeHash =
                HashToken(verificationCode);

            pendingUser.VerificationCodeHash =
                verificationCodeHash;

            pendingUser.ExpiresAt =
                DateTime.UtcNow.AddMinutes(5);

            await _authRepository.UpdatePendingUserVerificationCodeAsync(
                pendingUser.Id,
                pendingUser.VerificationCodeHash,
                pendingUser.ExpiresAt
            );

            await _emailService.SendVerificationEmailAsync(
                new VerificationEmailDto
                {
                    To = email,
                    Code = verificationCode
                }
            );

            return new MessageResponseDto
            {
                Message =
                    "Se ha enviado un nuevo código de verificación a tu correo."
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
        {
            // 1. Hash token received from client
            var tokenHash = HashToken(dto.RefreshToken);

            // 2. Search refresToken in DB
            var refreshToken =
                await _authRepository.GetRefreshTokenAsync(tokenHash);

            if (refreshToken == null)
            {
                throw new InvalidOperationException(
                    "El refresh token no es válido.");
            }

            // 3. Verify if token its revoked
            if (refreshToken.RevokedAt != null)
            {
                throw new InvalidOperationException(
                    "El refresh token ya fue revocado.");
            }

            // 4. Verify expiration
            if (DateTime.UtcNow > refreshToken.ExpiresAt)
            {
                throw new InvalidOperationException(
                    "El refresh token ha expirado.");
            }

            // 5. Get user
            var user =
                await _authRepository.GetUserByIdAsync(
                    refreshToken.UserId);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "El usuario asociado al token no existe.");
            }

            // 6. Rovoke actual refreshToken
            await _authRepository.RevokeRefreshTokenAsync(
                refreshToken.Id);

            // 7. Generate new tokens
            var accessToken =
                GenerateAccessToken(user);

            var newRefreshToken =
                GenerateRefreshToken();

            var newRefreshTokenHash =
                HashToken(newRefreshToken);

            // 8. Get a new expiration for the new refresh token
            var refreshTokenExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            // 9. Save a new refresh token in DB
            var refreshTokenModel = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    refreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.CreateRefreshTokenAsync(
                refreshTokenModel);

            // 10. Expires in for the access token
            var accessTokenExpirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:AccessTokenExpirationMinutes");

            // 11. Response
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = accessTokenExpirationMinutes * 60,
                Role = user.Role
            };
        }

        public async Task<MessageResponseDto> LogoutAsync(RefreshTokenDto dto)
        {
            // 1. Do the hash of the refresh token
            var tokenHash = HashToken(dto.RefreshToken);

            // 2. Search the refresh token in DB
            var refreshToken =
                await _authRepository.GetRefreshTokenAsync(tokenHash);

            // 3. Verify that it exists
            if (refreshToken == null)
            {
                throw new InvalidOperationException(
                    "El refresh token no es válido.");
            }

            // 4. Verify if it has already been revoked
            if (refreshToken.RevokedAt != null)
            {
                throw new InvalidOperationException(
                    "El refresh token ya fue revocado.");
            }

            // 5. Revoke the refresh token in DB
            await _authRepository.RevokeRefreshTokenAsync(
                refreshToken.Id);

            return new MessageResponseDto
            {
                Message = "Sesión cerrada correctamente."
            };
        }

        // ============================================================
        // PASSWORD / ARGON2
        // ============================================================

        private async Task<string> HashPasswordAsync(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var argon2 = new Argon2id(
                Encoding.UTF8.GetBytes(password));

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 2;
            argon2.Iterations = 4;
            argon2.MemorySize = 65536;

            byte[] hash = await argon2.GetBytesAsync(32);

            return $"$argon2id$v=19$m=65536,t=4,p=2${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private async Task<bool> VerifyPasswordAsync(string password,string storedHash)
        {
            try
            {
                var parts = storedHash.Split('$');

                if (parts.Length != 6)
                    return false;

                var parameters = parts[3];

                var salt = Convert.FromBase64String(parts[4]);
                var expectedHash = Convert.FromBase64String(parts[5]);

                var parameterParts = parameters.Split(',');

                var memory = int.Parse(
                    parameterParts[0].Split('=')[1]);

                var iterations = int.Parse(
                    parameterParts[1].Split('=')[1]);

                var parallelism = int.Parse(
                    parameterParts[2].Split('=')[1]);

                var argon2 = new Argon2id(
                    Encoding.UTF8.GetBytes(password));

                argon2.Salt = salt;
                argon2.MemorySize = memory;
                argon2.Iterations = iterations;
                argon2.DegreeOfParallelism = parallelism;

                var actualHash =
                    await argon2.GetBytesAsync(expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash);
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // JWT
        // ============================================================

        private string GenerateAccessToken(UserAuthData user)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "La clave JWT no está configurada.");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var expirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:AccessTokenExpirationMinutes");

            var claims = new[]
            {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.Email,
            user.Email),

        new Claim(
            ClaimTypes.Role,
            user.Role)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // ============================================================
        // REFRESH TOKEN
        // ============================================================

        private string GenerateRefreshToken()
        {
            byte[] randomBytes =
                RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(randomBytes);
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(token);

            var hash = sha256.ComputeHash(bytes);

            return Convert.ToHexString(hash);
        }

        // ============================================================
        // AUTH RESPONSE
        // ============================================================

        private AuthResponseDto CreateAuthResponse(
            UserAuthData user,
            string refreshToken)
        {
            var expirationMinutes = int.Parse(
                _configuration
                    .GetSection("Jwt")["ExpirationMinutes"]
                ?? "15");

            return new AuthResponseDto
            {
                AccessToken = GenerateAccessToken(user),

                RefreshToken = refreshToken,

                ExpiresIn = expirationMinutes * 60
            };
        }
    }
}
