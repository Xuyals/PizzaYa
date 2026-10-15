using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU6 – Perfil del cliente (favoritas, último pedido y cupones).</summary>
public class HU6PerfilTests : IClassFixture<TiendaFactory>
{
    private readonly TiendaFactory _tienda;

    public HU6PerfilTests(TiendaFactory tienda)
    {
        _tienda = tienda;
    }

    // ── Servicios armados a mano (pruebas rápidas sin servidor) ─────────

    private class Escenario
    {
        public InMemoryMenuRepository Menu = InMemoryMenuRepository.ConDatosDeEjemplo();
        public InMemoryPedidoRepository Pedidos = new();
        public InMemoryFavoritaRepository Favoritas = new();
        public InMemoryCuponRepository CuponesRepo = InMemoryCuponRepository.ConDatosDeEjemplo();
        public CarritoService Carrito = new();
        public PrecioService Precio = new();
        public CuponService Cupones;
        public PerfilService Perfil;
        public PedidoService PedidoService;

        public Escenario()
        {
            Cupones = new CuponService(CuponesRepo, Precio);
            Perfil = new PerfilService(Favoritas, Pedidos, Menu, new ArmadoService(Menu, Precio), Carrito, Cupones);
            PedidoService = new PedidoService(Pedidos, Menu);
        }

        public static PizzaPersonalizada Hawaiana() => new()
        {
            PizzaId = 1, TamanoId = 2, BordeId = 2, ExtrasIds = { 8 }, QuitadosIds = { 4 },
        };

        public async Task<Pedido> PedidoDeAsync(int usuarioId, string numero, int cantidad, DateTime creadoEn)
        {
            var carrito = new Carrito();
            carrito.Items.Add((await new ArmadoService(Menu, Precio).ConstruirItemAsync(Hawaiana(), cantidad))!);
            var pedido = await PedidoService.CrearDesdeCarritoAsync(carrito,
                new PedidoService.DatosConfirmacion(numero, "Ana", "3001234567", false, null, 0m, 0m, usuarioId));
            pedido.CreadoEn = creadoEn;
            return pedido;
        }
    }

    // ── Criterio 1: pizzas favoritas ────────────────────────────────────

    [Fact]
    public async Task Favorita_QuedaGuardadaEnElPerfil()
    {
        var e = new Escenario();

        var favorita = await e.Perfil.MarcarFavoritaAsync(1, Escenario.Hawaiana());
        var perfil = await e.Perfil.ObtenerPerfilAsync(1, "Ana");

        Assert.NotNull(favorita);
        var vista = Assert.Single(perfil.Favoritas);
        Assert.Equal("Hawaiana", vista.NombrePizza);
        Assert.Equal("Mediana", vista.NombreTamano);
        Assert.Equal("Relleno de queso", vista.NombreBorde);
        Assert.Equal(new[] { "Pepperoni" }, vista.Extras);
        Assert.Equal(new[] { "Champiñones" }, vista.Quitados);
    }

    [Fact]
    public async Task Favorita_SePuedePedirDeNuevoEnUnClic()
    {
        var e = new Escenario();
        var favorita = await e.Perfil.MarcarFavoritaAsync(1, Escenario.Hawaiana());
        var sesion = new FakeSession();

        var resultado = await e.Perfil.PedirFavoritaAsync(1, favorita!.Id, sesion);

        Assert.True(resultado);
        var item = Assert.Single(e.Carrito.ObtenerCarrito(sesion).Items);
        Assert.Equal("Hawaiana", item.NombrePizza);
        Assert.Equal(new[] { 8 }, item.Seleccion.ExtrasIds);
        Assert.Equal(new[] { 4 }, item.Seleccion.QuitadosIds);
    }

    [Fact]
    public async Task Favorita_MarcarDosVecesLaMismaPizza_NoLaDuplica()
    {
        var e = new Escenario();
        await e.Perfil.MarcarFavoritaAsync(1, Escenario.Hawaiana());
        await e.Perfil.MarcarFavoritaAsync(1, Escenario.Hawaiana());

        Assert.Single((await e.Perfil.ObtenerPerfilAsync(1, "Ana")).Favoritas);
    }

    [Fact]
    public async Task Favorita_EsDeCadaCliente()
    {
        var e = new Escenario();
        var deAna = await e.Perfil.MarcarFavoritaAsync(1, Escenario.Hawaiana());

        Assert.Empty((await e.Perfil.ObtenerPerfilAsync(2, "Luis")).Favoritas);
        Assert.False(await e.Perfil.PedirFavoritaAsync(2, deAna!.Id, new FakeSession())); // no puede pedir la de otro
    }

    [Fact]
    public async Task Favorita_SinTamanoNiBorde_NoSeGuarda()
    {
        var e = new Escenario();
        var incompleta = new PizzaPersonalizada { PizzaId = 1 };

        Assert.Null(await e.Perfil.MarcarFavoritaAsync(1, incompleta));
        Assert.Empty((await e.Perfil.ObtenerPerfilAsync(1, "Ana")).Favoritas);
    }

    // ── Criterio 2: último pedido ───────────────────────────────────────

    [Fact]
    public async Task UltimoPedido_ElPerfilMuestraElMasReciente()
    {
        var e = new Escenario();
        var ahora = DateTime.UtcNow;
        await e.PedidoDeAsync(1, "PZ-VIEJO", 1, ahora.AddDays(-3));
        await e.PedidoDeAsync(1, "PZ-NUEVO", 2, ahora);
        await e.PedidoDeAsync(2, "PZ-DE-OTRO", 1, ahora.AddMinutes(5));

        var perfil = await e.Perfil.ObtenerPerfilAsync(1, "Ana");

        Assert.Equal("PZ-NUEVO", perfil.UltimoPedido!.NumeroPedido);
    }

    [Fact]
    public async Task UltimoPedido_SePuedeRepetir()
    {
        var e = new Escenario();
        await e.PedidoDeAsync(1, "PZ-1", 2, DateTime.UtcNow);
        var sesion = new FakeSession();

        var (agregadas, omitidas) = await e.Perfil.RepetirUltimoPedidoAsync(1, sesion);

        Assert.Equal((1, 0), (agregadas, omitidas));
        var item = Assert.Single(e.Carrito.ObtenerCarrito(sesion).Items);
        Assert.Equal("Hawaiana", item.NombrePizza);
        Assert.Equal(2, item.Cantidad);
        Assert.Equal(new[] { 8 }, item.Seleccion.ExtrasIds);
    }

    // ── Criterio 3: mis cupones ─────────────────────────────────────────

    [Fact]
    public async Task MisCupones_MuestraLosVigentesYNoLosVencidosNiLosDeOtros()
    {
        var e = new Escenario();
        var ahora = DateTime.UtcNow;
        e.CuponesRepo.Agregar(new CuponDescuento("VENCIDO", 5000m, "Ya venció", ahora.AddDays(-1)));
        e.CuponesRepo.Agregar(new CuponDescuento("MIO", 7000m, "Solo para mí", ahora.AddDays(5), UsuarioId: 1));
        e.CuponesRepo.Agregar(new CuponDescuento("AJENO", 9000m, "De otro cliente", ahora.AddDays(5), UsuarioId: 2));

        var codigos = (await e.Perfil.ObtenerPerfilAsync(1, "Ana")).Cupones.Select(c => c.Codigo).ToList();

        Assert.Contains("PIZZA10", codigos);
        Assert.Contains("MIO", codigos);
        Assert.DoesNotContain("VENCIDO", codigos);
        Assert.DoesNotContain("AJENO", codigos);
    }

    [Fact]
    public async Task MisCupones_SePuedenAplicarAlPedido()
    {
        var e = new Escenario();

        var (descuento, aplicado, _) = await e.Cupones.ResolverAsync("PIZZA10", 100_000m, 1);

        Assert.True(aplicado);
        Assert.Equal(5_000m, descuento);
    }

    // ── Criterio 4: cupón inválido o vencido ────────────────────────────

    [Fact]
    public async Task Cupon_Inexistente_DaErrorYNoCambiaElTotal()
    {
        var e = new Escenario();

        var (descuento, aplicado, mensaje) = await e.Cupones.ResolverAsync("NOEXISTE", 100_000m, 1);

        Assert.False(aplicado);
        Assert.Equal(0m, descuento);
        Assert.False(string.IsNullOrWhiteSpace(mensaje));
        Assert.Equal(100_000m, e.Precio.CalcularTotal(100_000m, false, descuento));
    }

    [Fact]
    public async Task Cupon_Vencido_DaErrorYNoCambiaElTotal()
    {
        var e = new Escenario();
        e.CuponesRepo.Agregar(new CuponDescuento("VENCIDO", 5000m, "Ya venció", DateTime.UtcNow.AddDays(-1)));

        var (descuento, aplicado, mensaje) = await e.Cupones.ResolverAsync("VENCIDO", 100_000m, 1);

        Assert.False(aplicado);
        Assert.Equal(0m, descuento);
        Assert.Contains("vencido", mensaje);
    }

    [Fact]
    public async Task Cupon_DeOtroCliente_NoSePuedeUsar()
    {
        var e = new Escenario();
        e.CuponesRepo.Agregar(new CuponDescuento("AJENO", 9000m, "De otro", DateTime.UtcNow.AddDays(5), UsuarioId: 2));

        var (descuento, aplicado, _) = await e.Cupones.ResolverAsync("AJENO", 100_000m, 1);

        Assert.False(aplicado);
        Assert.Equal(0m, descuento);
    }

    // ── Criterio 5: perfil sin datos ────────────────────────────────────

    [Fact]
    public async Task Perfil_SinFavoritasNiPedidos_IndicaQueAunNoTiene()
    {
        var e = new Escenario();

        var perfil = await e.Perfil.ObtenerPerfilAsync(1, "Ana");

        Assert.True(perfil.SinDatos);
        Assert.Empty(perfil.Favoritas);
        Assert.Null(perfil.UltimoPedido);
    }

    // ── Flujo completo en la app (páginas, sesión y cookies) ────────────

    private async Task<(HttpClient Navegador, int UsuarioId)> ClienteAsync()
    {
        var correo = $"perfil{Guid.NewGuid():N}@correo.test";
        var navegador = _tienda.NuevoNavegador();
        await navegador.RegistrarAsync("Cliente Perfil", correo, "Clave12345");
        using var alcance = _tienda.Services.CreateScope();
        var usuario = await alcance.ServiceProvider.GetRequiredService<IUsuarioRepository>().ObtenerPorCorreoAsync(correo);
        return (navegador, usuario!.Id);
    }

    private static async Task ArmarPizzaAsync(HttpClient navegador)
    {
        await navegador.EnviarFormularioAsync("/Menu/Personalizar/1", "/Menu/Personalizar/1", new[]
        {
            new KeyValuePair<string, string>("Seleccion.TamanoId", "2"),
            new KeyValuePair<string, string>("Seleccion.BordeId", "2"),
            new KeyValuePair<string, string>("Seleccion.ExtrasIds", "8"),
        });
    }

    [Fact]
    public async Task Perfil_RequiereIniciarSesion()
    {
        var respuesta = await _tienda.NuevoNavegador().GetAsync("/Perfil");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/Ingresar", respuesta.RutaDeRedireccion());
    }

    [Fact]
    public async Task Perfil_ClienteNuevo_VeElMensajeDeQueAunNoTiene()
    {
        var (navegador, _) = await ClienteAsync();

        var html = await navegador.GetStringAsync("/Perfil");

        Assert.Contains("Aún no tienes pizzas favoritas ni pedidos", html);
    }

    [Fact]
    public async Task Flujo_MarcarFavoritaYPedirlaDeNuevo()
    {
        var (navegador, _) = await ClienteAsync();
        await ArmarPizzaAsync(navegador);

        var marcar = await navegador.EnviarFormularioAsync("/Precio/Resumen", "/Perfil/MarcarFavorita", Array.Empty<KeyValuePair<string, string>>());
        Assert.Equal(HttpStatusCode.Redirect, marcar.StatusCode);

        var perfil = await navegador.GetStringAsync("/Perfil");
        Assert.Contains("Hawaiana", perfil);
        Assert.Contains("Pedir de nuevo", perfil);
        Assert.DoesNotContain("Aún no tienes pizzas favoritas ni pedidos", perfil);

        var id = System.Text.RegularExpressions.Regex.Match(perfil, "data-favorita=\"(\\d+)\"").Groups[1].Value;
        var pedir = await navegador.EnviarFormularioAsync("/Perfil", $"/Perfil/PedirFavorita/{id}", Array.Empty<KeyValuePair<string, string>>());
        Assert.Equal("/Carrito", pedir.RutaDeRedireccion());
        Assert.Contains("Hawaiana", await navegador.GetStringAsync("/Carrito"));
    }

    [Fact]
    public async Task Flujo_ElPedidoConfirmadoApareceComoUltimoPedidoYSePuedeRepetir()
    {
        var (navegador, _) = await ClienteAsync();
        await ArmarPizzaAsync(navegador);
        await navegador.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "false") });
        var confirmar = await navegador.EnviarFormularioAsync("/Carrito/Confirmar", "/Carrito/Confirmar", new[]
        {
            new KeyValuePair<string, string>("Nombre", "Cliente Perfil"),
            new KeyValuePair<string, string>("Telefono", "3001234567"),
            new KeyValuePair<string, string>("EsDomicilio", "false"),
        });
        var numero = System.Text.RegularExpressions.Regex.Match(await confirmar.Content.ReadAsStringAsync(), @"PZ-\d{8}-[0-9A-F]{4}").Value;
        Assert.NotEmpty(numero);

        var perfil = await navegador.GetStringAsync("/Perfil");
        Assert.Contains(numero, perfil);
        Assert.Contains("Repetir este pedido", perfil);

        var repetir = await navegador.EnviarFormularioAsync("/Perfil", "/Perfil/RepetirUltimo", Array.Empty<KeyValuePair<string, string>>());
        Assert.Equal("/Carrito", repetir.RutaDeRedireccion());
        Assert.Contains("Hawaiana", await navegador.GetStringAsync("/Carrito"));
    }

    [Fact]
    public async Task Flujo_CuponDelPerfil_SeDescuentaAlConfirmar()
    {
        var (navegador, _) = await ClienteAsync();
        await ArmarPizzaAsync(navegador);
        await navegador.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "false") });

        var aplicar = await navegador.EnviarFormularioAsync("/Perfil", "/Perfil/AplicarCupon",
            new[] { new KeyValuePair<string, string>("codigo", "PIZZA10") });

        Assert.Equal("/Carrito/Confirmar", aplicar.RutaDeRedireccion());
        var confirmar = await navegador.GetStringAsync("/Carrito/Confirmar");
        Assert.Contains("Descuento (PIZZA10)", confirmar);
    }

    [Fact]
    public async Task Flujo_CuponInvalidoEnElPerfil_MuestraErrorYNoLoDejaListo()
    {
        var (navegador, _) = await ClienteAsync();
        await ArmarPizzaAsync(navegador);
        await navegador.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "false") });

        var aplicar = await navegador.EnviarFormularioAsync("/Perfil", "/Perfil/AplicarCupon",
            new[] { new KeyValuePair<string, string>("codigo", "NOEXISTE") });

        Assert.Equal("/Perfil", aplicar.RutaDeRedireccion());
        Assert.Contains("Cupón no válido o expirado.", await navegador.GetStringAsync("/Perfil"));
        Assert.DoesNotContain("Descuento (", await navegador.GetStringAsync("/Carrito/Confirmar"));
    }

    [Fact]
    public async Task Flujo_CuponVencidoAlConfirmar_NoConfirmaYMuestraElError()
    {
        ((InMemoryCuponRepository)_tienda.Services.GetRequiredService<ICuponRepository>())
            .Agregar(new CuponDescuento("VENCIDO22", 5000m, "Ya venció", DateTime.UtcNow.AddDays(-1)));
        var (navegador, _) = await ClienteAsync();
        await ArmarPizzaAsync(navegador);
        await navegador.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "false") });

        var confirmar = await navegador.EnviarFormularioAsync("/Carrito/Confirmar", "/Carrito/Confirmar", new[]
        {
            new KeyValuePair<string, string>("Nombre", "Cliente Perfil"),
            new KeyValuePair<string, string>("Telefono", "3001234567"),
            new KeyValuePair<string, string>("EsDomicilio", "false"),
            new KeyValuePair<string, string>("CodigoCupon", "VENCIDO22"),
        });

        var html = await confirmar.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, confirmar.StatusCode); // sigue en el formulario: no se confirmó
        Assert.Contains("está vencido", html);
        Assert.DoesNotContain("¡Pedido confirmado!", html);
    }
}
