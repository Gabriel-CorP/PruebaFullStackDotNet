using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Application.Common.Constants;
using Ventas.Application.DTOs;
using Ventas.Application.Features.Ventas;

namespace Ventas.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Administrador)]
public class ReportesController(ISender sender) : ControllerBase
{
    [HttpGet("ventas/pdf")]
    public Task<IActionResult> Pdf([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct) =>
        Exportar(desde, hasta, FormatoReporte.Pdf, ct);

    [HttpGet("ventas/excel")]
    public Task<IActionResult> Excel([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, CancellationToken ct) =>
        Exportar(desde, hasta, FormatoReporte.Excel, ct);

    private async Task<IActionResult> Exportar(DateTime? desde, DateTime? hasta, FormatoReporte formato, CancellationToken ct)
    {
        var archivo = await sender.Send(new ExportarReporteVentasQuery(desde, hasta, formato), ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
