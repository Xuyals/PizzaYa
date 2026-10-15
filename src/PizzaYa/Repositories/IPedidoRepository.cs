using PizzaYa.Models;

namespace PizzaYa.Repositories
{
    public interface IPedidoRepository
    {
        Task<Pedido> CrearPedidoAsync(Pedido pedido);
        Task<Pedido?> ObtenerPorIdAsync(int id);
    }
}