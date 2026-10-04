namespace Ventas.Application.DTOs;

public class UsuarioAuthDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public string PasswordSalt { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int RolId { get; set; }
    public string Rol { get; set; } = string.Empty;
}

public record LoginRequest(string NombreUsuario, string Password);

public record LoginResponse(string Token, DateTime ExpiraEn, int UsuarioId, string NombreUsuario, string NombreCompleto, string Rol);

public record RegistrarUsuarioRequest(int RolId, string NombreUsuario, string NombreCompleto, string Password);

public record ArchivoDto(byte[] Contenido, string ContentType, string NombreArchivo);

public enum FormatoReporte { Pdf, Excel }
