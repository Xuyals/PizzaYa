using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Controllers;

public class MenuController : Controller
{
    private readonly IMenuRepository _menu;
    private readonly PersonalizacionService _personalizacion;

    public MenuController(IMenuRepository menu, PersonalizacionService personalizacion)
    {
        _menu = menu;
        _personalizacion = personalizacion;
    }

    // GET: /Menu
    public async Task<IActionResult> Index()
    {
        var modelo = new MenuViewModel { Pizzas = await _menu.ObtenerPizzasAsync() };
        return View(modelo);
    }

    // GET: /Menu/Personalizar/5
    public async Task<IActionResult> Personalizar(int id)
    {
        var pizza = await _menu.ObtenerPizzaAsync(id);
        if (pizza is null) return RedirectToAction(nameof(Index));

        var bordes = await _menu.ObtenerBordesAsync();
        var anterior = LeerDeSesion();
        var seleccion = anterior?.PizzaId == id
            ? anterior
            : new PizzaPersonalizada { PizzaId = id, BordeId = bordes.FirstOrDefault()?.Id };

        return View(await ArmarModeloAsync(pizza, seleccion, bordes));
    }

    // POST: /Menu/Personalizar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Personalizar(int id, [Bind(Prefix = "Seleccion")] PizzaPersonalizada seleccion)
    {
        var pizza = await _menu.ObtenerPizzaAsync(id);
        if (pizza is null) return RedirectToAction(nameof(Index));
        seleccion.PizzaId = id;

        var tamanos = await _menu.ObtenerTamanosAsync();
        var bordes = await _menu.ObtenerBordesAsync();
        var ingredientes = await _menu.ObtenerIngredientesAsync();

        var errores = _personalizacion.Validar(seleccion, pizza, tamanos, bordes, ingredientes);
        if (errores.Count > 0)
        {
            foreach (var (campo, mensaje) in errores)
                ModelState.AddModelError($"Seleccion.{campo}", mensaje);
            return View(await ArmarModeloAsync(pizza, seleccion, bordes, tamanos, ingredientes));
        }

        var limpia = _personalizacion.Normalizar(seleccion);
        HttpContext.Session.SetString(PizzaPersonalizada.ClaveSesion, JsonSerializer.Serialize(limpia));
        return RedirectToAction(nameof(Resumen));
    }

    // GET: /Menu/Resumen
    public async Task<IActionResult> Resumen()
    {
        var seleccion = LeerDeSesion();
        if (seleccion is null) return RedirectToAction(nameof(Index));

        var pizza = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);
        var tamano = (await _menu.ObtenerTamanosAsync()).FirstOrDefault(t => t.Id == seleccion.TamanoId);
        var borde = (await _menu.ObtenerBordesAsync()).FirstOrDefault(b => b.Id == seleccion.BordeId);
        if (pizza is null || tamano is null || borde is null) return RedirectToAction(nameof(Index));

        var ingredientes = await _menu.ObtenerIngredientesAsync();
        return View(new ResumenPizzaViewModel
        {
            Pizza = pizza,
            Tamano = tamano,
            Borde = borde,
            Extras = ingredientes.Where(i => seleccion.ExtrasIds.Contains(i.Id)).ToList(),
            Quitados = pizza.IngredientesBase.Where(i => seleccion.QuitadosIds.Contains(i.Id)).ToList(),
        });
    }

    private PizzaPersonalizada? LeerDeSesion()
    {
        var json = HttpContext.Session.GetString(PizzaPersonalizada.ClaveSesion);
        return json is null ? null : JsonSerializer.Deserialize<PizzaPersonalizada>(json);
    }

    private async Task<PersonalizarViewModel> ArmarModeloAsync(
        Pizza pizza,
        PizzaPersonalizada seleccion,
        List<Borde> bordes,
        List<Tamano>? tamanos = null,
        List<Ingrediente>? ingredientes = null) => new()
    {
        Pizza = pizza,
        Seleccion = seleccion,
        Bordes = bordes,
        Tamanos = tamanos ?? await _menu.ObtenerTamanosAsync(),
        Extras = ingredientes ?? await _menu.ObtenerIngredientesAsync(),
    };
}
