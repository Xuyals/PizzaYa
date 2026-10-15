using System.ComponentModel.DataAnnotations;

namespace PizzaYa.Models;

// ─────────────────────────────────────────────────────────
// HU2 – ViewModels del Resumen con precio en tiempo real
// ─────────────────────────────────────────────────────────

/// <summary>
/// Extiende ResumenPizzaViewModel con los datos de precio (HU2).
/// Es el modelo que usa Views/Menu/Resumen.cshtml.
/// </summary>
public class ResumenConPrecioViewModel
{
    // ── Descripción de la pizza ──
    public Pizza Pizza { get; set; } = new();
    public Tamano Tamano { get; set; } = new();
    public Borde Borde { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public List<Ingrediente> Quitados { get; set; } = new();

    // ── Desglose de precio (HU2) ──
    public decimal PrecioBase { get; set; }
    public decimal PrecioConTamano { get; set; }
    public decimal PrecioBorde { get; set; }
    public decimal PrecioExtras { get; set; }
    public decimal CostoDomicilio { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    // ── Opciones de entrega ──
    public bool EsDomicilio { get; set; } = false;

    // ── Cupón ──
    public string? CodigoCupon { get; set; }
    public bool CuponAplicado { get; set; }
    public string? MensajeCupon { get; set; }
}

// ─────────────────────────────────────────────────────────
// HU3 – ViewModels del Carrito y Confirmación de Pedido
// ─────────────────────────────────────────────────────────

/// <summary>
/// Modelo de la vista del carrito (HU3).
/// </summary>
public class CarritoViewModel
{
    public Carrito Carrito { get; set; } = new();

    // Totales calculados incluyendo domicilio
    public decimal CostoDomicilio { get; set; }
    public decimal Total => Carrito.Subtotal + CostoDomicilio;
    public bool EsDomicilio { get; set; }
}

/// <summary>
/// Formulario para confirmar el pedido (HU3).
/// </summary>
public class ConfirmarPedidoViewModel
{
    public Carrito Carrito { get; set; } = new();

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [RegularExpression(@"^\+?[\d\s\-]{7,15}$", ErrorMessage = "Ingresa un teléfono válido.")]
    public string Telefono { get; set; } = "";

    public bool EsDomicilio { get; set; }

    [RequiredIfDomicilio]
    [StringLength(200)]
    public string? Direccion { get; set; }

    public decimal CostoDomicilio { get; set; }

    public decimal Subtotal => Carrito.Subtotal;
    public decimal Total => Subtotal + CostoDomicilio;

    public string? CodigoCupon { get; set; }
    public decimal Descuento { get; set; }
    public decimal TotalConDescuento => Math.Max(0, Total - Descuento);
}

/// <summary>
/// Atributo de validación: Dirección obligatoria solo si es a domicilio.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RequiredIfDomicilioAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var model = (ConfirmarPedidoViewModel)context.ObjectInstance;
        if (model.EsDomicilio && string.IsNullOrWhiteSpace(value as string))
            return new ValidationResult(
                "La dirección es obligatoria para domicilio.",
                new[] { context.MemberName ?? nameof(ConfirmarPedidoViewModel.Direccion) });
        return ValidationResult.Success;
    }
}

/// <summary>
/// Resumen del pedido confirmado (HU3).
/// </summary>
public class PedidoConfirmadoViewModel
{
    public string NumeroPedido { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public bool EsDomicilio { get; set; }
    public List<ItemCarrito> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal CostoDomicilio { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public DateTime FechaPedido { get; set; } = DateTime.Now;
}

/// <summary>
/// Cupones de descuento válidos (HU2).
/// </summary>
public record CuponDescuento(string Codigo, decimal MontoDescuento, string Descripcion);
