using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ventas.Application.Common.Interfaces;

namespace Ventas.Infrastructure.Persistence;

public sealed class UnitOfWork(
    VentasDbContext db,
    IStoredProcedureExecutor storedProcedures,
    IProductoRepository productos,
    IVentaRepository ventas,
    IUsuarioRepository usuarios) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public IProductoRepository Productos => productos;
    public IVentaRepository Ventas => ventas;
    public IUsuarioRepository Usuarios => usuarios;
    public IStoredProcedureExecutor StoredProcedures => storedProcedures;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null) return;
        _transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        try
        {
            await db.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        try { await _transaction.RollbackAsync(ct); }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        var owner = _transaction is null;
        if (owner) await BeginTransactionAsync(ct);

        try
        {
            var result = await action(ct);
            if (owner) await CommitAsync(ct);
            return result;
        }
        catch
        {
            if (owner) await RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
    }
}
