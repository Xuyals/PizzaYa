namespace PizzaYa.Models;

/// <summary>
/// Estados de un pedido (HU4). El orden de <see cref="Todos"/> es el único orden permitido:
/// el estado solo avanza, nunca se salta ni retrocede.
/// </summary>
public static class EstadoPedido
{
    public const string Recibido = "Recibido";
    public const string EnPreparacion = "En preparación";
    public const string EnEntrega = "En entrega";
    public const string Entregado = "Entregado";

    public static readonly IReadOnlyList<string> Todos =
        new[] { Recibido, EnPreparacion, EnEntrega, Entregado };
}

public static class MetodoPagoPedido
{
    public const string AntesDeLaEntrega = "Antes de la entrega";
    public const string AlRecoger = "Al recoger en tienda";
}

/// <summary>
/// Pedido confirmado. Es la fila de la tabla <c>pedidos</c> de database/02_hu2_hu3_precios_pedidos.sql:
/// lo crea el carrito (HU3) y lo consulta y avanza el módulo de pedidos (HU4).
/// </summary>
public class Pedido
{
    public int Id { get; set; }
    public string NumeroPedido { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string Telefono { get; set; } = "";
    public bool EsDomicilio { get; set; }
    public string? Direccion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal CostoDomicilio { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = MetodoPagoPedido.AntesDeLaEntrega;
    public string Estado { get; set; } = EstadoPedido.Recibido;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    /// <summary>Cliente registrado dueño del pedido (HU6); null si confirmó sin iniciar sesión.</summary>
    public int? UsuarioId { get; set; }

    public List<PedidoItem> Items { get; set; } = new();
}

/// <summary>Una pizza del pedido (fila de <c>pedido_items</c>).</summary>
public class PedidoItem
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int PizzaId { get; set; }
    public int TamanoId { get; set; }
    public int BordeId { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public string NombrePizza { get; set; } = "";
    public string NombreTamano { get; set; } = "";
    public string NombreBorde { get; set; } = "";

    // Ingredientes de esta pizza. Los ids salen de pedido_item_extras / pedido_item_quitados;
    // los nombres se rellenan al leer, para mostrarlos en el panel de cocina.
    public List<int> ExtrasIds { get; set; } = new();
    public List<int> QuitadosIds { get; set; } = new();
    public List<string> ExtrasNombres { get; set; } = new();
    public List<string> QuitadosNombres { get; set; } = new();
    public List<string> IngredientesBase { get; set; } = new();

    public decimal Subtotal => PrecioUnitario * Cantidad;
}
