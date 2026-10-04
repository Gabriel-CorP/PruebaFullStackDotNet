using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Application.Common.Constants;
using Ventas.Application.Common.Models;
using Ventas.Application.DTOs;
using Ventas.Application.Features.Auth;
using Ventas.Application.Features.Usuarios;

namespace Ventas.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.NombreUsuario, request.Password), ct);
        return Ok(ApiResponse.Ok(result, "Inicio de sesión exitoso."));
    }

    [HttpPost("registrar")]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new RegistrarUsuarioCommand(request.RolId, request.NombreUsuario, request.NombreCompleto, request.Password), ct);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(new { id }, "Usuario registrado correctamente."));
    }
}
