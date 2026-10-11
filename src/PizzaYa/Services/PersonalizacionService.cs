using PizzaYa.Models;

namespace PizzaYa.Services;

/// <summary>Reglas de negocio de la HU1 para armar una pizza.</summary>
public class PersonalizacionService
{
    /// <summary>
    /// Valida la pizza armada contra el menú. Devuelve los errores por campo;
    /// si la lista está vacía la pizza es válida.
    /// </summary>
    public List<(string Campo, string Mensaje)> Validar(
        PizzaPersonalizada seleccion,
        Pizza pizza,
        IEnumerable<Tamano> tamanos,
        IEnumerable<Borde> bordes,
        IEnumerable<Ingrediente> ingredientes)
    {
        var errores = new List<(string, string)>();

        if (seleccion.TamanoId is null)
            errores.Add((nameof(seleccion.TamanoId), "Escoge un tamaño para continuar."));
        else if (!tamanos.Any(t => t.Id == seleccion.TamanoId))
            errores.Add((nameof(seleccion.TamanoId), "El tamaño escogido no existe."));

        if (seleccion.BordeId is null)
            errores.Add((nameof(seleccion.BordeId), "Escoge un borde."));
        else if (!bordes.Any(b => b.Id == seleccion.BordeId))
            errores.Add((nameof(seleccion.BordeId), "El borde escogido no existe."));

        var extras = seleccion.ExtrasIds.Distinct().ToList();
        if (extras.Count > PizzaPersonalizada.MaximoExtras)
            errores.Add((nameof(seleccion.ExtrasIds),
                $"Puedes agregar máximo {PizzaPersonalizada.MaximoExtras} ingredientes extra."));
        var idsIngredientes = ingredientes.Select(i => i.Id).ToHashSet();
        if (extras.Any(id => !idsIngredientes.Contains(id)))
            errores.Add((nameof(seleccion.ExtrasIds), "Algún ingrediente extra no está disponible."));

        var idsBase = pizza.IngredientesBase.Select(i => i.Id).ToHashSet();
        if (seleccion.QuitadosIds.Any(id => !idsBase.Contains(id)))
            errores.Add((nameof(seleccion.QuitadosIds), "Solo se pueden quitar ingredientes que traiga la pizza."));

        return errores;
    }

    /// <summary>Quita repetidos para que la selección quede limpia antes de guardarla.</summary>
    public PizzaPersonalizada Normalizar(PizzaPersonalizada seleccion) => new()
    {
        PizzaId = seleccion.PizzaId,
        TamanoId = seleccion.TamanoId,
        BordeId = seleccion.BordeId,
        ExtrasIds = seleccion.ExtrasIds.Distinct().ToList(),
        QuitadosIds = seleccion.QuitadosIds.Distinct().ToList(),
    };
}
