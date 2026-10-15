using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU8 (sugerida) – Registro e inicio de sesión.</summary>
public class HU8LoginTests : IClassFixture<TiendaFactory>
{
    private readonly TiendaFactory _tienda;

    public HU8LoginTests(TiendaFactory tienda)
    {
        _tienda = tienda;
    }

    private static string CorreoNuevo() => $"cliente{Guid.NewGuid():N}@correo.test";

    private async Task<HttpClient> ClienteRegistradoAsync()
    {
        var navegador = _tienda.NuevoNavegador();
        var respuesta = await navegador.RegistrarAsync("Cliente Prueba", CorreoNuevo(), "Clave12345");
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        return navegador;
    }

    // Criterio 1: registro
    [Fact]
    public async Task Registro_CreaLaCuentaYElClienteEntraALaTienda()
    {
        var navegador = _tienda.NuevoNavegador();

        var respuesta = await navegador.RegistrarAsync("Ana Pérez", CorreoNuevo(), "Clave12345");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/", respuesta.RutaDeRedireccion());
        var inicio = await navegador.GetStringAsync("/Menu");
        Assert.Contains("Hola, Ana Pérez", inicio); // ya tiene la sesión iniciada
    }

    [Fact]
    public async Task Registro_GuardaLaContrasenaConHash_NoEnTextoPlano()
    {
        var repo = new InMemoryUsuarioRepository();
        var cuentas = new CuentaService(repo);

        var resultado = await cuentas.RegistrarAsync("Ana", "ana@correo.test", "Clave12345");

        Assert.True(resultado.Exitoso);
        Assert.NotEqual("Clave12345", resultado.Usuario!.PasswordHash);
        Assert.DoesNotContain("Clave12345", resultado.Usuario.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<Usuario>().VerifyHashedPassword(resultado.Usuario, resultado.Usuario.PasswordHash, "Clave12345"));
    }

    [Fact]
    public async Task Registro_SiemprePorDefectoComoCliente()
    {
        var cuentas = new CuentaService(new InMemoryUsuarioRepository());
        var resultado = await cuentas.RegistrarAsync("Ana", "ana@correo.test", "Clave12345");
        Assert.Equal(RolUsuario.Cliente, resultado.Usuario!.Rol);
    }

    [Theory]
    [InlineData("corta1")]
    [InlineData("solamenteletras")]
    [InlineData("1234567890")]
    public async Task Registro_RechazaContrasenasDebiles(string password)
    {
        var cuentas = new CuentaService(new InMemoryUsuarioRepository());
        var resultado = await cuentas.RegistrarAsync("Ana", "ana@correo.test", password);
        Assert.False(resultado.Exitoso);
    }

    // Criterio 2: inicio de sesión según el rol
    [Fact]
    public async Task Inicio_ConDatosCorrectos_ElClienteEntraALaTienda()
    {
        var correo = CorreoNuevo();
        await _tienda.NuevoNavegador().RegistrarAsync("Luis", correo, "Clave12345");

        var navegador = _tienda.NuevoNavegador();
        var respuesta = await navegador.IngresarAsync(correo, "Clave12345");

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/", respuesta.RutaDeRedireccion());
        Assert.Contains("Hola, Luis", await navegador.GetStringAsync("/Menu"));
    }

    [Fact]
    public async Task Inicio_ElAdministradorEntraASuPanel()
    {
        var navegador = _tienda.NuevoNavegador();

        var respuesta = await navegador.IngresarAsync(TiendaFactory.CorreoAdmin, TiendaFactory.PasswordAdmin);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Inventario", respuesta.RutaDeRedireccion());
    }

    [Fact]
    public async Task Inicio_ElCorreoNoDistingueMayusculas()
    {
        var navegador = _tienda.NuevoNavegador();
        var respuesta = await navegador.IngresarAsync(TiendaFactory.CorreoAdmin.ToUpperInvariant(), TiendaFactory.PasswordAdmin);
        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
    }

    // Criterio 3: datos incorrectos
    [Theory]
    [InlineData("admin@pizzaya.test", "ClaveEquivocada1")]
    [InlineData("nadie@pizzaya.test", "Admin12345")]
    public async Task Inicio_ConDatosIncorrectos_MuestraErrorYNoDejaEntrar(string correo, string password)
    {
        var navegador = _tienda.NuevoNavegador();

        var respuesta = await navegador.IngresarAsync(correo, password);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); // vuelve al formulario, no redirige
        Assert.Contains("El correo o la contraseña no son correctos.", await respuesta.Content.ReadAsStringAsync());
        var inventario = await navegador.GetAsync("/Inventario");
        Assert.Equal(HttpStatusCode.Redirect, inventario.StatusCode); // sigue sin sesión
    }

    // Criterio 4: correo repetido
    [Fact]
    public async Task Registro_ConCorreoRepetido_AvisaQueYaExiste()
    {
        var correo = CorreoNuevo();
        await _tienda.NuevoNavegador().RegistrarAsync("Primera", correo, "Clave12345");

        var respuesta = await _tienda.NuevoNavegador().RegistrarAsync("Segunda", correo.ToUpperInvariant(), "Otra12345");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Ese correo ya está registrado.", await respuesta.Content.ReadAsStringAsync());
    }

    // Criterio 5: acceso restringido
    [Theory]
    [InlineData("/Pedidos")]
    [InlineData("/Inventario")]
    [InlineData("/Inventario/NuevoPedido")]
    public async Task Cliente_NoPuedeEntrarAlPanelDeCocinaNiAlDeInventario(string ruta)
    {
        var navegador = await ClienteRegistradoAsync();

        var respuesta = await navegador.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.RutaDeRedireccion());
    }

    [Fact]
    public async Task Cliente_NoPuedeCambiarElEstadoDeUnPedido()
    {
        var navegador = await ClienteRegistradoAsync();

        var respuesta = await navegador.EnviarFormularioAsync("/Menu", "/Pedidos/Cambiar", new[]
        {
            new KeyValuePair<string, string>("id", "1"),
            new KeyValuePair<string, string>("nuevoEstado", "En preparación"),
        });

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/AccesoDenegado", respuesta.RutaDeRedireccion());
    }

    [Theory]
    [InlineData("/Pedidos")]
    [InlineData("/Inventario")]
    public async Task SinSesion_MandaAIniciarSesion(string ruta)
    {
        var respuesta = await _tienda.NuevoNavegador().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/Cuenta/Ingresar", respuesta.RutaDeRedireccion());
    }

    [Fact]
    public async Task Administrador_SiPuedeEntrarALosPaneles()
    {
        var navegador = _tienda.NuevoNavegador();
        await navegador.IngresarAsync(TiendaFactory.CorreoAdmin, TiendaFactory.PasswordAdmin);

        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync("/Pedidos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync("/Inventario")).StatusCode);
    }

    [Fact]
    public async Task Cocinero_EntraAlPanelDeCocinaYAlDeInventario()
    {
        using (var alcance = _tienda.Services.CreateScope())
        {
            var cuentas = alcance.ServiceProvider.GetRequiredService<CuentaService>();
            await cuentas.RegistrarAsync("Cocinero", "cocina@pizzaya.test", "Cocina12345", RolUsuario.Cocinero);
        }
        var navegador = _tienda.NuevoNavegador();

        var respuesta = await navegador.IngresarAsync("cocina@pizzaya.test", "Cocina12345");

        Assert.Equal("/Pedidos", respuesta.RutaDeRedireccion());
        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync("/Pedidos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await navegador.GetAsync("/Inventario")).StatusCode);
    }

    [Fact]
    public async Task Salir_CierraLaSesion()
    {
        var navegador = _tienda.NuevoNavegador();
        await navegador.IngresarAsync(TiendaFactory.CorreoAdmin, TiendaFactory.PasswordAdmin);

        var respuesta = await navegador.EnviarFormularioAsync("/Menu", "/Cuenta/Salir", Array.Empty<KeyValuePair<string, string>>());

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await navegador.GetAsync("/Inventario")).StatusCode);
    }

    [Fact]
    public async Task Ingresar_NoRedirigeASitiosExternos()
    {
        var navegador = _tienda.NuevoNavegador();

        var respuesta = await navegador.EnviarFormularioAsync("/Cuenta/Ingresar", "/Cuenta/Ingresar", new[]
        {
            new KeyValuePair<string, string>("Correo", TiendaFactory.CorreoAdmin),
            new KeyValuePair<string, string>("Password", TiendaFactory.PasswordAdmin),
            new KeyValuePair<string, string>("ReturnUrl", "https://sitio-malicioso.example/"),
        });

        Assert.Equal("/Inventario", respuesta.RutaDeRedireccion());
    }
}
