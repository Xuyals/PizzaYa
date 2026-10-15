using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Administración del menú en Supabase (tablas de database/01_hu1_menu_pizzas.sql).</summary>
public class SupabaseMenuAdminRepository : IMenuAdminRepository
{
    private const string ViolacionDeUnicidad = "23505";

    private readonly NpgsqlDataSource _db;

    public SupabaseMenuAdminRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    private class IngredienteDePizza
    {
        public int PizzaId { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public decimal PrecioExtra { get; set; }
    }

    public async Task<List<Pizza>> ObtenerTodasLasPizzasAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        var pizzas = (await conn.QueryAsync<Pizza>(
            "select id, nombre, descripcion, precio_base, imagen_url, disponible from pizzas order by nombre")).ToList();
        var ingredientes = (await conn.QueryAsync<IngredienteDePizza>(
            """
            select pi.pizza_id, i.id, i.nombre, i.precio_extra
            from pizza_ingredientes pi join ingredientes i on i.id = pi.ingrediente_id
            order by i.nombre
            """)).ToLookup(i => i.PizzaId);
        foreach (var pizza in pizzas)
            pizza.IngredientesBase = ingredientes[pizza.Id]
                .Select(i => new Ingrediente { Id = i.Id, Nombre = i.Nombre, PrecioExtra = i.PrecioExtra }).ToList();
        return pizzas;
    }

    public async Task<Pizza?> ObtenerPizzaParaEditarAsync(int id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        var pizza = await conn.QuerySingleOrDefaultAsync<Pizza>(
            "select id, nombre, descripcion, precio_base, imagen_url, disponible from pizzas where id = @id", new { id });
        if (pizza is null) return null;
        pizza.IngredientesBase = (await conn.QueryAsync<Ingrediente>(
            """
            select i.id, i.nombre, i.precio_extra
            from pizza_ingredientes pi join ingredientes i on i.id = pi.ingrediente_id
            where pi.pizza_id = @id order by i.nombre
            """, new { id })).ToList();
        return pizza;
    }

    public async Task<Pizza?> CrearPizzaAsync(Pizza pizza, IEnumerable<int> ingredientesBaseIds)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var id = await conn.ExecuteScalarAsync<int?>(
            """
            insert into pizzas (nombre, descripcion, precio_base, imagen_url, disponible)
            values (@Nombre, @Descripcion, @PrecioBase, @ImagenUrl, @Disponible)
            on conflict (nombre) do nothing
            returning id
            """, pizza, tx);
        if (id is null) return null;

        pizza.Id = id.Value;
        await GuardarIngredientesBaseAsync(conn, tx, pizza.Id, ingredientesBaseIds);
        await tx.CommitAsync();
        return pizza;
    }

    public async Task<bool> ActualizarPizzaAsync(int id, string nombre, string? descripcion, decimal precioBase, IEnumerable<int> ingredientesBaseIds)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            var filas = await conn.ExecuteAsync(
                "update pizzas set nombre = @nombre, descripcion = @descripcion, precio_base = @precioBase where id = @id",
                new { id, nombre, descripcion, precioBase }, tx);
            if (filas == 0) return false;

            await conn.ExecuteAsync("delete from pizza_ingredientes where pizza_id = @id", new { id }, tx);
            await GuardarIngredientesBaseAsync(conn, tx, id, ingredientesBaseIds);
            await tx.CommitAsync();
            return true;
        }
        catch (PostgresException ex) when (ex.SqlState == ViolacionDeUnicidad)
        {
            return false; // ya hay otra pizza con ese nombre
        }
    }

    private static Task GuardarIngredientesBaseAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int pizzaId, IEnumerable<int> ids) =>
        conn.ExecuteAsync(
            "insert into pizza_ingredientes (pizza_id, ingrediente_id) select @pizzaId, unnest(@ids) on conflict do nothing",
            new { pizzaId, ids = ids.Distinct().ToArray() }, tx);

    public async Task<bool> CambiarDisponibilidadPizzaAsync(int id, bool disponible)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.ExecuteAsync(
            "update pizzas set disponible = @disponible where id = @id", new { id, disponible }) > 0;
    }

    public async Task<Borde?> CrearBordeAsync(string nombre, decimal precio)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Borde>(
            """
            insert into bordes (nombre, precio, orden)
            values (@nombre, @precio, (select coalesce(max(orden), 0) + 1 from bordes))
            on conflict (nombre) do nothing
            returning id, nombre, precio
            """, new { nombre, precio });
    }

    public Task<bool> ActualizarBordeAsync(int id, string nombre, decimal precio) =>
        ActualizarAsync("update bordes set nombre = @nombre, precio = @precio where id = @id", new { id, nombre, precio });

    public async Task<Ingrediente?> CrearIngredienteAsync(string nombre, decimal precioExtra)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Ingrediente>(
            """
            insert into ingredientes (nombre, precio_extra)
            values (@nombre, @precioExtra)
            on conflict (nombre) do nothing
            returning id, nombre, precio_extra
            """, new { nombre, precioExtra });
    }

    public Task<bool> ActualizarIngredienteAsync(int id, string nombre, decimal precioExtra) =>
        ActualizarAsync("update ingredientes set nombre = @nombre, precio_extra = @precioExtra where id = @id", new { id, nombre, precioExtra });

    public async Task<Tamano?> CrearTamanoAsync(string nombre, decimal factorPrecio)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<Tamano>(
            """
            insert into tamanos (nombre, factor_precio, orden)
            values (@nombre, @factorPrecio, (select coalesce(max(orden), 0) + 1 from tamanos))
            on conflict (nombre) do nothing
            returning id, nombre, factor_precio
            """, new { nombre, factorPrecio });
    }

    public Task<bool> ActualizarTamanoAsync(int id, string nombre, decimal factorPrecio) =>
        ActualizarAsync("update tamanos set nombre = @nombre, factor_precio = @factorPrecio where id = @id", new { id, nombre, factorPrecio });

    private async Task<bool> ActualizarAsync(string sql, object parametros)
    {
        await using var conn = await _db.OpenConnectionAsync();
        try
        {
            return await conn.ExecuteAsync(sql, parametros) > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == ViolacionDeUnicidad)
        {
            return false;
        }
    }
}
