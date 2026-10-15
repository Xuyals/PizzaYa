using System;
using System.Collections.Generic;

namespace PizzaYa.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string DireccionEntrega { get; set; } = string.Empty;
        public string Estado { get; set; } = "Pendiente"; // Pendiente, En Cocina, Listo, Entregado
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        public List<DetallePedidoItem> Items { get; set; } = new();
    }

    public class DetallePedidoItem
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public string NombrePizza { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}