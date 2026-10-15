using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>
/// Pedidos en memoria. Se usa cuando no hay cadena de conexión a Supabase configurada,
/// para que la app no se caiga en modo local. Los datos se pierden al reiniciar.
/// </summary>
public class InMemoryPedidoRepository : IPedidoRepository
{
    private readonly List<Pedido> _pedidos = new();
    private readonly object _candado = new();
    private int _siguienteId = 1;

    public Task<Pedido> CrearPedidoAsync(Pedido pedido)
    {
        lock (_candado)
        {
            pedido.Id = _siguienteId++;
            pedido.FechaCreacion = DateTime.UtcNow;
            pedido.Estado = "Pendiente";
            _pedidos.Add(pedido);
        }
        return Task.FromResult(pedido);
    }

    public Task<Pedido?> ObtenerPorIdAsync(int id)
    {
        lock (_candado)
        {
            return Task.FromResult(_pedidos.FirstOrDefault(p => p.Id == id));
        }
    }
}
