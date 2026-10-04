using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Web.Services;

namespace Ventas.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ReportesController(IVentasApiClient api) : Controller
{
    public Task<IActionResult> Pdf(DateTime? desde, DateTime? hasta, CancellationToken ct) => Descargar("pdf", desde, hasta, ct);

    public Task<IActionResult> Excel(DateTime? desde, DateTime? hasta, CancellationToken ct) => Descargar("excel", desde, hasta, ct);

    private async Task<IActionResult> Descargar(string formato, DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        try
        {
            var archivo = await api.DescargarReporteAsync(formato, desde, hasta, ct);
            return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 404)
        {
            // p. ej. "No se encontraron ventas en el rango seleccionado"
            TempData["Warning"] = ex.Message;
            return RedirectToAction("Historial", "Ventas", new
            {
                desde = desde?.ToString("yyyy-MM-dd"),
                hasta = hasta?.ToString("yyyy-MM-dd")
            });
        }
    }
}
