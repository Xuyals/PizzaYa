using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using PizzaYa.Controllers;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU4 – Panel de cocina y seguimiento de pedidos.</summary>
public class HU4PedidosTests
{
    private readonly InMemoryPedidoRepository _repo = new();
    private readonly PedidoService _servicio;

    public HU4PedidosTests()
    {
        _servicio = new PedidoService(_repo, InMemoryMenuRepository.ConDatosDeEjemplo());
    }

    private PedidosController NuevoController() => new(_repo, _servicio)
    {
        TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataFalso()),
    };

    private async Task<Pedido> CrearPedidoAsync(string numero, DateTime creadoEn, string nombre = "Ana")
    {
        var pedido = new Pedido
        {
            NumeroPedido = numero,
            NombreCliente = nombre,
            Telefono = "3001234567",
            Subtotal = 28_000m,
            Total = 28_000m,
            CreadoEn = creadoEn,
            Items =
            {
                new PedidoItem
                {
                    PizzaId = 1, TamanoId = 2, BordeId = 2, Cantidad = 1, PrecioUnitario = 28_000m,
                    NombrePizza = "Hawaiana", NombreTamano = "Mediana", NombreBorde = "Relleno de queso",
                    IngredientesBase = { "Salsa de tomate", "Jamón", "Piña" },
                    ExtrasNombres = { "Pepperoni" },
                    QuitadosNombres = { "Piña" },
                },
            },
        };
        return await _repo.CrearPedidoAsync(pedido);
    }

    // Criterio 1: pedidos nuevos
    [Fact]
    public async Task Panel_MuestraLosPedidosDelMasAntiguoAlMasReciente()
    {
        var ahora = DateTime.UtcNow;
        await CrearPedidoAsync("PZ-3", ahora);
        await CrearPedidoAsync("PZ-1", ahora.AddMinutes(-30));
        await CrearPedidoAsync("PZ-2", ahora.AddMinutes(-10));

        var vista = Assert.IsType<ViewResult>(await NuevoController().Index());
        var pedidos = Assert.IsType<List<Pedido>>(vista.Model);

        Assert.Equal(new[] { "PZ-1", "PZ-2", "PZ-3" }, pedidos.Select(p => p.NumeroPedido));
    }

    [Fact]
    public async Task Panel_MuestraPizzasTamanosEIngredientes()
    {
        await CrearPedidoAsync("PZ-1", DateTime.UtcNow);

        var vista = Assert.IsType<ViewResult>(await NuevoController().Index());
        var item = Assert.Single(Assert.Single(Assert.IsType<List<Pedido>>(vista.Model)).Items);

        Assert.Equal("Hawaiana", item.NombrePizza);
        Assert.Equal("Mediana", item.NombreTamano);
        Assert.Contains("Jamón", item.IngredientesBase);
        Assert.Contains("Pepperoni", item.ExtrasNombres);
        Assert.Contains("Piña", item.QuitadosNombres);
    }

    [Fact]
    public async Task Panel_SinPedidos_MuestraLista()
    {
        var vista = Assert.IsType<ViewResult>(await NuevoController().Index());
        Assert.Empty(Assert.IsType<List<Pedido>>(vista.Model));
    }

    // Criterio 2: avanzar estado
    [Fact]
    public async Task PedidoNuevo_EmpiezaEnRecibido()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);
        Assert.Equal("Recibido", pedido.Estado);
    }

    [Fact]
    public async Task Cocinero_AvanzaRecibidoPreparacionEntregaYEntregado()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);

        foreach (var esperado in new[] { "En preparación", "En entrega", "Entregado" })
        {
            var resultado = await _servicio.CambiarEstadoAsync(pedido.Id, esperado);
            Assert.True(resultado.Exitoso, resultado.Mensaje);
            Assert.Equal(esperado, (await _repo.ObtenerPorIdAsync(pedido.Id))!.Estado);
        }
    }

    [Fact]
    public async Task Panel_CambiarEstadoDesdeElControlador_AvanzaElPedido()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);
        var controller = NuevoController();

        await controller.Cambiar(pedido.Id, "En preparación");

        Assert.Equal("En preparación", (await _repo.ObtenerPorIdAsync(pedido.Id))!.Estado);
        Assert.NotNull(controller.TempData["Exito"]);
    }

    // Criterio 3: sin saltar ni retroceder
    [Theory]
    [InlineData("Recibido", "En entrega")]
    [InlineData("Recibido", "Entregado")]
    [InlineData("En preparación", "Entregado")]
    [InlineData("En preparación", "Recibido")]
    [InlineData("En entrega", "En preparación")]
    [InlineData("En entrega", "Recibido")]
    [InlineData("Entregado", "En entrega")]
    [InlineData("Entregado", "Recibido")]
    public void NoSePermiteSaltarNiRetroceder(string actual, string nuevo)
    {
        Assert.False(PedidoService.PuedeCambiar(actual, nuevo));
    }

    [Fact]
    public async Task IntentarSaltarUnEstado_NoCambiaElPedido()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);

        var resultado = await _servicio.CambiarEstadoAsync(pedido.Id, "Entregado");

        Assert.False(resultado.Exitoso);
        Assert.Equal("Recibido", (await _repo.ObtenerPorIdAsync(pedido.Id))!.Estado);
    }

    [Fact]
    public async Task IntentarDevolverUnEstado_NoCambiaElPedido()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);
        await _servicio.CambiarEstadoAsync(pedido.Id, "En preparación");

        var resultado = await _servicio.CambiarEstadoAsync(pedido.Id, "Recibido");

        Assert.False(resultado.Exitoso);
        Assert.Equal("En preparación", (await _repo.ObtenerPorIdAsync(pedido.Id))!.Estado);
    }

    [Fact]
    public async Task PedidoEntregado_NoPuedeCambiar()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);
        foreach (var estado in new[] { "En preparación", "En entrega", "Entregado" })
            await _servicio.CambiarEstadoAsync(pedido.Id, estado);

        Assert.Null(PedidoService.SiguienteEstado("Entregado"));
        Assert.False((await _servicio.CambiarEstadoAsync(pedido.Id, "Recibido")).Exitoso);
    }

    [Fact]
    public async Task Panel_IntentarSaltar_MuestraMensajeDeError()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);
        var controller = NuevoController();

        await controller.Cambiar(pedido.Id, "Entregado");

        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal("Recibido", (await _repo.ObtenerPorIdAsync(pedido.Id))!.Estado);
    }

    [Fact]
    public async Task CambiarEstado_CuandoOtroYaLoCambio_NoPisaElCambio()
    {
        var pedido = await CrearPedidoAsync("PZ-1", DateTime.UtcNow);

        // Alguien más ya lo pasó a "En preparación"; este intento parte de "Recibido"
        Assert.True(await _repo.ActualizarEstadoAsync(pedido.Id, "Recibido", "En preparación"));
        Assert.False(await _repo.ActualizarEstadoAsync(pedido.Id, "Recibido", "En preparación"));
    }

    // Criterio 4: el cliente consulta su pedido
    [Fact]
    public async Task Cliente_ConNumeroValido_VeElEstadoActual()
    {
        var pedido = await CrearPedidoAsync("PZ-20261014-AAAA", DateTime.UtcNow);
        await _servicio.CambiarEstadoAsync(pedido.Id, "En preparación");

        var vista = Assert.IsType<ViewResult>(await NuevoController().Seguimiento("PZ-20261014-AAAA"));
        var modelo = Assert.IsType<SeguimientoViewModel>(vista.Model);

        Assert.NotNull(modelo.Pedido);
        Assert.Equal("En preparación", modelo.Pedido!.Estado);
    }

    [Fact]
    public async Task Cliente_PuedeEscribirElNumeroEnMinusculasYConEspacios()
    {
        await CrearPedidoAsync("PZ-20261014-AAAA", DateTime.UtcNow);

        var vista = Assert.IsType<ViewResult>(await NuevoController().Seguimiento("  pz-20261014-aaaa "));

        Assert.NotNull(Assert.IsType<SeguimientoViewModel>(vista.Model).Pedido);
    }

    // Criterio 5: número inválido
    [Fact]
    public async Task Cliente_ConNumeroInexistente_VeMensajeDePedidoNoEncontrado()
    {
        var resultado = await NuevoController().Seguimiento("PZ-NO-EXISTE");

        var vista = Assert.IsType<ViewResult>(resultado); // una vista normal, no NotFound ni error
        var modelo = Assert.IsType<SeguimientoViewModel>(vista.Model);
        Assert.True(modelo.Buscado);
        Assert.Null(modelo.Pedido);
    }

    [Fact]
    public async Task Cliente_SinEscribirNumero_VeElFormularioSinMensajeDeError()
    {
        var vista = Assert.IsType<ViewResult>(await NuevoController().Seguimiento(null));
        var modelo = Assert.IsType<SeguimientoViewModel>(vista.Model);

        Assert.False(modelo.Buscado);
        Assert.Null(modelo.Pedido);
    }

    private class TempDataFalso : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
