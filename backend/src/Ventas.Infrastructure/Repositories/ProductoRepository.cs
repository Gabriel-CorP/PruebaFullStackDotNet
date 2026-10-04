using System.Data;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;
using Ventas.Infrastructure.Persistence;

namespace Ventas.Infrastructure.Repositories;

public sealed class ProductoRepository(IStoredProcedureExecutor sp) : IProductoRepository
{
    public Task<List<ProductoDto>> ListarAsync(string? busqueda, CancellationToken ct) =>
        sp.QueryAsync<ProductoDto>("dbo.usp_Producto_Listar",
            [SqlParams.In("@Busqueda", SqlDbType.VarChar, string.IsNullOrWhiteSpace(busqueda) ? null : busqueda, 100)], ct);

    public async Task<ProductoDto?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        (await sp.QueryAsync<ProductoDto>("dbo.usp_Producto_ObtenerPorId",
            [SqlParams.In("@Id", SqlDbType.Int, id)], ct)).FirstOrDefault();

    public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct) =>
        (await sp.QueryAsync<ProductoDto>("dbo.usp_Producto_ObtenerPorCodigo",
            [SqlParams.In("@Codigo", SqlDbType.VarChar, codigo, 30)], ct)).FirstOrDefault();

    public async Task<int> InsertarAsync(ProductoRequest p, CancellationToken ct)
    {
        var nuevoId = SqlParams.OutInt("@NuevoId");
        await sp.ExecuteAsync("dbo.usp_Producto_Insertar",
        [
            SqlParams.In("@Codigo", SqlDbType.VarChar, p.Codigo, 30),
            SqlParams.In("@Nombre", SqlDbType.VarChar, p.Nombre, 150),
            SqlParams.In("@Descripcion", SqlDbType.VarChar, p.Descripcion, 500),
            SqlParams.Money("@Precio", p.Precio),
            SqlParams.In("@Stock", SqlDbType.Int, p.Stock),
            nuevoId
        ], ct);
        return (int)nuevoId.Value;
    }

    public Task ActualizarAsync(int id, ProductoRequest p, CancellationToken ct) =>
        sp.ExecuteAsync("dbo.usp_Producto_Actualizar",
        [
            SqlParams.In("@Id", SqlDbType.Int, id),
            SqlParams.In("@Codigo", SqlDbType.VarChar, p.Codigo, 30),
            SqlParams.In("@Nombre", SqlDbType.VarChar, p.Nombre, 150),
            SqlParams.In("@Descripcion", SqlDbType.VarChar, p.Descripcion, 500),
            SqlParams.Money("@Precio", p.Precio),
            SqlParams.In("@Stock", SqlDbType.Int, p.Stock)
        ], ct);

    public Task EliminarAsync(int id, CancellationToken ct) =>
        sp.ExecuteAsync("dbo.usp_Producto_Eliminar", [SqlParams.In("@Id", SqlDbType.Int, id)], ct);
}
