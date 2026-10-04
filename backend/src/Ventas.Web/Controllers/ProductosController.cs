using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Web.Models.ViewModels;
using Ventas.Web.Services;

namespace Ventas.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ProductosController(IVentasApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken ct) =>
        View(new ProductosIndexViewModel { Busqueda = q, Productos = await api.ListarProductosAsync(q, ct) });

    [HttpGet]
    public IActionResult Create() => View("Form", new ProductoFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductoFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Form", model);

        try
        {
            var resp = await api.CrearProductoAsync(model.ToRequest(), ct);
            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 404 or 409)
        {
            AgregarErrores(ex);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct) =>
        View("Form", ProductoFormViewModel.From(await api.ObtenerProductoAsync(id, ct)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductoFormViewModel model, CancellationToken ct)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View("Form", model);

        try
        {
            var resp = await api.ActualizarProductoAsync(id, model.ToRequest(), ct);
            TempData["Success"] = resp.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 404 or 409)
        {
            AgregarErrores(ex);
            return View("Form", model);
        }
    }

    /// <summary>La confirmación ("¿está seguro?") se hace en el navegador antes de enviar este POST.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var resp = await api.EliminarProductoAsync(id, ct);
            TempData["Success"] = resp.Message;
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 404 or 409)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private void AgregarErrores(ApiException ex)
    {
        ModelState.AddModelError(string.Empty, ex.Message);
        foreach (var e in ex.Errors) ModelState.AddModelError(string.Empty, e);
    }
}
