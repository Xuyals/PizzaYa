using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PizzaYa.Controllers;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU5 (Equipo 4): inventario y pedidos a proveedores.</summary>
public class HU5InventarioTests
{
    private readonly InventarioService _servicio = new();
    private readonly InMemoryInventarioRepository _repo;

    public HU5InventarioTests()
    {
        _repo = new InMemoryInventarioRepository(new[]
        {
            new IngredienteInventario { Id = 1, Nombre = "Queso", Unidad = "kg", Cantidad = 4, CantidadMinima = 8 },  // por agotarse
            new IngredienteInventario { Id = 2, Nombre = "Masa", Unidad = "kg", Cantidad = 20, CantidadMinima = 10 }, // suficiente
            new IngredienteInventario { Id = 3, Nombre = "Piña", Unidad = "kg", Cantidad = 3, CantidadMinima = 3 },   // justo en el mínimo
        });
    }

    private InventarioController NuevoController() => new(_repo, _servicio)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataFalso()),
    };

    private static NuevoPedidoProveedor Pedido(string? proveedor, params (int Id, decimal? Cantidad)[] lineas) => new()
    {
        Proveedor = proveedor,
        Lineas = lineas.Select(l => new LineaPedido { IngredienteId = l.Id, Cantidad = l.Cantidad }).ToList(),
    };

    private async Task<decimal> CantidadDeAsync(string nombre) =>
        (await _repo.ObtenerInventarioAsync()).First(i => i.Nombre == nombre).Cantidad;

    // Criterio 1: ver inventario
    [Fact]
    public async Task Inventario_MuestraCadaIngredienteConSuCantidad()
    {
        var resultado = Assert.IsType<ViewResult>(await NuevoController().Index());
        var modelo = Assert.IsType<InventarioViewModel>(resultado.Model);

        Assert.Equal(3, modelo.Ingredientes.Count);
        Assert.Equal(4, modelo.Ingredientes.First(i => i.Nombre == "Queso").Cantidad);
        Assert.Equal(20, modelo.Ingredientes.First(i => i.Nombre == "Masa").Cantidad);
    }

    // Criterio 2: ingrediente por agotarse
    [Fact]
    public async Task BajoElMinimo_SeMarcaPorAgotarse()
    {
        var resultado = Assert.IsType<ViewResult>(await NuevoController().Index());
        var modelo = Assert.IsType<InventarioViewModel>(resultado.Model);

        Assert.True(modelo.Ingredientes.First(i => i.Nombre == "Queso").PorAgotarse);
        Assert.Equal(1, modelo.PorAgotarse);
    }

    [Fact]
    public async Task SobreElMinimoOIgual_NoSeMarcaPorAgotarse()
    {
        var inventario = await _repo.ObtenerInventarioAsync();

        Assert.False(inventario.First(i => i.Nombre == "Masa").PorAgotarse);
        Assert.False(inventario.First(i => i.Nombre == "Piña").PorAgotarse); // 3 de 3: solo cuenta estar "por debajo"
    }

    [Fact]
    public async Task Pedir_PrellenaSoloLoQueEstaPorAgotarse()
    {
        var resultado = Assert.IsType<ViewResult>(await NuevoController().NuevoPedido(null, true));
        var modelo = Assert.IsType<NuevoPedidoViewModel>(resultado.Model);

        var conCantidad = modelo.Pedido.Lineas.Where(l => l.Cantidad is not null).ToList();
        Assert.Single(conCantidad);
        Assert.Equal(1, conCantidad[0].IngredienteId);
        Assert.True(conCantidad[0].Cantidad > 0);
    }

    [Fact]
    public async Task PedirUnIngrediente_PrellenaEsaFila()
    {
        var resultado = Assert.IsType<ViewResult>(await NuevoController().NuevoPedido(1, false));
        var modelo = Assert.IsType<NuevoPedidoViewModel>(resultado.Model);

        Assert.Equal(1, modelo.Pedido.Lineas.Count(l => l.Cantidad is not null));
    }

    // Criterio 3: crear pedido al proveedor
    [Fact]
    public async Task PedidoCompleto_QuedaPendiente()
    {
        var controller = NuevoController();

        var resultado = await controller.NuevoPedido(Pedido("Distribuidora La Sabana", (1, 10m), (2, 5m), (3, null)));

        Assert.IsType<RedirectToActionResult>(resultado);
        var pedidos = await _repo.ObtenerPedidosAsync();
        var pedido = Assert.Single(pedidos);
        Assert.Equal(EstadoPedidoProveedor.Pendiente, pedido.Estado);
        Assert.Equal("Distribuidora La Sabana", pedido.Proveedor);
        Assert.Equal(2, pedido.Items.Count); // la fila sin cantidad no se pide
    }

    [Fact]
    public async Task CrearPedido_NoCambiaElInventarioTodavia()
    {
        await NuevoController().NuevoPedido(Pedido("Proveedor X", (1, 10m)));

        Assert.Equal(4, await CantidadDeAsync("Queso"));
    }

    // Criterio 4: datos obligatorios
    [Fact]
    public async Task SinProveedor_NoGuardaYMarcaElCampo()
    {
        var controller = NuevoController();

        var resultado = await controller.NuevoPedido(Pedido("   ", (1, 10m)));

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey("Pedido.Proveedor"));
        Assert.Empty(await _repo.ObtenerPedidosAsync());
    }

    [Fact]
    public async Task SinCantidades_NoGuarda()
    {
        var controller = NuevoController();

        var resultado = await controller.NuevoPedido(Pedido("Proveedor X", (1, null), (2, null)));

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey("Pedido.Lineas"));
        Assert.Empty(await _repo.ObtenerPedidosAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CantidadCeroONegativa_NoGuardaYMarcaLaFila(int cantidad)
    {
        var controller = NuevoController();

        var resultado = await controller.NuevoPedido(Pedido("Proveedor X", (1, cantidad)));

        Assert.IsType<ViewResult>(resultado);
        Assert.True(controller.ModelState.ContainsKey("Pedido.Lineas[0].Cantidad"));
        Assert.Empty(await _repo.ObtenerPedidosAsync());
    }

    [Fact]
    public async Task Ingrediente_Inexistente_EsRechazado()
    {
        var errores = _servicio.Validar(Pedido("Proveedor X", (99, 5m)), await _repo.ObtenerInventarioAsync());

        Assert.Contains(errores, e => e.Campo == "Lineas[0].IngredienteId");
    }

    // Criterio 5: recibir pedido
    [Fact]
    public async Task RecibirPedido_CambiaEstadoYSumaAlInventario()
    {
        var id = await _repo.CrearPedidoAsync("Proveedor X", new[] { new ItemPedido(1, 10m), new ItemPedido(2, 5m) });

        var resultado = await NuevoController().Recibir(id);

        Assert.IsType<RedirectToActionResult>(resultado);
        var pedido = Assert.Single(await _repo.ObtenerPedidosAsync());
        Assert.Equal(EstadoPedidoProveedor.Recibido, pedido.Estado);
        Assert.NotNull(pedido.RecibidoEn);
        Assert.Equal(14, await CantidadDeAsync("Queso")); // 4 + 10
        Assert.Equal(25, await CantidadDeAsync("Masa"));  // 20 + 5
        Assert.False((await _repo.ObtenerInventarioAsync()).First(i => i.Nombre == "Queso").PorAgotarse);
    }

    [Fact]
    public async Task RecibirDosVeces_NoSumaDosVeces()
    {
        var id = await _repo.CrearPedidoAsync("Proveedor X", new[] { new ItemPedido(1, 10m) });

        Assert.True(await _repo.RecibirPedidoAsync(id));
        Assert.False(await _repo.RecibirPedidoAsync(id));

        Assert.Equal(14, await CantidadDeAsync("Queso"));
    }

    [Fact]
    public async Task RecibirPedidoInexistente_EsRechazado()
    {
        Assert.False(await _repo.RecibirPedidoAsync(999));
    }

    private class TempDataFalso : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
