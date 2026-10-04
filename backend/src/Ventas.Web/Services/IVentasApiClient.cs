using Ventas.Web.Models.Api;

namespace Ventas.Web.Services;

public interface IVentasApiClient
{
    Task<LoginResponse> LoginAsync(string nombreUsuario, string password, CancellationToken ct = default);

    Task<List<ProductoDto>> ListarProductosAsync(string? busqueda, CancellationToken ct = default);
    Task<ProductoDto> ObtenerProductoAsync(int id, CancellationToken ct = default);
    Task<ProductoDto> ObtenerProductoPorCodigoAsync(string codigo, CancellationToken ct = default);
    Task<ApiResponse<ProductoDto>> CrearProductoAsync(ProductoRequest request, CancellationToken ct = default);
    Task<ApiResponse<ProductoDto>> ActualizarProductoAsync(int id, ProductoRequest request, CancellationToken ct = default);
    Task<ApiResponse<object>> EliminarProductoAsync(int id, CancellationToken ct = default);

    Task<ApiResponse<VentaDto>> RegistrarVentaAsync(RegistrarVentaRequest request, CancellationToken ct = default);
    Task<VentaDto> ObtenerVentaAsync(int id, CancellationToken ct = default);
    Task<List<VentaResumenDto>> ListarVentasAsync(DateTime? desde, DateTime? hasta, CancellationToken ct = default);

    Task<ArchivoDescarga> DescargarReporteAsync(string formato, DateTime? desde, DateTime? hasta, CancellationToken ct = default);
}
