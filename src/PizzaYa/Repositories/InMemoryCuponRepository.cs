using PizzaYa.Models;

namespace PizzaYa.Repositories;

/// <summary>Cupones en memoria: los mismos de database/02_hu2_hu3_precios_pedidos.sql.</summary>
public class InMemoryCuponRepository : ICuponRepository
{
    private readonly List<CuponDescuento> _cupones;

    public InMemoryCuponRepository(IEnumerable<CuponDescuento> cupones)
    {
        _cupones = cupones.ToList();
    }

    public static InMemoryCuponRepository ConDatosDeEjemplo() => new(new[]
    {
        new CuponDescuento("PIZZA10", 5000m, "Descuento $5.000"),
        new CuponDescuento("PROMO20", 10000m, "Descuento $10.000"),
        new CuponDescuento("GRATIS", 15000m, "Descuento $15.000"),
    });

    /// <summary>Agrega un cupón (en las pruebas, para probar vencidos y cupones de un solo cliente).</summary>
    public void Agregar(CuponDescuento cupon)
    {
        lock (_cupones) _cupones.Add(cupon);
    }

    public Task<List<CuponDescuento>> ObtenerParaUsuarioAsync(int? usuarioId) =>
        Task.FromResult(Copia(c => c.UsuarioId is null || c.UsuarioId == usuarioId));

    private List<CuponDescuento> Copia(Func<CuponDescuento, bool> filtro)
    {
        lock (_cupones) return _cupones.Where(filtro).ToList();
    }
}
