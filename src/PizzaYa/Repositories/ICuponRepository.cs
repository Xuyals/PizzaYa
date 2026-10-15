using PizzaYa.Models;

namespace PizzaYa.Repositories;

public interface ICuponRepository
{
    /// <summary>
    /// Cupones activos que el cliente puede usar: los generales (sin dueño) y los suyos.
    /// Incluye los vencidos para poder avisar "está vencido"; quien los muestra filtra por vigencia.
    /// Con <paramref name="usuarioId"/> en null solo devuelve los generales.
    /// </summary>
    Task<List<CuponDescuento>> ObtenerParaUsuarioAsync(int? usuarioId);
}
