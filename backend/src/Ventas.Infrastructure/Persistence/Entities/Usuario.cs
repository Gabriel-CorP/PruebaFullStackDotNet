using System;
using System.Collections.Generic;

namespace Ventas.Infrastructure.Persistence.Entities;

public partial class Usuario
{
    public int Id { get; set; }

    public int RolId { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public string PasswordSalt { get; set; } = null!;

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Role Rol { get; set; } = null!;

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}
