using System.Text.Encodings.Web;
using System.Text.Unicode;
using Dapper;
using Microsoft.Extensions.WebEncoders;
using Npgsql;
using PizzaYa.Repositories;
using PizzaYa.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
// Que tildes y ñ salgan tal cual en el HTML en lugar de &#xE1;
builder.Services.Configure<WebEncoderOptions>(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
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
}
else
{
    builder.Services.AddSingleton<IMenuRepository>(InMemoryMenuRepository.ConDatosDeEjemplo());
    builder.Services.AddSingleton<IInventarioRepository>(InMemoryInventarioRepository.ConDatosDeEjemplo()); // HU5 (Equipo 4)
    builder.Services.AddSingleton<IPedidoRepository, InMemoryPedidoRepository>();
}
builder.Services.AddSingleton<PersonalizacionService>();
builder.Services.AddSingleton<InventarioService>(); // HU5 (Equipo 4)
builder.Services.AddSingleton<PrecioService>();
builder.Services.AddSingleton<CarritoService>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Menu}/{action=Index}/{id?}");

app.Run();
