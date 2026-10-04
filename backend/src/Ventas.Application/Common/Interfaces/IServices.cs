using Ventas.Application.DTOs;

namespace Ventas.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string GenerateSalt();
    byte[] Hash(string password, string salt);
    bool Verify(string password, string salt, byte[] expectedHash);
}

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) Generate(UsuarioAuthDto usuario);
}

public interface ICurrentUserService
{
    int UserId { get; }
    string? UserName { get; }
    string? Role { get; }
}

public interface IReportService
{
    byte[] GenerarPdf(IReadOnlyList<VentaReporteRow> filas, DateTime? desde, DateTime? hasta);
    byte[] GenerarExcel(IReadOnlyList<VentaReporteRow> filas, DateTime? desde, DateTime? hasta);
}
