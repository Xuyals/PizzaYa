namespace PizzaYa.Models;

/// <summary>Un ingrediente visto desde el inventario (HU5, Equipo 4).</summary>
public class IngredienteInventario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Unidad { get; set; } = "kg";
    public decimal Cantidad { get; set; }
    public decimal CantidadMinima { get; set; }

    /// <summary>"Por agotarse": la cantidad está por debajo de la mínima (igual al mínimo todavía no cuenta).</summary>
    public bool PorAgotarse => Cantidad < CantidadMinima;
}

public static class EstadoPedidoProveedor
{
    public const string Pendiente = "Pendiente";
    public const string Recibido = "Recibido";
}

public class PedidoProveedor
{
    public int Id { get; set; }
    public string Proveedor { get; set; } = "";
    public string Estado { get; set; } = EstadoPedidoProveedor.Pendiente;
    public DateTime CreadoEn { get; set; }
    public DateTime? RecibidoEn { get; set; }
    public List<PedidoProveedorItem> Items { get; set; } = new();
}

public class PedidoProveedorItem
{
    public int IngredienteId { get; set; }
    public string IngredienteNombre { get; set; } = "";
    public string Unidad { get; set; } = "";
    public decimal Cantidad { get; set; }
}

/// <summary>Línea ya validada que se guarda en el pedido.</summary>
public record ItemPedido(int IngredienteId, decimal Cantidad);

/// <summary>Lo que llega del formulario "Nuevo pedido".</summary>
public class NuevoPedidoProveedor
{
    public string? Proveedor { get; set; }
    public List<LineaPedido> Lineas { get; set; } = new();
}

/// <summary>Una fila del formulario. Cantidad vacía = ese ingrediente no se pide.</summary>
public class LineaPedido
{
    public int? IngredienteId { get; set; }
    public decimal? Cantidad { get; set; }
}

public class InventarioViewModel
{
    public List<IngredienteInventario> Ingredientes { get; set; } = new();
    public List<PedidoProveedor> Pedidos { get; set; } = new();
    public int PorAgotarse => Ingredientes.Count(i => i.PorAgotarse);
}

public class NuevoPedidoViewModel
{
    public List<IngredienteInventario> Ingredientes { get; set; } = new();
    public NuevoPedidoProveedor Pedido { get; set; } = new();
}
