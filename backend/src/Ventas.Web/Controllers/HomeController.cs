using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Web.Models.ViewModels;

namespace Ventas.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>Pantalla inicial según rol: el operador solo puede vender.</summary>
    public IActionResult Index() =>
        User.IsInRole("Administrador")
            ? RedirectToAction("Index", "Productos")
            : RedirectToAction("Nueva", "Ventas");

    [AllowAnonymous]
    public IActionResult ApiError() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
