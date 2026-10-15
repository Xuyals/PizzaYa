using Dapper;
using Npgsql;
using PizzaYa.Models;

namespace PizzaYa.Repositories
{
    public class SupabasePedidoRepository : IPedidoRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public SupabasePedidoRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<Pedido> CrearPedidoAsync(Pedido pedido)
        {
            using var connection = _dataSource.OpenConnection();
            string query = @"INSERT INTO pedidos (cliente_nombre, direccion_entrega, fecha_creacion, estado) 
                             VALUES (@ClienteNombre, @DireccionEntrega, @FechaCreacion, @Estado) 
                             RETURNING *;";
            
            pedido.FechaCreacion = DateTime.UtcNow;
            pedido.Estado = "Pendiente";

            var pedidoCreado = await connection.QuerySingleOrDefaultAsync<Pedido>(query, pedido);
            return pedidoCreado ?? pedido;
        }

        public async Task<Pedido?> ObtenerPorIdAsync(int id)
        {
            using var connection = _dataSource.OpenConnection();
            string query = "SELECT * FROM pedidos WHERE id = @Id;";
            
            return await connection.QuerySingleOrDefaultAsync<Pedido>(query, new { Id = id });
        }
    }
}