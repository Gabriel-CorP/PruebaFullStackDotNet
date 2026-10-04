using System;
using System.Collections.Generic;

namespace Ventas.Infrastructure.Persistence.Entities;

public partial class Venta
{
    public int Id { get; set; }

    public string? NumeroVenta { get; set; }

    public DateTime Fecha { get; set; }

    public int UsuarioId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Total { get; set; }

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    public virtual Usuario Usuario { get; set; } = null!;
}
