using MediatR;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;

namespace Ventas.Application.Features.Productos;

public sealed record GetProductosQuery(string? Busqueda) : IRequest<List<ProductoDto>>;

public sealed class GetProductosQueryHandler(IUnitOfWork uow) : IRequestHandler<GetProductosQuery, List<ProductoDto>>
{
    public Task<List<ProductoDto>> Handle(GetProductosQuery request, CancellationToken ct) =>
        uow.Productos.ListarAsync(request.Busqueda?.Trim(), ct);
}

public sealed record GetProductoByIdQuery(int Id) : IRequest<ProductoDto>;

public sealed class GetProductoByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetProductoByIdQuery, ProductoDto>
{
    public async Task<ProductoDto> Handle(GetProductoByIdQuery request, CancellationToken ct) =>
        await uow.Productos.ObtenerPorIdAsync(request.Id, ct)
        ?? throw new NotFoundException("El producto no existe.");
}

public sealed record GetProductoByCodigoQuery(string Codigo) : IRequest<ProductoDto>;

public sealed class GetProductoByCodigoQueryHandler(IUnitOfWork uow) : IRequestHandler<GetProductoByCodigoQuery, ProductoDto>
{
    public async Task<ProductoDto> Handle(GetProductoByCodigoQuery request, CancellationToken ct) =>
        await uow.Productos.ObtenerPorCodigoAsync(request.Codigo.Trim().ToUpperInvariant(), ct)
        ?? throw new NotFoundException($"No se encontró un producto con el código '{request.Codigo}'.");
}
