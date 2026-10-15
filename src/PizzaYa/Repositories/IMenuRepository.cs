using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface IMenuRepository
{
    /// <summary>Pizzas disponibles con sus ingredientes base.</summary>
    Task<List<Pizza>> ObtenerPizzasAsync();
    Task<Pizza?> ObtenerPizzaAsync(int id);
    Task<List<Tamano>> ObtenerTamanosAsync();
    Task<List<Borde>> ObtenerBordesAsync();
    /// <summary>Ingredientes que se pueden agregar como extra.</summary>
    Task<List<Ingrediente>> ObtenerIngredientesAsync();
}
