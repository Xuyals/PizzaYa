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
    private readonly ConsumoService? _consumo;

    // El inventario (HU9) es opcional: sin él, el menú no revisa ingredientes agotados
    public MenuController(IMenuRepository menu, PersonalizacionService personalizacion, ConsumoService? consumo = null)
    {
        _menu = menu;
        _personalizacion = personalizacion;
        _consumo = consumo;
    }

    private async Task<HashSet<int>> ObtenerAgotadosAsync() =>
        _consumo is null ? new HashSet<int>() : await _consumo.ObtenerAgotadosAsync();

    // GET: /Menu
    public async Task<IActionResult> Index()
    {
        var pizzas = await _menu.ObtenerPizzasAsync();
        var agotados = await ObtenerAgotadosAsync();

        // HU9: la pizza con un ingrediente base agotado se muestra como no disponible
        var modelo = new MenuViewModel
        {
            Pizzas = pizzas,
            PizzasSinStock = pizzas.Where(p => ConsumoService.SinStock(p, agotados)).Select(p => p.Id).ToHashSet(),
        };
        return View(modelo);
    }

    // GET: /Menu/Personalizar/5
    public async Task<IActionResult> Personalizar(int id)
    {
        var pizza = await _menu.ObtenerPizzaAsync(id);
        if (pizza is null) return RedirectToAction(nameof(Index));
        if (ConsumoService.SinStock(pizza, await ObtenerAgotadosAsync())) return PizzaSinStock(pizza);

        var bordes = await ObtenerBordesDisponiblesAsync(pizza);
        var anterior = LeerDeSesion();
        var seleccion = anterior?.PizzaId == id
            ? anterior
            : new PizzaPersonalizada
            {
                PizzaId = id,
                BordeId = pizza.EsPizzaPersonalizada
                    ? bordes.FirstOrDefault(b => b.Nombre == "Sin borde")?.Id
                    : bordes.FirstOrDefault()?.Id,
            };

        return View(await ArmarModeloAsync(pizza, seleccion, bordes));
    }

    // POST: /Menu/Personalizar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Personalizar(int id, [Bind(Prefix = "Seleccion")] PizzaPersonalizada seleccion)
    {
        var pizza = await _menu.ObtenerPizzaAsync(id);
        if (pizza is null) return RedirectToAction(nameof(Index));
        var agotados = await ObtenerAgotadosAsync();
        if (ConsumoService.SinStock(pizza, agotados)) return PizzaSinStock(pizza);
        seleccion.PizzaId = id;

        var tamanos = await _menu.ObtenerTamanosAsync();
        var bordes = await ObtenerBordesDisponiblesAsync(pizza);
        var ingredientes = _personalizacion
            .FiltrarOpciones(pizza, await _menu.ObtenerIngredientesAsync())
            .ToList();

        var errores = _personalizacion.Validar(seleccion, pizza, tamanos, bordes, ingredientes);

        // HU9: un ingrediente agotado no se puede escoger como extra
        foreach (var agotado in ingredientes.Where(i => seleccion.ExtrasIds.Contains(i.Id) && agotados.Contains(i.Id)))
            errores.Add(("ExtrasIds", $"{agotado.Nombre} está agotado y no se puede agregar como extra."));

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
        Extras = ingredientes ?? _personalizacion.FiltrarOpciones(pizza, await _menu.ObtenerIngredientesAsync()).ToList(),
        ExtrasAgotados = await ObtenerAgotadosAsync(),
    };

    private async Task<List<Borde>> ObtenerBordesDisponiblesAsync(Pizza pizza)
    {
        var bordes = await _menu.ObtenerBordesAsync();
        return pizza.EsPizzaPersonalizada
            ? bordes.Where(b => b.Nombre is "Sin borde" or "Relleno de queso" or "Relleno de bocadillo" or "Relleno de chocolate").ToList()
            : bordes;
    }

    private IActionResult PizzaSinStock(Pizza pizza)
    {
        TempData["Error"] = $"La pizza {pizza.Nombre} no está disponible por ahora.";
        return RedirectToAction(nameof(Index));
    }
}
