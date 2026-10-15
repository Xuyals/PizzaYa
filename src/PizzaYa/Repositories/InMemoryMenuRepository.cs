using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// Menú en memoria con los mismos datos de database/01_hu1_menu_pizzas.sql.
/// Se usa en las pruebas y cuando no hay cadena de conexión a Supabase configurada.
/// </summary>
public class InMemoryMenuRepository : IMenuRepository, IMenuAdminRepository
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
            ("Salsa de tomate", 0m), ("Tocineta", 4500m), ("Queso extra", 3000m), ("Carne", 4500m),
            ("Chorizo", 4000m), ("Guacamole", 3500m), ("Chicharrón", 4500m), ("Carne de cerdo", 4500m),
            ("Masa", 0m), ("Pasta de tomate", 0m), ("Queso base", 0m),
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
            new() { Id = 6, Nombre = "Arma tu pizza", Descripcion = "Crea una pizza a tu gusto con ingredientes seleccionados.",
                PrecioBase = 22000, EsPizzaPersonalizada = true,
                IngredientesBase = De("Masa", "Pasta de tomate", "Queso base") },
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
            new() { Id = 4, Nombre = "Sin borde", Precio = 0 },
            new() { Id = 5, Nombre = "Relleno de bocadillo", Precio = 4500 },
            new() { Id = 6, Nombre = "Relleno de chocolate", Precio = 5000 },
        };

        return new InMemoryMenuRepository(pizzas, tamanos, bordes, ing);
    }

    // El menú del cliente solo ve las pizzas disponibles (las ocultas por el administrador no salen)
    public Task<List<Pizza>> ObtenerPizzasAsync() => Task.FromResult(Copia(() => _pizzas.Where(p => p.Disponible).ToList()));
    public Task<Pizza?> ObtenerPizzaAsync(int id) => Task.FromResult(Copia(() => _pizzas.FirstOrDefault(p => p.Id == id && p.Disponible)));
    public Task<List<Tamano>> ObtenerTamanosAsync() => Task.FromResult(Copia(() => _tamanos.ToList()));
    public Task<List<Borde>> ObtenerBordesAsync() => Task.FromResult(Copia(() => _bordes.ToList()));
    public Task<List<Ingrediente>> ObtenerIngredientesAsync() => Task.FromResult(Copia(() => _ingredientes.ToList()));

    // ── HU7: administración del menú ────────────────────────────────────

    public Task<List<Pizza>> ObtenerTodasLasPizzasAsync() => Task.FromResult(Copia(() => _pizzas.OrderBy(p => p.Nombre).ToList()));

    public Task<Pizza?> ObtenerPizzaParaEditarAsync(int id) => Task.FromResult(Copia(() => _pizzas.FirstOrDefault(p => p.Id == id)));

    public Task<Pizza?> CrearPizzaAsync(Pizza pizza, IEnumerable<int> ingredientesBaseIds)
    {
        lock (_candado)
        {
            if (_pizzas.Any(p => Igual(p.Nombre, pizza.Nombre))) return Task.FromResult<Pizza?>(null);
            pizza.Id = _pizzas.Count == 0 ? 1 : _pizzas.Max(p => p.Id) + 1;
            pizza.IngredientesBase = IngredientesPorId(ingredientesBaseIds);
            _pizzas.Add(pizza);
            return Task.FromResult<Pizza?>(pizza);
        }
    }

    public Task<bool> ActualizarPizzaAsync(int id, string nombre, string? descripcion, decimal precioBase, IEnumerable<int> ingredientesBaseIds)
    {
        lock (_candado)
        {
            var pizza = _pizzas.FirstOrDefault(p => p.Id == id);
            if (pizza is null || _pizzas.Any(p => p.Id != id && Igual(p.Nombre, nombre))) return Task.FromResult(false);
            pizza.Nombre = nombre;
            pizza.Descripcion = descripcion;
            pizza.PrecioBase = precioBase;
            pizza.IngredientesBase = IngredientesPorId(ingredientesBaseIds);
            return Task.FromResult(true);
        }
    }

    public Task<bool> CambiarDisponibilidadPizzaAsync(int id, bool disponible)
    {
        lock (_candado)
        {
            var pizza = _pizzas.FirstOrDefault(p => p.Id == id);
            if (pizza is null) return Task.FromResult(false);
            pizza.Disponible = disponible;
            return Task.FromResult(true);
        }
    }

    public Task<Borde?> CrearBordeAsync(string nombre, decimal precio)
    {
        lock (_candado)
        {
            if (_bordes.Any(b => Igual(b.Nombre, nombre))) return Task.FromResult<Borde?>(null);
            var borde = new Borde { Id = _bordes.Count == 0 ? 1 : _bordes.Max(b => b.Id) + 1, Nombre = nombre, Precio = precio };
            _bordes.Add(borde);
            return Task.FromResult<Borde?>(borde);
        }
    }

    public Task<bool> ActualizarBordeAsync(int id, string nombre, decimal precio)
    {
        lock (_candado)
        {
            var borde = _bordes.FirstOrDefault(b => b.Id == id);
            if (borde is null || _bordes.Any(b => b.Id != id && Igual(b.Nombre, nombre))) return Task.FromResult(false);
            borde.Nombre = nombre;
            borde.Precio = precio;
            return Task.FromResult(true);
        }
    }

    public Task<Ingrediente?> CrearIngredienteAsync(string nombre, decimal precioExtra)
    {
        lock (_candado)
        {
            if (_ingredientes.Any(i => Igual(i.Nombre, nombre))) return Task.FromResult<Ingrediente?>(null);
            var ingrediente = new Ingrediente
            {
                Id = _ingredientes.Count == 0 ? 1 : _ingredientes.Max(i => i.Id) + 1,
                Nombre = nombre,
                PrecioExtra = precioExtra,
            };
            _ingredientes.Add(ingrediente);
            return Task.FromResult<Ingrediente?>(ingrediente);
        }
    }

    public Task<bool> ActualizarIngredienteAsync(int id, string nombre, decimal precioExtra)
    {
        lock (_candado)
        {
            var ingrediente = _ingredientes.FirstOrDefault(i => i.Id == id);
            if (ingrediente is null || _ingredientes.Any(i => i.Id != id && Igual(i.Nombre, nombre))) return Task.FromResult(false);
            ingrediente.Nombre = nombre;
            ingrediente.PrecioExtra = precioExtra;
            return Task.FromResult(true);
        }
    }

    public Task<Tamano?> CrearTamanoAsync(string nombre, decimal factorPrecio)
    {
        lock (_candado)
        {
            if (_tamanos.Any(t => Igual(t.Nombre, nombre))) return Task.FromResult<Tamano?>(null);
            var tamano = new Tamano { Id = _tamanos.Count == 0 ? 1 : _tamanos.Max(t => t.Id) + 1, Nombre = nombre, FactorPrecio = factorPrecio };
            _tamanos.Add(tamano);
            return Task.FromResult<Tamano?>(tamano);
        }
    }

    public Task<bool> ActualizarTamanoAsync(int id, string nombre, decimal factorPrecio)
    {
        lock (_candado)
        {
            var tamano = _tamanos.FirstOrDefault(t => t.Id == id);
            if (tamano is null || _tamanos.Any(t => t.Id != id && Igual(t.Nombre, nombre))) return Task.FromResult(false);
            tamano.Nombre = nombre;
            tamano.FactorPrecio = factorPrecio;
            return Task.FromResult(true);
        }
    }

    private readonly object _candado = new();

    private T Copia<T>(Func<T> lectura)
    {
        lock (_candado) return lectura();
    }

    private List<Ingrediente> IngredientesPorId(IEnumerable<int> ids) =>
        _ingredientes.Where(i => ids.Contains(i.Id)).ToList();

    private static bool Igual(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
