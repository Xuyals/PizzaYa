using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU9 (sugerida) – Descontar el inventario al confirmar un pedido.</summary>
public class HU9DescontarInventarioTests : IClassFixture<TiendaFactory>
{
    // Ids de InMemoryMenuRepository (orden alfabético): 4 Champiñones, 6 Jamón, 8 Pepperoni, 10 Piña, 12 Queso, 13 Salsa
    private const int Champinones = 4, Jamon = 6, Pepperoni = 8, Pina = 10, Queso = 12, Salsa = 13;

    private readonly TiendaFactory _tienda;

    public HU9DescontarInventarioTests(TiendaFactory tienda)
    {
        _tienda = tienda;
    }

    // ── Escenario con inventario propio ─────────────────────────────────

    private static InMemoryInventarioRepository Inventario(params (int Id, string Nombre, decimal Cantidad, decimal Minimo)[] filas) =>
        new(filas.Select(f => new IngredienteInventario { Id = f.Id, Nombre = f.Nombre, Unidad = "kg", Cantidad = f.Cantidad, CantidadMinima = f.Minimo }));

    /// <summary>Inventario con todo lo de la Hawaiana y el pepperoni, en abundancia salvo lo que se indique.</summary>
    private static InMemoryInventarioRepository InventarioAbundante(Action<Dictionary<int, decimal>>? ajustar = null)
    {
        var cantidades = new Dictionary<int, decimal>
        {
            [Champinones] = 20m, [Jamon] = 20m, [Pepperoni] = 20m, [Pina] = 20m, [Queso] = 20m, [Salsa] = 20m,
        };
        ajustar?.Invoke(cantidades);
        var nombres = new Dictionary<int, string>
        {
            [Champinones] = "Champiñones", [Jamon] = "Jamón", [Pepperoni] = "Pepperoni", [Pina] = "Piña",
            [Queso] = "Queso mozzarella", [Salsa] = "Salsa de tomate",
        };
        return Inventario(cantidades.Select(c => (c.Key, nombres[c.Key], c.Value, 2m)).ToArray());
    }

    private WebApplicationFactory<Program> TiendaCon(InMemoryInventarioRepository inventario) =>
        _tienda.WithWebHostBuilder(b => b.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<IInventarioRepository>();
            servicios.AddSingleton<IInventarioRepository>(inventario);
        }));

    private static HttpClient Navegador(WebApplicationFactory<Program> tienda) =>
        tienda.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<decimal> CantidadAsync(InMemoryInventarioRepository inventario, int id) =>
        (await inventario.ObtenerInventarioAsync()).First(i => i.Id == id).Cantidad;

    private static ItemCarrito Item(int pizzaId, int tamanoId, int cantidad = 1, int[]? extras = null, int[]? quitados = null) => new()
    {
        Seleccion = new PizzaPersonalizada
        {
            PizzaId = pizzaId, TamanoId = tamanoId, BordeId = 1,
            ExtrasIds = (extras ?? Array.Empty<int>()).ToList(), QuitadosIds = (quitados ?? Array.Empty<int>()).ToList(),
        },
        NombrePizza = "Pizza", Cantidad = cantidad, PrecioUnitario = 30_000m,
    };

    private static ConsumoService ServicioConsumo(InMemoryInventarioRepository inventario) =>
        new(InMemoryMenuRepository.ConDatosDeEjemplo(), inventario);

    // Hawaiana (id 1) = Salsa de tomate, Queso mozzarella, Jamón y Piña. Una pizza personal gasta 0,1 de cada uno.

    // ── Criterio 1: descontar ingredientes ──────────────────────────────

    [Fact]
    public async Task ConfirmarPedido_DescuentaLosIngredientesDeCadaPizza()
    {
        var inventario = InventarioAbundante();
        var carrito = new Carrito();
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 1, cantidad: 2)); // 2 Hawaianas personales

        var reserva = await ServicioConsumo(inventario).DescontarAsync(carrito);

        Assert.True(reserva.Exitoso);
        foreach (var id in new[] { Salsa, Queso, Jamon, Pina })
            Assert.Equal(20m - 0.2m, await CantidadAsync(inventario, id)); // 0,1 × 2 pizzas
        Assert.Equal(20m, await CantidadAsync(inventario, Pepperoni));      // lo que no lleva no se toca
    }

    [Fact]
    public async Task Descuento_EscalaConElTamanoYCuentaExtrasYQuitados()
    {
        var inventario = InventarioAbundante();
        var carrito = new Carrito();
        // Familiar (×2,2): quita la piña y agrega pepperoni
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 3, extras: new[] { Pepperoni }, quitados: new[] { Pina }));

        await ServicioConsumo(inventario).DescontarAsync(carrito);

        Assert.Equal(20m - 0.22m, await CantidadAsync(inventario, Queso));
        Assert.Equal(20m - 0.22m, await CantidadAsync(inventario, Pepperoni)); // el extra también se gasta
        Assert.Equal(20m, await CantidadAsync(inventario, Pina));              // quitada: no se gasta
    }

    [Fact]
    public async Task ConfirmarPedidoEnLaApp_BajaElInventario()
    {
        var inventario = InventarioAbundante();
        var tienda = TiendaCon(inventario);
        var cliente = Navegador(tienda);
        await ArmarYAgregarAsync(cliente);

        var numero = await ConfirmarAsync(cliente);

        Assert.NotEmpty(numero);
        Assert.Equal(20m - 0.16m, await CantidadAsync(inventario, Queso)); // Hawaiana mediana: 0,1 × 1,6
    }

    // ── Criterio 2: ingrediente agotado, no se escoge como extra ────────

    [Fact]
    public async Task ExtraAgotado_NoSePuedeEscoger_EnLaPaginaDePersonalizar()
    {
        var inventario = InventarioAbundante(c => c[Pepperoni] = 0m);
        var cliente = Navegador(TiendaCon(inventario));

        var html = await cliente.GetStringAsync("/Menu/Personalizar/1");

        Assert.Matches(new Regex(@"id=""extra-8""[^>]*disabled"), html);
        Assert.Contains("Agotado", html);
        Assert.DoesNotMatch(new Regex(@"id=""extra-4""[^>]*disabled"), html); // los demás siguen disponibles
    }

    [Fact]
    public async Task ExtraAgotado_ElServidorRechazaLaPeticionAunqueSeFuerce()
    {
        var inventario = InventarioAbundante(c => c[Pepperoni] = 0m);
        var cliente = Navegador(TiendaCon(inventario));

        var respuesta = await cliente.EnviarFormularioAsync("/Menu/Personalizar/1", "/Menu/Personalizar/1", new[]
        {
            new KeyValuePair<string, string>("Seleccion.TamanoId", "1"),
            new KeyValuePair<string, string>("Seleccion.BordeId", "1"),
            new KeyValuePair<string, string>("Seleccion.ExtrasIds", Pepperoni.ToString()),
        });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // no avanza al resumen
        Assert.Contains("Pepperoni está agotado y no se puede agregar como extra.", await respuesta.Content.ReadAsStringAsync());
    }

    // ── Criterio 3: pizza no disponible ─────────────────────────────────

    [Fact]
    public async Task IngredienteBaseAgotado_LaPizzaSeMuestraComoNoDisponible()
    {
        var inventario = InventarioAbundante(c => c[Jamon] = 0m); // la Hawaiana lleva jamón
        var cliente = Navegador(TiendaCon(inventario));

        var menu = await cliente.GetStringAsync("/Menu");

        Assert.Contains("data-sin-stock=\"1\"", menu);       // Hawaiana
        Assert.DoesNotContain("data-sin-stock=\"3\"", menu); // la Pepperoni sigue disponible
        Assert.DoesNotContain("/Menu/Personalizar/1\"", menu);
        Assert.Contains("/Menu/Personalizar/3\"", menu);
    }

    [Fact]
    public async Task PizzaSinStock_NoSePuedePersonalizarAunqueSeEntreAlEnlace()
    {
        var inventario = InventarioAbundante(c => c[Jamon] = 0m);
        var cliente = Navegador(TiendaCon(inventario));

        var respuesta = await cliente.GetAsync("/Menu/Personalizar/1");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("no está disponible por ahora", await cliente.GetStringAsync("/Menu"));
    }

    // ── Criterio 4: aviso de mínimo ─────────────────────────────────────

    [Fact]
    public async Task AlBajarDelMinimo_QuedaMarcadoPorAgotarse()
    {
        var inventario = Inventario(
            (Queso, "Queso mozzarella", 2.05m, 2m), (Salsa, "Salsa de tomate", 20m, 2m),
            (Jamon, "Jamón", 20m, 2m), (Pina, "Piña", 20m, 2m));
        Assert.False((await inventario.ObtenerInventarioAsync()).First(i => i.Id == Queso).PorAgotarse);
        var carrito = new Carrito();
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 1));

        await ServicioConsumo(inventario).DescontarAsync(carrito); // el queso queda en 1,95: bajo el mínimo de 2

        var queso = (await inventario.ObtenerInventarioAsync()).First(i => i.Id == Queso);
        Assert.Equal(1.95m, queso.Cantidad);
        Assert.True(queso.PorAgotarse);
        Assert.False((await inventario.ObtenerInventarioAsync()).First(i => i.Id == Jamon).PorAgotarse);
    }

    [Fact]
    public async Task AlBajarDelMinimo_ElInventarioLoMuestraComoPorAgotarse()
    {
        var inventario = InventarioAbundante(c => c[Queso] = 2.05m);
        var tienda = TiendaCon(inventario);
        var cliente = Navegador(tienda);
        await ArmarYAgregarAsync(cliente);
        await ConfirmarAsync(cliente); // gasta 0,16 de queso → 1,89, bajo el mínimo de 2

        var admin = Navegador(tienda);
        await admin.IngresarAsync(TiendaFactory.CorreoAdmin, TiendaFactory.PasswordAdmin);
        var html = await admin.GetStringAsync("/Inventario");

        Assert.Contains("Por agotarse", html);
    }

    // ── Criterio 5: sin cantidades negativas ────────────────────────────

    [Fact]
    public async Task PedidoQuePideMasDeLoDisponible_NoSeConfirmaYElInventarioNoCambia()
    {
        var inventario = InventarioAbundante(c => c[Queso] = 0.05m); // no alcanza ni para una pizza
        var tienda = TiendaCon(inventario);
        var cliente = Navegador(tienda);
        await ArmarYAgregarAsync(cliente);

        var respuesta = await cliente.EnviarFormularioAsync("/Carrito/Confirmar", "/Carrito/Confirmar", DatosDeConfirmacion());
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // sigue en el formulario
        Assert.Contains("No tenemos ingredientes suficientes", html);
        Assert.Contains("Queso mozzarella", html);
        Assert.DoesNotContain("¡Pedido confirmado!", html);
        Assert.Equal(0.05m, await CantidadAsync(inventario, Queso)); // nunca queda negativo
        using var alcance = tienda.Services.CreateScope();
        Assert.Empty(await alcance.ServiceProvider.GetRequiredService<IPedidoRepository>().ObtenerTodosAsync());
    }

    [Fact]
    public async Task SiUnIngredienteNoAlcanza_NoSeDescuentaNingunoDelPedido()
    {
        var inventario = InventarioAbundante(c => c[Pina] = 0.05m);
        var carrito = new Carrito();
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 1));

        var reserva = await ServicioConsumo(inventario).DescontarAsync(carrito);

        Assert.False(reserva.Exitoso);
        foreach (var id in new[] { Salsa, Queso, Jamon })
            Assert.Equal(20m, await CantidadAsync(inventario, id)); // ni el queso ni el jamón se descontaron
        Assert.Equal(0.05m, await CantidadAsync(inventario, Pina));
    }

    [Fact]
    public async Task DosPedidosSeguidos_ElSegundoNoPuedeGastarLoQueYaSeGasto()
    {
        var inventario = InventarioAbundante(c => c[Queso] = 0.15m); // alcanza para una personal (0,1), no para dos
        var servicio = ServicioConsumo(inventario);
        var carrito = new Carrito();
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 1));

        var primero = await servicio.DescontarAsync(carrito);
        var segundo = await servicio.DescontarAsync(carrito);

        Assert.True(primero.Exitoso);
        Assert.False(segundo.Exitoso);
        Assert.Equal(0.05m, await CantidadAsync(inventario, Queso));
    }

    [Fact]
    public async Task PizzaOcultaEnElCarrito_NoSeConfirma()
    {
        var inventario = InventarioAbundante();
        var menu = InMemoryMenuRepository.ConDatosDeEjemplo();
        await menu.CambiarDisponibilidadPizzaAsync(1, false);
        var carrito = new Carrito();
        carrito.Items.Add(Item(pizzaId: 1, tamanoId: 1));

        var reserva = await new ConsumoService(menu, inventario).DescontarAsync(carrito);

        Assert.False(reserva.Exitoso);
        Assert.Equal(20m, await CantidadAsync(inventario, Queso));
    }

    // ── Ayudas para los flujos en la app ────────────────────────────────

    private static async Task ArmarYAgregarAsync(HttpClient cliente)
    {
        var personalizar = await cliente.EnviarFormularioAsync("/Menu/Personalizar/1", "/Menu/Personalizar/1", new[]
        {
            new KeyValuePair<string, string>("Seleccion.TamanoId", "2"),
            new KeyValuePair<string, string>("Seleccion.BordeId", "1"),
        });
        Assert.Equal(HttpStatusCode.Redirect, personalizar.StatusCode);
        await cliente.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "false") });
    }

    private static List<KeyValuePair<string, string>> DatosDeConfirmacion() => new()
    {
        new("Nombre", "Cliente"),
        new("Telefono", "3001234567"),
        new("EsDomicilio", "false"),
    };

    private static async Task<string> ConfirmarAsync(HttpClient cliente)
    {
        var respuesta = await cliente.EnviarFormularioAsync("/Carrito/Confirmar", "/Carrito/Confirmar", DatosDeConfirmacion());
        return Regex.Match(await respuesta.Content.ReadAsStringAsync(), @"PZ-\d{8}-[0-9A-F]{4}").Value;
    }
}
