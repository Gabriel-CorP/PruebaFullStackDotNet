using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ventas.Web.Models.ViewModels;
using Ventas.Web.Services;

namespace Ventas.Web.Controllers;

public class AccountController(IVentasApiClient api) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var sesion = await api.LoginAsync(model.NombreUsuario.Trim(), model.Password, ct);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
                new(ClaimTypes.Name, sesion.NombreCompleto),
                new("username", sesion.NombreUsuario),
                new(ClaimTypes.Role, sesion.Rol)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);

            // La cookie vence cuando vence el JWT; el token viaja dentro de la cookie cifrada.
            var properties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = new DateTimeOffset(DateTime.SpecifyKind(sesion.ExpiraEn, DateTimeKind.Utc))
            };
            properties.StoreTokens(new[] { new AuthenticationToken { Name = "access_token", Value = sesion.Token } });

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties);

            TempData["Success"] = $"Bienvenido, {sesion.NombreCompleto}";
            return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 401)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.Password = string.Empty;
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
