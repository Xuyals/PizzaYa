using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>
/// HU2 – Calcula el precio en tiempo real y gestiona el Resumen de la pizza.
/// Sustituye las acciones Resumen del MenuController del Equipo 1
/// (se redirige a este controller desde el Resumen original).
/// </summary>
public class PrecioController : Controller
{
    private readonly IMenuRepository _menu;
    private readonly PrecioService _precio;
    private readonly CarritoService _carrito;
    private readonly CuponService _cupones;

    public PrecioController(IMenuRepository menu, PrecioService precio, CarritoService carrito, CuponService cupones)
    {
        _menu = menu;
        _precio = precio;
        _carrito = carrito;
        _cupones = cupones;
    }

    // GET: /Precio/Resumen?esDomicilio=false&cupon=
    /// <summary>
    /// Muestra el resumen de la pizza con el precio calculado.
    /// Lee la sesión de PizzaPersonalizada (generada por Equipo 1).
    /// </summary>
    public async Task<IActionResult> Resumen(bool esDomicilio = false, string? cupon = null)
    {
        var seleccion = LeerSeleccionDeSesion();
        if (seleccion is null) return RedirectToAction("Index", "Menu");

        var modelo = await ConstruirResumenAsync(seleccion, esDomicilio, cupon);
        if (modelo is null) return RedirectToAction("Index", "Menu");

        return View(modelo);
    }

    // POST: /Precio/AplicarCupon
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AplicarCupon(bool esDomicilio, string? cupon)
        => RedirectToAction(nameof(Resumen), new { esDomicilio, cupon });

    // POST: /Precio/CambiarEntrega
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEntrega(bool esDomicilio, string? cupon)
        => RedirectToAction(nameof(Resumen), new { esDomicilio, cupon });

    // POST: /Precio/AgregarAlCarrito
    /// <summary>
    /// Agrega la pizza en sesión al carrito (HU3) y redirige al carrito.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarAlCarrito(bool esDomicilio, string? cupon)
    {
        var seleccion = LeerSeleccionDeSesion();
        if (seleccion is null)
        {
            TempData["Error"] = "No hay pizza seleccionada.";
            return RedirectToAction("Index", "Menu");
        }

        var pizza     = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);
        var tamanos   = await _menu.ObtenerTamanosAsync();
        var bordes    = await _menu.ObtenerBordesAsync();
        var ingredientes = await _menu.ObtenerIngredientesAsync();

        var tamano = tamanos.FirstOrDefault(t => t.Id == seleccion.TamanoId);
        var borde  = bordes.FirstOrDefault(b  => b.Id == seleccion.BordeId);

        if (pizza is null || tamano is null || borde is null)
        {
            TempData["Error"] = "La selección de pizza no es válida.";
            return RedirectToAction("Index", "Menu");
        }

        var extras   = ingredientes.Where(i => seleccion.ExtrasIds.Contains(i.Id)).ToList();
        var quitados = ingredientes.Where(i => seleccion.QuitadosIds.Contains(i.Id)).ToList();

        var precioBase = _precio.CalcularPrecioBase(pizza, tamano, borde, extras);

        var item = new ItemCarrito
        {
            Seleccion       = seleccion,
            NombrePizza     = pizza.Nombre,
            NombreTamano    = tamano.Nombre,
            NombreBorde     = borde.Nombre,
            NombresExtras   = extras.Select(e => e.Nombre).ToList(),
            PrecioUnitario  = precioBase,
            Cantidad        = 1,
        };

        _carrito.AgregarItem(HttpContext.Session, item);

        // Limpiamos la pizza en sesión para evitar duplicados al volver
        HttpContext.Session.Remove(PizzaPersonalizada.ClaveSesion);

        TempData["Exito"] = $"✓ {pizza.Nombre} agregada al carrito.";
        return RedirectToAction("Index", "Carrito");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private PizzaPersonalizada? LeerSeleccionDeSesion()
    {
        var json = HttpContext.Session.GetString(PizzaPersonalizada.ClaveSesion);
        return json is null ? null : JsonSerializer.Deserialize<PizzaPersonalizada>(json);
    }

    private async Task<ResumenConPrecioViewModel?> ConstruirResumenAsync(
        PizzaPersonalizada seleccion, bool esDomicilio, string? cupon)
    {
        var pizza    = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);
        var tamanos  = await _menu.ObtenerTamanosAsync();
        var bordes   = await _menu.ObtenerBordesAsync();
        var ingredientes = await _menu.ObtenerIngredientesAsync();

        var tamano = tamanos.FirstOrDefault(t => t.Id == seleccion.TamanoId);
        var borde  = bordes.FirstOrDefault(b  => b.Id == seleccion.BordeId);

        if (pizza is null || tamano is null || borde is null) return null;

        var extras   = ingredientes.Where(i => seleccion.ExtrasIds.Contains(i.Id)).ToList();
        var quitados = ingredientes.Where(i => seleccion.QuitadosIds.Contains(i.Id)).ToList();

        // Los cupones salen de la base de datos (HU6): pueden vencer o ser de un solo cliente
        var cupones = await _cupones.ObtenerParaUsuarioAsync(User.UsuarioId());
        return _precio.ArmarResumen(pizza, tamano, borde, extras, quitados, esDomicilio, cupon, cupones);
    }
}
