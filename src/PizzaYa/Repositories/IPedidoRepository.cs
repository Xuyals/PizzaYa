using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface IPedidoRepository
{
    /// <summary>Guarda el pedido con sus pizzas y devuelve el pedido con su id.</summary>
    Task<Pedido> CrearPedidoAsync(Pedido pedido);

    Task<Pedido?> ObtenerPorIdAsync(int id);

    /// <summary>Busca por el número que ve el cliente (por ejemplo PZ-20261014-A3F7).</summary>
    Task<Pedido?> ObtenerPorNumeroAsync(string numeroPedido);

    /// <summary>Todos los pedidos, del más antiguo al más reciente.</summary>
    Task<List<Pedido>> ObtenerTodosAsync();

    /// <summary>
    /// Cambia el estado solo si el pedido sigue en <paramref name="estadoEsperado"/>
    /// (así dos cocineros no se pisan). Devuelve false si el pedido no existe o ya cambió.
    /// </summary>
    Task<bool> ActualizarEstadoAsync(int id, string estadoEsperado, string estadoNuevo);
}
