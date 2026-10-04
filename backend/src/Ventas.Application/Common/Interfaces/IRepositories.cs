using Ventas.Application.DTOs;

namespace Ventas.Application.Common.Interfaces;

public interface IProductoRepository
{
    Task<List<ProductoDto>> ListarAsync(string? busqueda, CancellationToken ct);
    Task<ProductoDto?> ObtenerPorIdAsync(int id, CancellationToken ct);
    Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct);
    Task<int> InsertarAsync(ProductoRequest producto, CancellationToken ct);
    Task ActualizarAsync(int id, ProductoRequest producto, CancellationToken ct);
    Task EliminarAsync(int id, CancellationToken ct);
}

public interface IVentaRepository
{
    Task<int> RegistrarAsync(int usuarioId, IEnumerable<VentaItemRequest> items, CancellationToken ct);
    Task<VentaDto?> ObtenerPorIdAsync(int id, CancellationToken ct);
    Task<List<VentaResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta, CancellationToken ct);
    Task<List<VentaReporteRow>> ReporteAsync(DateTime? desde, DateTime? hasta, CancellationToken ct);
}

public interface IUsuarioRepository
{
    Task<UsuarioAuthDto?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct);
    Task<int> RegistrarAsync(int rolId, string nombreUsuario, string nombreCompleto,
        byte[] passwordHash, string passwordSalt, CancellationToken ct);
}
