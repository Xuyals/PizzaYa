namespace PizzaYa.Models;

public class MenuViewModel
{
    public List<Pizza> Pizzas { get; set; } = new();
    public bool HayPizzas => Pizzas.Count > 0;
    public Pizza? PizzaParaArmar => Pizzas.FirstOrDefault(p => p.EsPizzaPersonalizada);
    public List<Pizza> Especialidades => Pizzas.Where(p => !p.EsPizzaPersonalizada).ToList();

    /// <summary>HU9: ids de las pizzas con algún ingrediente base agotado; se muestran como "No disponible".</summary>
    public HashSet<int> PizzasSinStock { get; set; } = new();
}

public class PersonalizarViewModel
{
    public Pizza Pizza { get; set; } = new();
    public List<Tamano> Tamanos { get; set; } = new();
    public List<Borde> Bordes { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public PizzaPersonalizada Seleccion { get; set; } = new();

    /// <summary>HU9: ids de los extras agotados; no se pueden escoger.</summary>
    public HashSet<int> ExtrasAgotados { get; set; } = new();
}

public class ResumenPizzaViewModel
{
    public Pizza Pizza { get; set; } = new();
    public Tamano Tamano { get; set; } = new();
    public Borde Borde { get; set; } = new();
    public List<Ingrediente> Extras { get; set; } = new();
    public List<Ingrediente> Quitados { get; set; } = new();
}
