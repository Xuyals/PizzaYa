using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface IInventarioRepository
{
    /// <summary>Todos los ingredientes con su cantidad disponible y mínima.</summary>
    Task<List<IngredienteInventario>> ObtenerInventarioAsync();

    /// <summary>Pedidos a proveedores con sus ingredientes, del más reciente al más antiguo.</summary>
    Task<List<PedidoProveedor>> ObtenerPedidosAsync();

    /// <summary>Guarda un pedido en estado "Pendiente" y devuelve su id.</summary>
    Task<int> CrearPedidoAsync(string proveedor, IEnumerable<ItemPedido> items);

    /// <summary>
    /// Marca el pedido como "Recibido" y suma sus cantidades al inventario, todo en un solo paso.
    /// Devuelve false si el pedido no existe o ya no está "Pendiente" (no suma dos veces).
    /// </summary>
    Task<bool> RecibirPedidoAsync(int pedidoId);
}
