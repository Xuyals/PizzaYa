using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace PizzaYa.Tests;

/// <summary>
/// Arranca la app completa en memoria (sin Supabase) para pruebas de integración:
/// cada instancia tiene sus propios repositorios, así que las pruebas no se afectan entre sí.
/// </summary>
public class TiendaFactory : WebApplicationFactory<Program>
{
    public const string CorreoAdmin = "admin@pizzaya.test";
    public const string PasswordAdmin = "Admin12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Se agrega al final para que gane sobre user-secrets: las pruebas nunca tocan Supabase
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Supabase"] = "",
                ["Admin:Correo"] = CorreoAdmin,
                ["Admin:Password"] = PasswordAdmin,
                ["Admin:Nombre"] = "Admin de prueba",
            });
        });
    }

    /// <summary>Cliente que conserva las cookies y no sigue redirecciones, para poder revisarlas.</summary>
    public HttpClient NuevoNavegador() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });
}

public static class NavegadorExtensiones
{
    private static readonly Regex Token =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>Hace POST de un formulario: primero abre la página para sacar el token antiforgery.</summary>
    public static async Task<HttpResponseMessage> EnviarFormularioAsync(
        this HttpClient navegador, string rutaFormulario, string rutaPost, IEnumerable<KeyValuePair<string, string>> campos)
    {
        var html = await navegador.GetStringAsync(rutaFormulario);
        var token = Token.Match(html);
        var datos = campos.ToList();
        if (token.Success) datos.Add(new("__RequestVerificationToken", token.Groups[1].Value));
        return await navegador.PostAsync(rutaPost, new FormUrlEncodedContent(datos));
    }

    public static Task<HttpResponseMessage> EnviarFormularioAsync(
        this HttpClient navegador, string ruta, params (string Nombre, string Valor)[] campos) =>
        navegador.EnviarFormularioAsync(ruta, ruta,
            campos.Select(c => new KeyValuePair<string, string>(c.Nombre, c.Valor)));

    public static async Task<HttpResponseMessage> IngresarAsync(this HttpClient navegador, string correo, string password) =>
        await navegador.EnviarFormularioAsync("/Cuenta/Ingresar", ("Correo", correo), ("Password", password));

    public static async Task<HttpResponseMessage> RegistrarAsync(
        this HttpClient navegador, string nombre, string correo, string password) =>
        await navegador.EnviarFormularioAsync("/Cuenta/Registrar",
            ("Nombre", nombre), ("Correo", correo), ("Password", password), ("ConfirmarPassword", password));

    /// <summary>Ruta (sin dominio ni parámetros) a la que redirige la respuesta, o null si no redirige.</summary>
    public static string? RutaDeRedireccion(this HttpResponseMessage respuesta)
    {
        var destino = respuesta.Headers.Location;
        if (destino is null) return null;
        return destino.IsAbsoluteUri ? destino.AbsolutePath : destino.OriginalString.Split('?')[0];
    }
}
