namespace Ventas.Web.Services;

/// <summary>Error devuelto por la API (o por no poder alcanzarla).</summary>
public class ApiException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    public ApiException(string message, int statusCode, IEnumerable<string>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors?.ToList() ?? new List<string>();
    }
}
