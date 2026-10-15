using PizzaYa.Models;

namespace PizzaYa.Services;

/// <summary>
/// HU2 – Calcula el precio de una pizza personalizada.
/// Fórmula: (PrecioBase × FactorTamano) + PrecioBorde + ΣExtras + CostoDomicilio - Descuento.
/// El total nunca baja de 0.
/// </summary>
public class PrecioService
{
    /// <summary>Costo fijo de domicilio (en pesos colombianos).</summary>
    public const decimal CostoDomicilioFijo = 5000m;

    private static readonly List<CuponDescuento> _cupones = new()
    {
        new CuponDescuento("PIZZA10",  5000m,  "Descuento $5.000"),
        new CuponDescuento("PROMO20", 10000m,  "Descuento $10.000"),
        new CuponDescuento("GRATIS",  15000m,  "Descuento $15.000"),
    };

    /// <summary>
    /// Calcula el precio base de la pizza (sin domicilio ni cupón).
    /// </summary>
    public decimal CalcularPrecioBase(
        Pizza pizza,
        Tamano tamano,
        Borde borde,
        IEnumerable<Ingrediente> extras)
    {
        var precioTamano = pizza.PrecioBase * tamano.FactorPrecio;
        var precioExtras = extras.Sum(e => e.PrecioExtra);
        return precioTamano + borde.Precio + precioExtras;
    }

    /// <summary>
    /// Calcula el total final incluyendo domicilio y descuento por cupón.
    /// </summary>
    public decimal CalcularTotal(
        decimal precioBase,
        bool esDomicilio,
        decimal descuento = 0m)
    {
        var costo = esDomicilio ? CostoDomicilioFijo : 0m;
        var total = precioBase + costo - descuento;
        return Math.Max(0m, total);
    }

    /// <summary>
    /// Arma el ViewModel completo de precio para la vista Resumen.
    /// </summary>
    public ResumenConPrecioViewModel ArmarResumen(
        Pizza pizza,
        Tamano tamano,
        Borde borde,
        List<Ingrediente> extras,
        List<Ingrediente> quitados,
        bool esDomicilio,
        string? codigoCupon)
    {
        var precioConTamano = pizza.PrecioBase * tamano.FactorPrecio;
        var precioExtras = extras.Sum(e => e.PrecioExtra);
        var precioBase = precioConTamano + borde.Precio + precioExtras;
        var costoDomicilio = esDomicilio ? CostoDomicilioFijo : 0m;

        var (descuento, cuponAplicado, mensajeCupon) = ResolverCupon(codigoCupon, precioBase + costoDomicilio);

        var total = Math.Max(0m, precioBase + costoDomicilio - descuento);

        return new ResumenConPrecioViewModel
        {
            Pizza = pizza,
            Tamano = tamano,
            Borde = borde,
            Extras = extras,
            Quitados = quitados,
            PrecioBase = pizza.PrecioBase,
            PrecioConTamano = precioConTamano,
            PrecioBorde = borde.Precio,
            PrecioExtras = precioExtras,
            CostoDomicilio = costoDomicilio,
            Descuento = descuento,
            Total = total,
            EsDomicilio = esDomicilio,
            CodigoCupon = codigoCupon,
            CuponAplicado = cuponAplicado,
            MensajeCupon = mensajeCupon,
        };
    }

    /// <summary>
    /// Busca el cupón y devuelve el descuento aplicable.
    /// </summary>
    public (decimal Descuento, bool Aplicado, string? Mensaje) ResolverCupon(
        string? codigo,
        decimal totalSinDescuento)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return (0m, false, null);

        var cupon = _cupones.FirstOrDefault(
            c => c.Codigo.Equals(codigo.Trim(), StringComparison.OrdinalIgnoreCase));

        if (cupon is null)
            return (0m, false, "Cupón no válido o expirado.");

        // El descuento no puede superar el total
        var descuento = Math.Min(cupon.MontoDescuento, totalSinDescuento);
        return (descuento, true, $"✓ {cupon.Descripcion} aplicado.");
    }

    /// <summary>Lista de cupones disponibles (para pruebas unitarias).</summary>
    public IReadOnlyList<CuponDescuento> CuponesDisponibles => _cupones.AsReadOnly();
}
