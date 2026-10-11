using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// Menú en memoria con los mismos datos de database/01_hu1_menu_pizzas.sql.
/// Se usa en las pruebas y cuando no hay cadena de conexión a Supabase configurada.
/// </summary>
public class InMemoryMenuRepository : IMenuRepository
{
    private readonly List<Pizza> _pizzas;
    private readonly List<Tamano> _tamanos;
    private readonly List<Borde> _bordes;
    private readonly List<Ingrediente> _ingredientes;

    public InMemoryMenuRepository(
        List<Pizza> pizzas, List<Tamano> tamanos, List<Borde> bordes, List<Ingrediente> ingredientes)
    {
        _pizzas = pizzas;
        _tamanos = tamanos;
        _bordes = bordes;
        _ingredientes = ingredientes;
    }

    public static InMemoryMenuRepository ConDatosDeEjemplo()
    {
        var ing = new[]
        {
            ("Aceitunas", 3000m), ("Carne molida", 4000m), ("Cebolla", 1500m), ("Champiñones", 3000m),
            ("Jalapeños", 2500m), ("Jamón", 3500m), ("Maíz tierno", 2000m), ("Pepperoni", 4000m),
            ("Pimentón", 2000m), ("Piña", 2500m), ("Pollo", 4000m), ("Queso mozzarella", 3000m),
            ("Salsa de tomate", 0m), ("Tocineta", 4500m),
        }.Select((x, i) => new Ingrediente { Id = i + 1, Nombre = x.Item1, PrecioExtra = x.Item2 }).ToList();

        List<Ingrediente> De(params string[] nombres) =>
            ing.Where(i => nombres.Contains(i.Nombre)).ToList();

        var pizzas = new List<Pizza>
        {
            new() { Id = 1, Nombre = "Hawaiana", Descripcion = "La clásica dulce y salada.", PrecioBase = 28000,
                IngredientesBase = De("Salsa de tomate", "Queso mozzarella", "Jamón", "Piña") },
            new() { Id = 2, Nombre = "Mexicana", Descripcion = "Picante, con carne y jalapeños.", PrecioBase = 32000,
                IngredientesBase = De("Salsa de tomate", "Queso mozzarella", "Carne molida", "Jalapeños", "Maíz tierno", "Cebolla") },
            new() { Id = 3, Nombre = "Pepperoni", Descripcion = "Abundante pepperoni sobre mozzarella.", PrecioBase = 30000,
                IngredientesBase = De("Salsa de tomate", "Queso mozzarella", "Pepperoni") },
            new() { Id = 4, Nombre = "Pollo con champiñones", Descripcion = "Pollo desmechado y champiñones.", PrecioBase = 31000,
                IngredientesBase = De("Salsa de tomate", "Queso mozzarella", "Pollo", "Champiñones") },
            new() { Id = 5, Nombre = "Vegetariana", Descripcion = "Llena de vegetales frescos.", PrecioBase = 27000,
                IngredientesBase = De("Salsa de tomate", "Queso mozzarella", "Champiñones", "Pimentón", "Cebolla", "Aceitunas", "Maíz tierno") },
        };

        var tamanos = new List<Tamano>
        {
            new() { Id = 1, Nombre = "Personal", FactorPrecio = 1.0m },
            new() { Id = 2, Nombre = "Mediana", FactorPrecio = 1.6m },
            new() { Id = 3, Nombre = "Familiar", FactorPrecio = 2.2m },
        };

        var bordes = new List<Borde>
        {
            new() { Id = 1, Nombre = "Tradicional", Precio = 0 },
            new() { Id = 2, Nombre = "Relleno de queso", Precio = 5000 },
            new() { Id = 3, Nombre = "Ajo y finas hierbas", Precio = 3000 },
        };

        return new InMemoryMenuRepository(pizzas, tamanos, bordes, ing);
    }

    public Task<List<Pizza>> ObtenerPizzasAsync() => Task.FromResult(_pizzas.ToList());
    public Task<Pizza?> ObtenerPizzaAsync(int id) => Task.FromResult(_pizzas.FirstOrDefault(p => p.Id == id));
    public Task<List<Tamano>> ObtenerTamanosAsync() => Task.FromResult(_tamanos.ToList());
    public Task<List<Borde>> ObtenerBordesAsync() => Task.FromResult(_bordes.ToList());
    public Task<List<Ingrediente>> ObtenerIngredientesAsync() => Task.FromResult(_ingredientes.ToList());
}
