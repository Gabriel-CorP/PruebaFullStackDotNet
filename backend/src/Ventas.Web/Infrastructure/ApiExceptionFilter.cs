using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Ventas.Web.Services;

namespace Ventas.Web.Infrastructure;

/// <summary>
/// Manejo de errores centralizado:
///  - AJAX (X-Requested-With): responde JSON { success:false, message, errors } con el código HTTP real.
///  - Navegación normal: guarda el mensaje en TempData (se muestra como notificación) y redirige.
/// </summary>
public sealed class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, ITempDataDictionaryFactory tempDataFactory) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var http = context.HttpContext;
        var esAjax = http.Request.Headers.XRequestedWith == "XMLHttpRequest";

        if (context.Exception is ApiException api)
        {
            if (api.StatusCode == StatusCodes.Status401Unauthorized)
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (api.StatusCode >= 500) logger.LogError(api, "Error de la API: {Message}", api.Message);

            context.Result = esAjax ? JsonError(api.Message, api.Errors, api.StatusCode) : RedirigirConMensaje(http, api);
            context.ExceptionHandled = true;
            return;
        }

        if (esAjax)
        {
            logger.LogError(context.Exception, "Error no controlado en petición AJAX");
            context.Result = JsonError("Ocurrió un error inesperado. Intente nuevamente.", Array.Empty<string>(), StatusCodes.Status500InternalServerError);
            context.ExceptionHandled = true;
        }
        // Si no es AJAX, lo atiende UseExceptionHandler("/Home/Error").
    }

    private static JsonResult JsonError(string message, IReadOnlyList<string> errors, int status) =>
        new(new { success = false, message, errors }) { StatusCode = status };

    private IActionResult RedirigirConMensaje(HttpContext http, ApiException api)
    {
        var temp = tempDataFactory.GetTempData(http);

        if (api.StatusCode == StatusCodes.Status401Unauthorized)
        {
            temp["Warning"] = api.Message;
            temp.Save();
            return new RedirectToActionResult("Login", "Account", null);
        }

        if (api.StatusCode == StatusCodes.Status403Forbidden)
            return new RedirectToActionResult("AccessDenied", "Account", null);

        if (api.StatusCode >= 500)
        {
            // Pantalla sin llamadas a la API (evita bucles de redirección si la API está caída).
            temp["ApiError"] = api.Message;
            temp.Save();
            return new RedirectToActionResult("ApiError", "Home", null);
        }

        temp["Error"] = api.Message;
        temp.Save();
        return new RedirectToActionResult("Index", "Home", null);
    }
}
