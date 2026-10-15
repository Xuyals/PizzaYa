namespace PizzaYa.Models;

/// <summary>
/// Representa un ítem dentro del carrito de compras (HU3).
/// </summary>
public class ItemCarrito
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Pizza personalizada tal como la armó el cliente (de HU1).</summary>
    public PizzaPersonalizada Seleccion { get; set; } = new();

    // ---- Datos desnormalizados para mostrar sin ir a BD ----
    public string NombrePizza { get; set; } = "";
    public string NombreTamano { get; set; } = "";
    public string NombreBorde { get; set; } = "";
    public List<string> NombresExtras { get; set; } = new();

    /// <summary>Precio unitario ya calculado (sin domicilio ni cupón).</summary>
    public decimal PrecioUnitario { get; set; }

    public int Cantidad { get; set; } = 1;

    public decimal Subtotal => PrecioUnitario * Cantidad;
}

/// <summary>
/// Carrito de compras completo. Se serializa en la sesión HTTP.
/// </summary>
public class Carrito
{
    public const string ClaveSesion = "Carrito";

    public List<ItemCarrito> Items { get; set; } = new();

    public bool EstaVacio => Items.Count == 0;

    public decimal Subtotal => Items.Sum(i => i.Subtotal);
}
