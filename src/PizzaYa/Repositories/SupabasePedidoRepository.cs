using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Pedidos en Supabase (tablas de database/02_hu2_hu3_precios_pedidos.sql).</summary>
public class SupabasePedidoRepository : IPedidoRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SupabasePedidoRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<Pedido> CrearPedidoAsync(Pedido pedido)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        await using var tx = await conexion.BeginTransactionAsync();

        pedido.Id = await conexion.ExecuteScalarAsync<int>(
            @"insert into pedidos (numero_pedido, nombre_cliente, telefono, es_domicilio, direccion,
                                   subtotal, costo_domicilio, descuento, total, estado, creado_en, usuario_id)
              values (@NumeroPedido, @NombreCliente, @Telefono, @EsDomicilio, @Direccion,
                      @Subtotal, @CostoDomicilio, @Descuento, @Total, @Estado, @CreadoEn, @UsuarioId)
              returning id;", pedido, tx);

        foreach (var item in pedido.Items)
        {
            item.PedidoId = pedido.Id;
            item.Id = await conexion.ExecuteScalarAsync<int>(
                @"insert into pedido_items (pedido_id, pizza_id, tamano_id, borde_id, cantidad, precio_unitario,
                                            nombre_pizza, nombre_tamano, nombre_borde)
                  values (@PedidoId, @PizzaId, @TamanoId, @BordeId, @Cantidad, @PrecioUnitario,
                          @NombrePizza, @NombreTamano, @NombreBorde)
                  returning id;", item, tx);

            foreach (var ingredienteId in item.ExtrasIds.Distinct())
                await conexion.ExecuteAsync(
                    "insert into pedido_item_extras (pedido_item_id, ingrediente_id) values (@Item, @Ing) on conflict do nothing;",
                    new { Item = item.Id, Ing = ingredienteId }, tx);

            foreach (var ingredienteId in item.QuitadosIds.Distinct())
                await conexion.ExecuteAsync(
                    "insert into pedido_item_quitados (pedido_item_id, ingrediente_id) values (@Item, @Ing) on conflict do nothing;",
                    new { Item = item.Id, Ing = ingredienteId }, tx);
        }

        await tx.CommitAsync();
        return pedido;
    }

    public async Task<Pedido?> ObtenerPorIdAsync(int id)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var pedido = await conexion.QuerySingleOrDefaultAsync<Pedido>(
            "select * from pedidos where id = @Id;", new { Id = id });
        if (pedido is null) return null;
        await CargarItemsAsync(conexion, new List<Pedido> { pedido });
        return pedido;
    }

    public async Task<Pedido?> ObtenerPorNumeroAsync(string numeroPedido)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var pedido = await conexion.QuerySingleOrDefaultAsync<Pedido>(
            "select * from pedidos where upper(numero_pedido) = upper(@Numero);",
            new { Numero = numeroPedido?.Trim() });
        if (pedido is null) return null;
        await CargarItemsAsync(conexion, new List<Pedido> { pedido });
        return pedido;
    }

    public async Task<Pedido?> ObtenerUltimoPorUsuarioAsync(int usuarioId)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var pedido = await conexion.QueryFirstOrDefaultAsync<Pedido>(
            "select * from pedidos where usuario_id = @UsuarioId order by creado_en desc, id desc limit 1;",
            new { UsuarioId = usuarioId });
        if (pedido is null) return null;
        await CargarItemsAsync(conexion, new List<Pedido> { pedido });
        return pedido;
    }

    public async Task<List<Pedido>> ObtenerTodosAsync()
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var pedidos = (await conexion.QueryAsync<Pedido>(
            "select * from pedidos order by creado_en, id;")).ToList();
        await CargarItemsAsync(conexion, pedidos);
        return pedidos;
    }

    public async Task<bool> ActualizarEstadoAsync(int id, string estadoEsperado, string estadoNuevo)
    {
        await using var conexion = await _dataSource.OpenConnectionAsync();
        var filas = await conexion.ExecuteAsync(
            "update pedidos set estado = @Nuevo where id = @Id and estado = @Esperado;",
            new { Id = id, Esperado = estadoEsperado, Nuevo = estadoNuevo });
        return filas > 0;
    }

    /// <summary>Rellena las pizzas de cada pedido, con sus extras, quitados e ingredientes base.</summary>
    private static async Task CargarItemsAsync(NpgsqlConnection conexion, List<Pedido> pedidos)
    {
        if (pedidos.Count == 0) return;
        var pedidoIds = pedidos.Select(p => p.Id).ToArray();

        var items = (await conexion.QueryAsync<PedidoItem>(
            "select * from pedido_items where pedido_id = any(@Ids) order by id;",
            new { Ids = pedidoIds })).ToList();
        var itemIds = items.Select(i => i.Id).ToArray();
        var pizzaIds = items.Select(i => i.PizzaId).Distinct().ToArray();

        var extras = (await conexion.QueryAsync<(int PedidoItemId, int IngredienteId, string Nombre)>(
            @"select e.pedido_item_id, e.ingrediente_id, i.nombre
              from pedido_item_extras e join ingredientes i on i.id = e.ingrediente_id
              where e.pedido_item_id = any(@Ids);", new { Ids = itemIds })).ToList();
        var quitados = (await conexion.QueryAsync<(int PedidoItemId, int IngredienteId, string Nombre)>(
            @"select q.pedido_item_id, q.ingrediente_id, i.nombre
              from pedido_item_quitados q join ingredientes i on i.id = q.ingrediente_id
              where q.pedido_item_id = any(@Ids);", new { Ids = itemIds })).ToList();
        var bases = (await conexion.QueryAsync<(int PizzaId, string Nombre)>(
            @"select pi.pizza_id, i.nombre
              from pizza_ingredientes pi join ingredientes i on i.id = pi.ingrediente_id
              where pi.pizza_id = any(@Ids) order by i.nombre;", new { Ids = pizzaIds })).ToList();

        foreach (var item in items)
        {
            item.ExtrasIds = extras.Where(e => e.PedidoItemId == item.Id).Select(e => e.IngredienteId).ToList();
            item.ExtrasNombres = extras.Where(e => e.PedidoItemId == item.Id).Select(e => e.Nombre).ToList();
            item.QuitadosIds = quitados.Where(q => q.PedidoItemId == item.Id).Select(q => q.IngredienteId).ToList();
            item.QuitadosNombres = quitados.Where(q => q.PedidoItemId == item.Id).Select(q => q.Nombre).ToList();
            item.IngredientesBase = bases.Where(b => b.PizzaId == item.PizzaId).Select(b => b.Nombre).ToList();
        }

        foreach (var pedido in pedidos)
            pedido.Items = items.Where(i => i.PedidoId == pedido.Id).ToList();
    }
}
