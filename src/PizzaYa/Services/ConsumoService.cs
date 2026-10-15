using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>
/// HU9 – Conecta los pedidos con el inventario: calcula qué ingredientes gasta un pedido, los descuenta al
/// confirmarlo y dice cuáles ingredientes están agotados (para no ofrecerlos como extra ni vender pizzas sin stock).
/// </summary>
public class ConsumoService
{
    private readonly IMenuRepository _menu;
    private readonly IInventarioRepository _inventario;

    public ConsumoService(IMenuRepository menu, IInventarioRepository inventario)
    {
        _menu = menu;
        _inventario = inventario;
    }

    /// <summary>Resultado de preparar el descuento de un carrito.</summary>
    public record ResultadoConsumo(Dictionary<int, decimal> Consumo, List<string> PizzasNoDisponibles);

    /// <summary>
    /// Ingredientes que gasta el carrito: por cada pizza, sus ingredientes base (menos los quitados) y los extras,
    /// cada uno por su consumo y por el factor del tamaño, por la cantidad pedida.
    /// Las pizzas que ya no están en el menú (ocultas o borradas) no se pueden calcular y se devuelven aparte.
    /// </summary>
    public async Task<ResultadoConsumo> CalcularAsync(IEnumerable<ItemCarrito> items)
    {
        var consumoPorIngrediente = (await _inventario.ObtenerInventarioAsync())
            .ToDictionary(i => i.Id, i => i.ConsumoPorPizza);
        var factores = (await _menu.ObtenerTamanosAsync()).ToDictionary(t => t.Id, t => t.FactorPrecio);

        var consumo = new Dictionary<int, decimal>();
        var noDisponibles = new List<string>();

        foreach (var item in items)
        {
            var seleccion = item.Seleccion;
            var pizza = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);
            if (pizza is null)
            {
                noDisponibles.Add(item.NombrePizza);
                continue;
            }

            var factor = seleccion.TamanoId is { } t && factores.TryGetValue(t, out var f) ? f : 1m;
            var usados = pizza.IngredientesBase.Select(i => i.Id)
                .Where(id => !seleccion.QuitadosIds.Contains(id))
                .Concat(seleccion.ExtrasIds);

            foreach (var id in usados)
            {
                if (!consumoPorIngrediente.TryGetValue(id, out var porPizza)) continue; // el inventario no lo controla
                consumo[id] = consumo.GetValueOrDefault(id) + porPizza * factor * item.Cantidad;
            }
        }

        // El inventario guarda 2 decimales: se redondea el total de cada ingrediente (mínimo 0,01)
        foreach (var id in consumo.Keys.ToList())
            consumo[id] = Math.Max(0.01m, decimal.Round(consumo[id], 2, MidpointRounding.AwayFromZero));

        return new ResultadoConsumo(consumo, noDisponibles);
    }

    /// <summary>Resultado de reservar el inventario de un carrito.</summary>
    public record ResultadoReserva(bool Exitoso, string? Mensaje, IReadOnlyDictionary<int, decimal> Descontado)
    {
        public static ResultadoReserva Fallo(string mensaje) =>
            new(false, mensaje, new Dictionary<int, decimal>());
    }

    /// <summary>
    /// Descuenta del inventario lo que gasta el carrito. Si algún ingrediente no alcanza (o una pizza ya no
    /// está en el menú) no se descuenta nada y se devuelve el aviso para el cliente.
    /// </summary>
    public async Task<ResultadoReserva> DescontarAsync(Carrito carrito)
    {
        var calculo = await CalcularAsync(carrito.Items);
        if (calculo.PizzasNoDisponibles.Count > 0)
            return ResultadoReserva.Fallo(
                $"Estas pizzas ya no están disponibles: {string.Join(", ", calculo.PizzasNoDisponibles.Distinct())}. Quítalas del carrito para continuar.");

        var resultado = await _inventario.DescontarAsync(calculo.Consumo);
        if (!resultado.Exitoso)
        {
            var nombres = string.Join(", ", resultado.Faltantes.Select(f => f.Nombre));
            return ResultadoReserva.Fallo(
                $"No tenemos ingredientes suficientes para tu pedido ({nombres}). Quita o cambia alguna pizza e inténtalo de nuevo.");
        }
        return new ResultadoReserva(true, null, calculo.Consumo);
    }

    /// <summary>Devuelve al inventario lo que se había descontado (el pedido no se pudo guardar).</summary>
    public Task ReponerAsync(IReadOnlyDictionary<int, decimal> descontado) => _inventario.ReponerAsync(descontado);

    /// <summary>Ids de los ingredientes agotados (en cero o menos). Los ingredientes sin control de inventario no aparecen.</summary>
    public async Task<HashSet<int>> ObtenerAgotadosAsync() =>
        (await _inventario.ObtenerInventarioAsync()).Where(i => i.Agotado).Select(i => i.Id).ToHashSet();

    /// <summary>Una pizza no está disponible si alguno de sus ingredientes base está agotado.</summary>
    public static bool SinStock(Pizza pizza, HashSet<int> agotados) =>
        pizza.IngredientesBase.Any(i => agotados.Contains(i.Id));
}
