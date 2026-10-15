using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>
/// El carrito (HU3) y el módulo de pedidos (HU4) comparten una sola tabla y un solo repositorio:
/// lo que se confirma en el carrito es lo que se consulta después.
/// </summary>
public class PedidoUnificadoTests
{
    private readonly InMemoryPedidoRepository _repo = new();
    private readonly PedidoService _servicio;

    public PedidoUnificadoTests()
    {
        _servicio = new PedidoService(_repo, InMemoryMenuRepository.ConDatosDeEjemplo());
    }

    private static Carrito CarritoConUnaPizza()
    {
        var carrito = new Carrito();
        carrito.Items.Add(new ItemCarrito
        {
            Seleccion = new PizzaPersonalizada { PizzaId = 1, TamanoId = 2, BordeId = 2, ExtrasIds = { 8 }, QuitadosIds = { 4 } },
            NombrePizza = "Hawaiana",
            NombreTamano = "Mediana",
            NombreBorde = "Relleno de queso",
            NombresExtras = { "Pepperoni" },
            PrecioUnitario = 50_000m,
            Cantidad = 2,
        });
        return carrito;
    }

    private static PedidoService.DatosConfirmacion Datos(string numero = "PZ-20261014-AAAA") =>
        new(numero, "Ana", "3001234567", true, "Calle 1 # 2-3", 5_000m, 5_000m);

    [Fact]
    public async Task PedidoConfirmado_QuedaEnEstadoRecibido()
    {
        var pedido = await _servicio.CrearDesdeCarritoAsync(CarritoConUnaPizza(), Datos());

        Assert.True(pedido.Id > 0);
        Assert.Equal(EstadoPedido.Recibido, pedido.Estado);
    }

    [Fact]
    public async Task PedidoConfirmado_SeConsultaPorNumeroEnElMismoRepositorio()
    {
        await _servicio.CrearDesdeCarritoAsync(CarritoConUnaPizza(), Datos("PZ-20261014-BBBB"));

        var encontrado = await _repo.ObtenerPorNumeroAsync("pz-20261014-bbbb");

        Assert.NotNull(encontrado);
        Assert.Equal("Ana", encontrado!.NombreCliente);
    }

    [Fact]
    public async Task PedidoConfirmado_GuardaTotalesYDatosDeEntrega()
    {
        var pedido = await _servicio.CrearDesdeCarritoAsync(CarritoConUnaPizza(), Datos());

        Assert.Equal(100_000m, pedido.Subtotal);
        Assert.Equal(5_000m, pedido.CostoDomicilio);
        Assert.Equal(5_000m, pedido.Descuento);
        Assert.Equal(100_000m, pedido.Total);
        Assert.True(pedido.EsDomicilio);
        Assert.Equal("Calle 1 # 2-3", pedido.Direccion);
    }

    [Fact]
    public async Task PedidoConfirmado_GuardaCadaPizzaConSusIngredientes()
    {
        var pedido = await _servicio.CrearDesdeCarritoAsync(CarritoConUnaPizza(), Datos());

        var item = Assert.Single(pedido.Items);
        Assert.Equal("Hawaiana", item.NombrePizza);
        Assert.Equal(2, item.Cantidad);
        Assert.Contains("Jamón", item.IngredientesBase);
        Assert.Equal(new[] { "Pepperoni" }, item.ExtrasNombres);
        Assert.Equal(new[] { "Champiñones" }, item.QuitadosNombres);
    }

    [Fact]
    public async Task RecogerEnTienda_NoGuardaDireccion()
    {
        var datos = Datos() with { EsDomicilio = false, CostoDomicilio = 0m, Descuento = 0m };

        var pedido = await _servicio.CrearDesdeCarritoAsync(CarritoConUnaPizza(), datos);

        Assert.Null(pedido.Direccion);
        Assert.Equal(100_000m, pedido.Total);
    }
}
