using PizzaYa.Models;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>
/// Criterios de aceptación de la HU2 – Cálculo de precio en tiempo real.
/// </summary>
public class HU2PrecioTests
{
    private readonly PrecioService _svc = new();

    // Datos de prueba reutilizables
    private static Pizza PizzaEjemplo => new() { Id = 1, Nombre = "Hawaiana", PrecioBase = 28_000m };
    private static Tamano TamanoPersonal  => new() { Id = 1, Nombre = "Personal",  FactorPrecio = 1.00m };
    private static Tamano TamanoMediana   => new() { Id = 2, Nombre = "Mediana",   FactorPrecio = 1.60m };
    private static Tamano TamanoFamiliar  => new() { Id = 3, Nombre = "Familiar",  FactorPrecio = 2.20m };
    private static Borde BordeSinCosto    => new() { Id = 1, Nombre = "Tradicional",       Precio = 0m };
    private static Borde BordeConCosto    => new() { Id = 2, Nombre = "Relleno de queso",  Precio = 5_000m };

    // ── Criterio: precio base × factor tamaño ──────────────────────────

    [Theory]
    [InlineData(1.00, 28_000)]   // Personal
    [InlineData(1.60, 44_800)]   // Mediana
    [InlineData(2.20, 61_600)]   // Familiar
    public void PrecioConTamano_EsBaseMultiplicadoPorFactor(decimal factor, decimal esperado)
    {
        var tamano = new Tamano { FactorPrecio = factor };
        var resultado = _svc.CalcularPrecioBase(PizzaEjemplo, tamano, BordeSinCosto, Enumerable.Empty<Ingrediente>());
        Assert.Equal(esperado, resultado);
    }

    // ── Criterio: precio del borde se suma al resultado ─────────────────

    [Fact]
    public void BordeConCosto_SeSumaAlPrecio()
    {
        var resultado = _svc.CalcularPrecioBase(PizzaEjemplo, TamanoPersonal, BordeConCosto, Enumerable.Empty<Ingrediente>());
        // 28.000 × 1 + 5.000 = 33.000
        Assert.Equal(33_000m, resultado);
    }

    [Fact]
    public void BordeSinCosto_NoAumentaElPrecio()
    {
        var resultado = _svc.CalcularPrecioBase(PizzaEjemplo, TamanoPersonal, BordeSinCosto, Enumerable.Empty<Ingrediente>());
        Assert.Equal(28_000m, resultado);
    }

    // ── Criterio: extras se suman ───────────────────────────────────────

    [Fact]
    public void Extras_SeAcumulanAlPrecio()
    {
        var extras = new List<Ingrediente>
        {
            new() { PrecioExtra = 3_000m },
            new() { PrecioExtra = 4_000m },
        };
        var resultado = _svc.CalcularPrecioBase(PizzaEjemplo, TamanoPersonal, BordeSinCosto, extras);
        // 28.000 + 3.000 + 4.000 = 35.000
        Assert.Equal(35_000m, resultado);
    }

    // ── Criterio: domicilio suma $5.000 ────────────────────────────────

    [Fact]
    public void EsDomicilio_AgregaCostoDomicilio()
    {
        var total = _svc.CalcularTotal(28_000m, esDomicilio: true);
        Assert.Equal(28_000m + PrecioService.CostoDomicilioFijo, total);
    }

    [Fact]
    public void NoEsDomicilio_NoseSumaCostoDomicilio()
    {
        var total = _svc.CalcularTotal(28_000m, esDomicilio: false);
        Assert.Equal(28_000m, total);
    }

    // ── Criterio: el total nunca baja de 0 ─────────────────────────────

    [Fact]
    public void DescuentoMayorAlTotal_ResultaEnCero()
    {
        var total = _svc.CalcularTotal(5_000m, esDomicilio: false, descuento: 50_000m);
        Assert.Equal(0m, total);
    }

    [Fact]
    public void DescuentoExacto_ResultaEnCero()
    {
        var total = _svc.CalcularTotal(28_000m, esDomicilio: false, descuento: 28_000m);
        Assert.Equal(0m, total);
    }

    // ── Criterio: cupones ───────────────────────────────────────────────

    [Theory]
    [InlineData("PIZZA10",  5_000)]
    [InlineData("PROMO20", 10_000)]
    [InlineData("GRATIS",  15_000)]
    public void CuponValido_AplicaDescuento(string codigo, decimal descuentoEsperado)
    {
        var (descuento, aplicado, mensaje) = _svc.ResolverCupon(codigo, 100_000m);

        Assert.True(aplicado);
        Assert.Equal(descuentoEsperado, descuento);
        Assert.NotNull(mensaje);
    }

    [Fact]
    public void CuponInvalido_NoAplicaDescuento()
    {
        var (descuento, aplicado, mensaje) = _svc.ResolverCupon("INVALIDO", 100_000m);

        Assert.False(aplicado);
        Assert.Equal(0m, descuento);
        Assert.NotNull(mensaje);
    }

    [Fact]
    public void CuponNulo_NoAplicaDescuento()
    {
        var (descuento, aplicado, _) = _svc.ResolverCupon(null, 100_000m);
        Assert.False(aplicado);
        Assert.Equal(0m, descuento);
    }

    [Fact]
    public void CuponCaseInsensitive_Funciona()
    {
        var (descuento, aplicado, _) = _svc.ResolverCupon("pizza10", 100_000m);
        Assert.True(aplicado);
        Assert.Equal(5_000m, descuento);
    }

    [Fact]
    public void Cupon_NoDesbordaElTotal()
    {
        // Si el total es 3.000 y el cupón da 5.000, el descuento es solo 3.000
        var (descuento, aplicado, _) = _svc.ResolverCupon("PIZZA10", 3_000m);
        Assert.True(aplicado);
        Assert.Equal(3_000m, descuento); // capped al total
    }

    // ── Criterio: ArmarResumen integra todo correctamente ───────────────

    [Fact]
    public void ArmarResumen_CalculaTotalCorrectamente()
    {
        var extras = new List<Ingrediente>
        {
            new() { Id = 1, Nombre = "Jamón",  PrecioExtra = 3_500m },
            new() { Id = 2, Nombre = "Piña",   PrecioExtra = 2_500m },
        };

        var vm = _svc.ArmarResumen(
            PizzaEjemplo, TamanoMediana, BordeConCosto,
            extras, new List<Ingrediente>(),
            esDomicilio: true, codigoCupon: "PIZZA10");

        // 28.000 × 1.6 + 5.000 + 6.000 = 55.800 + 5.000 domicilio - 5.000 cupón = 55.800
        Assert.Equal(55_800m, vm.Total);
        Assert.True(vm.CuponAplicado);
        Assert.Equal(5_000m, vm.Descuento);
        Assert.Equal(5_000m, vm.CostoDomicilio);
    }
}
