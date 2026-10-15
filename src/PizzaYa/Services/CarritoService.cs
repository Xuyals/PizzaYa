using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PizzaYa.Models;

namespace PizzaYa.Services;

/// <summary>
/// HU3 – Gestiona el carrito de compras en la sesión HTTP.
/// </summary>
public class CarritoService
{
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    // ── Lectura / Escritura de sesión ──────────────────────────────────

    public Carrito ObtenerCarrito(ISession session)
    {
        var json = session.GetString(Carrito.ClaveSesion);
        if (string.IsNullOrEmpty(json)) return new Carrito();
        return JsonSerializer.Deserialize<Carrito>(json, _jsonOpts) ?? new Carrito();
    }

    private void GuardarCarrito(ISession session, Carrito carrito)
        => session.SetString(Carrito.ClaveSesion, JsonSerializer.Serialize(carrito, _jsonOpts));

    // ── Operaciones del carrito ────────────────────────────────────────

    /// <summary>
    /// Agrega una pizza personalizada al carrito.
    /// Si ya existe el mismo ítem (misma selección) incrementa la cantidad.
    /// </summary>
    public void AgregarItem(ISession session, ItemCarrito item)
    {
        var carrito = ObtenerCarrito(session);

        // No combinamos ítems distintos; cada pizza armada es un ítem único.
        carrito.Items.Add(item);
        GuardarCarrito(session, carrito);
    }

    /// <summary>
    /// Elimina un ítem del carrito por su Id.
    /// </summary>
    public bool EliminarItem(ISession session, Guid itemId)
    {
        var carrito = ObtenerCarrito(session);
        var item = carrito.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return false;
        carrito.Items.Remove(item);
        GuardarCarrito(session, carrito);
        return true;
    }

    /// <summary>
    /// Cambia la cantidad de un ítem. Si la cantidad ≤ 0 lo elimina.
    /// </summary>
    public bool CambiarCantidad(ISession session, Guid itemId, int nuevaCantidad)
    {
        var carrito = ObtenerCarrito(session);
        var item = carrito.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return false;

        if (nuevaCantidad <= 0)
        {
            carrito.Items.Remove(item);
        }
        else
        {
            item.Cantidad = nuevaCantidad;
        }

        GuardarCarrito(session, carrito);
        return true;
    }

    /// <summary>
    /// Vacía el carrito (se llama al confirmar el pedido).
    /// </summary>
    public void VaciarCarrito(ISession session)
    {
        GuardarCarrito(session, new Carrito());
    }

    // ── Generación de pedido ───────────────────────────────────────────

    /// <summary>
    /// Genera un número de pedido único y legible.
    /// Formato: PZ-YYYYMMDD-XXXX (ej: PZ-20261014-A3F7)
    /// </summary>
    public string GenerarNumeroPedido()
    {
        var sufijo = Guid.NewGuid().ToString("N")[..4].ToUpper();
        return $"PZ-{DateTime.Now:yyyyMMdd}-{sufijo}";
    }
}
