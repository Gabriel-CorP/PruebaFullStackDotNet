using System.ComponentModel.DataAnnotations;
using Ventas.Web.Models.Api;

namespace Ventas.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class ProductoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(30, ErrorMessage = "El código no puede exceder 30 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0, 999999999.99, ErrorMessage = "Ingrese un precio válido (mayor o igual a 0).")]
    [Display(Name = "Precio")]
    public decimal? Precio { get; set; }

    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "Ingrese un stock válido (mayor o igual a 0).")]
    [Display(Name = "Stock")]
    public int? Stock { get; set; }

    public static ProductoFormViewModel From(ProductoDto p) => new()
    {
        Id = p.Id,
        Codigo = p.Codigo,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        Precio = p.Precio,
        Stock = p.Stock
    };

    public ProductoRequest ToRequest() => new()
    {
        Codigo = Codigo.Trim().ToUpperInvariant(),
        Nombre = Nombre.Trim(),
        Descripcion = string.IsNullOrWhiteSpace(Descripcion) ? null : Descripcion.Trim(),
        Precio = Precio ?? 0,
        Stock = Stock ?? 0
    };
}

public class ProductosIndexViewModel
{
    public string? Busqueda { get; set; }
    public List<ProductoDto> Productos { get; set; } = new();
}

public class NuevaVentaViewModel
{
    public decimal IvaRate { get; set; }
}

public class HistorialViewModel
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public List<VentaResumenDto> Ventas { get; set; } = new();
    public decimal TotalPeriodo => Ventas.Sum(v => v.Total);
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
}
