using FluentValidation;
using MediatR;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Application.Features.Auth;

public sealed record LoginCommand(string NombreUsuario, string Password) : IRequest<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("El usuario es obligatorio.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public sealed class LoginCommandHandler(IUnitOfWork uow, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var usuario = await uow.Usuarios.ObtenerPorNombreUsuarioAsync(request.NombreUsuario.Trim(), ct);

        // Mensaje genérico a propósito: no revelar si el usuario existe.
        if (usuario is null || !usuario.Activo ||
            !hasher.Verify(request.Password, usuario.PasswordSalt, usuario.PasswordHash))
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");

        var (token, expiresAt) = jwt.Generate(usuario);
        return new LoginResponse(token, expiresAt, usuario.Id, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Rol);
    }
}
