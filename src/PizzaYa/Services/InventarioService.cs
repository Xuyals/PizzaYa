using PizzaYa.Models;

namespace PizzaYa.Services;

/// <summary>Reglas de negocio de la HU5 (inventario y pedidos a proveedores).</summary>
public class InventarioService
{
    public const int MaximoLongitudProveedor = 80;
    public const decimal MaximoCantidad = 100000m;

    /// <summary>
    /// Valida el pedido contra el inventario. Devuelve los errores por campo (relativos a
    /// NuevoPedidoProveedor, p. ej. "Proveedor" o "Lineas[2].Cantidad"); lista vacía = válido.
    /// Una fila sin cantidad se interpreta como "ese ingrediente no se pide".
    /// </summary>
    public List<(string Campo, string Mensaje)> Validar(
        NuevoPedidoProveedor pedido,
        IEnumerable<IngredienteInventario> inventario)
    {
        var errores = new List<(string, string)>();

        if (string.IsNullOrWhiteSpace(pedido.Proveedor))
            errores.Add((nameof(pedido.Proveedor), "Indica el proveedor."));
        else if (pedido.Proveedor.Trim().Length > MaximoLongitudProveedor)
            errores.Add((nameof(pedido.Proveedor),
                $"El nombre del proveedor no puede pasar de {MaximoLongitudProveedor} caracteres."));

        var porId = inventario.ToDictionary(i => i.Id);
        var pedidos = 0;

        for (var i = 0; i < pedido.Lineas.Count; i++)
        {
            var linea = pedido.Lineas[i];
            if (linea.Cantidad is null) continue;

            if (linea.IngredienteId is null || !porId.TryGetValue(linea.IngredienteId.Value, out var ing))
            {
                errores.Add(($"Lineas[{i}].IngredienteId", "Uno de los ingredientes del pedido no existe."));
                continue;
            }

            if (linea.Cantidad <= 0)
                errores.Add(($"Lineas[{i}].Cantidad", $"La cantidad de {ing.Nombre} debe ser mayor a 0."));
            else if (linea.Cantidad > MaximoCantidad)
                errores.Add(($"Lineas[{i}].Cantidad", $"La cantidad de {ing.Nombre} es demasiado grande."));
            else
                pedidos++;
        }

        var repetidos = pedido.Lineas
            .Where(l => l.IngredienteId is not null && l.Cantidad is > 0)
            .GroupBy(l => l.IngredienteId)
            .Any(g => g.Count() > 1);
        if (repetidos)
            errores.Add((nameof(pedido.Lineas), "Un ingrediente aparece repetido en el pedido."));

        // Ninguna fila con cantidad: es el caso "faltan las cantidades"
        if (pedidos == 0 && !pedido.Lineas.Any(l => l.Cantidad is not null))
            errores.Add((nameof(pedido.Lineas), "Indica la cantidad de al menos un ingrediente."));

        return errores;
    }

    /// <summary>Filas que realmente se piden (con cantidad mayor a 0). Llamar solo con un pedido válido.</summary>
    public List<ItemPedido> LineasAPedir(NuevoPedidoProveedor pedido) => pedido.Lineas
        .Where(l => l.IngredienteId is not null && l.Cantidad is > 0)
        .Select(l => new ItemPedido(l.IngredienteId!.Value, decimal.Round(l.Cantidad!.Value, 2)))
        .ToList();

    /// <summary>Cantidad sugerida al pulsar "Pedir": lo necesario para llegar al doble del mínimo (al menos 1).</summary>
    public decimal SugerirCantidad(IngredienteInventario ingrediente) =>
        Math.Max(Math.Ceiling(ingrediente.CantidadMinima * 2 - ingrediente.Cantidad), 1m);
}
