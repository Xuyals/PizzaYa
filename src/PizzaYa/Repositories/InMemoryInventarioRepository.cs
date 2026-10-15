using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// Inventario en memoria con los mismos ingredientes de database/01_hu1_menu_pizzas.sql.
/// Se usa en las pruebas y cuando no hay cadena de conexión a Supabase configurada.
/// </summary>
public class InMemoryInventarioRepository : IInventarioRepository
{
    private readonly object _candado = new();
    private readonly List<IngredienteInventario> _ingredientes;
    private readonly List<PedidoProveedor> _pedidos = new();
    private int _siguienteId = 1;

    public InMemoryInventarioRepository(IEnumerable<IngredienteInventario> ingredientes)
    {
        _ingredientes = ingredientes.Select(Copiar).ToList();
    }

    /// <summary>Mismos ids que InMemoryMenuRepository (orden alfabético). Algunos quedan por agotarse.</summary>
    public static InMemoryInventarioRepository ConDatosDeEjemplo()
    {
        var datos = new (string Nombre, string Unidad, decimal Cantidad, decimal Minimo)[]
        {
            ("Aceitunas", "kg", 3m, 2m), ("Carne molida", "kg", 12m, 6m), ("Cebolla", "kg", 9m, 4m),
            ("Champiñones", "kg", 1.5m, 3m), ("Jalapeños", "kg", 4m, 2m), ("Jamón", "kg", 10m, 4m),
            ("Maíz tierno", "kg", 6m, 3m), ("Pepperoni", "kg", 2m, 5m), ("Pimentón", "kg", 5m, 2m),
            ("Piña", "kg", 7m, 3m), ("Pollo", "kg", 3m, 8m), ("Queso mozzarella", "kg", 4m, 10m),
            ("Salsa de tomate", "L", 12m, 5m), ("Tocineta", "kg", 4m, 2m),
        };

        return new InMemoryInventarioRepository(datos.Select((d, i) => new IngredienteInventario
        {
            Id = i + 1, Nombre = d.Nombre, Unidad = d.Unidad, Cantidad = d.Cantidad, CantidadMinima = d.Minimo,
        }));
    }

    public Task<List<IngredienteInventario>> ObtenerInventarioAsync()
    {
        lock (_candado)
        {
            return Task.FromResult(_ingredientes.Select(Copiar).OrderBy(i => i.Nombre).ToList());
        }
    }

    public Task<List<PedidoProveedor>> ObtenerPedidosAsync()
    {
        lock (_candado)
        {
            return Task.FromResult(_pedidos
                .OrderByDescending(p => p.CreadoEn).ThenByDescending(p => p.Id)
                .Select(Copiar).ToList());
        }
    }

    public Task<int> CrearPedidoAsync(string proveedor, IEnumerable<ItemPedido> items)
    {
        lock (_candado)
        {
            var pedido = new PedidoProveedor
            {
                Id = _siguienteId++,
                Proveedor = proveedor,
                Estado = EstadoPedidoProveedor.Pendiente,
                CreadoEn = DateTime.UtcNow,
                Items = items.Select(it =>
                {
                    var ing = _ingredientes.First(i => i.Id == it.IngredienteId);
                    return new PedidoProveedorItem
                    {
                        IngredienteId = ing.Id,
                        IngredienteNombre = ing.Nombre,
                        Unidad = ing.Unidad,
                        Cantidad = it.Cantidad,
                    };
                }).ToList(),
            };
            _pedidos.Add(pedido);
            return Task.FromResult(pedido.Id);
        }
    }

    public Task<bool> RecibirPedidoAsync(int pedidoId)
    {
        lock (_candado)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == pedidoId);
            if (pedido is null || pedido.Estado != EstadoPedidoProveedor.Pendiente)
                return Task.FromResult(false);

            foreach (var item in pedido.Items)
            {
                var ing = _ingredientes.FirstOrDefault(i => i.Id == item.IngredienteId);
                if (ing is not null) ing.Cantidad += item.Cantidad;
            }
            pedido.Estado = EstadoPedidoProveedor.Recibido;
            pedido.RecibidoEn = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    private static IngredienteInventario Copiar(IngredienteInventario i) => new()
    {
        Id = i.Id, Nombre = i.Nombre, Unidad = i.Unidad, Cantidad = i.Cantidad, CantidadMinima = i.CantidadMinima,
    };

    private static PedidoProveedor Copiar(PedidoProveedor p) => new()
    {
        Id = p.Id, Proveedor = p.Proveedor, Estado = p.Estado, CreadoEn = p.CreadoEn, RecibidoEn = p.RecibidoEn,
        Items = p.Items.Select(it => new PedidoProveedorItem
        {
            IngredienteId = it.IngredienteId, IngredienteNombre = it.IngredienteNombre,
            Unidad = it.Unidad, Cantidad = it.Cantidad,
        }).ToList(),
    };
}
