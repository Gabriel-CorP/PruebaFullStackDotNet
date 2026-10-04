using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ventas.Application.Common.Exceptions;
using Ventas.Application.Common.Interfaces;

namespace Ventas.Infrastructure.Persistence;

public sealed class StoredProcedureExecutor(VentasDbContext db) : IStoredProcedureExecutor
{
    public async Task<List<T>> QueryAsync<T>(string procedureName, IEnumerable<DbParameter>? parameters = null,
        CancellationToken ct = default) where T : class
    {
        var ps = parameters?.ToArray() ?? Array.Empty<DbParameter>();
        try
        {
            // SqlQueryRaw participa en la transacción actual del DbContext.
            return await db.Database
                .SqlQueryRaw<T>(BuildCall(procedureName, ps), ps.Cast<object>().ToArray())
                .ToListAsync(ct);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            throw new BusinessException(ex.Message, ex.Number);
        }
    }

    public async Task<int> ExecuteAsync(string procedureName, IEnumerable<DbParameter>? parameters = null,
        CancellationToken ct = default)
    {
        var ps = parameters?.ToArray() ?? Array.Empty<DbParameter>();
        try
        {
            return await db.Database.ExecuteSqlRawAsync(BuildCall(procedureName, ps), ps.Cast<object>(), ct);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            throw new BusinessException(ex.Message, ex.Number);
        }
    }

    public async Task<TResult> QueryMultipleAsync<TResult>(string procedureName, IEnumerable<DbParameter>? parameters,
        Func<DbDataReader, CancellationToken, Task<TResult>> readerMapper, CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            if (parameters is not null)
                foreach (var p in parameters) command.Parameters.Add(p);

            await using var reader = await command.ExecuteReaderAsync(ct);
            return await readerMapper(reader, ct);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            throw new BusinessException(ex.Message, ex.Number);
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    // EXEC dbo.usp_X @A = @A, @Out = @Out OUTPUT
    private static string BuildCall(string procedureName, IReadOnlyCollection<DbParameter> ps)
    {
        if (ps.Count == 0) return $"EXEC {procedureName}";

        var args = ps.Select(p =>
            $"{p.ParameterName} = {p.ParameterName}" +
            (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput ? " OUTPUT" : ""));
        return $"EXEC {procedureName} {string.Join(", ", args)}";
    }
}
