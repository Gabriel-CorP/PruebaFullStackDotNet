using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace Ventas.Web.Services;

/// <summary>Adjunta el JWT (guardado en la cookie de sesión) a cada llamada hacia la API.</summary>
public sealed class BearerTokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext;
        if (context is not null)
        {
            var token = await context.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
