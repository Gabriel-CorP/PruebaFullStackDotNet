using FluentValidation;
using MediatR;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Application.Features.Productos;

public sealed class ProductoRequestValidator : AbstractValidator<ProductoRequest>
{
    public ProductoRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().WithMessage("El código es obligatorio.")
            .MaximumLength(30).WithMessage("El código no puede exceder 30 caracteres.");
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no puede exceder 150 caracteres.");
        RuleFor(x => x.Descripcion).MaximumLength(500);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
    }
}

// ---------- Crear ----------
public sealed record CreateProductoCommand(ProductoRequest Datos) : IRequest<ProductoDto>;

public sealed class CreateProductoCommandValidator : AbstractValidator<CreateProductoCommand>
{
    public CreateProductoCommandValidator() => RuleFor(x => x.Datos).SetValidator(new ProductoRequestValidator());
}

public sealed class CreateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<CreateProductoCommand, ProductoDto>
{
    public async Task<ProductoDto> Handle(CreateProductoCommand request, CancellationToken ct)
    {
        request.Datos.Codigo = request.Datos.Codigo.Trim().ToUpperInvariant();
        var id = await uow.Productos.InsertarAsync(request.Datos, ct);
        return (await uow.Productos.ObtenerPorIdAsync(id, ct))!;
    }
}

// ---------- Actualizar ----------
public sealed record UpdateProductoCommand(int Id, ProductoRequest Datos) : IRequest<ProductoDto>;

public sealed class UpdateProductoCommandValidator : AbstractValidator<UpdateProductoCommand>
{
    public UpdateProductoCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Datos).SetValidator(new ProductoRequestValidator());
    }
}

public sealed class UpdateProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateProductoCommand, ProductoDto>
{
    public async Task<ProductoDto> Handle(UpdateProductoCommand request, CancellationToken ct)
    {
        request.Datos.Codigo = request.Datos.Codigo.Trim().ToUpperInvariant();
        await uow.Productos.ActualizarAsync(request.Id, request.Datos, ct);
        return (await uow.Productos.ObtenerPorIdAsync(request.Id, ct))
               ?? throw new NotFoundException("El producto no existe.");
    }
}

// ---------- Eliminar ----------
public sealed record DeleteProductoCommand(int Id) : IRequest;

public sealed class DeleteProductoCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteProductoCommand>
{
    public Task Handle(DeleteProductoCommand request, CancellationToken ct) =>
        uow.Productos.EliminarAsync(request.Id, ct);
}
