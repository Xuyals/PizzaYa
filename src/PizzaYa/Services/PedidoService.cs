using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>
/// Reglas de negocio de los pedidos: crear el pedido desde el carrito (HU3) y,
/// para la HU4, cambiar su estado respetando el orden.
/// </summary>
public class PedidoService
{
    private readonly IPedidoRepository _pedidos;
    private readonly IMenuRepository _menu;

    public PedidoService(IPedidoRepository pedidos, IMenuRepository menu)
    {
        _pedidos = pedidos;
        _menu = menu;
    }

    /// <summary>Datos que el cliente escribe al confirmar el carrito.</summary>
    public record DatosConfirmacion(
        string NumeroPedido, string Nombre, string Telefono, bool EsDomicilio, string? Direccion,
        decimal CostoDomicilio, decimal Descuento);

    /// <summary>Guarda el carrito como pedido nuevo, en estado "Recibido".</summary>
    public async Task<Pedido> CrearDesdeCarritoAsync(Carrito carrito, DatosConfirmacion datos)
    {
        var ingredientes = (await _menu.ObtenerIngredientesAsync()).ToDictionary(i => i.Id);

        var pedido = new Pedido
        {
            NumeroPedido = datos.NumeroPedido,
            NombreCliente = datos.Nombre.Trim(),
            Telefono = datos.Telefono.Trim(),
            EsDomicilio = datos.EsDomicilio,
            Direccion = datos.EsDomicilio ? datos.Direccion?.Trim() : null,
            Subtotal = carrito.Subtotal,
            CostoDomicilio = datos.CostoDomicilio,
            Descuento = datos.Descuento,
            Total = Math.Max(0m, carrito.Subtotal + datos.CostoDomicilio - datos.Descuento),
            Estado = EstadoPedido.Recibido,
            CreadoEn = DateTime.UtcNow,
        };

        foreach (var item in carrito.Items)
        {
            var seleccion = item.Seleccion;
            var pizza = await _menu.ObtenerPizzaAsync(seleccion.PizzaId);

            pedido.Items.Add(new PedidoItem
            {
                PizzaId = seleccion.PizzaId,
                TamanoId = seleccion.TamanoId ?? 0,
                BordeId = seleccion.BordeId ?? 0,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                NombrePizza = item.NombrePizza,
                NombreTamano = item.NombreTamano,
                NombreBorde = item.NombreBorde,
                ExtrasIds = seleccion.ExtrasIds.ToList(),
                QuitadosIds = seleccion.QuitadosIds.ToList(),
                ExtrasNombres = item.NombresExtras.ToList(),
                QuitadosNombres = seleccion.QuitadosIds
                    .Where(ingredientes.ContainsKey).Select(id => ingredientes[id].Nombre).ToList(),
                IngredientesBase = pizza?.IngredientesBase.Select(i => i.Nombre).ToList() ?? new List<string>(),
            });
        }

        return await _pedidos.CrearPedidoAsync(pedido);
    }

    // ── HU4: estados ──────────────────────────────────────────────────

    /// <summary>Resultado de intentar cambiar el estado de un pedido.</summary>
    public record ResultadoCambio(bool Exitoso, string Mensaje, Pedido? Pedido = null);

    /// <summary>Estado que sigue al actual, o null si ya está "Entregado" (o el estado no existe).</summary>
    public static string? SiguienteEstado(string estadoActual)
    {
        var posicion = IndiceDe(estadoActual);
        return posicion < 0 || posicion >= EstadoPedido.Todos.Count - 1 ? null : EstadoPedido.Todos[posicion + 1];
    }

    /// <summary>Solo se permite pasar al estado inmediatamente siguiente: no se salta ni se retrocede.</summary>
    public static bool PuedeCambiar(string estadoActual, string estadoNuevo) =>
        SiguienteEstado(estadoActual) == estadoNuevo;

    /// <summary>Cambia el estado del pedido a <paramref name="estadoNuevo"/> si es el que sigue en el orden.</summary>
    public async Task<ResultadoCambio> CambiarEstadoAsync(int pedidoId, string estadoNuevo)
    {
        var pedido = await _pedidos.ObtenerPorIdAsync(pedidoId);
        if (pedido is null)
            return new ResultadoCambio(false, "Pedido no encontrado.");

        if (!PuedeCambiar(pedido.Estado, estadoNuevo))
        {
            var siguiente = SiguienteEstado(pedido.Estado);
            var mensaje = siguiente is null
                ? $"El pedido {pedido.NumeroPedido} ya está \"{pedido.Estado}\" y no puede cambiar."
                : $"El pedido {pedido.NumeroPedido} está \"{pedido.Estado}\": solo puede pasar a \"{siguiente}\". No se puede saltar ni devolver un estado.";
            return new ResultadoCambio(false, mensaje, pedido);
        }

        var cambio = await _pedidos.ActualizarEstadoAsync(pedido.Id, pedido.Estado, estadoNuevo);
        if (!cambio)
            return new ResultadoCambio(false,
                $"El pedido {pedido.NumeroPedido} acaba de cambiar de estado. Revisa el panel e inténtalo de nuevo.",
                await _pedidos.ObtenerPorIdAsync(pedido.Id));

        pedido.Estado = estadoNuevo;
        return new ResultadoCambio(true, $"El pedido {pedido.NumeroPedido} pasó a \"{estadoNuevo}\".", pedido);
    }

    private static int IndiceDe(string estado)
    {
        for (var i = 0; i < EstadoPedido.Todos.Count; i++)
            if (EstadoPedido.Todos[i] == estado) return i;
        return -1;
    }
}
