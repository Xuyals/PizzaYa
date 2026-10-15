using Microsoft.AspNetCore.Mvc;
using PizzaYa.Controllers;
using PizzaYa.Models;
using PizzaYa.Repositories;
using PizzaYa.Services;

namespace PizzaYa.Tests;

/// <summary>Criterios de aceptación de la HU1 (Equipo 1).</summary>
public class HU1PersonalizacionTests
{
    private readonly InMemoryMenuRepository _repo = InMemoryMenuRepository.ConDatosDeEjemplo();
    private readonly PersonalizacionService _servicio = new();

    private async Task<List<(string Campo, string Mensaje)>> ValidarAsync(PizzaPersonalizada seleccion)
    {
        var pizza = (await _repo.ObtenerPizzaAsync(seleccion.PizzaId))!;
        return _servicio.Validar(seleccion, pizza,
            await _repo.ObtenerTamanosAsync(),
            await _repo.ObtenerBordesAsync(),
            await _repo.ObtenerIngredientesAsync());
    }

    // Criterio 1: menú de pizzas
    [Fact]
    public async Task Menu_MuestraPizzasConNombreIngredientesYPrecio()
    {
        var controller = new MenuController(_repo, _servicio);

        var resultado = Assert.IsType<ViewResult>(await controller.Index());
        var modelo = Assert.IsType<MenuViewModel>(resultado.Model);

        Assert.True(modelo.HayPizzas);
        Assert.All(modelo.Pizzas, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Nombre));
            Assert.NotEmpty(p.IngredientesBase);
            Assert.True(p.PrecioBase > 0);
        });
    }

    // Criterio 2: elegir tamaño (obligatorio)
    [Fact]
    public async Task SinTamano_NoPermiteContinuar()
    {
        var errores = await ValidarAsync(new PizzaPersonalizada { PizzaId = 1, BordeId = 1 });

        Assert.Contains(errores, e => e.Campo == nameof(PizzaPersonalizada.TamanoId));
    }

    [Theory]
    [InlineData(1)] // Personal
    [InlineData(2)] // Mediana
    [InlineData(3)] // Familiar
    public async Task ConTamanoValido_EsValida(int tamanoId)
    {
        var errores = await ValidarAsync(new PizzaPersonalizada { PizzaId = 1, TamanoId = tamanoId, BordeId = 1 });

        Assert.Empty(errores);
    }

    [Fact]
    public async Task TamanoInexistente_EsRechazado()
    {
        var errores = await ValidarAsync(new PizzaPersonalizada { PizzaId = 1, TamanoId = 99, BordeId = 1 });

        Assert.Contains(errores, e => e.Campo == nameof(PizzaPersonalizada.TamanoId));
    }

    // Criterio 3: elegir borde
    [Fact]
    public async Task BordeEscogido_QuedaRegistrado()
    {
        var seleccion = new PizzaPersonalizada { PizzaId = 1, TamanoId = 1, BordeId = 2 };

        Assert.Empty(await ValidarAsync(seleccion));
        Assert.Equal(2, _servicio.Normalizar(seleccion).BordeId);
    }

    [Fact]
    public async Task BordeInexistente_EsRechazado()
    {
        var errores = await ValidarAsync(new PizzaPersonalizada { PizzaId = 1, TamanoId = 1, BordeId = 99 });

        Assert.Contains(errores, e => e.Campo == nameof(PizzaPersonalizada.BordeId));
    }

    // Criterio 4: ingredientes extra o quitados, máximo 6 extras
    [Fact]
    public async Task SeisExtrasYQuitados_EsValida()
    {
        var seleccion = new PizzaPersonalizada
        {
            PizzaId = 1, TamanoId = 2, BordeId = 1,
            ExtrasIds = new() { 1, 2, 3, 4, 5, 6 },
            QuitadosIds = new() { 6 }, // Hawaiana sin jamón
        };

        Assert.Empty(await ValidarAsync(seleccion));
    }

    [Fact]
    public async Task SieteExtras_EsRechazado()
    {
        var seleccion = new PizzaPersonalizada
        {
            PizzaId = 1, TamanoId = 2, BordeId = 1,
            ExtrasIds = new() { 1, 2, 3, 4, 5, 6, 7 },
        };

        var errores = await ValidarAsync(seleccion);

        Assert.Contains(errores, e => e.Campo == nameof(PizzaPersonalizada.ExtrasIds));
    }

    [Fact]
    public async Task ExtrasRepetidos_CuentanUnaSolaVez()
    {
        var seleccion = new PizzaPersonalizada
        {
            PizzaId = 1, TamanoId = 1, BordeId = 1,
            ExtrasIds = new() { 1, 1, 2, 2, 3, 3, 4 },
        };

        Assert.Empty(await ValidarAsync(seleccion));
        Assert.Equal(4, _servicio.Normalizar(seleccion).ExtrasIds.Count);
    }

    [Fact]
    public async Task QuitarIngredienteQueNoTraeLaPizza_EsRechazado()
    {
        var seleccion = new PizzaPersonalizada
        {
            PizzaId = 1, TamanoId = 1, BordeId = 1,
            QuitadosIds = new() { 8 }, // Pepperoni no está en la Hawaiana
        };

        var errores = await ValidarAsync(seleccion);

        Assert.Contains(errores, e => e.Campo == nameof(PizzaPersonalizada.QuitadosIds));
    }

    // Criterio 5: menú sin pizzas
    [Fact]
    public async Task MenuSinPizzas_IndicaQueNoHay()
    {
        var vacio = new InMemoryMenuRepository(new(), new(), new(), new());
        var controller = new MenuController(vacio, _servicio);

        var resultado = Assert.IsType<ViewResult>(await controller.Index());
        var modelo = Assert.IsType<MenuViewModel>(resultado.Model);

        Assert.False(modelo.HayPizzas);
    }
}
