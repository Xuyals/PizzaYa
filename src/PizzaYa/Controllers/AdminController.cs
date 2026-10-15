using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

/// <summary>HU7 (sugerida) – El administrador registra y edita pizzas, precios, bordes, extras, tamaños y el valor del domicilio.</summary>
[Authorize(Roles = RolUsuario.Administrador)]
public class AdminController : Controller
{
    private readonly IMenuAdminRepository _admin;
    private readonly IMenuRepository _menu;
    private readonly MenuAdminService _servicio;
    private readonly ConfiguracionService _configuracion;

    public AdminController(
        IMenuAdminRepository admin, IMenuRepository menu, MenuAdminService servicio, ConfiguracionService configuracion)
    {
        _admin = admin;
        _menu = menu;
        _servicio = servicio;
        _configuracion = configuracion;
    }

    // GET: /Admin
    public async Task<IActionResult> Index() => View(new AdminMenuViewModel
    {
        Pizzas = await _admin.ObtenerTodasLasPizzasAsync(),
        Bordes = await _menu.ObtenerBordesAsync(),
        Extras = await _menu.ObtenerIngredientesAsync(),
        Tamanos = await _menu.ObtenerTamanosAsync(),
        ValorDomicilio = await _configuracion.ObtenerCostoDomicilioAsync(),
    });

    // GET: /Admin/Pizza       (nueva)
    // GET: /Admin/Pizza/5     (editar)
    [HttpGet]
    public async Task<IActionResult> Pizza(int? id)
    {
        var modelo = new PizzaFormViewModel { Id = id, Ingredientes = await _menu.ObtenerIngredientesAsync() };
        if (id is not null)
        {
            var pizza = await _admin.ObtenerPizzaParaEditarAsync(id.Value);
            if (pizza is null) return RedirectToAction(nameof(Index));
            modelo.Nombre = pizza.Nombre;
            modelo.Descripcion = pizza.Descripcion;
            modelo.PrecioBase = pizza.PrecioBase.ToString("0.##", CultureInfo.InvariantCulture);
            modelo.IngredientesBaseIds = pizza.IngredientesBase.Select(i => i.Id).ToList();
        }
        return View(modelo);
    }

    // POST: /Admin/Pizza
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pizza(PizzaFormViewModel modelo)
    {
        modelo.Ingredientes = await _menu.ObtenerIngredientesAsync();

        var precio = LeerNumero(modelo.PrecioBase, out var precioInvalido);
        if (precioInvalido) ModelState.AddModelError(nameof(modelo.PrecioBase), "Escribe un número válido para el precio.");

        // Campos faltantes: se resaltan y no se guarda nada
        var resultado = await _servicio.GuardarPizzaAsync(
            modelo.Id, modelo.Nombre, modelo.Descripcion, precioInvalido ? null : precio, modelo.IngredientesBaseIds);
        if (!resultado.Exitoso)
        {
            foreach (var error in resultado.Errores)
                if (!(precioInvalido && error.Campo == nameof(modelo.PrecioBase)))
                    ModelState.AddModelError(error.Campo, error.Mensaje);
            return View(modelo);
        }

        TempData["Exito"] = modelo.EsNueva
            ? $"✓ Pizza \"{modelo.Nombre!.Trim()}\" registrada: ya aparece en el menú."
            : $"✓ Pizza \"{modelo.Nombre!.Trim()}\" actualizada: el nuevo precio se usa en los pedidos nuevos.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/CambiarDisponibilidad
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarDisponibilidad(int id, bool disponible)
    {
        TempData[await _servicio.CambiarDisponibilidadAsync(id, disponible) ? "Exito" : "Error"] =
            disponible ? "La pizza vuelve a mostrarse en el menú." : "La pizza ya no se muestra en el menú del cliente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Admin/GuardarBorde, /GuardarExtra, /GuardarTamano  (id vacío = registrar uno nuevo)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarBorde(int? id, string? nombre, string? valor) =>
        Informar(await _servicio.GuardarBordeAsync(id, nombre, LeerNumeroOInvalido(valor)), "Borde guardado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarExtra(int? id, string? nombre, string? valor) =>
        Informar(await _servicio.GuardarIngredienteAsync(id, nombre, LeerNumeroOInvalido(valor)), "Extra guardado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarTamano(int? id, string? nombre, string? valor) =>
        Informar(await _servicio.GuardarTamanoAsync(id, nombre, LeerNumeroOInvalido(valor)), "Tamaño guardado.");

    // POST: /Admin/GuardarDomicilio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarDomicilio(string? valor)
    {
        var numero = LeerNumero(valor, out var invalido);
        var error = invalido ? "Escribe un número válido para el domicilio." : ConfiguracionService.Validar(numero);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        await _configuracion.GuardarCostoDomicilioAsync(numero!.Value);
        TempData["Exito"] = "✓ Valor del domicilio actualizado: se suma en los pedidos a domicilio nuevos.";
        return RedirectToAction(nameof(Index));
    }

    // ── Ayudas ──────────────────────────────────────────────────────────

    private IActionResult Informar(MenuAdminService.Resultado resultado, string exito)
    {
        if (resultado.Exitoso) TempData["Exito"] = "✓ " + exito;
        else TempData["Error"] = string.Join(" ", resultado.Errores.Select(e => e.Mensaje));
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Número escrito por el administrador (acepta punto decimal); null si está vacío.</summary>
    private static decimal? LeerNumero(string? texto, out bool invalido)
    {
        invalido = false;
        if (string.IsNullOrWhiteSpace(texto)) return null;
        if (decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var valor)) return valor;
        invalido = true;
        return null;
    }

    /// <summary>Un texto que no es número se trata como "falta el valor" para que el servicio lo rechace.</summary>
    private static decimal? LeerNumeroOInvalido(string? texto) => LeerNumero(texto, out _);
}
