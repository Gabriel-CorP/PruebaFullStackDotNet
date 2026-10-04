using System.Data;
using Microsoft.Data.SqlClient;

namespace Ventas.Infrastructure.Persistence;

/// <summary>Fábrica de SqlParameter para mantener los repositorios legibles.</summary>
internal static class SqlParams
{
    public static SqlParameter In(string name, SqlDbType type, object? value, int size = 0)
    {
        var p = size > 0 ? new SqlParameter(name, type, size) : new SqlParameter(name, type);
        p.Value = value ?? DBNull.Value;
        return p;
    }

    public static SqlParameter Money(string name, decimal value) =>
        new(name, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = value };

    public static SqlParameter OutInt(string name) =>
        new(name, SqlDbType.Int) { Direction = ParameterDirection.Output };

    /// <summary>Table-Valued Parameter (ej. dbo.tvp_DetalleVenta).</summary>
    public static SqlParameter Tvp(string name, string typeName, DataTable table) =>
        new(name, SqlDbType.Structured) { TypeName = typeName, Value = table };
}
