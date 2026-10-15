using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Inventario y pedidos a proveedores sobre la base de datos Postgres de Supabase.</summary>
public class SupabaseInventarioRepository : IInventarioRepository
{
    private readonly NpgsqlDataSource _db;

    public SupabaseInventarioRepository(NpgsqlDataSource db)
    {
        _db = db;
    }

    public async Task<List<IngredienteInventario>> ObtenerInventarioAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        return (await conn.QueryAsync<IngredienteInventario>(
            """
            select id, nombre, unidad, cantidad, cantidad_minima
            from ingredientes
            order by nombre
            """)).ToList();
    }

    public async Task<List<PedidoProveedor>> ObtenerPedidosAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        var pedidos = (await conn.QueryAsync<PedidoProveedor>(
            """
            select id, proveedor, estado, creado_en, recibido_en
            from pedidos_proveedor
            order by creado_en desc, id desc
            """)).ToList();

        var items = await conn.QueryAsync<ItemDePedido>(
            """
            select it.pedido_id, it.ingrediente_id, i.nombre as ingrediente_nombre, i.unidad, it.cantidad
            from pedido_proveedor_items it
            join ingredientes i on i.id = it.ingrediente_id
            order by i.nombre
            """);

        var porPedido = items.ToLookup(x => x.PedidoId);
        foreach (var pedido in pedidos)
        {
            pedido.Items = porPedido[pedido.Id]
                .Select(x => new PedidoProveedorItem
                {
                    IngredienteId = x.IngredienteId,
                    IngredienteNombre = x.IngredienteNombre,
                    Unidad = x.Unidad,
                    Cantidad = x.Cantidad,
                })
                .ToList();
        }
        return pedidos;
    }

    public async Task<int> CrearPedidoAsync(string proveedor, IEnumerable<ItemPedido> items)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var id = await conn.ExecuteScalarAsync<int>(
            "insert into pedidos_proveedor (proveedor) values (@proveedor) returning id",
            new { proveedor }, tx);

        foreach (var item in items)
        {
            await conn.ExecuteAsync(
                """
                insert into pedido_proveedor_items (pedido_id, ingrediente_id, cantidad)
                values (@id, @ingredienteId, @cantidad)
                """,
                new { id, ingredienteId = item.IngredienteId, cantidad = item.Cantidad }, tx);
        }

        await tx.CommitAsync();
        return id;
    }

    public async Task<bool> RecibirPedidoAsync(int pedidoId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Solo cambia si sigue "Pendiente": así un doble clic no suma dos veces.
        var cambiados = await conn.ExecuteAsync(
            """
            update pedidos_proveedor
            set estado = 'Recibido', recibido_en = now()
            where id = @pedidoId and estado = 'Pendiente'
            """, new { pedidoId }, tx);

        if (cambiados == 0)
        {
            await tx.RollbackAsync();
            return false;
        }

        await conn.ExecuteAsync(
            """
            update ingredientes i
            set cantidad = i.cantidad + it.cantidad
            from pedido_proveedor_items it
            where it.pedido_id = @pedidoId and it.ingrediente_id = i.id
            """, new { pedidoId }, tx);

        await tx.CommitAsync();
        return true;
    }

    private class ItemDePedido
    {
        public int PedidoId { get; set; }
        public int IngredienteId { get; set; }
        public string IngredienteNombre { get; set; } = "";
        public string Unidad { get; set; } = "";
        public decimal Cantidad { get; set; }
    }
}
