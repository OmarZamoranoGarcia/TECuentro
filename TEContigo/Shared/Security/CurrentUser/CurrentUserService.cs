using System.Security.Claims;
using TEContigo.Shared.Security.CurrentUser;

namespace TEContigo.Shared.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long UserId
    {
        get
        {
            var userId = _httpContextAccessor
                .HttpContext?
                .User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo obtener el ID del usuario autenticado.");
            }

            return long.Parse(userId);
        }
    }

    public string Role
    {
        get
        {
            var role = _httpContextAccessor
                .HttpContext?
                .User?
                .FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(role))
            {
                throw new UnauthorizedAccessException(
                    "No se pudo obtener el rol del usuario autenticado.");
            }

            return role;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor
                .HttpContext?
                .User?
                .Identity?
                .IsAuthenticated
                ?? false;
        }
    }

    public bool IsInRole(string role)
    {
        return _httpContextAccessor
            .HttpContext?
            .User?
            .IsInRole(role)
            ?? false;
    }
}