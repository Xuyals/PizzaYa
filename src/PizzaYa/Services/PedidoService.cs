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
}
