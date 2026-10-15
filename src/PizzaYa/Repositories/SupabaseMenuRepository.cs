using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Lee el menú desde la base de datos Postgres de Supabase.</summary>
public class SupabaseMenuRepository : IMenuRepository
{
    private readonly NpgsqlDataSource _db;

    public SupabaseMenuRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    public async Task<List<Pizza>> ObtenerPizzasAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        var pizzas = (await conn.QueryAsync<Pizza>(
            """
            select id, nombre, descripcion, precio_base, imagen_url,
                   (nombre = 'Arma tu pizza') as es_pizza_personalizada
            from pizzas
            where disponible
            order by nombre
            """)).ToList();

        var ingredientes = await conn.QueryAsync<IngredienteDePizza>(
            """
            select pi.pizza_id, i.id, i.nombre, i.precio_extra
            from pizza_ingredientes pi
            join ingredientes i on i.id = pi.ingrediente_id
            join pizzas p on p.id = pi.pizza_id
            where p.disponible
            order by i.nombre
            """);

        var porPizza = ingredientes.ToLookup(i => i.PizzaId);
        foreach (var pizza in pizzas)
        {
            pizza.IngredientesBase = porPizza[pizza.Id]
                .Select(i => new Ingrediente { Id = i.Id, Nombre = i.Nombre, PrecioExtra = i.PrecioExtra })
                .ToList();
        }
        return pizzas;
    }

    public async Task<Pizza?> ObtenerPizzaAsync(int id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        var pizza = await conn.QuerySingleOrDefaultAsync<Pizza>(
            """
            select id, nombre, descripcion, precio_base, imagen_url,
                   (nombre = 'Arma tu pizza') as es_pizza_personalizada
            from pizzas
            where id = @id and disponible
            """, new { id });
        if (pizza is null) return null;

        pizza.IngredientesBase = (await conn.QueryAsync<Ingrediente>(
            """
            select i.id, i.nombre, i.precio_extra
            from pizza_ingredientes pi
            join ingredientes i on i.id = pi.ingrediente_id
            where pi.pizza_id = @id
            order by i.nombre
            """, new { id })).ToList();
        return pizza;
    }

    public async Task<List<Tamano>> ObtenerTamanosAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        return (await conn.QueryAsync<Tamano>(
            "select id, nombre, factor_precio from tamanos order by orden, id")).ToList();
    }

    public async Task<List<Borde>> ObtenerBordesAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        return (await conn.QueryAsync<Borde>(
            "select id, nombre, precio from bordes order by orden, id")).ToList();
    }

    public async Task<List<Ingrediente>> ObtenerIngredientesAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        return (await conn.QueryAsync<Ingrediente>(
            "select id, nombre, precio_extra from ingredientes where disponible order by nombre")).ToList();
    }

    private class IngredienteDePizza
    {
        public int PizzaId { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal PrecioExtra { get; set; }
    }
}
