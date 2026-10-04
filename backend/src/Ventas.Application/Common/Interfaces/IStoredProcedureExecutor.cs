using System.Data.Common;

namespace Ventas.Application.Common.Interfaces;

/// <summary>
/// Ejecución de procedimientos almacenados de SQL Server.
/// Participa automáticamente en la transacción abierta por el UnitOfWork.
/// </summary>
public interface IStoredProcedureExecutor
{
    /// <summary>SP que devuelve un resultset mapeado a T (propiedades = columnas).</summary>
    Task<List<T>> QueryAsync<T>(string procedureName, IEnumerable<DbParameter>? parameters = null,
        CancellationToken ct = default) where T : class;

    /// <summary>SP sin resultset (INSERT/UPDATE/DELETE). Soporta parámetros OUTPUT y TVP.</summary>
    Task<int> ExecuteAsync(string procedureName, IEnumerable<DbParameter>? parameters = null,
        CancellationToken ct = default);

    /// <summary>SP con múltiples resultsets: el mapeo lo hace el llamador con el DbDataReader.</summary>
    Task<TResult> QueryMultipleAsync<TResult>(string procedureName, IEnumerable<DbParameter>? parameters,
        Func<DbDataReader, CancellationToken, Task<TResult>> readerMapper, CancellationToken ct = default);
}
