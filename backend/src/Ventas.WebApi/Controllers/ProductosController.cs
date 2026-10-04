using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Application.Common.Constants;
using Ventas.Application.Common.Models;
using Ventas.Application.DTOs;
using Ventas.Application.Features.Productos;

namespace Ventas.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Administrador)]   // CRUD solo para Administrador
public class ProductosController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new GetProductosQuery(busqueda), ct), "Productos obtenidos."));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new GetProductoByIdQuery(id), ct), "Producto obtenido."));

    /// <summary>Búsqueda por código para la pantalla de ventas (Operador y Administrador).</summary>
    [HttpGet("codigo/{codigo}")]
    [Authorize(Roles = AppRoles.Todos)]
    public async Task<IActionResult> ObtenerPorCodigo(string codigo, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new GetProductoByCodigoQuery(codigo), ct), "Producto encontrado."));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ProductoRequest request, CancellationToken ct)
    {
        var producto = await sender.Send(new CreateProductoCommand(request), ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = producto.Id },
            ApiResponse.Ok(producto, "Producto creado correctamente."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ProductoRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new UpdateProductoCommand(id, request), ct), "Producto actualizado correctamente."));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await sender.Send(new DeleteProductoCommand(id), ct);
        return Ok(ApiResponse.Ok<object?>(null, "Producto eliminado correctamente."));
    }
}
