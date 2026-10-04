using System.Data;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;
using Ventas.Infrastructure.Persistence;

namespace Ventas.Infrastructure.Repositories;

public sealed class VentaRepository(IStoredProcedureExecutor sp) : IVentaRepository
{
    public async Task<int> RegistrarAsync(int usuarioId, IEnumerable<VentaItemRequest> items, CancellationToken ct)
    {
        // TVP: dbo.tvp_DetalleVenta (ProductoId INT, Cantidad INT)
        var tabla = new DataTable();
        tabla.Columns.Add("ProductoId", typeof(int));
        tabla.Columns.Add("Cantidad", typeof(int));
        foreach (var i in items) tabla.Rows.Add(i.ProductoId, i.Cantidad);

        var nuevoId = SqlParams.OutInt("@NuevoId");
        await sp.ExecuteAsync("dbo.usp_Venta_Registrar",
        [
            SqlParams.In("@UsuarioId", SqlDbType.Int, usuarioId),
            SqlParams.Tvp("@Detalle", "dbo.tvp_DetalleVenta", tabla),
            nuevoId
        ], ct);
        return (int)nuevoId.Value;
    }

    public Task<VentaDto?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        sp.QueryMultipleAsync<VentaDto?>("dbo.usp_Venta_ObtenerPorId",
            [SqlParams.In("@Id", SqlDbType.Int, id)],
            async (r, token) =>
            {
                if (!await r.ReadAsync(token)) return null;

                var venta = new VentaDto
                {
                    Id = r.GetInt32(r.GetOrdinal("Id")),
                    NumeroVenta = r.GetString(r.GetOrdinal("NumeroVenta")),
                    Fecha = r.GetDateTime(r.GetOrdinal("Fecha")),
                    UsuarioId = r.GetInt32(r.GetOrdinal("UsuarioId")),
                    Usuario = r.GetString(r.GetOrdinal("Usuario")),
                    Subtotal = r.GetDecimal(r.GetOrdinal("Subtotal")),
                    Iva = r.GetDecimal(r.GetOrdinal("Iva")),
                    Total = r.GetDecimal(r.GetOrdinal("Total"))
                };

                await r.NextResultAsync(token);
                while (await r.ReadAsync(token))
                {
                    venta.Detalle.Add(new VentaDetalleDto
                    {
                        Id = r.GetInt32(r.GetOrdinal("Id")),
                        ProductoId = r.GetInt32(r.GetOrdinal("ProductoId")),
                        Codigo = r.GetString(r.GetOrdinal("Codigo")),
                        Producto = r.GetString(r.GetOrdinal("Producto")),
                        Cantidad = r.GetInt32(r.GetOrdinal("Cantidad")),
                        PrecioUnitario = r.GetDecimal(r.GetOrdinal("PrecioUnitario")),
                        Subtotal = r.GetDecimal(r.GetOrdinal("Subtotal"))
                    });
                }
                return venta;
            }, ct);

    public Task<List<VentaResumenDto>> ListarAsync(DateTime? desde, DateTime? hasta, CancellationToken ct) =>
        sp.QueryAsync<VentaResumenDto>("dbo.usp_Venta_Listar", RangoFechas(desde, hasta), ct);

    public Task<List<VentaReporteRow>> ReporteAsync(DateTime? desde, DateTime? hasta, CancellationToken ct) =>
        sp.QueryAsync<VentaReporteRow>("dbo.usp_Venta_Reporte", RangoFechas(desde, hasta), ct);

    private static Microsoft.Data.SqlClient.SqlParameter[] RangoFechas(DateTime? desde, DateTime? hasta) =>
    [
        SqlParams.In("@FechaInicio", SqlDbType.Date, desde?.Date),
        SqlParams.In("@FechaFin", SqlDbType.Date, hasta?.Date)
    ];
}
