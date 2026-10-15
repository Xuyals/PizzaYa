namespace PizzaYa.Models;

public class MenuViewModel
{
    public List<Pizza> Pizzas { get; set; } = new();
    public bool HayPizzas => Pizzas.Count > 0;
}

public class PersonalizarViewModel
{
    public Pizza Pizza { get; set; } = new();
    public List<Tamano> Tamanos { get; set; } = new();
    public List<Borde> Bordes { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public PizzaPersonalizada Seleccion { get; set; } = new();
}

public class ResumenPizzaViewModel
{
    public Pizza Pizza { get; set; } = new();
    public Tamano Tamano { get; set; } = new();
    public Borde Borde { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public List<Ingrediente> Quitados { get; set; } = new();
}
