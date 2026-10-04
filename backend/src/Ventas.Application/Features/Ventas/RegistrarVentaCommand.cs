using FluentValidation;
using MediatR;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Application.Features.Ventas;

public sealed record RegistrarVentaCommand(List<VentaItemRequest> Items) : IRequest<VentaDto>;

public sealed class RegistrarVentaCommandValidator : AbstractValidator<RegistrarVentaCommand>
{
    public RegistrarVentaCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Debe agregar al menos un producto a la venta.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductoId).GreaterThan(0).WithMessage("Producto inválido.");
            item.RuleFor(i => i.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor a cero.");
        });
    }
}

public sealed class RegistrarVentaCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<RegistrarVentaCommand, VentaDto>
{
    public async Task<VentaDto> Handle(RegistrarVentaCommand request, CancellationToken ct)
    {
        // usp_Venta_Registrar ya maneja su propia transacción (cabecera + detalle + stock).
        var ventaId = await uow.Ventas.RegistrarAsync(currentUser.UserId, request.Items, ct);

        return await uow.Ventas.ObtenerPorIdAsync(ventaId, ct)
               ?? throw new NotFoundException("No se pudo recuperar la venta registrada.");
    }
}
