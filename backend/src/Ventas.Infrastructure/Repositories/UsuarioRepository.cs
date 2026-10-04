using System.Data;
using Ventas.Application.Common.Interfaces;
using Ventas.Application.DTOs;
using Ventas.Infrastructure.Persistence;

namespace Ventas.Infrastructure.Repositories;

public sealed class UsuarioRepository(IStoredProcedureExecutor sp) : IUsuarioRepository
{
    public async Task<UsuarioAuthDto?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct) =>
        (await sp.QueryAsync<UsuarioAuthDto>("dbo.usp_Usuario_ObtenerPorNombreUsuario",
            [SqlParams.In("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50)], ct)).FirstOrDefault();

    public async Task<int> RegistrarAsync(int rolId, string nombreUsuario, string nombreCompleto,
        byte[] passwordHash, string passwordSalt, CancellationToken ct)
    {
        var nuevoId = SqlParams.OutInt("@NuevoId");
        await sp.ExecuteAsync("dbo.usp_Usuario_Registrar",
        [
            SqlParams.In("@RolId", SqlDbType.Int, rolId),
            SqlParams.In("@NombreUsuario", SqlDbType.VarChar, nombreUsuario, 50),
            SqlParams.In("@NombreCompleto", SqlDbType.VarChar, nombreCompleto, 150),
            SqlParams.In("@PasswordHash", SqlDbType.VarBinary, passwordHash, 64),
            SqlParams.In("@PasswordSalt", SqlDbType.VarChar, passwordSalt, 36),
            nuevoId
        ], ct);
        return (int)nuevoId.Value;
    }
}
