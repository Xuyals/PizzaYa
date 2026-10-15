namespace PizzaYa.Models;

public class Pizza
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public decimal PrecioBase { get; set; }
    public string? ImagenUrl { get; set; }

    /// <summary>false = oculta del menú del cliente (HU7). El menú de los clientes solo trae las disponibles.</summary>
    public bool Disponible { get; set; } = true;

    public List<Ingrediente> IngredientesBase { get; set; } = new();
}

public class Ingrediente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioExtra { get; set; }
}

public class Tamano
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal FactorPrecio { get; set; }
}

public class Borde
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Precio { get; set; }
}
