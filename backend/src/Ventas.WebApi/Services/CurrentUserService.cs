using System.Security.Claims;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;

namespace Ventas.WebApi.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public int UserId =>
        int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedException("Sesión inválida. Inicie sesión nuevamente.");

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
