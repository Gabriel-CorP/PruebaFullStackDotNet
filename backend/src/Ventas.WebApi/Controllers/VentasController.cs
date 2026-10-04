using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Application.Common.Constants;
using Ventas.Application.Common.Models;
using Ventas.Application.DTOs;
using Ventas.Application.Features.Ventas;

namespace Ventas.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Todos)]
public class VentasController(ISender sender) : ControllerBase
{
    /// <summary>Registra una venta. El servidor recalcula precios, IVA, total y descuenta stock.</summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaRequest request, CancellationToken ct)
    {
        var venta = await sender.Send(new RegistrarVentaCommand(request.Items), ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = venta.Id },
            ApiResponse.Ok(venta, $"Venta {venta.NumeroVenta} registrada correctamente."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new GetVentaByIdQuery(id), ct), "Venta obtenida."));

    [HttpGet]
    [Authorize(Roles = AppRoles.Administrador)]
    public async Task<IActionResult> Listar([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await sender.Send(new GetVentasQuery(desde, hasta), ct), "Ventas obtenidas."));
}
