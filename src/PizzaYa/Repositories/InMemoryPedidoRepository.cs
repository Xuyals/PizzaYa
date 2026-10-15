using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// Pedidos en memoria. Se usa en las pruebas y cuando no hay cadena de conexión a Supabase,
/// para que la app no se caiga en modo local. Los datos se pierden al reiniciar.
/// </summary>
public class InMemoryPedidoRepository : IPedidoRepository
{
    private readonly List<Pedido> _pedidos = new();
    private readonly object _candado = new();
    private int _siguienteId = 1;
    private int _siguienteItemId = 1;

    public Task<Pedido> CrearPedidoAsync(Pedido pedido)
    {
        lock (_candado)
        {
            pedido.Id = _siguienteId++;
            foreach (var item in pedido.Items)
            {
                item.Id = _siguienteItemId++;
                item.PedidoId = pedido.Id;
            }
            _pedidos.Add(pedido);
        }
        return Task.FromResult(pedido);
    }

    public Task<Pedido?> ObtenerPorIdAsync(int id)
    {
        lock (_candado)
            return Task.FromResult(_pedidos.FirstOrDefault(p => p.Id == id));
    }

    public Task<Pedido?> ObtenerPorNumeroAsync(string numeroPedido)
    {
        lock (_candado)
            return Task.FromResult(_pedidos.FirstOrDefault(p =>
                string.Equals(p.NumeroPedido, numeroPedido?.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    public Task<List<Pedido>> ObtenerTodosAsync()
    {
        lock (_candado)
            return Task.FromResult(_pedidos.OrderBy(p => p.CreadoEn).ThenBy(p => p.Id).ToList());
    }

    public Task<bool> ActualizarEstadoAsync(int id, string estadoEsperado, string estadoNuevo)
    {
        lock (_candado)
        {
            var pedido = _pedidos.FirstOrDefault(p => p.Id == id);
            if (pedido is null || pedido.Estado != estadoEsperado) return Task.FromResult(false);
            pedido.Estado = estadoNuevo;
            return Task.FromResult(true);
        }
    }
}
