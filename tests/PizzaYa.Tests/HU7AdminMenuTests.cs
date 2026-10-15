using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU7 (sugerida) – Administrar el menú, los precios y el domicilio.</summary>
public class HU7AdminMenuTests : IClassFixture<TiendaFactory>
{
    private readonly TiendaFactory _tienda;

    public HU7AdminMenuTests(TiendaFactory tienda)
    {
        _tienda = tienda;
    }

    private static string NombreUnico(string baseNombre) => $"{baseNombre} {Guid.NewGuid().ToString("N")[..6]}";

    private async Task<HttpClient> AdministradorAsync()
    {
        var navegador = _tienda.NuevoNavegador();
        await navegador.IngresarAsync(TiendaFactory.CorreoAdmin, TiendaFactory.PasswordAdmin);
        return navegador;
    }

    private static Task<HttpResponseMessage> GuardarPizzaAsync(
        HttpClient admin, string? nombre, string? precio, params int[] ingredientes)
    {
        var campos = new List<KeyValuePair<string, string>>
        {
            new("Nombre", nombre ?? ""),
            new("PrecioBase", precio ?? ""),
            new("Descripcion", "Creada en una prueba"),
        };
        campos.AddRange(ingredientes.Select(i => new KeyValuePair<string, string>("IngredientesBaseIds", i.ToString())));
        return admin.EnviarFormularioAsync("/Admin/Pizza", "/Admin/Pizza", campos);
    }

    // ── Criterio 1: registrar pizza ─────────────────────────────────────

    [Fact]
    public async Task RegistrarPizza_ConDatosCompletos_AparecenEnElMenuDelCliente()
    {
        var admin = await AdministradorAsync();
        var nombre = NombreUnico("Pizza nueva");

        var respuesta = await GuardarPizzaAsync(admin, nombre, "33000", 12, 1);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        var menuDelCliente = await _tienda.NuevoNavegador().GetStringAsync("/Menu");
        Assert.Contains(nombre, menuDelCliente);
        Assert.Contains("33.000", menuDelCliente);
    }

    [Fact]
    public async Task RegistrarPizza_NoDejaNombreRepetido()
    {
        var admin = await AdministradorAsync();
        var nombre = NombreUnico("Repetida");
        await GuardarPizzaAsync(admin, nombre, "30000", 1);

        var segunda = await GuardarPizzaAsync(admin, nombre.ToUpperInvariant(), "31000", 1);

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Contains("Ya existe una pizza con ese nombre.", await segunda.Content.ReadAsStringAsync());
    }

    // ── Criterio 2: editar precios ──────────────────────────────────────

    [Fact]
    public async Task EditarPrecioDeUnaPizza_ElMenuMuestraElNuevoPrecio()
    {
        var admin = await AdministradorAsync();
        var nombre = NombreUnico("Pizza cara");
        await GuardarPizzaAsync(admin, nombre, "30000", 1);
        var id = await IdDePizzaAsync(nombre);

        var respuesta = await admin.EnviarFormularioAsync($"/Admin/Pizza/{id}", "/Admin/Pizza", new[]
        {
            new KeyValuePair<string, string>("Id", id.ToString()),
            new KeyValuePair<string, string>("Nombre", nombre),
            new KeyValuePair<string, string>("PrecioBase", "41500"),
            new KeyValuePair<string, string>("IngredientesBaseIds", "1"),
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Contains("41.500", await _tienda.NuevoNavegador().GetStringAsync("/Menu"));
    }

    [Fact]
    public async Task EditarPrecios_SeUsanEnLosPedidosNuevosYNoCambianLosYaHechos()
    {
        var menu = InMemoryMenuRepository.ConDatosDeEjemplo();
        var servicio = new MenuAdminService(menu, menu);
        var armado = new ArmadoService(menu, new PrecioService());
        var seleccion = new PizzaPersonalizada { PizzaId = 1, TamanoId = 1, BordeId = 2, ExtrasIds = { 8 } };

        var antes = await armado.ConstruirItemAsync(seleccion); // 28.000 + borde 5.000 + extra 4.000
        Assert.Equal(37_000m, antes!.PrecioUnitario);

        Assert.True((await servicio.GuardarPizzaAsync(1, "Hawaiana", null, 30_000m, new[] { 1 })).Exitoso);
        Assert.True((await servicio.GuardarBordeAsync(2, "Relleno de queso", 6_000m)).Exitoso);
        Assert.True((await servicio.GuardarIngredienteAsync(8, "Pepperoni", 5_000m)).Exitoso);

        var despues = await armado.ConstruirItemAsync(seleccion);
        Assert.Equal(41_000m, despues!.PrecioUnitario); // 30.000 + 6.000 + 5.000
        Assert.Equal(37_000m, antes.PrecioUnitario);    // lo que ya estaba en un carrito/pedido no se toca
    }

    [Fact]
    public async Task EditarBordeExtraYTamano_DesdeLaPaginaDeAdministracion()
    {
        var admin = await AdministradorAsync();

        var borde = await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarBorde", new[]
        {
            new KeyValuePair<string, string>("id", "3"),
            new KeyValuePair<string, string>("nombre", "Ajo y finas hierbas"),
            new KeyValuePair<string, string>("valor", "3500"),
        });
        var extra = await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarExtra", new[]
        {
            new KeyValuePair<string, string>("nombre", NombreUnico("Extra")),
            new KeyValuePair<string, string>("valor", "2200"),
        });
        var tamano = await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarTamano", new[]
        {
            new KeyValuePair<string, string>("id", "3"),
            new KeyValuePair<string, string>("nombre", "Familiar"),
            new KeyValuePair<string, string>("valor", "2.3"),
        });

        Assert.All(new[] { borde, extra, tamano }, r => Assert.Equal("/Admin", r.RutaDeRedireccion()));
        var menu = _tienda.Services.GetRequiredService<IMenuRepository>();
        Assert.Equal(3_500m, (await menu.ObtenerBordesAsync()).First(b => b.Id == 3).Precio);
        Assert.Equal(2.3m, (await menu.ObtenerTamanosAsync()).First(t => t.Id == 3).FactorPrecio);
    }

    // ── Criterio 3: valor del domicilio ─────────────────────────────────

    [Fact]
    public async Task ValorDelDomicilio_SeSumaEnLosPedidosADomicilio()
    {
        var admin = await AdministradorAsync();
        var guardar = await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarDomicilio",
            new[] { new KeyValuePair<string, string>("valor", "7000") });
        Assert.Equal("/Admin", guardar.RutaDeRedireccion());

        // Un cliente arma una pizza y confirma a domicilio
        var cliente = _tienda.NuevoNavegador();
        await cliente.EnviarFormularioAsync("/Menu/Personalizar/1", "/Menu/Personalizar/1", new[]
        {
            new KeyValuePair<string, string>("Seleccion.TamanoId", "1"),
            new KeyValuePair<string, string>("Seleccion.BordeId", "1"),
        });
        var resumen = await cliente.GetStringAsync("/Precio/Resumen?esDomicilio=true");
        Assert.Contains("7.000", resumen); // el resumen ya muestra el valor nuevo
        await cliente.EnviarFormularioAsync("/Precio/Resumen", "/Precio/AgregarAlCarrito",
            new[] { new KeyValuePair<string, string>("esDomicilio", "true") });
        var numero = await ConfirmarAsync(cliente, domicilio: true);

        using var alcance = _tienda.Services.CreateScope();
        var pedido = await alcance.ServiceProvider.GetRequiredService<IPedidoRepository>().ObtenerPorNumeroAsync(numero);
        Assert.Equal(7_000m, pedido!.CostoDomicilio);
        Assert.Equal(28_000m + 7_000m, pedido.Total);
    }

    [Fact]
    public async Task ValorDelDomicilio_RecogerEnTiendaNoCobra()
    {
        var admin = await AdministradorAsync();
        await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarDomicilio",
            new[] { new KeyValuePair<string, string>("valor", "7000") });
        var configuracion = new ConfiguracionService(new InMemoryConfiguracionRepository());
        await configuracion.GuardarCostoDomicilioAsync(7_000m);
        var precio = new PrecioService();

        var total = precio.CalcularTotal(28_000m, esDomicilio: false, valorDomicilio: await configuracion.ObtenerCostoDomicilioAsync());

        Assert.Equal(28_000m, total);
    }

    [Fact]
    public async Task ValorDelDomicilio_SinConfigurarUsaElValorPorDefecto()
    {
        var configuracion = new ConfiguracionService(new InMemoryConfiguracionRepository());
        Assert.Equal(PrecioService.CostoDomicilioFijo, await configuracion.ObtenerCostoDomicilioAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("-500")]
    [InlineData("abc")]
    [InlineData("999999999")]
    public async Task ValorDelDomicilio_Invalido_NoSeGuarda(string valor)
    {
        var admin = await AdministradorAsync();
        await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarDomicilio",
            new[] { new KeyValuePair<string, string>("valor", "6000") });

        var respuesta = await admin.EnviarFormularioAsync("/Admin", "/Admin/GuardarDomicilio",
            new[] { new KeyValuePair<string, string>("valor", valor) });

        Assert.Equal("/Admin", respuesta.RutaDeRedireccion());
        using var alcance = _tienda.Services.CreateScope();
        Assert.Equal(6_000m, await alcance.ServiceProvider.GetRequiredService<ConfiguracionService>().ObtenerCostoDomicilioAsync());
    }

    // ── Criterio 4: datos obligatorios ──────────────────────────────────

    [Fact]
    public async Task RegistrarPizza_SinNombre_ResaltaElCampoYNoGuarda()
    {
        var admin = await AdministradorAsync();
        var antes = await CantidadDePizzasAsync();

        var respuesta = await GuardarPizzaAsync(admin, "", "30000", 1);

        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // se queda en el formulario
        Assert.Contains("El nombre de la pizza es obligatorio.", html);
        Assert.Matches(new Regex(@"name=""Nombre""[^>]*is-invalid|is-invalid[^>]*name=""Nombre"""), html);
        Assert.Equal(antes, await CantidadDePizzasAsync());
    }

    [Fact]
    public async Task RegistrarPizza_SinPrecio_ResaltaElCampoYNoGuarda()
    {
        var admin = await AdministradorAsync();
        var antes = await CantidadDePizzasAsync();

        var respuesta = await GuardarPizzaAsync(admin, NombreUnico("Sin precio"), "", 1);

        var html = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El precio de la pizza es obligatorio.", html);
        Assert.Matches(new Regex(@"name=""PrecioBase""[^>]*is-invalid|is-invalid[^>]*name=""PrecioBase"""), html);
        Assert.Equal(antes, await CantidadDePizzasAsync());
    }

    [Fact]
    public async Task RegistrarPizza_SinNombreNiPrecio_ResaltaLosDosCampos()
    {
        var admin = await AdministradorAsync();

        var html = await (await GuardarPizzaAsync(admin, "  ", null, 1)).Content.ReadAsStringAsync();

        Assert.Contains("El nombre de la pizza es obligatorio.", html);
        Assert.Contains("El precio de la pizza es obligatorio.", html);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("2000000")]
    public async Task RegistrarPizza_ConPrecioInvalido_NoGuarda(string precio)
    {
        var admin = await AdministradorAsync();
        var antes = await CantidadDePizzasAsync();

        var respuesta = await GuardarPizzaAsync(admin, NombreUnico("Precio malo"), precio, 1);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(antes, await CantidadDePizzasAsync());
    }

    [Fact]
    public async Task RegistrarPizza_SinIngredientesBase_NoGuarda()
    {
        var admin = await AdministradorAsync();
        var respuesta = await GuardarPizzaAsync(admin, NombreUnico("Sin ingredientes"), "30000");
        Assert.Contains("Escoge al menos un ingrediente base.", await respuesta.Content.ReadAsStringAsync());
    }

    // ── Criterio 5: ocultar pizza ───────────────────────────────────────

    [Fact]
    public async Task OcultarPizza_DejaDeMostrarseEnElMenuDelCliente()
    {
        var admin = await AdministradorAsync();
        var nombre = NombreUnico("Pizza oculta");
        await GuardarPizzaAsync(admin, nombre, "30000", 1);
        var id = await IdDePizzaAsync(nombre);
        Assert.Contains(nombre, await _tienda.NuevoNavegador().GetStringAsync("/Menu"));

        var ocultar = await admin.EnviarFormularioAsync("/Admin", "/Admin/CambiarDisponibilidad", new[]
        {
            new KeyValuePair<string, string>("id", id.ToString()),
            new KeyValuePair<string, string>("disponible", "false"),
        });

        Assert.Equal("/Admin", ocultar.RutaDeRedireccion());
        var cliente = _tienda.NuevoNavegador();
        Assert.DoesNotContain(nombre, await cliente.GetStringAsync("/Menu"));
        Assert.Equal(HttpStatusCode.Redirect, (await cliente.GetAsync($"/Menu/Personalizar/{id}")).StatusCode);
        Assert.Contains(nombre, await admin.GetStringAsync("/Admin")); // el administrador la sigue viendo
    }

    [Fact]
    public async Task OcultarPizza_SePuedeVolverAMostrar()
    {
        var admin = await AdministradorAsync();
        var nombre = NombreUnico("Pizza vuelve");
        await GuardarPizzaAsync(admin, nombre, "30000", 1);
        var id = await IdDePizzaAsync(nombre);
        foreach (var disponible in new[] { "false", "true" })
            await admin.EnviarFormularioAsync("/Admin", "/Admin/CambiarDisponibilidad", new[]
            {
                new KeyValuePair<string, string>("id", id.ToString()),
                new KeyValuePair<string, string>("disponible", disponible),
            });

        Assert.Contains(nombre, await _tienda.NuevoNavegador().GetStringAsync("/Menu"));
    }

    // ── Solo el administrador ───────────────────────────────────────────

    [Theory]
    [InlineData("/Admin")]
    [InlineData("/Admin/Pizza")]
    public async Task ElClienteNoPuedeAdministrarElMenu(string ruta)
    {
        var cliente = _tienda.NuevoNavegador();
        await cliente.RegistrarAsync("Cliente", $"c{Guid.NewGuid():N}@correo.test", "Clave12345");

        var respuesta = await cliente.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.RutaDeRedireccion());
    }

    [Fact]
    public async Task SinSesion_AdministrarElMenuPideIniciarSesion()
    {
        var respuesta = await _tienda.NuevoNavegador().GetAsync("/Admin");
        Assert.Equal("/Cuenta/Ingresar", respuesta.RutaDeRedireccion());
    }

    // ── Ayudas ──────────────────────────────────────────────────────────

    private async Task<int> CantidadDePizzasAsync()
    {
        using var alcance = _tienda.Services.CreateScope();
        return (await alcance.ServiceProvider.GetRequiredService<IMenuAdminRepository>().ObtenerTodasLasPizzasAsync()).Count;
    }

    private async Task<int> IdDePizzaAsync(string nombre)
    {
        using var alcance = _tienda.Services.CreateScope();
        var pizzas = await alcance.ServiceProvider.GetRequiredService<IMenuAdminRepository>().ObtenerTodasLasPizzasAsync();
        return pizzas.First(p => p.Nombre == nombre).Id;
    }

    private static async Task<string> ConfirmarAsync(HttpClient cliente, bool domicilio)
    {
        var campos = new List<KeyValuePair<string, string>>
        {
            new("Nombre", "Cliente"),
            new("Telefono", "3001234567"),
            new("EsDomicilio", domicilio ? "true" : "false"),
        };
        if (domicilio) campos.Add(new("Direccion", "Calle 1 # 2-3"));
        var respuesta = await cliente.EnviarFormularioAsync("/Carrito/Confirmar", "/Carrito/Confirmar", campos);
        var numero = Regex.Match(await respuesta.Content.ReadAsStringAsync(), @"PZ-\d{8}-[0-9A-F]{4}").Value;
        Assert.NotEmpty(numero);
        return numero;
    }
}
