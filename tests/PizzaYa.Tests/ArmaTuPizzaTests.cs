using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

public class ArmaTuPizzaTests
{
    [Fact]
    public async Task PizzaLibre_ExponeLaBaseYLasOpcionesSolicitadas()
    {
        var menu = InMemoryMenuRepository.ConDatosDeEjemplo();
        var pizza = (await menu.ObtenerPizzasAsync())
            .Single(p => p.EsPizzaPersonalizada);
        var personalizacion = new PersonalizacionService();
        var ingredientes = personalizacion
            .FiltrarOpciones(pizza, await menu.ObtenerIngredientesAsync())
            .Select(i => i.Nombre)
            .ToHashSet();

        Assert.Equal(
            new[] { "Masa", "Pasta de tomate", "Queso base" },
            pizza.IngredientesBase.Select(i => i.Nombre));
        Assert.Equal(
            new[]
            {
                "Piña", "Champiñones", "Jalapeños", "Queso extra", "Pollo", "Carne",
                "Chorizo", "Guacamole", "Chicharrón", "Carne de cerdo", "Jamón", "Pepperoni",
            }.OrderBy(nombre => nombre, StringComparer.Ordinal),
            ingredientes.OrderBy(nombre => nombre, StringComparer.Ordinal));
    }

    [Fact]
    public async Task PizzaLibre_NoPermiteQuitarLaBaseIncluida()
    {
        var menu = InMemoryMenuRepository.ConDatosDeEjemplo();
        var pizza = (await menu.ObtenerPizzasAsync()).Single(p => p.EsPizzaPersonalizada);
        var seleccion = new PizzaPersonalizada
        {
            PizzaId = pizza.Id,
            TamanoId = 1,
            BordeId = 4,
            QuitadosIds = new() { pizza.IngredientesBase[0].Id },
        };

        var errores = new PersonalizacionService().Validar(
            seleccion,
            pizza,
            await menu.ObtenerTamanosAsync(),
            await menu.ObtenerBordesAsync(),
            new PersonalizacionService().FiltrarOpciones(pizza, await menu.ObtenerIngredientesAsync()));

        Assert.Contains(errores, error => error.Campo == nameof(PizzaPersonalizada.QuitadosIds));
    }
}
