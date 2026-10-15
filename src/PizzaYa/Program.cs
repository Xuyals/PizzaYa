using System.Text.Encodings.Web;
using System.Text.Unicode;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.WebEncoders;
using Npgsql;
using PizzaYa.Repositories;
using PizzaYa.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Que tildes y ñ salgan tal cual en el HTML en lugar de &#xE1;
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Ingresar";
        o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        o.Cookie.Name = "PizzaYa.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(2);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// Supabase: la cadena de conexión va en user-secrets (ver README), nunca en el repositorio.
DefaultTypeMap.MatchNamesWithUnderscores = true;
var cadenaSupabase = builder.Configuration.GetConnectionString("Supabase");
if (!string.IsNullOrWhiteSpace(cadenaSupabase))
{
    builder.Services.AddSingleton(NpgsqlDataSource.Create(cadenaSupabase));
    builder.Services.AddScoped<IMenuRepository, SupabaseMenuRepository>();
    builder.Services.AddScoped<IInventarioRepository, SupabaseInventarioRepository>(); // HU5 (Equipo 4)
    builder.Services.AddScoped<IPedidoRepository, SupabasePedidoRepository>();
    builder.Services.AddScoped<IUsuarioRepository, SupabaseUsuarioRepository>();
    builder.Services.AddScoped<ICuponRepository, SupabaseCuponRepository>();
    builder.Services.AddScoped<IFavoritaRepository, SupabaseFavoritaRepository>();
}
else
{
    builder.Services.AddSingleton<IMenuRepository>(InMemoryMenuRepository.ConDatosDeEjemplo());
    builder.Services.AddSingleton<IInventarioRepository>(InMemoryInventarioRepository.ConDatosDeEjemplo()); // HU5 (Equipo 4)
    builder.Services.AddSingleton<IPedidoRepository, InMemoryPedidoRepository>();
    builder.Services.AddSingleton<IUsuarioRepository, InMemoryUsuarioRepository>();
    builder.Services.AddSingleton<ICuponRepository>(InMemoryCuponRepository.ConDatosDeEjemplo());
    builder.Services.AddSingleton<IFavoritaRepository, InMemoryFavoritaRepository>();
}
builder.Services.AddSingleton<PersonalizacionService>();
builder.Services.AddSingleton<InventarioService>(); // HU5 (Equipo 4)
builder.Services.AddSingleton<PrecioService>();
builder.Services.AddSingleton<CarritoService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<CuentaService>();
builder.Services.AddScoped<CuponService>();
builder.Services.AddScoped<ArmadoService>();
builder.Services.AddScoped<PerfilService>();

var app = builder.Build();

if (string.IsNullOrWhiteSpace(cadenaSupabase))
{
    app.Logger.LogWarning(
        "No hay ConnectionStrings:Supabase configurada: se usa un menú de ejemplo en memoria.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Index}/{id?}");

// Administrador inicial: se crea al arrancar si la configuración trae Admin:Correo y Admin:Password
// (user-secrets o variables de entorno; ver README). No se guarda en el repositorio.
var correoAdmin = app.Configuration["Admin:Correo"];
var passwordAdmin = app.Configuration["Admin:Password"];
if (!string.IsNullOrWhiteSpace(correoAdmin) && !string.IsNullOrWhiteSpace(passwordAdmin))
{
    try
    {
        using var alcance = app.Services.CreateScope();
        var cuentas = alcance.ServiceProvider.GetRequiredService<CuentaService>();
        var nombreAdmin = app.Configuration["Admin:Nombre"] ?? "Administrador";
        if (await cuentas.AsegurarAdministradorAsync(nombreAdmin, correoAdmin, passwordAdmin))
            app.Logger.LogInformation("Administrador inicial creado: {Correo}", correoAdmin);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudo crear el administrador inicial.");
    }
}

app.Run();

// Para que las pruebas de integración (WebApplicationFactory) puedan arrancar la app
public partial class Program { }
