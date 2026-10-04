using FluentValidation;
using MediatR;
using Ventas.Application.Common.Interfaces;

namespace Ventas.Application.Features.Usuarios;

public sealed record RegistrarUsuarioCommand(int RolId, string NombreUsuario, string NombreCompleto, string Password)
    : IRequest<int>;

public sealed class RegistrarUsuarioCommandValidator : AbstractValidator<RegistrarUsuarioCommand>
{
    public RegistrarUsuarioCommandValidator()
    {
        RuleFor(x => x.RolId).GreaterThan(0).WithMessage("El rol es obligatorio.");
        RuleFor(x => x.NombreUsuario).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}

public sealed class RegistrarUsuarioCommandHandler(IUnitOfWork uow, IPasswordHasher hasher)
    : IRequestHandler<RegistrarUsuarioCommand, int>
{
    public Task<int> Handle(RegistrarUsuarioCommand request, CancellationToken ct)
    {
        var salt = hasher.GenerateSalt();
        var hash = hasher.Hash(request.Password, salt);
        return uow.Usuarios.RegistrarAsync(request.RolId, request.NombreUsuario.Trim(),
            request.NombreCompleto.Trim(), hash, salt, ct);
    }
}
