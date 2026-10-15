using PizzaYa.Models;
using PizzaYa.Repositories;

namespace PizzaYa.Services;

/// <summary>
/// Cupones de la base de datos (HU6). Usa la misma regla de descuento de la HU2 (<see cref="PrecioService"/>),
/// pero con los cupones guardados, que pueden vencer o ser de un solo cliente.
/// </summary>
public class CuponService
{
    /// <summary>Clave de sesión del cupón que el cliente escogió en su perfil para su próximo pedido.</summary>
    public const string ClaveSesion = "CuponSeleccionado";

    private readonly ICuponRepository _cupones;
    private readonly PrecioService _precio;

    public CuponService(ICuponRepository cupones, PrecioService precio)
    {
        _cupones = cupones;
        _precio = precio;
    }

    /// <summary>Cupones que este cliente (o un visitante, con null) puede intentar usar, vencidos incluidos.</summary>
    public async Task<IReadOnlyList<CuponDescuento>> ObtenerParaUsuarioAsync(int? usuarioId) =>
        await _cupones.ObtenerParaUsuarioAsync(usuarioId);

    /// <summary>"Mis cupones": los vigentes del cliente, del que vence primero al que vence después.</summary>
    public async Task<List<CuponVista>> ObtenerVigentesAsync(int usuarioId, DateTime? ahora = null)
    {
        var hoy = ahora ?? DateTime.UtcNow;
        return (await _cupones.ObtenerParaUsuarioAsync(usuarioId))
            .Where(c => !c.EstaVencido(hoy))
            .OrderBy(c => c.VenceEn ?? DateTime.MaxValue).ThenBy(c => c.Codigo)
            .Select(c => new CuponVista
            {
                Codigo = c.Codigo,
                Descripcion = c.Descripcion,
                Monto = c.MontoDescuento,
                VenceEn = c.VenceEn,
            })
            .ToList();
    }

    /// <summary>
    /// Intenta aplicar un cupón a un total. Si no existe o está vencido, devuelve el mensaje de error
    /// y un descuento de 0: el total no cambia.
    /// </summary>
    public async Task<(decimal Descuento, bool Aplicado, string? Mensaje)> ResolverAsync(
        string? codigo, decimal totalSinDescuento, int? usuarioId, DateTime? ahora = null) =>
        _precio.ResolverCupon(codigo, totalSinDescuento, await ObtenerParaUsuarioAsync(usuarioId), ahora);
}
