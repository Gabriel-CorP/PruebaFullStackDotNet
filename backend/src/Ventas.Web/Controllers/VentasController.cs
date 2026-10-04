using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Web.Models.Api;
using Ventas.Web.Models.ViewModels;
using Ventas.Web.Services;

namespace Ventas.Web.Controllers;

[Authorize(Roles = "Administrador,Operador")]
public class VentasController(IVentasApiClient api, IConfiguration config) : Controller
{
    // ---------- Pantalla de venta (Operador y Administrador) ----------
    [HttpGet]
    public IActionResult Nueva() =>
        View(new NuevaVentaViewModel { IvaRate = config.GetValue("Ventas:IvaRate", 0.13m) });

    /// <summary>AJAX: busca un producto por código para agregarlo al detalle.</summary>
    [HttpGet]
    public async Task<IActionResult> BuscarProducto(string codigo, CancellationToken ct)
    {
        var producto = await api.ObtenerProductoPorCodigoAsync(codigo.Trim().ToUpperInvariant(), ct);
        return Json(new { success = true, data = producto });
    }

    /// <summary>AJAX: registra la venta a través de la API (el servidor recalcula precios, IVA y stock).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Debe agregar al menos un producto a la venta." });

        var resp = await api.RegistrarVentaAsync(request, ct);
        return Json(new { success = true, message = resp.Message, data = new { id = resp.Data!.Id, numeroVenta = resp.Data.NumeroVenta } });
    }

    /// <summary>Vista parcial con el detalle/comprobante de una venta (modal).</summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken ct) =>
        PartialView("_VentaDetalle", await api.ObtenerVentaAsync(id, ct));

    // ---------- Historial (solo Administrador) ----------
    [Authorize(Roles = "Administrador")]
    [HttpGet]
    public async Task<IActionResult> Historial(DateTime? desde, DateTime? hasta, CancellationToken ct) =>
        View(new HistorialViewModel
        {
            Desde = desde,
            Hasta = hasta,
            Ventas = await api.ListarVentasAsync(desde, hasta, ct)
        });
}
