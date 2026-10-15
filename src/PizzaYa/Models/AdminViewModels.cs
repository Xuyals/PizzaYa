namespace PizzaYa.Models;

/// <summary>HU7 – Todo lo que el administrador ve en /Admin.</summary>
public class AdminMenuViewModel
{
    public List<Pizza> Pizzas { get; set; } = new();
    public List<Borde> Bordes { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public List<Tamano> Tamanos { get; set; } = new();
    public decimal ValorDomicilio { get; set; }
}

/// <summary>HU7 – Formulario para registrar o editar una pizza. Los números llegan como texto para validarlos en español.</summary>
public class PizzaFormViewModel
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string? PrecioBase { get; set; }
    public List<int> IngredientesBaseIds { get; set; } = new();

    /// <summary>Todos los ingredientes, para escoger los de la pizza.</summary>
    public List<Ingrediente> Ingredientes { get; set; } = new();

    public bool EsNueva => Id is null;
}
