using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ventas.Web.Models.Api;

namespace Ventas.Web.Services;

public sealed class VentasApiClient(HttpClient http) : IVentasApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ---------------- Auth ----------------
    public async Task<LoginResponse> LoginAsync(string nombreUsuario, string password, CancellationToken ct = default) =>
        (await SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login",
            new LoginRequest { NombreUsuario = nombreUsuario, Password = password }, ct)).Data!;

    // ---------------- Productos ----------------
    public async Task<List<ProductoDto>> ListarProductosAsync(string? busqueda, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(busqueda) ? "api/productos" : $"api/productos?busqueda={Uri.EscapeDataString(busqueda)}";
        return (await SendAsync<List<ProductoDto>>(HttpMethod.Get, url, null, ct)).Data ?? new();
    }

    public async Task<ProductoDto> ObtenerProductoAsync(int id, CancellationToken ct = default) =>
        (await SendAsync<ProductoDto>(HttpMethod.Get, $"api/productos/{id}", null, ct)).Data!;

    public async Task<ProductoDto> ObtenerProductoPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        (await SendAsync<ProductoDto>(HttpMethod.Get, $"api/productos/codigo/{Uri.EscapeDataString(codigo)}", null, ct)).Data!;

    public Task<ApiResponse<ProductoDto>> CrearProductoAsync(ProductoRequest request, CancellationToken ct = default) =>
        SendAsync<ProductoDto>(HttpMethod.Post, "api/productos", request, ct);

    public Task<ApiResponse<ProductoDto>> ActualizarProductoAsync(int id, ProductoRequest request, CancellationToken ct = default) =>
        SendAsync<ProductoDto>(HttpMethod.Put, $"api/productos/{id}", request, ct);

    public Task<ApiResponse<object>> EliminarProductoAsync(int id, CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Delete, $"api/productos/{id}", null, ct);

    // ---------------- Ventas ----------------
    public Task<ApiResponse<VentaDto>> RegistrarVentaAsync(RegistrarVentaRequest request, CancellationToken ct = default) =>
        SendAsync<VentaDto>(HttpMethod.Post, "api/ventas", request, ct);

    public async Task<VentaDto> ObtenerVentaAsync(int id, CancellationToken ct = default) =>
        (await SendAsync<VentaDto>(HttpMethod.Get, $"api/ventas/{id}", null, ct)).Data!;

    public async Task<List<VentaResumenDto>> ListarVentasAsync(DateTime? desde, DateTime? hasta, CancellationToken ct = default) =>
        (await SendAsync<List<VentaResumenDto>>(HttpMethod.Get, $"api/ventas{RangoQuery(desde, hasta)}", null, ct)).Data ?? new();

    // ---------------- Reportes ----------------
    public async Task<ArchivoDescarga> DescargarReporteAsync(string formato, DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/reportes/ventas/{formato}{RangoQuery(desde, hasta)}");
        using var response = await SendRawAsync(request, ct);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var disposition = response.Content.Headers.ContentDisposition;
        var nombre = (disposition?.FileNameStar ?? disposition?.FileName)?.Trim('"') ?? $"ReporteVentas.{(formato == "pdf" ? "pdf" : "xlsx")}";

        return new ArchivoDescarga(await response.Content.ReadAsByteArrayAsync(ct), contentType, nombre);
    }

    // ---------------- Infraestructura HTTP ----------------
    private static string RangoQuery(DateTime? desde, DateTime? hasta)
    {
        var parts = new List<string>();
        if (desde.HasValue) parts.Add($"desde={desde.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        if (hasta.HasValue) parts.Add($"hasta={hasta.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);

        using var response = await SendRawAsync(request, ct);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json, ct);
        return result ?? throw new ApiException("La API devolvió una respuesta inválida.", (int)HttpStatusCode.BadGateway);
    }

    /// <summary>Envía la petición y convierte cualquier fallo en ApiException con un mensaje apto para el usuario.</summary>
    private async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("No se pudo conectar con la API. Verifique que el servicio esté disponible.", (int)HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("La API tardó demasiado en responder. Intente nuevamente.", (int)HttpStatusCode.GatewayTimeout);
        }

        if (response.IsSuccessStatusCode) return response;

        using (response)
        {
            throw await ToApiExceptionAsync(response, ct);
        }
    }

    private static async Task<ApiException> ToApiExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        ApiResponse<object>? body = null;
        try
        {
            body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(Json, ct);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // cuerpo vacío o no JSON (ej. 401/403 sin contenido)
        }

        var message = !string.IsNullOrWhiteSpace(body?.Message)
            ? body!.Message
            : response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Su sesión expiró. Inicie sesión nuevamente.",
                HttpStatusCode.Forbidden => "No tiene permisos para realizar esta operación.",
                HttpStatusCode.NotFound => "El recurso solicitado no existe.",
                _ => "Ocurrió un error inesperado en la API."
            };

        return new ApiException(message, status, body?.Errors);
    }
}
