using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>
/// Convierte una pizza configurada (HU1) en un ítem del carrito (HU3) con el precio de hoy (HU2).
/// Lo usan "pedir de nuevo" una favorita y "repetir el último pedido" del perfil (HU6).
/// </summary>
public class ArmadoService
{
    private readonly IMenuRepository _menu;
    private readonly PrecioService _precio;

    public ArmadoService(IMenuRepository menu, PrecioService precio)
    {
        _menu = menu;
        _precio = precio;
    }

    /// <summary>Devuelve null si la pizza, el tamaño o el borde ya no existen en el menú.</summary>
    public async Task<ItemCarrito?> ConstruirItemAsync(PizzaPersonalizada seleccion, int cantidad = 1)
    {
        var pizza = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);
        var tamano = (await _menu.ObtenerTamanosAsync()).FirstOrDefault(t => t.Id == seleccion.TamanoId);
        var borde = (await _menu.ObtenerBordesAsync()).FirstOrDefault(b => b.Id == seleccion.BordeId);
        if (pizza is null || tamano is null || borde is null) return null;

        var ingredientes = await _menu.ObtenerIngredientesAsync();
        var extras = ingredientes.Where(i => seleccion.ExtrasIds.Contains(i.Id)).ToList();

        return new ItemCarrito
        {
            Seleccion = seleccion,
            NombrePizza = pizza.Nombre,
            NombreTamano = tamano.Nombre,
            NombreBorde = borde.Nombre,
            NombresExtras = extras.Select(e => e.Nombre).ToList(),
            PrecioUnitario = _precio.CalcularPrecioBase(pizza, tamano, borde, extras),
            Cantidad = Math.Max(1, cantidad),
        };
    }
}
