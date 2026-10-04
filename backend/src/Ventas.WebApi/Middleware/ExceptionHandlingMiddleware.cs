using FluentValidation;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Models;

namespace Ventas.WebApi.Middleware;

/// <summary>Manejo centralizado de errores: siempre responde con ApiResponse (JSON) para que el front muestre notificaciones.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        int status;
        string message;
        IEnumerable<string>? errors = null;

        switch (ex)
        {
            case ValidationException v:
                status = StatusCodes.Status400BadRequest;
                message = "Hay errores de validación.";
                errors = v.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
                break;
            case NotFoundException n:
                status = StatusCodes.Status404NotFound;
                message = n.Message;
                break;
            case UnauthorizedException u:
                status = StatusCodes.Status401Unauthorized;
                message = u.Message;
                break;
            case BusinessException b:
                status = b.Code switch
                {
                    50002 => StatusCodes.Status404NotFound,                                   // no existe
                    50001 or 50010 or 50022 => StatusCodes.Status409Conflict,                 // duplicado / stock
                    _ => StatusCodes.Status400BadRequest
                };
                message = b.Message;
                break;
            default:
                status = StatusCodes.Status500InternalServerError;
                message = "Ocurrió un error inesperado. Intente nuevamente.";
                logger.LogError(ex, "Error no controlado en {Method} {Path}", context.Request.Method, context.Request.Path);
                break;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message, errors));
    }
}
