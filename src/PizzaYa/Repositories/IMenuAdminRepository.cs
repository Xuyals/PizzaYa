using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// HU7 – Lo que el administrador puede registrar y editar del menú. Las operaciones de crear devuelven
/// null (y las de editar false) si el nombre ya existe o el elemento no existe.
/// </summary>
public interface IMenuAdminRepository
{
    /// <summary>Todas las pizzas, también las ocultas, con sus ingredientes base.</summary>
    Task<List<Pizza>> ObtenerTodasLasPizzasAsync();

    /// <summary>Una pizza (visible u oculta) con sus ingredientes base, para editarla.</summary>
    Task<Pizza?> ObtenerPizzaParaEditarAsync(int id);

    Task<Pizza?> CrearPizzaAsync(Pizza pizza, IEnumerable<int> ingredientesBaseIds);
    Task<bool> ActualizarPizzaAsync(int id, string nombre, string? descripcion, decimal precioBase, IEnumerable<int> ingredientesBaseIds);

    /// <summary>Oculta (false) o vuelve a mostrar (true) la pizza en el menú del cliente.</summary>
    Task<bool> CambiarDisponibilidadPizzaAsync(int id, bool disponible);

    Task<Borde?> CrearBordeAsync(string nombre, decimal precio);
    Task<bool> ActualizarBordeAsync(int id, string nombre, decimal precio);

    /// <summary>Ingredientes que se pueden agregar como extra.</summary>
    Task<Ingrediente?> CrearIngredienteAsync(string nombre, decimal precioExtra);
    Task<bool> ActualizarIngredienteAsync(int id, string nombre, decimal precioExtra);

    Task<Tamano?> CrearTamanoAsync(string nombre, decimal factorPrecio);
    Task<bool> ActualizarTamanoAsync(int id, string nombre, decimal factorPrecio);
}
