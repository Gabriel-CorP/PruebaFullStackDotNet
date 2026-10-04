namespace Ventas.Application.Common.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IProductoRepository Productos { get; }
    IVentaRepository Ventas { get; }
    IUsuarioRepository Usuarios { get; }

    /// <summary>Acceso directo a SP para casos que no encajen en un repositorio.</summary>
    IStoredProcedureExecutor StoredProcedures { get; }

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);

    /// <summary>Persiste cambios hechos con EF (entidades del scaffold).</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Ejecuta varias operaciones (EF y/o SP) en una sola transacción.</summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
}
